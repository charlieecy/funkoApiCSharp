using System.ComponentModel.DataAnnotations;

namespace FunkoApi.DTO;

public class FunkoPostPutRequestDTO
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La categoría es obligatoria")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "La categoría debe tener entre 2 y 100 caracteres")]
    public string Categoria { get; set; } = string.Empty;

    [Range(0.01, 9999.99, ErrorMessage = "El precio debe estar entre 0.01 y 9999.99")]
    public double Precio { get; set; }
}