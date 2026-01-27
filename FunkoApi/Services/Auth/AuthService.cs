using CSharpFunctionalExtensions;
using FunkoApi.DTO.User;
using FunkoApi.Error;
using FunkoApi.Models;
using FunkoApi.Repository.Users;

namespace FunkoApi.Services.Auth;

public class AuthService(
    IUserRepository userRepository,
    IJwtService jwtService,
    ILogger<AuthService> logger
) : IAuthService
{

    
    public async Task<Result<AuthResponseDTO, AuthError>> SignUpAsync(RegisterDTO dto)
    {
        var sanitizedUsername = dto.Username?.Replace("\n", "").Replace("\r", "");
        logger.LogInformation("SignUp request for username: {Username}", sanitizedUsername);
        

        var duplicateCheck = await CheckDuplicatesAsync(dto);
        if (duplicateCheck.IsFailure)
        {
            return Result.Failure<AuthResponseDTO, AuthError>(duplicateCheck.Error);
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 11);

        var user = new User
        {
            Username = dto.Username!,
            Email = dto.Email!,
            PasswordHash = passwordHash,
            Role = User.UserRoles.USER,
            IsDeleted = false
        };

        var savedUser = await userRepository.SaveAsync(user);
        var authResponse = GenerateAuthResponse(savedUser);

        logger.LogInformation("User registered successfully: {Username}", sanitizedUsername);

        return Result.Success<AuthResponseDTO, AuthError>(authResponse);
    }

    /// <summary>
    /// Autentica un usuario existente.
    /// Devuelve: Result.Success(AuthResponseDto) | Result.Failure(Validation/Unauthorized/NotFound)
    /// </summary>
    public async Task<Result<AuthResponseDTO, AuthError>> SignInAsync(LoginDto dto)
    {
        var sanitizedUsername = dto.Username?.Replace("\n", "").Replace("\r", "");
        logger.LogInformation("SignIn request for username: {Username}", sanitizedUsername);
        

        var user = await userRepository.FindByUsernameAsync(dto.Username!);
        if (user is null)
        {
            logger.LogWarning("SignIn fallido: Usuario no encontrado - {Username}", sanitizedUsername);
            return Result.Failure<AuthResponseDTO, AuthError>(
                new UnauthorizedError("Credenciales inválidas")
            );
        }

        var passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!passwordValid)
        {
            logger.LogWarning("SignIn fallido: Password inválido - {Username}", sanitizedUsername);
            return Result.Failure<AuthResponseDTO, AuthError>(
                new UnauthorizedError("Credenciales inválidas")
            );
        }

        var authResponse = GenerateAuthResponse(user);
        logger.LogInformation("Usuario inició sesión correctamente: {Username}", sanitizedUsername);

        return Result.Success<AuthResponseDTO, AuthError>(authResponse);
    }
    
    private async Task<UnitResult<AuthError>> CheckDuplicatesAsync(RegisterDTO dto)
    {
        var usernameCheckTask = userRepository.FindByUsernameAsync(dto.Username!);
        var emailCheckTask = userRepository.FindByEmailAsync(dto.Email!);

        await Task.WhenAll(usernameCheckTask, emailCheckTask);

        var existingUser = await usernameCheckTask;
        if (existingUser is not null)
        {
            return UnitResult.Failure<AuthError>(new ConflictError("username ya en uso:"+existingUser.Username));
        }

        var existingEmail = await emailCheckTask;
        if (existingEmail is not null)
        {
            return UnitResult.Failure<AuthError>(new ConflictError("email ya en uso"+existingEmail.Email));
        }

        return UnitResult.Success<AuthError>();
    }

    
    private AuthResponseDTO GenerateAuthResponse(User user)
    {
        var token = jwtService.GenerateToken(user);

        var userDto = new UserDTO(
            user.Id,
            user.Username,
            user.Email,
            user.Role,
            user.CreatedAt
        );

        return new AuthResponseDTO(token, userDto);
    }
}