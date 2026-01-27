using CSharpFunctionalExtensions;
using FunkoApi.DTO.User;
using FunkoApi.Error;
using FunkoApi.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace FunkoApi.Controller;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class AuthController(
    IAuthService authService,
    ILogger<AuthController> logger
) : ControllerBase
{

    [HttpPost("signup")]
    [ProducesResponseType(typeof(AuthResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SignUp([FromBody] RegisterDTO dto)
    {
        logger.LogInformation("Signup request received for user: {Username}", dto.Username);

        var resultado = await authService.SignUpAsync(dto);

        return resultado.Match(
            response => CreatedAtAction(nameof(SignUp), response),
            error => error switch
            {
                ValidationError validationError => BadRequest(new { message = validationError.Error }),
                ConflictError conflictError => Conflict(new { message = conflictError.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }


    [HttpPost("signin")]
    [ProducesResponseType(typeof(AuthResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignIn([FromBody] LoginDto dto)
    {
        logger.LogInformation("Petición de inicio de sesión recibida para usuario: {Username}", dto.Username);

        var resultado = await authService.SignInAsync(dto);

        return resultado.Match(
            response => Ok(response),
            error => error switch
            {
                UnauthorizedError unauthorizedError => Unauthorized(new { message = unauthorizedError.Error }),
                ValidationError validationError => BadRequest(new { message = validationError.Error }),
                _ => StatusCode(500, new { message = error.Error })
            }
        );
    }
}