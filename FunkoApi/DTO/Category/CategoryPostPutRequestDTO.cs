using System.ComponentModel.DataAnnotations;

namespace FunkoApi.DTO;

public class CategoryPostPutRequestDTO
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
    public string Nombre { get; set; } = string.Empty;
}