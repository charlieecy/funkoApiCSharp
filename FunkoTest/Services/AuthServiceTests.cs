using CSharpFunctionalExtensions;
using FluentAssertions;
using FunkoApi.DTO.User;
using FunkoApi.Error;
using FunkoApi.Models;
using FunkoApi.Repository.Users;
using FunkoApi.Services.Auth;
using Microsoft.Extensions.Logging;
using Moq;

namespace FunkoTest.Services;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IUserRepository> _userRepositoryMock = null!;
    private Mock<IJwtService> _jwtServiceMock = null!;
    private Mock<ILogger<AuthService>> _loggerMock = null!;
    private AuthService _authService = null!;

    [SetUp]
    public void Setup()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _jwtServiceMock = new Mock<IJwtService>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _authService = new AuthService(_userRepositoryMock.Object, _jwtServiceMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task SignUpAsync_ValidUser_ReturnsSuccess()
    {
        // Arrange
        var registerDto = new RegisterDTO { Username = "newUser", Password = "pass123", Email = "new@test.com" };
        var savedUser = new User
        {
            Id = 1, Username = "newUser", Email = "new@test.com", Role = User.UserRoles.USER, CreatedAt = DateTime.UtcNow
        };

        _userRepositoryMock.Setup(r => r.FindByUsernameAsync(registerDto.Username!)).ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(r => r.FindByEmailAsync(registerDto.Email!)).ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(r => r.SaveAsync(It.IsAny<User>())).ReturnsAsync(savedUser);
        _jwtServiceMock.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("valid_token");

        // Act
        var result = await _authService.SignUpAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("valid_token");
        result.Value.User.Username.Should().Be("newUser");
        _userRepositoryMock.Verify(r => r.SaveAsync(It.IsAny<User>()), Times.Once);
    }

    [Test]
    public async Task SignUpAsync_DuplicateUsername_ReturnsFailure()
    {
        // Arrange
        var registerDto = new RegisterDTO { Username = "existingUser", Password = "pass123", Email = "email@test.com" };
        var existingUser = new User { Username = "existingUser" };

        _userRepositoryMock.Setup(r => r.FindByUsernameAsync("existingUser")).ReturnsAsync(existingUser);
        _userRepositoryMock.Setup(r => r.FindByEmailAsync("email@test.com")).ReturnsAsync((User?)null);

        // Act
        var result = await _authService.SignUpAsync(registerDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
        result.Error.Error.Should().Contain("username ya en uso");
        _userRepositoryMock.Verify(r => r.SaveAsync(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public async Task SignUpAsync_DuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var registerDto = new RegisterDTO { Username = "newUser", Password = "pass123", Email = "existing@test.com" };
        var existingUser = new User { Email = "existing@test.com" };

        _userRepositoryMock.Setup(r => r.FindByUsernameAsync("newUser")).ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(r => r.FindByEmailAsync("existing@test.com")).ReturnsAsync(existingUser);

        // Act
        var result = await _authService.SignUpAsync(registerDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
        result.Error.Error.Should().Contain("email ya en uso");
        _userRepositoryMock.Verify(r => r.SaveAsync(It.IsAny<User>()), Times.Never);
    }
    
    [Test]
    public async Task SignInAsync_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var loginDto = new LoginDto { Username = "validUser", Password = "pass123" };
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("pass123");
        var user = new User { Id = 1, Username = "validUser", PasswordHash = passwordHash, Role = User.UserRoles.USER };

        _userRepositoryMock.Setup(r => r.FindByUsernameAsync("validUser")).ReturnsAsync(user);
        _jwtServiceMock.Setup(j => j.GenerateToken(user)).Returns("valid_token");

        // Act
        var result = await _authService.SignInAsync(loginDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("valid_token");
        result.Value.User.Username.Should().Be("validUser");
    }

    [Test]
    public async Task SignInAsync_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var loginDto = new LoginDto { Username = "unknownUser", Password = "pass123" };
        _userRepositoryMock.Setup(r => r.FindByUsernameAsync("unknownUser")).ReturnsAsync((User?)null);

        // Act
        var result = await _authService.SignInAsync(loginDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UnauthorizedError>();
        result.Error.Error.Should().Be("Credenciales inválidas");
    }

    [Test]
    public async Task SignInAsync_InvalidPassword_ReturnsFailure()
    {
        // Arrange
        var loginDto = new LoginDto { Username = "validUser", Password = "wrongPass" };
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("correctPass");
        var user = new User { Username = "validUser", PasswordHash = passwordHash };

        _userRepositoryMock.Setup(r => r.FindByUsernameAsync("validUser")).ReturnsAsync(user);

        // Act
        var result = await _authService.SignInAsync(loginDto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<UnauthorizedError>();
    }
}
