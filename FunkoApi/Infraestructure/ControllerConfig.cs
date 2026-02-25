namespace FunkoApi.Infraestructure;

public static class ControllerConfig
{

    public static IServiceCollection AddControllersConfiguration(this IServiceCollection services)
    {
        // Registramos nuestros controladores para que la API pueda gestionar las rutas
        services.AddControllers();
        
        // Esta opción hace que no se suprima el sufijo Async de los nombres de los métodos,
        // así el POST del controller no se lía en el parámetro nameof()
        services.AddControllers(options =>
        {
            options.SuppressAsyncSuffixInActionNames = false;
        });

        return services;
    }
}
