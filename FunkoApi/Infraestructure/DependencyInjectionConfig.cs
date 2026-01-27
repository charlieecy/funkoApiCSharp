using FunkoApi.GraphQL.Publisher;
using FunkoApi.Repository;
using FunkoApi.Services;
using FunkoApi.Storage;

namespace FunkoApi.Infraestructure;

public static class DependencyInjectionConfig
{

    public static IServiceCollection AddRepositoriesAndServices(this IServiceCollection services)
    {
        // Repositorios
        services.AddScoped<IFunkoRepository, FunkoRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        
        // Servicios
        services.AddScoped<IFunkoService, FunkoService>();
        services.AddScoped<ICategoryService, CategoryService>();
        
        // Storage
        services.AddScoped<IFunkoStorage, FunkoStorageService>();
        
        // Eventos
        services.AddScoped<IEventPublisher, EventPublisher>();

        return services;
    }
}
