using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FunkoApi.DTO;
using FunkoApi.Error;
using FunkoApi.GraphQL.Events;
using FunkoApi.GraphQL.Publisher;
using FunkoApi.Mail;
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
    IEmailService emailService,
    IConfiguration configuration,
    ILogger<FunkoService> logger) 
    : IFunkoService
{
    
    private const string CacheKeyPrefix = "Funko_";
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
    
    public async Task<Result<FunkoResponseDTO, FunkoError>> GetByIdAsync(long id)
    {
        logger.LogDebug("Buscando Funko con id: {Id}", id);
        var cacheKey = CacheKeyPrefix +id;

        if (cache.TryGetValue(cacheKey, out Funko? cachedFunko))
        {
            if (cachedFunko != null)
            {
                logger.LogDebug("Funko con id {Id} encontrado en caché", id);
                return cachedFunko.ToDto();
            }
        }
        
        var funko = await repository.GetByIdAsync(id);
        if (funko == null)
        {
            logger.LogWarning("Funko con id {Id} no encontrado en la base de datos", id);
            return Result.Failure<FunkoResponseDTO, FunkoError>(new FunkoNotFoundError($"No se encontró el Funko con id: {id}."));
        }
        
        cache.Set(cacheKey, funko, _cacheDuration);
        logger.LogDebug("Funko con id {Id} obtenido de BD y almacenado en caché", id);
        return funko.ToDto();
    }

    public async Task<Result<PageResponse<FunkoResponseDTO>, FunkoError>> GetAllAsync(FilterDTO filter)
    {
        logger.LogDebug("Obteniendo listado de Funkos con filtros - Nombre: {Nombre}, Categoria: {Categoria}, MaxPrecio: {MaxPrecio}",
            filter.Nombre, filter.Categoria, filter.MaxPrecio);

        var (funkos, totalCount) = await repository.GetAllAsync(filter);
        var response = funkos.Select(it => it.ToDto()).ToList();

        var page = new PageResponse<FunkoResponseDTO>
        {
            Items = response,
            TotalCount = totalCount,
            Page = filter.Page,
            Size = filter.Size
        };

        logger.LogInformation("Listado de Funkos obtenido, total encontrado: {Total}, página: {Page}", totalCount, filter.Page);
        return Result.Success<PageResponse<FunkoResponseDTO>, FunkoError>(page);
    }

    public async Task<Result<FunkoResponseDTO, FunkoError>> CreateAsync(FunkoPostPutRequestDTO dto)
    {
        logger.LogInformation("Creando nuevo Funko: {Nombre}, Categoria: {Categoria}, Precio: {Precio}",
            dto.Nombre, dto.Categoria, dto.Precio);
        
        var foundCategory = await categoryRepository.GetByNameAsync(dto.Categoria);
        if (foundCategory == null)
        {
            logger.LogWarning("Intento de crear Funko con categoría inexistente: {Categoria}", dto.Categoria);
            return Result.Failure<FunkoResponseDTO, FunkoError>(new FunkoConflictError($"La categoría: {dto.Categoria} no existe."));
        }
        
        var funkoModel = dto.ToModel();
        
        // Asignarmos el CategoryId obtenido de la búsqueda
        // Para establecer la relación de FK correctamente
        funkoModel.CategoryId = foundCategory.Id;
        
        var savedFunko = await repository.CreateAsync(funkoModel);
        logger.LogInformation("Funko creado exitosamente con id: {Id}, Nombre: {Nombre}", savedFunko.Id, savedFunko.Nombre);
        
        //Notificamos mediante GraphQL
        GraphQlNotifyCreation(savedFunko);
        //Enviamos mail a Mailtrap
        EnviarEmailProductoCreado(savedFunko);
        
        return savedFunko.ToDto();
    }

    public async Task<Result<FunkoResponseDTO, FunkoError>> UpdateAsync(long id, FunkoPostPutRequestDTO dto)
    {
        logger.LogInformation("Actualizando Funko con id: {Id}, Nombre: {Nombre}, Categoria: {Categoria}",
            id, dto.Nombre, dto.Categoria);
        
        var foundCategory = await categoryRepository.GetByNameAsync(dto.Categoria);
        if (foundCategory == null)
        {
            logger.LogWarning("Intento de actualizar Funko id {Id} con categoría inexistente: {Categoria}", id, dto.Categoria);
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
            logger.LogWarning("Funko con id {Id} no encontrado para actualizar", id);
            return Result.Failure<FunkoResponseDTO, FunkoError>(new FunkoNotFoundError($"No se encontró el Funko con id: {id}."));
        }

        logger.LogInformation("Funko id {Id} actualizado exitosamente", id);
        //Notificamos mediante GraphQL
        GraphQlNotifyUpdate(updatedFunko);
        
        cache.Remove(CacheKeyPrefix + id);
        return updatedFunko.ToDto();
    }

    public async Task<Result<FunkoResponseDTO, FunkoError>> PatchAsync(long id, FunkoPatchRequestDTO dto)
    {
        logger.LogInformation("Aplicando PATCH a Funko id: {Id}", id);
        
        var foundFunko = await repository.GetByIdAsync(id);
        if (foundFunko == null)
        {
            logger.LogWarning("Funko con id {Id} no encontrado para aplicar PATCH", id);
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
                logger.LogWarning("Intento de PATCH con categoría inexistente: {Categoria}", dto.Categoria);
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
        logger.LogInformation("PATCH aplicado exitosamente a Funko id {Id}", id);
    
        //Notificamos mediante GraphQL
        GraphQlNotifyUpdate(foundFunko);
        
        cache.Remove(CacheKeyPrefix + id);
        return foundFunko.ToDto();
    }

    public async Task<Result<FunkoResponseDTO, FunkoError>> DeleteAsync(long id)
    {
        logger.LogInformation("Eliminando Funko con id: {Id}", id);
        var deletedFunko = await repository.DeleteAsync(id);

        if (deletedFunko == null)
        {
            logger.LogWarning("Funko con id {Id} no encontrado para eliminar", id);
            return Result.Failure<FunkoResponseDTO, FunkoError>(new FunkoNotFoundError($"No se encontró el Funko con id: {id}."));
        }
        
        logger.LogInformation("Funko id {Id} eliminado exitosamente de la BD", id);
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
    
    private void EnviarEmailProductoCreado(Funko funko)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var adminEmail = configuration["Smtp:AdminEmail"];
                if (string.IsNullOrEmpty(adminEmail)) return;

                var content = EmailTemplates.ProductoCreado(funko.Nombre, funko.Precio, funko.Category.Nombre, funko.Id);
                var body = EmailTemplates.CreateBase("Nuevo Producto Creado", content);

                var emailMessage = new EmailMessage
                {
                    To = adminEmail,
                    Subject = "🆕 Nuevo Producto en Tienda DAW",
                    Body = body,
                    IsHtml = true
                };
                await emailService.EnqueueEmailAsync(emailMessage);
                logger.LogDebug("Email de notificación encolado tras crear producto");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al encolar email de notificación tras crear producto");
            }
        });
    }
    
}