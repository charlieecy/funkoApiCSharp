using CSharpFunctionalExtensions;
using FunkoApi.DTO;
using FunkoApi.Error;

namespace FunkoApi.Services;

public interface ICategoryService
{
    Task<Result<CategoryResponseDTO, FunkoError>> GetByIdAsync(Guid id);
    Task<List<CategoryResponseDTO>> GetAllAsync();
    Task<Result<CategoryResponseDTO, FunkoError>> CreateAsync(CategoryPostPutRequestDTO dto);
    Task<Result<CategoryResponseDTO, FunkoError>> UpdateAsync(Guid id, CategoryPostPutRequestDTO dto);
    Task<Result<CategoryResponseDTO, FunkoError>> DeleteAsync(Guid id);
}