namespace FunkoApi.DTO;

public class FunkoResponseDTO
{
    public long Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public double Precio { get; set; }
    
    public string Imagen { get; set; } = "default.png";
}