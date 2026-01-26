using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FunkoApi.DTO;
using FunkoApi.Error;
using FunkoApi.GraphQL.Events;
using FunkoApi.GraphQL.Publisher;
using FunkoApi.Mapper;
using FunkoApi.Models;
using FunkoApi.Repository;
using Microsoft.Extensions.Caching.Memory;

namespace FunkoApi.Services;

public class FunkoService (
    IMemoryCache cache,
    IFunkoRepository repository, 
    ICategoryRepository categoryRepository,
    IEventPublisher eventPublisher,
    ILogger<FunkoService> logger) 
    : IFunkoService
{
    
    private const string CacheKeyPrefix = "Funko_";
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
    
    public async Task<Result<FunkoResponseDTO, FunkoError>> GetByIdAsync(long id)
    {
        var cacheKey = CacheKeyPrefix +id;

        if (cache.TryGetValue(cacheKey, out Funko? cachedFunko))
        {
            if (cachedFunko != null)
            {
                return cachedFunko.ToDto();
            }
        }
        
        var funko = await repository.GetByIdAsync(id);
        if (funko == null)
        {
            return Result.Failure<FunkoResponseDTO, FunkoError>(new FunkoNotFoundError($"No se encontró el Funko con id: {id}."));
        }
        
        cache.Set(cacheKey, funko, _cacheDuration);
        return funko.ToDto();
    }

    public async Task<Result<PageResponse<FunkoResponseDTO>, FunkoError>> GetAllAsync(FilterDTO filter)
    {

        var (funkos, totalCount) = await repository.GetAllAsync(filter);
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
        var foundCategory = await categoryRepository.GetByNameAsync(dto.Categoria);
        if (foundCategory == null)
        {
            return Result.Failure<FunkoResponseDTO, FunkoError>(new FunkoConflictError($"La categoría: {dto.Categoria} no existe."));
        }
        
        var funkoModel = dto.ToModel();
        
        // Asignarmos el CategoryId obtenido de la búsqueda
        // Para establecer la relación de FK correctamente
        funkoModel.CategoryId = foundCategory.Id;
        
        var savedFunko = await repository.CreateAsync(funkoModel);
        
        //Notificamos mediante GraphQL
        GraphQlNotifyCreation(savedFunko);
        
        return savedFunko.ToDto();
    }

    public async Task<Result<FunkoResponseDTO, FunkoError>> UpdateAsync(long id, FunkoPostPutRequestDTO dto)
    {
        var foundCategory = await categoryRepository.GetByNameAsync(dto.Categoria);
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

        var updatedFunko = await repository.UpdateAsync(id, funkoToUpdate);

        if (updatedFunko == null)
        {
            return Result.Failure<FunkoResponseDTO, FunkoError>(new FunkoNotFoundError($"No se encontró el Funko con id: {id}."));
        }

        //Notificamos mediante GraphQL
        GraphQlNotifyUpdate(updatedFunko);
        
        cache.Remove(CacheKeyPrefix + id);
        return updatedFunko.ToDto();
    }

    public async Task<Result<FunkoResponseDTO, FunkoError>> PatchAsync(long id, FunkoPatchRequestDTO dto)
    {
        var foundFunko = await repository.GetByIdAsync(id);
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
            var foundCategory = await categoryRepository.GetByNameAsync(dto.Categoria);
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

        await repository.UpdateAsync(id, foundFunko);
    
        //Notificamos mediante GraphQL
        GraphQlNotifyUpdate(foundFunko);
        
        cache.Remove(CacheKeyPrefix + id);
        return foundFunko.ToDto();
    }

    public async Task<Result<FunkoResponseDTO, FunkoError>> DeleteAsync(long id)
    {
        var deletedFunko = await repository.DeleteAsync(id);

        if (deletedFunko == null)
        {
            return Result.Failure<FunkoResponseDTO, FunkoError>(new FunkoNotFoundError($"No se encontró el Funko con id: {id}."));
        }
        
        //Notificamos mediante GraphQL
        GraphQlNotifyDelete(deletedFunko);
        
        cache.Remove(CacheKeyPrefix + id);
        return deletedFunko.ToDto();
    }

    private void GraphQlNotifyCreation(Funko createdFunko)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await eventPublisher.PublishAsync("onCreatedFunko", new FunkoCreatedEvent
                {
                    Id = createdFunko.Id,
                    Nombre = createdFunko.Nombre,
                    Categoria = createdFunko.Category.Nombre,
                    Precio = createdFunko.Precio,
                    CreatedAt = createdFunko.CreatedAt,
                });
                logger.LogInformation("Emitida notificación de creación de Funko mediante GraphQL");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error al emitir notificación de creación de Funko mediante GraphQL");
            }
        });
    }
    
    private void GraphQlNotifyUpdate(Funko updatedFunko)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await eventPublisher.PublishAsync("onUpdatedFunko", new FunkoUpdatedEvent()
                {
                    Id = updatedFunko.Id,
                    Nombre = updatedFunko.Nombre,
                    Categoria = updatedFunko.Category.Nombre,
                    Precio = updatedFunko.Precio,
                    UpdatedAt = updatedFunko.UpdatedAt,
                });
                logger.LogInformation("Emitida notificación de actualización de Funko mediante GraphQL");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error al emitir notificación de actualización de Funko mediante GraphQL");
            }
        });
    }
    
    private void GraphQlNotifyDelete(Funko deletedFunko)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await eventPublisher.PublishAsync("onDeletedFunko", new FunkoDeletedEvent()
                {
                    Id = deletedFunko.Id,
                    Nombre = deletedFunko.Nombre,
                    Categoria = deletedFunko.Category.Nombre,
                    Precio = deletedFunko.Precio,
                    DeletedAt = DateTime.UtcNow,
                });
                logger.LogInformation("Emitida notificación de borrado de Funko mediante GraphQL");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error al emitir notificación de borrado de Funko mediante GraphQL");
            }
        });
    }
    
}