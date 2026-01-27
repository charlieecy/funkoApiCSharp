using FunkoApi.Infraestructure;
using FunkoApi.SignalR;

// 1. EL BUILDER: Configuramos nuestro contenedor de dependencias (Servicios)
var builder = WebApplication.CreateBuilder(args);

// Configuración de Controladores
builder.Services.AddControllersConfiguration();

// Configuración de Base de Datos
builder.Services.AddDatabase(builder.Configuration);

// Configuración de Autenticación y Autorización
builder.Services.AddAuthentication(builder.Configuration);

// Inyección de Dependencias: Repositorios, Servicios, Storage, Eventos
builder.Services.AddRepositoriesAndServices();

// Configuración de Cache Redis
builder.Services.AddCache(builder.Configuration);

// Configuración de GraphQL
builder.Services.AddGraphQL();

// Configuración de Email con MailKit
builder.Services.AddEmailService();

// Configuración de SignalR + CORS
builder.Services.AddSignalRWithCors();

// Personalización de errores de validación
builder.Services.AddCustomValidation();

// 2. CONSTRUCCIÓN DE LA APP
var app = builder.Build();

// 3. INICIALIZACIÓN DE DATOS (Seed)
app.SeedDatabase();

// 4. EL PIPELINE (Middleware)
// Middleware para la captura global de excepciones
app.UseGlobalExceptionHandler();

// CORS
app.UseCorsPolicy();

// Redirección automática a HTTPS
app.UseHttpsRedirection();

// Autenticación
app.UseAuthentication();

// Archivos estáticos desde wwwroot (imágenes, etc.)
app.UseStaticFiles();

// Autorización
app.UseAuthorization();

// Mapeamos los controladores
app.MapControllers();

// WebSockets
app.UseWebSockets();

// Mapeamos el endpoint de GraphQL
app.MapGraphQL();

// CORS para SignalR
app.UseCors("AllowSignalR");

// SignalR Hub
app.MapHub<FunkoHub>("/hubs/funkos");

// 5. Arranca la aplicación
app.Run();