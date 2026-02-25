namespace FunkoApi.GraphQL.Events;

public record FunkoCreatedEvent()
{
    public long Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Categoria { get; init; } = string.Empty;
    public double Precio { get; init; }
    public DateTime CreatedAt { get; init; }
}