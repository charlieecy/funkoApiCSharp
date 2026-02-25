using CSharpFunctionalExtensions;
using FunkoApi.DTO;
using FunkoApi.Error;
using FunkoApi.Services;
using FunkoApi.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Path = System.IO.Path;

namespace FunkoApi.Controller;

[ApiController]
//Hacemos que la url genérica sea /funkos, elimina la palabra controller
//y se queda con el nombre de la clase
[Route("[controller]")]
//Especificamos que va a devolver .json
[Produces("application/json")]
public class FunkosController(IFunkoService service, IFunkoStorage storage, ILogger<FunkosController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<FunkoResponseDTO>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? nombre = null,
        [FromQuery] string? categoria  = null,
        [FromQuery] double? maxPrecio = null,
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string sortby = "id",
        [FromQuery] string direction = "asc")
    {
        logger.LogInformation("Solicitando listado de Funkos con filtros - Nombre: {Nombre}, Categoria: {Categoria}, MaxPrecio: {MaxPrecio}, Page: {Page}, Size: {Size}", 
            nombre, categoria, maxPrecio, page, size);
        
        var filter = new FilterDTO(nombre, categoria, maxPrecio, page, size, direction);
        var result = await service.GetAllAsync(filter);
        
        if (result.IsSuccess)
        {
            logger.LogInformation("Listado de Funkos obtenido exitosamente, total: {Total}", result.Value.TotalCount);
        }
        
        return result.Match(
            onSuccess: Ok,
            onFailure:error => error switch
            {
                FunkoNotFoundError => NotFound(new {message = error.Message}),
                _ => StatusCode(500, new { message = error.Message})
            }
        );
    }

    //El path es /funkos/id
    [HttpGet("{id}")]
    //Devuelve un código 200 con el FunkoDTO como body
    [ProducesResponseType(typeof(FunkoResponseDTO), StatusCodes.Status200OK)]
    //Devuelve un código 404 en caso de no encontrarse el Funko
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> GetByIdAsync(long id)
    {
        logger.LogInformation("Solicitando Funko con id: {Id}", id);
        var result = await service.GetByIdAsync(id);

        if (result.IsFailure)
        {
            logger.LogWarning("Funko con id {Id} no encontrado", id);
            //ojo! el NotFound no es el error de dominio, sino el código 404 de C#
            if (result.Error is FunkoNotFoundError)
            {
                return NotFound(new { message = result.Error.Message });
            }
            return BadRequest(new { message = result.Error.Message });
        }

        logger.LogInformation("Funko con id {Id} obtenido exitosamente: {Nombre}", id, result.Value.Nombre);
        return Ok(result.Value);
    }

    //El path es /funkos
    [HttpPost]
    //Devuelve un código 201 con el FunkoDTO como body
    [ProducesResponseType(typeof(FunkoResponseDTO), StatusCodes.Status201Created)]
    //Devuelve un código 400 en caso de que el body tenga errores de validación
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    //Devuelve un código 409 en caso de que la categoría no exista
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> PostAsync([FromForm] FunkoPostPutRequestDTO request, [FromForm] IFormFile? file = null)
    {
        logger.LogInformation("Creando nuevo Funko: {Nombre}, Categoria: {Categoria}, Precio: {Precio}", 
            request.Nombre, request.Categoria, request.Precio);
        
        if (file != null)
        {
            logger.LogDebug("Guardando imagen para nuevo Funko: {FileName}", file.FileName);
            var relativePath = await storage.SaveFileAsync(file, "funkos");
            if (relativePath.IsFailure)
            {
                logger.LogWarning("Error al guardar imagen: {Error}", relativePath.Error.Message);
                return BadRequest(new { message = relativePath.Error.Message });
            }
            request.Imagen = relativePath.Value;
        }
        
        if (string.IsNullOrEmpty(request.Imagen))
        {
            logger.LogWarning("Intento de crear Funko sin imagen");
            return BadRequest(new { message = "El campo 'Imagen' es obligatorio en un PUT. Debe subir un archivo." });
        }
        
        var result = await service.CreateAsync(request);

        if (result.IsFailure)
        {
            logger.LogWarning("Error al crear Funko: {Error}", result.Error.Message);
            if (result.Error is FunkoConflictError)
            {
                return Conflict(new { message = result.Error.Message });
            }
            return BadRequest(new { message = result.Error.Message });
        }

        logger.LogInformation("Funko creado exitosamente con id: {Id}, Nombre: {Nombre}", result.Value.Id, result.Value.Nombre);
        
        //Devolvemos un 201
        // 1. Nombre de la función que da el detalle (GetByIdAsync), en base a la cual se calcula la url donde podemos encontrar
        // el Funko recién creado.
        // 2. Parámetros para esa función (el ID del nuevo funko)
        // 3. El objeto creado en sí
        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { id = result.Value.Id },
            result.Value);
    }

    //El path es /funkos/id
    [HttpPut("{id}")]
    //Devuelve un código 200 con el FunkoDTO actualizado como body
    [ProducesResponseType(typeof(FunkoResponseDTO), StatusCodes.Status200OK)]
    //Devuelve un código 404 en caso de que el Funko a actualizar no exista
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    //Devuelve un código 400 en caso de que el body tenga errores de validación
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> PutAsync(long id, [FromForm] FunkoPostPutRequestDTO request, [FromForm] IFormFile? file = null)
    {
        logger.LogInformation("Actualizando Funko con id: {Id}, Nombre: {Nombre}, Categoria: {Categoria}", 
            id, request.Nombre, request.Categoria);
        
        if (file != null)
        {
            logger.LogDebug("Actualizando imagen para Funko id: {Id}", id);
            var relativePath = await storage.SaveFileAsync(file, "funkos");
            if (relativePath.IsFailure)
            {
                logger.LogWarning("Error al guardar imagen para Funko id {Id}: {Error}", id, relativePath.Error.Message);
                return BadRequest(new { message = relativePath.Error.Message });
            }
            request.Imagen = relativePath.Value;
        }
        
        if (string.IsNullOrEmpty(request.Imagen))
        {
            logger.LogWarning("Intento de actualizar Funko id {Id} sin imagen", id);
            return BadRequest(new { message = "El campo 'Imagen' es obligatorio en un PUT. Debe subir un archivo." });
        }
        
        var result = await service.UpdateAsync(id, request);

        if (result.IsFailure)
        {
            logger.LogWarning("Error al actualizar Funko id {Id}: {Error}", id, result.Error.Message);
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

        logger.LogInformation("Funko id {Id} actualizado exitosamente", id);
        return Ok(result.Value);
    }

    //El path es /funkos/id
    [HttpPatch("{id}")]
    //Devuelve un código 200 con el FunkoDTO actualizado parcialmente como body
    [ProducesResponseType(typeof(FunkoResponseDTO), StatusCodes.Status200OK)]
    //Devuelve un código 404 en caso de que el Funko a actualizar no exista
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    //Devuelve un código 409 en caso de que la categoría nueva no sea válida
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> PatchAsync(long id, [FromForm] FunkoPatchRequestDTO request, [FromForm]  IFormFile? file = null)
    {
        logger.LogInformation("Aplicando PATCH a Funko id: {Id}", id);
        
        if (file != null)
        {
            logger.LogDebug("Actualizando imagen parcialmente para Funko id: {Id}", id);
            var relativePath = await storage.SaveFileAsync(file, "funkos");
            if (relativePath.IsFailure)
            {
                logger.LogWarning("Error al guardar imagen en PATCH para Funko id {Id}: {Error}", id, relativePath.Error.Message);
                return BadRequest(new { message = relativePath.Error.Message });
            }
            request.Imagen = relativePath.Value;
        }
        
        var result = await service.PatchAsync(id, request);

        if (result.IsFailure)
        {
            logger.LogWarning("Error al aplicar PATCH a Funko id {Id}: {Error}", id, result.Error.Message);
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

        logger.LogInformation("PATCH aplicado exitosamente a Funko id {Id}", id);
        return Ok(result.Value);
    }

    //El path es /funkos/id
    [HttpDelete("{id}")]
    //Devuelve un código 200 con el FunkoDTO que ha sido eliminado
    [ProducesResponseType(typeof(FunkoResponseDTO), StatusCodes.Status200OK)]
    //Devuelve un código 404 en caso de que el Funko a eliminar no exista
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> DeleteAsync(long id)
    {
        logger.LogInformation("Eliminando Funko con id: {Id}", id);
        var result = await service.DeleteAsync(id);

        if (result.IsFailure)
        {
            logger.LogWarning("Error al eliminar Funko id {Id}: {Error}", id, result.Error.Message);
            if (result.Error is FunkoNotFoundError)
            {
                return NotFound(new { message = result.Error.Message });
            }
            return BadRequest(new { message = result.Error.Message });
        }

        logger.LogInformation("Funko id {Id} eliminado exitosamente, eliminando imagen asociada", id);
        await storage.DeleteFileAsync(Path.GetFileName(result.Value.Imagen));
        return Ok(result.Value);
    }
}