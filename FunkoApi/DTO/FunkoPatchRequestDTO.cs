using System.ComponentModel.DataAnnotations;

namespace FunkoApi.DTO;

public class FunkoPatchRequestDTO
{
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    public string? Nombre { get; set; }

    [StringLength(100, MinimumLength = 2, ErrorMessage = "La categoría debe tener entre 2 y 100 caracteres")]
    public string? Categoria { get; set; }

    [Range(0.01, 9999.99, ErrorMessage = "El precio debe estar entre 0.01 y 9999.99")]
    public double? Precio { get; set; }
}