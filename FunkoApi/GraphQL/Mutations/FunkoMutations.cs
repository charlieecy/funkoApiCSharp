using CSharpFunctionalExtensions;
using FunkoApi.DTO;
using FunkoApi.Error;
using FunkoApi.GraphQL.Inputs;
using FunkoApi.Services;
using HotChocolate;

namespace FunkoApi.GraphQL.Mutations;

public class FunkoMutations
{
    
    public async Task<FunkoResponseDTO> CreateFunkoAsync(
        PostPutFunkoInput input,
        [Service] IFunkoService funkoService
    )
    {
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
            success => success,
            error => throw new GraphQLException(ErrorBuilder.New()
                .SetMessage(error.Message)
                .SetCode(error.GetType().ToString())
                .Build())
        );
    }

    public async Task<FunkoResponseDTO> UpdateFunkoAsync(
        long id,
        PostPutFunkoInput input,
        [Service] IFunkoService funkoService
    )
    {
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
            success => success,
            error => throw new GraphQLException(ErrorBuilder.New()
                .SetMessage(error.Message)
                .SetCode(error.GetType().ToString())
                .Build())
        );
    }

    public async Task<FunkoResponseDTO> PatchFunkoAsync(
        long id,
        PatchFunkoInput input,
        [Service] IFunkoService funkoService
    )
    {
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
            success => success,
            error => throw new GraphQLException(ErrorBuilder.New()
                .SetMessage(error.Message)
                .SetCode(error.GetType().ToString())
                .Build())
        );
    }

    public async Task<FunkoResponseDTO> DeleteFunkoAsync(
        long id,
        [Service] IFunkoService funkoService
    )
    {
        var result = await funkoService.DeleteAsync(id);

        // Usamos Match para desenvolver el Result: 
        // Si es Success, devuelve el DTO. Si es Failure, lanza una excepción de GraphQL.
        return result.Match(
            success => success,
            error => throw new GraphQLException(ErrorBuilder.New()
                .SetMessage(error.Message)
                .SetCode(error.GetType().ToString())
                .Build())
        );
    }
}