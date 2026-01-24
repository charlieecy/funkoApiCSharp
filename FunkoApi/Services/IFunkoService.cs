using CSharpFunctionalExtensions;
using FunkoApi.DTO;
using FunkoApi.Error;

namespace FunkoApi.Services;

public interface IFunkoService
{
    Task<Result<FunkoResponseDTO, FunkoError>>  GetByIdAsync(long id);
    Task<List<FunkoResponseDTO>> GetAllAsync();
    Task<Result<FunkoResponseDTO, FunkoError>> CreateAsync(FunkoPostPutRequestDTO dto);
    Task<Result<FunkoResponseDTO, FunkoError>> UpdateAsync(long id, FunkoPostPutRequestDTO dto);
    Task<Result<FunkoResponseDTO, FunkoError>> PatchAsync(long id, FunkoPatchRequestDTO dto);

    Task<Result<FunkoResponseDTO, FunkoError>>DeleteAsync(long id);
}