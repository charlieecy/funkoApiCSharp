namespace FunkoApi.Infraestructure;

public static class CorsExtensions
{
   
    public static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app)
    {
        var env = ((WebApplication)app).Environment;

        var policyName = env.IsDevelopment() ? "AllowAll" : "ProductionPolicy";

        return app.UseCors(policyName);
    }
}