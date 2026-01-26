using CSharpFunctionalExtensions;
using FunkoApi.DTO;
using FunkoApi.Error;
using FunkoApi.GraphQL.Inputs;
using FunkoApi.Services;
using HotChocolate;

namespace FunkoApi.GraphQL.Mutations;

public class FunkoMutations
{
    
    private readonly IFunkoService _funkoService;

    public FunkoMutations(IFunkoService funkoService)
    {
        _funkoService = funkoService;
    }
    
    public async Task<Result<FunkoResponseDTO, FunkoError>> CreateFunkoAsync(
        PostPutFunkoInput input,
        [Service] IFunkoService funkoService
    )
    {
        //Transformamos el input a DTO para poder pasárselo al servicio
        var dto = new FunkoPostPutRequestDTO()
        {
            Nombre =  input.Nombre,
            Categoria =  input.Categoria,
            Precio =  input.Precio,
            Imagen = input.Imagen,
        };
        //Lo creamos llamando al servicio
        return await funkoService.CreateAsync(dto);
    }
    
    public async Task<Result<FunkoResponseDTO, FunkoError>> UpdateFunkoAsync(
        long id,
        PostPutFunkoInput input,
        [Service] IFunkoService funkoService
    )
    {
        //Transformamos el input a DTO para poder pasárselo al servicio
        var dto = new FunkoPostPutRequestDTO()
        {
            Nombre =  input.Nombre,
            Categoria =  input.Categoria,
            Precio =  input.Precio,
            Imagen = input.Imagen,
        };
        //Lo actualizamos llamando al servicio
        return await funkoService.UpdateAsync(id, dto);
    }

    public async Task<Result<FunkoResponseDTO, FunkoError>> PatchFunkoAsync(
        long id,
        PatchFunkoInput input,
        [Service] IFunkoService funkoService
    )
    {
        //Transformamos el input a DTO para poder pasárselo al servicio
        var dto = new FunkoPatchRequestDTO()
        {
            Nombre =  input.Nombre,
            Categoria =  input.Categoria,
            Precio =  input.Precio,
            Imagen = input.Imagen,
        };
        
        return await funkoService.PatchAsync(id, dto);
    }

    public async Task<Result<FunkoResponseDTO, FunkoError>> DeleteFunkoAsync(
        long id,
        [Service] IFunkoService funkoService
    )
    {
        return await  funkoService.DeleteAsync(id);
    }
    
}