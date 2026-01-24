namespace FunkoApi.DTO;

public record PageResponse<T>
{
    //Lista de todos los resultados obtenidos
    public IEnumerable<T> Items { get; init; } = [];
    //Total de resultados obtenidos
    public int TotalCount { get; init; }
    //Número de página actual
    public int Page { get; init; }
    //Número máximo de resultados por página
    public int Size { get; init; }
    //Cálculo del número total de páginas. 
    //Si el tamaño de página es mayor a 0, se divide el total de resultados entre el tamaño máximo de 
    //página para hallar el total de páginas. En caso contrario, el total de páginas es 0. 
    public int TotalPages => Size > 0 ? (int)Math.Ceiling((double)TotalCount / Size) : 0;
    public bool HasNexPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}