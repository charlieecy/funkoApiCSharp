using FunkoApi.DataBase;
using FunkoApi.Repository;
using FunkoApi.Services;
using FunkoApi.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// 1. EL BUILDER: Configuramos nuestro contenedor de dependencias (Servicios)
var builder = WebApplication.CreateBuilder(args);

// Registramos nuestros controladores para que la API pueda gestionar las rutas
builder.Services.AddControllers();
//Esta opción hace que no se suprima el sufijo Async de los nombres de los métodos, así el POST
//del controller no se lía en el parámetro nameof()
builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});

// Configuramos la base de datos: usamos una base de datos en memoria 
builder.Services.AddDbContext<Context>(options => 
    options.UseInMemoryDatabase("FunkoDatabase"));

// Inyección de Dependencias
// Registramos repositorios y servicios con ciclo de vida Scoped (una instancia por petición HTTP)
builder.Services.AddScoped<IFunkoRepository, FunkoRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IFunkoService, FunkoService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IFunkoStorage, FunkoStorageService>();


// Añadimos el servicio de caché en memoria que utiliza el FunkoService
builder.Services.AddMemoryCache();

// PERSONALIZAMOS LOS ERRORES DE VALIDACIÓN
// Configuramos cómo queremos que responda nuestra API cuando los datos de un DTO no sean válidos
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        // Extraemos todos los mensajes de error y los unimos en una sola cadena de texto
        var mensaje = string.Join(", ", context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage));
        
        // Devolvemos propio objeto BadRequest con el formato JSON que hemos definido
        return new BadRequestObjectResult(new { message = mensaje });
    };
});

// 2. CONSTRUCCIÓN DE LA APP
// Una vez que hemos terminado de configurar los servicios, construimos nuestra aplicación
var app = builder.Build();

// 3. INICIALIZACIÓN DE DATOS (Seed)
// Creamos un entorno temporal para acceder a la base de datos e inicializarla antes de arrancar
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<Context>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("Estamos inicializando la Base de Datos...");
    // Ejecutamos EnsureCreated para crear la base de datos y cargar nuestros datos iniciales
    context.Database.EnsureCreated(); 
    logger.LogInformation("Hemos terminado de preparar la Base de Datos.");
}

// 4. EL PIPELINE (Middleware)
// Definimos el camino que seguirá cada petición HTTP cuando llegue a nuestro servidor.

// Añadimos nuestro propio Middleware para la captura global de excepciones
// Si ocurre cualquier excepción, la capturamos aquí y devolvemos un 500
app.Use(async (context, next) =>
{
    try
    {
        await next(); // Intentamos pasar la petición al siguiente paso del pipeline
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { message = "Error inesperado: " + ex.Message });
    }
});


// Activamos la redirección automática a HTTPS por seguridad
app.UseHttpsRedirection();

// Habilitamos el servicio de archivos estáticos desde wwwroot
// Esto permite acceder a las imágenes subidas
app.UseStaticFiles();

// Habilitamos el sistema de autorización en nuestro pipeline
app.UseAuthorization(); 


// Mapeamos nuestros controladores para que las rutas (como /funkos) sean accesibles
app.MapControllers(); 

// 5. Arranca la aplicación
app.Run();