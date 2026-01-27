using HotChocolate.Subscriptions;

namespace FunkoApi.GraphQL.Publisher;

public class EventPublisher : IEventPublisher
{
    private readonly ITopicEventSender _eventSender;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(ITopicEventSender eventSender, ILogger<EventPublisher> logger)
    {
        _eventSender = eventSender;
        _logger = logger;
    }
    
    public async Task PublishAsync<T>(string topic, T payload)
    {
        try
        {
            _logger.LogDebug("Publicando evento GraphQL en topic: {Topic}, Tipo: {Type}", topic, typeof(T).Name);
            await _eventSender.SendAsync(topic, payload);
            _logger.LogDebug("Evento GraphQL publicado exitosamente en topic: {Topic}", topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al publicar evento GraphQL en topic: {Topic}", topic);
            throw;
        }
    }
}

public static class EventPublisherExtensions
    {
        public static IServiceCollection AddGraphQlPubSub(this IServiceCollection services)
        {
            services.AddSingleton<IEventPublisher, EventPublisher>();
            return services;
        }
    }