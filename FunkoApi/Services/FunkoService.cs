using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FunkoApi.DTO;
using FunkoApi.Error;
using FunkoApi.Mapper;
using FunkoApi.Models;
using FunkoApi.Repository;
using Microsoft.Extensions.Caching.Memory;

namespace FunkoApi.Services;

public class FunkoService (IFunkoRepository repository, IMemoryCache cache, ICategoryRepository categoryRepository) : IFunkoService
{
    
    private const string CacheKeyPrefix = "Funko_";
    private readonly IFunkoRepository _repository = repository;
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IMemoryCache _cache = cache;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
    
    public async Task<Result<FunkoResponseDTO, FunkoError>> GetByIdAsync(long id)
    {
        var cacheKey = CacheKeyPrefix +id;

        if (_cache.TryGetValue(cacheKey, out Funko? cachedFunko))
        {
            if (cachedFunko != null)
            {
                return cachedFunko.ToDto();
            }
        }
        
        var funko = await _repository.GetByIdAsync(id);
        if (funko == null)
        {
            return Result.Failure<FunkoResponseDTO, FunkoError>(new FunkoNotFoundError($"No se encontró el Funko con id: {id}."));
        }
        
        _cache.Set(cacheKey, funko, _cacheDuration);
        return funko.ToDto();
    }

    public async Task<Result<PageResponse<FunkoResponseDTO>, FunkoError>> GetAllAsync(FilterDTO filter)
    {

        var (funkos, totalCount) = await _repository.GetAllAsync(filter);
        var response = funkos.Select(it => it.ToDto()).ToList();

        var page = new PageResponse<FunkoResponseDTO>
        {
            Items = response,
            TotalCount = totalCount,
            Page = filter.Page,
            Size = filter.Size
        };

        return Result.Success<PageResponse<FunkoResponseDTO>, FunkoError>(page);
    }

    public async Task<Result<FunkoResponseDTO, FunkoError>> CreateAsync(FunkoPostPutRequestDTO dto)
    {
        var foundCategory = await _categoryRepository.GetByNameAsync(dto.Categoria);
        if (foundCategory == null)
        {
            return Result.Failure<FunkoResponseDTO, FunkoError>(new FunkoConflictError($"La categoría: {dto.Categoria} no existe."));
        }
        
        var funkoModel = dto.ToModel();
        
        // Asignarmos el CategoryId obtenido de la búsqueda
        // Para establecer la relación de FK correctamente
        funkoModel.CategoryId = foundCategory.Id;
        
        var savedFunko = await _repository.CreateAsync(funkoModel);
        
        return savedFunko.ToDto();
    }

    public async Task<Result<FunkoResponseDTO, FunkoError>> UpdateAsync(long id, FunkoPostPutRequestDTO dto)
    {
        var foundCategory = await _categoryRepository.GetByNameAsync(dto.Categoria);
        if (foundCategory == null)
        {
            return Result.Failure<FunkoResponseDTO, FunkoError>(new FunkoConflictError($"La categoría: {dto.Categoria} no existe."));
        }
        
        var funkoToUpdate = dto.ToModel();
        funkoToUpdate.Id = id;
        
        // Asignarmos el CategoryId obtenido de la búsqueda
        // Para establecer la relación de FK correctamente
        funkoToUpdate.CategoryId = foundCategory.Id;
    
        funkoToUpdate.UpdatedAt = DateTime.UtcNow;

        var updatedFunko = await _repository.UpdateAsync(id, funkoToUpdate);

        if (updatedFunko == null)
        {
            return Result.Failure<FunkoResponseDTO, FunkoError>(new FunkoNotFoundError($"No se encontró el Funko con id: {id}."));
        }

        _cache.Remove(CacheKeyPrefix + id);
        return updatedFunko.ToDto();
    }

    public async Task<Result<FunkoResponseDTO, FunkoError>> PatchAsync(long id, FunkoPatchRequestDTO dto)
    {
        var foundFunko = await _repository.GetByIdAsync(id);
        if (foundFunko == null)
        {
            return Result.Failure<FunkoResponseDTO, FunkoError>(new FunkoNotFoundError($"Funko {id} no encontrado"));
        }

        if (dto.Nombre != null) 
        {
            foundFunko.Nombre = dto.Nombre;
        }

        if (dto.Precio != null)
        {
            foundFunko.Precio = (double)dto.Precio;
        }

        if (dto.Categoria != null)
        {
            var foundCategory = await _categoryRepository.GetByNameAsync(dto.Categoria);
            if (foundCategory == null)
            {
                return Result.Failure<FunkoResponseDTO, FunkoError>(new FunkoConflictError($"La categoría: {dto.Categoria} no existe."));
            }
            // Asignarmos el CategoryId obtenido de la búsqueda
            // Para establecer la relación de FK correctamente
            foundFunko.Category = foundCategory;
            foundFunko.CategoryId = foundCategory.Id;
        }

        if (dto.Imagen != null)
        {
            foundFunko.Imagen = dto.Imagen;
        }

        await _repository.UpdateAsync(id, foundFunko);
    
        _cache.Remove(CacheKeyPrefix + id);
        return foundFunko.ToDto();
    }

    public async Task<Result<FunkoResponseDTO, FunkoError>> DeleteAsync(long id)
    {
        var deletedFunko = await _repository.DeleteAsync(id);

        if (deletedFunko == null)
        {
            return Result.Failure<FunkoResponseDTO, FunkoError>(new FunkoNotFoundError($"No se encontró el Funko con id: {id}."));
        }
        
        _cache.Remove(CacheKeyPrefix + id);
        return deletedFunko.ToDto();
    }
    
}