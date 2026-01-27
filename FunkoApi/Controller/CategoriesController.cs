using FunkoApi.DTO;
using FunkoApi.Error;
using FunkoApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FunkoApi.Controller;

[ApiController]
//Hacemos que la url genérica sea /categories, elimina la palabra controller
//y se queda con el nombre de la clase
[Route("[controller]")]
//Especificamos que va a devolver .json
[Produces("application/json")]
public class CategoriesController(ICategoryService service, ILogger<CategoriesController> logger) : ControllerBase
{
    //El path es /categories
    [HttpGet]
    //IEnumerable es la interfaz base de la mayoría de colecciones en 
    //C#, de la que hereda también List
    //Devuelve un código 200 cuyo body es la lista de CategoryDTO
    [ProducesResponseType(typeof(IEnumerable<CategoryResponseDTO>), StatusCodes.Status200OK)]
    //IActionResult es como el ResponseEntity de Java
    public async Task<IActionResult> GetAllAsync()
    {
        logger.LogInformation("Solicitando listado completo de categorías");
        var categories = await service.GetAllAsync();
        logger.LogInformation("Listado de categorías obtenido exitosamente, total: {Total}", categories.Count);
        return Ok(categories);
    }
    
    //El path es /categories/id
    [HttpGet("{id}")]
    //Devuelve un código 200 con el CategoryDTO como body
    [ProducesResponseType(typeof(CategoryResponseDTO), StatusCodes.Status200OK)]
    //Devuelve un código 404 en caso de no encontrarse la Categoría
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id)
    {
        logger.LogInformation("Solicitando categoría con id: {Id}", id);
        var result = await service.GetByIdAsync(id);

        if (result.IsFailure)
        {
            logger.LogWarning("Categoría con id {Id} no encontrada", id);
            //ojo! el NotFound no es el error de dominio, sino el código 404 de C#
            if (result.Error is FunkoNotFoundError)
            {
                return NotFound(new { message = result.Error.Message });
            }
            return BadRequest(new { message = result.Error.Message });
        }

        logger.LogInformation("Categoría con id {Id} obtenida exitosamente: {Nombre}", id, result.Value.Nombre);
        return Ok(result.Value);
    }
    
    //El path es /categories
    [HttpPost]
    //Devuelve un código 201 con el CategoryDTO como body
    [ProducesResponseType(typeof(CategoryResponseDTO), StatusCodes.Status201Created)]
    //Devuelve un código 400 en caso de que el body tenga errores de validación
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    //Devuelve un código 409 en caso de que la categoría ya exista
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PostAsync([FromBody] CategoryPostPutRequestDTO request)
    {
        logger.LogInformation("Creando nueva categoría: {Nombre}", request.Nombre);
        var result = await service.CreateAsync(request);

        if (result.IsFailure)
        {
            logger.LogWarning("Error al crear categoría: {Error}", result.Error.Message);
            if (result.Error is FunkoConflictError)
            {
                return Conflict(new { message = result.Error.Message });
            }
            return BadRequest(new { message = result.Error.Message });
        }

        logger.LogInformation("Categoría creada exitosamente con id: {Id}, Nombre: {Nombre}", result.Value.Id, result.Value.Nombre);
        
        //Devolvemos un 201
        // 1. Nombre de la función que da el detalle (GetByIdAsync), en base a la cual se calcula la url donde podemos encontrar
        // la Categoría recién creada.
        // 2. Parámetros para esa función (el ID de la nueva Categoría)
        // 3. El objeto creado en sí
        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { id = result.Value.Id },
            result.Value);
    }
    
    //El path es /categories/id
    [HttpPut("{id}")]
    //Devuelve un código 200 con el CategoryDTO actualizado como body
    [ProducesResponseType(typeof(CategoryResponseDTO), StatusCodes.Status200OK)]
    //Devuelve un código 404 en caso de que la Categoría a actualizar ya exista
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    //Devuelve un código 400 en caso de que el body tenga errores de validación
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PutAsync(Guid id, [FromBody] CategoryPostPutRequestDTO request)
    {
        logger.LogInformation("Actualizando categoría con id: {Id}, Nuevo nombre: {Nombre}", id, request.Nombre);
        var result = await service.UpdateAsync(id, request);

        if (result.IsFailure)
        {
            logger.LogWarning("Error al actualizar categoría id {Id}: {Error}", id, result.Error.Message);
            if (result.Error is FunkoNotFoundError)
            {
                return NotFound(new { message = result.Error.Message });
            }
            if (result.Error is FunkoConflictError)
            {
                return Conflict(new { message = result.Error.Message });
            }
            return BadRequest(new { message = result.Error.Message });
        }

        logger.LogInformation("Categoría id {Id} actualizada exitosamente", id);
        return Ok(result.Value);
    }
    
    //El path es /categories/id
    [HttpDelete("{id}")]
    //Devuelve un código 200 con el CategoryDTO que ha sido eliminada
    [ProducesResponseType(typeof(CategoryResponseDTO), StatusCodes.Status200OK)]
    //Devuelve un código 404 en caso de que la Categoría a eliminar no exista
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        logger.LogInformation("Eliminando categoría con id: {Id}", id);
        var result = await service.DeleteAsync(id);

        if (result.IsFailure)
        {
            logger.LogWarning("Error al eliminar categoría id {Id}: {Error}", id, result.Error.Message);
            if (result.Error is FunkoNotFoundError)
            {
                return NotFound(new { message = result.Error.Message });
            }
            return BadRequest(new { message = result.Error.Message });
        }

        logger.LogInformation("Categoría id {Id} eliminada exitosamente", id);
        return Ok(result.Value);
    }
}