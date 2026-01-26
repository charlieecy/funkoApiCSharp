using CSharpFunctionalExtensions;
using FunkoApi.DTO;
using FunkoApi.Error;
using FunkoApi.Services;
using FunkoApi.Storage;
using Microsoft.AspNetCore.Mvc;
using Path = System.IO.Path;

namespace FunkoApi.Controller;

[ApiController]
//Hacemos que la url genérica sea /funkos, elimina la palabra controller
//y se queda con el nombre de la clase
[Route("[controller]")]
//Especificamos que va a devolver .json
[Produces("application/json")]
public class FunkosController(IFunkoService service, IFunkoStorage storage) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<FunkoResponseDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? nombre = null,
        [FromQuery] string? categoria  = null,
        [FromQuery] double? maxPrecio = null,
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string sortby = "id",
        [FromQuery] string direction = "asc")
    {
        var filter = new FilterDTO(nombre, categoria, maxPrecio, page, size, direction);
        var result = await service.GetAllAsync(filter);
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
    public async Task<IActionResult> GetByIdAsync(long id)
    {
        var result = await service.GetByIdAsync(id);

        if (result.IsFailure)
        {
            //ojo! el NotFound no es el error de dominio, sino el código 404 de C#
            if (result.Error is FunkoNotFoundError)
            {
                return NotFound(new { message = result.Error.Message });
            }
            return BadRequest(new { message = result.Error.Message });
        }

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
    public async Task<IActionResult> PostAsync([FromForm] FunkoPostPutRequestDTO request, [FromForm] IFormFile? file = null)
    {
        if (file != null)
        {
            var relativePath = await storage.SaveFileAsync(file, "funkos");
            if (relativePath.IsFailure)
            {
                return BadRequest(new { message = relativePath.Error.Message });
            }
            request.Imagen = relativePath.Value;
        }
        
        if (string.IsNullOrEmpty(request.Imagen))
        {
            return BadRequest(new { message = "El campo 'Imagen' es obligatorio en un PUT. Debe subir un archivo." });
        }
        
        var result = await service.CreateAsync(request);

        if (result.IsFailure)
        {
            if (result.Error is FunkoConflictError)
            {
                return Conflict(new { message = result.Error.Message });
            }
            return BadRequest(new { message = result.Error.Message });
        }

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
    public async Task<IActionResult> PutAsync(long id, [FromForm] FunkoPostPutRequestDTO request, [FromForm] IFormFile? file = null)
    {
        
        if (file != null)
        {
            var relativePath = await storage.SaveFileAsync(file, "funkos");
            if (relativePath.IsFailure)
            {
                return BadRequest(new { message = relativePath.Error.Message });
            }
            request.Imagen = relativePath.Value;
        }
        
        if (string.IsNullOrEmpty(request.Imagen))
        {
            return BadRequest(new { message = "El campo 'Imagen' es obligatorio en un PUT. Debe subir un archivo." });
        }
        
        var result = await service.UpdateAsync(id, request);

        if (result.IsFailure)
        {
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
    public async Task<IActionResult> PatchAsync(long id, [FromForm] FunkoPatchRequestDTO request, [FromForm]  IFormFile? file = null)
    {
        
        if (file != null)
        {
            var relativePath = await storage.SaveFileAsync(file, "funkos");
            if (relativePath.IsFailure)
            {
                return BadRequest(new { message = relativePath.Error.Message });
            }
            request.Imagen = relativePath.Value;
        }
        
        var result = await service.PatchAsync(id, request);

        if (result.IsFailure)
        {
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

        return Ok(result.Value);
    }

    //El path es /funkos/id
    [HttpDelete("{id}")]
    //Devuelve un código 200 con el FunkoDTO que ha sido eliminado
    [ProducesResponseType(typeof(FunkoResponseDTO), StatusCodes.Status200OK)]
    //Devuelve un código 404 en caso de que el Funko a eliminar no exista
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(long id)
    {
        var result = await service.DeleteAsync(id);

        if (result.IsFailure)
        {
            if (result.Error is FunkoNotFoundError)
            {
                return NotFound(new { message = result.Error.Message });
            }
            return BadRequest(new { message = result.Error.Message });
        }

        await storage.DeleteFileAsync(Path.GetFileName(result.Value.Imagen));
        return Ok(result.Value);
    }
}