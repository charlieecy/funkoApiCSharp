namespace FunkoApi.Infraestructure;

public static class SignalRConfig
{

    public static IServiceCollection AddSignalRWithCors(this IServiceCollection services)
    {
        // CORS específico para SignalR
        services.AddCors(options =>
        {
            options.AddPolicy("AllowSignalR", policy =>
            {
                policy.SetIsOriginAllowed(origin => true)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
        
        services.AddSignalR();

        return services;
    }
}
