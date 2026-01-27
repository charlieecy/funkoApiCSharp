using FunkoApi.Services.Redis;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FunkoApi.Infraestructure;

public static class CacheConfig
{

    public static IServiceCollection AddCache(this IServiceCollection services)
    {

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = "redis:6379,password=estaeslapassdelacache";
            options.InstanceName = "FunkoApi:";
        });
        services.TryAddScoped<ICacheService, CacheService>();
        return services;
    }
}