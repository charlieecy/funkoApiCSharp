using CSharpFunctionalExtensions;
using FunkoApi.DTO;
using FunkoApi.GraphQL.Inputs;
using FunkoApi.Services;
using HotChocolate.Authorization;

namespace FunkoApi.GraphQL.Mutations;

public class FunkoMutations
{
    private readonly ILogger<FunkoMutations> _logger;

    public FunkoMutations(ILogger<FunkoMutations> logger)
    {
        _logger = logger;
    }
    
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<FunkoResponseDTO> CreateFunkoAsync(
        PostPutFunkoInput input,
        [Service] IFunkoService funkoService
    )
    {
        _logger.LogInformation("GraphQL Mutation: Creando Funko - Nombre: {Nombre}, Categoria: {Categoria}", 
            input.Nombre, input.Categoria);
        
        var dto = new FunkoPostPutRequestDTO()
        {
            Nombre = input.Nombre,
            Categoria = input.Categoria,
            Precio = input.Precio,
            Imagen = input.Imagen,
        };

        var result = await funkoService.CreateAsync(dto);
        // Usamos Match para desenvolver el Result: 
        // Si es Success, devuelve el DTO. Si es Failure, lanza una excepción de GraphQL.
        return result.Match(
            success => {
                _logger.LogInformation("GraphQL Mutation: Funko creado exitosamente con id: {Id}", success.Id);
                return success;
            },
            error => {
                _logger.LogWarning("GraphQL Mutation: Error al crear Funko - {Error}", error.Message);
                throw new GraphQLException(ErrorBuilder.New()
                    .SetMessage(error.Message)
                    .SetCode(error.GetType().ToString())
                    .Build());
            }
        );
    }
    
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<FunkoResponseDTO> UpdateFunkoAsync(
        long id,
        PostPutFunkoInput input,
        [Service] IFunkoService funkoService
    )
    {
        _logger.LogInformation("GraphQL Mutation: Actualizando Funko id: {Id}", id);
        
        var dto = new FunkoPostPutRequestDTO()
        {
            Nombre = input.Nombre,
            Categoria = input.Categoria,
            Precio = input.Precio,
            Imagen = input.Imagen,
        };

        var result = await funkoService.UpdateAsync(id, dto);

        // Usamos Match para desenvolver el Result: 
        // Si es Success, devuelve el DTO. Si es Failure, lanza una excepción de GraphQL.
        return result.Match(
            success => {
                _logger.LogInformation("GraphQL Mutation: Funko id {Id} actualizado exitosamente", id);
                return success;
            },
            error => {
                _logger.LogWarning("GraphQL Mutation: Error al actualizar Funko id {Id} - {Error}", id, error.Message);
                throw new GraphQLException(ErrorBuilder.New()
                    .SetMessage(error.Message)
                    .SetCode(error.GetType().ToString())
                    .Build());
            }
        );
    }

    [Authorize(Policy = "RequireAdminRole")]
    public async Task<FunkoResponseDTO> PatchFunkoAsync(
        long id,
        PatchFunkoInput input,
        [Service] IFunkoService funkoService
    )
    {
        _logger.LogInformation("GraphQL Mutation: Aplicando PATCH a Funko id: {Id}", id);
        
        var dto = new FunkoPatchRequestDTO()
        {
            Nombre = input.Nombre,
            Categoria = input.Categoria,
            Precio = input.Precio,
            Imagen = input.Imagen,
        };
        
        var result = await funkoService.PatchAsync(id, dto);

        // Usamos Match para desenvolver el Result: 
        // Si es Success, devuelve el DTO. Si es Failure, lanza una excepción de GraphQL.
        return result.Match(
            success => {
                _logger.LogInformation("GraphQL Mutation: PATCH aplicado exitosamente a Funko id {Id}", id);
                return success;
            },
            error => {
                _logger.LogWarning("GraphQL Mutation: Error al aplicar PATCH a Funko id {Id} - {Error}", id, error.Message);
                throw new GraphQLException(ErrorBuilder.New()
                    .SetMessage(error.Message)
                    .SetCode(error.GetType().ToString())
                    .Build());
            }
        );
    }

    [Authorize(Policy = "RequireAdminRole")]
    public async Task<FunkoResponseDTO> DeleteFunkoAsync(
        long id,
        [Service] IFunkoService funkoService
    )
    {
        _logger.LogInformation("GraphQL Mutation: Eliminando Funko id: {Id}", id);
        
        var result = await funkoService.DeleteAsync(id);

        // Usamos Match para desenvolver el Result: 
        // Si es Success, devuelve el DTO. Si es Failure, lanza una excepción de GraphQL.
        return result.Match(
            success => {
                _logger.LogInformation("GraphQL Mutation: Funko id {Id} eliminado exitosamente", id);
                return success;
            },
            error => {
                _logger.LogWarning("GraphQL Mutation: Error al eliminar Funko id {Id} - {Error}", id, error.Message);
                throw new GraphQLException(ErrorBuilder.New()
                    .SetMessage(error.Message)
                    .SetCode(error.GetType().ToString())
                    .Build());
            }
        );
    }
}