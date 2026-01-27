using FunkoApi.DTO.User;
using CSharpFunctionalExtensions;
using FunkoApi.Error;

namespace FunkoApi.Services.Auth;

public interface IAuthService
{
    
    Task<Result<AuthResponseDTO, AuthError>> SignUpAsync(RegisterDTO dto);
    

  
    Task<Result<AuthResponseDTO, AuthError>> SignInAsync(LoginDto dto);
}