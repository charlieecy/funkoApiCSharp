using CSharpFunctionalExtensions;
using FunkoApi.Error;

namespace FunkoApi.Storage;


public class FunkoStorageService : IFunkoStorage
{
    private readonly string _rootPath;
    private readonly string _uploadPath;
    private readonly long _maxFileSize;
    private readonly string[] _allowedExtensions;
    private readonly string[] _allowedContentTypes;
    private readonly ILogger<FunkoStorageService> _logger;
    
    public FunkoStorageService(IConfiguration configuration, ILogger<FunkoStorageService> logger, IWebHostEnvironment env)
    {
        _logger = logger;

        // Configuración desde appsettings.json (ruta relativa a wwwroot)
        _uploadPath = configuration["Storage:UploadPath"] ?? "uploads";
        _maxFileSize = configuration.GetValue<long>("Storage:MaxFileSize", 5 * 1024 * 1024);
        _allowedExtensions = configuration.GetSection("Storage:AllowedExtensions").Get<string[]>()
                             ?? [".jpg", ".jpeg", ".png", ".gif"];
        _allowedContentTypes = configuration.GetSection("Storage:AllowedContentTypes").Get<string[]>()
                               ?? ["image/jpeg", "image/png", "image/gif"];

        // Ruta absoluta: usar WebHostEnvironment.WebRootPath (apunta a wwwroot)
        _rootPath = System.IO.Path.Combine(env.WebRootPath, _uploadPath); //wwwroot + /uploads (este último definido en el archivo appsettings.json

        // Crear directorio si no existe
        if (!Directory.Exists(_rootPath))
        {
            Directory.CreateDirectory(_rootPath);
        }

        _logger.LogInformation("Storage service inicializado en: {Path}", _rootPath);
    }
    
    private static string GenerateUniqueFilename(string originalFilename)
    {
        var extension = System.IO.Path.GetExtension(originalFilename).ToLowerInvariant();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var sanitizedName = System.IO.Path.GetFileNameWithoutExtension(originalFilename)
            .Replace(" ", "_")
            .Replace("-", "_");
        return $"{timestamp}_{uniqueId}_{sanitizedName}{extension}";
    }
    
    private UnitResult<FunkoError> ValidateFile(IFormFile file)
    {
        if (file is null or { Length: 0 })
        {
            return UnitResult.Failure<FunkoError>(new FunkoStorageError("Archivo vacío"));
        }

        if (file.Length > _maxFileSize)
        {
            return UnitResult.Failure<FunkoError>(
                new FunkoStorageError("Archivo demasiado grande. Extensión máxima: {}" + _maxFileSize));
        }

        var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            return UnitResult.Failure<FunkoError>(
                new FunkoStorageError("Extensión no permitida: {}" + extension));
        }

        var contentType = file.ContentType?.ToLowerInvariant();
        if (contentType == null || !_allowedContentTypes.Any(ct => contentType.Contains(ct.Split('/')[1])))
        {
            return UnitResult.Failure<FunkoError>(
                new FunkoStorageError("Tipo de contenido no permitido: {}" + contentType));
        }

        var filename = System.IO.Path.GetFileName(file.FileName);
        if (filename.Contains("..") || filename.Contains('/') || filename.Contains('\\'))
        {
            return UnitResult.Failure<FunkoError>(new FunkoStorageError("Nombre de archivo no válido"));
        }

        return UnitResult.Success<FunkoError>();
    }
    
    public Task<Result<string, FunkoError>> SaveFileAsync(IFormFile file, string folder)
    {
        var validation = ValidateFile(file);
        if (validation.IsFailure)
        {
            return Task.FromResult(Result.Failure<string, FunkoError>(validation.Error));
        }

        try
        {
            // Generar nombre único
            var filename = GenerateUniqueFilename(file.FileName);

            // Crear directorio destino
            var folderPath = System.IO.Path.Combine(_rootPath, folder);
            Directory.CreateDirectory(folderPath);

            // Guardar ficheiro
            var filePath = System.IO.Path.Combine(folderPath, filename);
            var relativePath = GetRelativePath(filename, folder);

            using var stream = new FileStream(filePath, FileMode.Create);
            file.CopyTo(stream);

            _logger.LogInformation("Archivo guardado: {Path}", relativePath);

            return Task.FromResult(Result.Success<string, FunkoError>(relativePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando archivo");
            return Task.FromResult(Result.Failure<string, FunkoError>(
                new FunkoStorageError("Error  guardando archivo")));
        }
    }

    public Task<Result<bool, FunkoError>> DeleteFileAsync(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            return Task.FromResult(Result.Success<bool, FunkoError>(true));
        }

        try
        {
            var fullPath = GetFullPath(filename);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Archivo eliminado: {Filename}", filename);
            }

            return Task.FromResult(Result.Success<bool, FunkoError>(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando archivo {Filename}", filename);
            return Task.FromResult(Result.Failure<bool, FunkoError>(
                new FunkoStorageError("Error eliminando archivo.")));
        }
    }

    public bool FileExists(string filename)
    {
        if (string.IsNullOrEmpty(filename))
            return false;

        var fullPath = GetFullPath(filename);
        return File.Exists(fullPath);    }

    public string GetFullPath(string filename)
    {
        if (System.IO.Path.IsPathRooted(filename))
            return filename;

        var cleanFilename = filename;
        var prefix = $"/{_uploadPath}/";

        if (filename.StartsWith("/storage/", StringComparison.OrdinalIgnoreCase))
            cleanFilename = filename["/storage/".Length..];
        else if (filename.StartsWith("/storage", StringComparison.OrdinalIgnoreCase))
            cleanFilename = filename["/storage".Length..].TrimStart('/');
        else if (filename.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            cleanFilename = filename[prefix.Length..];

        return System.IO.Path.Combine(_rootPath, cleanFilename);
    }

    public string GetRelativePath(string filename, string folder = "funkos")
    {
        return $"/{_uploadPath}/{folder}/{filename}";    
    }
}