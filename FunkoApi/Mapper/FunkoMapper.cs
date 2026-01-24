using FunkoApi.DTO;
using FunkoApi.Models;

namespace FunkoApi.Mapper;

public static class FunkoMapper
{
    public static Funko ToModel(this FunkoPostPutRequestDTO dto)
    {
        return new Funko
        {
            Nombre = dto.Nombre,
            Precio = dto.Precio
            // No asignamos dto.Categoria aquí porque Category es un objeto,
            // no un string que es como llega del DTO.
            // El CategoryId lo asignará el Service después de buscar la categoría en la DB.
        };
    }
    
    public static FunkoResponseDTO ToDto(this Funko funko)
    {
        return new FunkoResponseDTO
        {
            Id = funko.Id,
            Nombre = funko.Nombre,
            Categoria = funko.Category.Nombre,
            Precio = funko.Precio
        };
    }

    public static CategoryResponseDTO ToDto(this Category category)
    {
        return new CategoryResponseDTO()
        {
            Id = category.Id.ToString(),
            Nombre = category.Nombre
        };
    }

    public static Category ToModel(this CategoryPostPutRequestDTO dto)
    {
        return new Category
        {
            Nombre = dto.Nombre,
        };
    }
}