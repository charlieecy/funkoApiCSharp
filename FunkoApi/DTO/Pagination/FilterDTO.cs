namespace FunkoApi.DTO;

//Filtros para el getAll con querys.
//Los que son nulables es porque pueden venir o no.
//El resto no son nullables pero se les da un valor por defecto
public record FilterDTO(
    string? Nombre,
    string? Categoria,
    double? MaxPrecio,
    int Page = 1,
    int Size = 10,
    string SortBy = "id",
    string Direction = "asc"
);