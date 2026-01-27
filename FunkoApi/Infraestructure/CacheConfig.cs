using FunkoApi.Services.Redis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration; // Added this using directive

namespace FunkoApi.Infraestructure;

public static class CacheConfig
{

    public static IServiceCollection AddCache(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            // Configuración de Redis desde variables de entorno
            var redisConnection = configuration["REDIS_CONNECTION"] 
                                ?? configuration.GetConnectionString("Redis") 
                                ?? "redis:6379,password=estaeslapassdelacache";

            options.Configuration = redisConnection;
            options.InstanceName = "FunkoCache:";
        });
        
        services.TryAddScoped<ICacheService, CacheService>();
        return services;
    }
}