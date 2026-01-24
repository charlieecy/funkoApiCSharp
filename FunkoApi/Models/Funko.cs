using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FunkoApi.Models;

[Table("funkos")]
public record Funko()
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; } = 0;

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 9999.99)]
    public double Precio { get; set; } = 0.0;

    // FK para la relación con Categoría
    [Required]
    [ForeignKey(nameof(Category))] 
    public Guid CategoryId { get; set; }

    // Objeto de navegación
    public Category Category { get; set; } = null!; 

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
};