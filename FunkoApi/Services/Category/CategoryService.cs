using CSharpFunctionalExtensions;
using FunkoApi.DTO;
using FunkoApi.Error;
using FunkoApi.Mapper;
using FunkoApi.Models;
using FunkoApi.Repository;
using Microsoft.Extensions.Caching.Memory;

namespace FunkoApi.Services;

public class CategoryService (ICategoryRepository repository, IMemoryCache cache, ILogger<CategoryService> logger) : ICategoryService
{
    private const string CacheKeyPrefix = "Category_";
    private readonly ICategoryRepository _repository = repository;
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<CategoryService> _logger = logger;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);


    public async Task<Result<CategoryResponseDTO, FunkoError>> GetByIdAsync(Guid id)
    {
        _logger.LogDebug("Buscando categoría con id: {Id}", id);
        var cacheKey = CacheKeyPrefix +id;

        if (_cache.TryGetValue(cacheKey, out Category? cachedCategory))
        {
            if (cachedCategory != null)
            {
                _logger.LogDebug("Categoría con id {Id} encontrada en caché", id);
                return cachedCategory.ToDto();
            }
        }
        
        var category = await _repository.GetByIdAsync(id);
        if (category == null)
        {
            _logger.LogWarning("Categoría con id {Id} no encontrada en la base de datos", id);
            return Result.Failure<CategoryResponseDTO, FunkoError>(new FunkoNotFoundError($"No se encontró la categoría con id: {id}."));
        }
        
        _cache.Set(cacheKey, category, _cacheDuration);
        _logger.LogDebug("Categoría con id {Id} obtenida de BD y almacenada en caché", id);
        return category.ToDto();    
    }

    public async Task<List<CategoryResponseDTO>> GetAllAsync()
    {
        _logger.LogDebug("Obteniendo listado completo de categorías");
        var categories = await _repository.GetAllAsync();
        
        _logger.LogInformation("Listado de categorías obtenido, total: {Total}", categories.Count);
        return categories
            .Select(it => it.ToDto())
            .ToList();
    }

    public async Task<Result<CategoryResponseDTO, FunkoError>> CreateAsync(CategoryPostPutRequestDTO dto)
    {
        _logger.LogInformation("Creando nueva categoría: {Nombre}", dto.Nombre);
        var alreadyExistingCategory = await _repository.GetByNameAsync(dto.Nombre);

        if (alreadyExistingCategory != null)
        {
            _logger.LogWarning("Intento de crear categoría que ya existe: {Nombre}", dto.Nombre);
            return Result.Failure<CategoryResponseDTO, FunkoError>(
                new FunkoConflictError($"La categoría: {dto.Nombre} ya existe."));
        }

        var categoryModel = dto.ToModel();
        
        var savedCategory = await _repository.CreateAsync(categoryModel);
        _logger.LogInformation("Categoría creada exitosamente con id: {Id}, Nombre: {Nombre}", savedCategory.Id, savedCategory.Nombre);
        
        return savedCategory.ToDto();
    }

    public async Task<Result<CategoryResponseDTO, FunkoError>> UpdateAsync(Guid id, CategoryPostPutRequestDTO dto)
    {
        _logger.LogInformation("Actualizando categoría con id: {Id}, Nuevo nombre: {Nombre}", id, dto.Nombre);
        var existingWithSameName = await _repository.GetByNameAsync(dto.Nombre);

        if (existingWithSameName != null && existingWithSameName.Id != id)
        {
            _logger.LogWarning("Intento de actualizar categoría id {Id} con nombre que ya existe: {Nombre}", id, dto.Nombre);
            return Result.Failure<CategoryResponseDTO, FunkoError>(
                new FunkoConflictError($"Ya existe otra categoría con el nombre: {dto.Nombre}."));
        }
    
        var updatedCategory = await _repository.UpdateAsync(id, new Category {Nombre = dto.Nombre});

        if (updatedCategory == null)
        {
            _logger.LogWarning("Categoría con id {Id} no encontrada para actualizar", id);
            return Result.Failure<CategoryResponseDTO, FunkoError>(
                new FunkoNotFoundError($"No se encontró la categoría con id: {id}."));
        }
    
        _logger.LogInformation("Categoría id {Id} actualizada exitosamente", id);
        _cache.Remove(CacheKeyPrefix + id);
        return updatedCategory.ToDto();
    }

    public async Task<Result<CategoryResponseDTO, FunkoError>> DeleteAsync(Guid id)
    {
        _logger.LogInformation("Eliminando categoría con id: {Id}", id);
        var deletedCategory = await _repository.DeleteAsync(id);

        if (deletedCategory == null)
        {
            _logger.LogWarning("Categoría con id {Id} no encontrada para eliminar", id);
            return Result.Failure<CategoryResponseDTO, FunkoError>(
                new FunkoNotFoundError($"No se encontró la categoría con id: {id}."));
        }
        
        _logger.LogInformation("Categoría id {Id} eliminada exitosamente de la BD", id);
        _cache.Remove(CacheKeyPrefix + id);
        return deletedCategory.ToDto();
    }
}