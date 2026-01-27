using FunkoApi.DataBase;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Infraestructure;

public static class DatabaseConfig
{
    
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<Context>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
