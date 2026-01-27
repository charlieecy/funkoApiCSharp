using FunkoApi.Models;

namespace FunkoApi.Services.Auth;

public interface IJwtService
{
    string GenerateToken(User user);

    string? ValidateToken(string token);
}