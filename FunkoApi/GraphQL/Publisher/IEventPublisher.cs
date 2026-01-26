namespace FunkoApi.GraphQL.Publisher;

public interface IEventPublisher
{
    Task PublishAsync<T>(string topic, T payload);

}