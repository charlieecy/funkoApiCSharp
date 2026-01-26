using HotChocolate.Subscriptions;

namespace FunkoApi.GraphQL.Publisher;

public class EventPublisher : IEventPublisher
{
    private readonly ITopicEventSender _eventSender;

    public EventPublisher(ITopicEventSender eventSender)
    {
        _eventSender = eventSender;
    }
    
    public async Task PublishAsync<T>(string topic, T payload)
    {
        await _eventSender.SendAsync(topic, payload);
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