namespace FunkoApi.GraphQL.Inputs;

public record PostPutFunkoInput
{
    public string Nombre { get; init; } = string.Empty;
    
    public string Categoria { get; init; } = string.Empty;
    
    public double Precio { get; init; } = 0.0;
    
    public string? Imagen { get; init; }
}