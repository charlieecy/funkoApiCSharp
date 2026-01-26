namespace FunkoApi.GraphQL.Inputs;

public record PatchFunkoInput
{
    public string? Nombre { get; init; }
    
    public string? Categoria { get; init; }
    
    public double? Precio { get; init; }
    
    public string? Imagen { get; init; }
}