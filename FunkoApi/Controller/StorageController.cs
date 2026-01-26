using FunkoApi.Storage;
using Microsoft.AspNetCore.Mvc;
using Path = System.IO.Path;

namespace FunkoApi.Controller;

[ApiController]
[Route("[controller]")]
public class StorageController(
    IWebHostEnvironment environment,
    ILogger<StorageController> logger,
    IFunkoStorage storage
) : ControllerBase
{
    private readonly IWebHostEnvironment _environment = environment;
    private readonly ILogger<StorageController> _logger = logger;
    private readonly IFunkoStorage _storage = storage;



    [HttpGet("{**path}")]
    [ProducesResponseType(typeof(FileStreamResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public IActionResult GetFile(string path)

    {
        if (string.IsNullOrEmpty(path))
        {
            return NotFound();
        }

        try
        {
            var basePath = System.IO.Path.Combine(_environment.ContentRootPath, "wwwroot");
            var fullPath = System.IO.Path.Combine(basePath, path);

            if (!System.IO.File.Exists(fullPath))
            {
                _logger.LogWarning("Archivo no encontrado: {Path}", fullPath);
                return NotFound(new { error = "Archivo no encontrado", path });
            }

            var fileInfo = new FileInfo(fullPath);
            if (fileInfo.Length > 10 * 1024 * 1024)
            {
                _logger.LogWarning("Archivo demasiado grande: {Path} ({Size} bytes)", path, fileInfo.Length);
                return BadRequest(new { error = "Archivo demasiado grande" });
            }

            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var contentType = GetContentType(fileInfo.Extension);

            _logger.LogInformation("Sirviendo archivo: {Path}", path);
            return File(fileStream, contentType);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Acceso denegado al archivo: {Path}", path);
            return Forbid();
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Error leyendo archivo: {Path}", path);
            return StatusCode(500, new { error = "Error leyendo archivo", details = ex.Message });
        }
    }
    
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromQuery] string folder = "funkos")
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No se ha proporcionado ningún archivo" });
        }

        // Ejecutamos el guardado usando el servicio inyectado
        var result = await _storage.SaveFileAsync(file, folder);

        if (result.IsFailure)
        {
            // El error viene de la validación interna de FunkoStorage
            return BadRequest(new { error = result.Error.Message });
        }

        // Construimos la URL pública (ej: http://localhost:5074/uploads/funkos/nombre.jpg)
        var fileUrl = $"{Request.Scheme}://{Request.Host}{result.Value}";

        return Created(fileUrl, new
        {
            fileName = Path.GetFileName(result.Value),
            url = fileUrl,
            relativePath = result.Value
        });
    }
    
    private static string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }
}