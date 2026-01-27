using System.Threading.Channels;
using FunkoApi.Mail;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FunkoApi.Infraestructure;

public static class EmailConfig
{

    public static IServiceCollection AddEmailService(this IServiceCollection services)
    {
        // Canal para comunicación entre servicios (cola de emails)
        services.AddSingleton(Channel.CreateUnbounded<EmailMessage>());
        
        // Servicio de background que procesa la cola de emails
        services.AddHostedService<EmailBackgroundService>();
        
        // Servicio de email
        services.TryAddScoped<IEmailService, MailKitEmailService>();

        return services;
    }
}
