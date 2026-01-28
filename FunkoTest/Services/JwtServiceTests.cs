using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using FunkoApi.Models;
using FunkoApi.Services.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace FunkoTest.Services;

[TestFixture]
public class JwtServiceTests
{
    private Mock<IConfiguration> _configurationMock = null!;
    private Mock<ILogger<JwtService>> _loggerMock = null!;
    private JwtService _jwtService = null!;
    private const string SecretKey = "super_secret_key_for_testing_purposes_must_be_long_enough";

    [SetUp]
    public void Setup()
    {
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<JwtService>>();

        _configurationMock.Setup(c => c["Jwt:Key"]).Returns(SecretKey);
        _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
        _configurationMock.Setup(c => c["Jwt:ExpireMinutes"]).Returns("60");

        _jwtService = new JwtService(_configurationMock.Object, _loggerMock.Object);
    }

    [Test]
    public void GenerateToken_ValidUser_ReturnsTokenString()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Role = User.UserRoles.USER
        };

        // Act
        var token = _jwtService.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be(user.Username);
        jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value.Should().Be(user.Email);
        jwtToken.Claims.First(c => c.Type == ClaimTypes.Role).Value.Should().Be(user.Role);
        jwtToken.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value.Should().Be(user.Id.ToString());
        jwtToken.Issuer.Should().Be("TestIssuer");
        jwtToken.Audiences.Should().Contain("TestAudience");
    }

    [Test]
    public void GenerateToken_MissingKey_ThrowsInvalidOperationException()
    {
        // Arrange
        _configurationMock.Setup(c => c["Jwt:Key"]).Returns((string?)null);
        var user = new User { Username = "testuser", Email = "test@example.com" };

        // Act
        Action act = () => _jwtService.GenerateToken(user);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("JWT Key no configurada");
    }

    [Test]
    public void ValidateToken_ValidToken_ReturnsUsername()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Role = User.UserRoles.USER
        };
        var token = _jwtService.GenerateToken(user);

        // Act
        var result = _jwtService.ValidateToken(token);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("testuser");
    }

    [Test]
    public void ValidateToken_InvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid.token.string";

        // Act
        var result = _jwtService.ValidateToken(invalidToken);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void ValidateToken_ExpiredToken_ReturnsNull()
    {
        // Arrange
        _configurationMock.Setup(c => c["Jwt:ExpireMinutes"]).Returns("-10"); 
        var expiredService = new JwtService(_configurationMock.Object, _loggerMock.Object);
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Role = User.UserRoles.USER
        };
        var expiredToken = expiredService.GenerateToken(user);

        // Act
        var result = _jwtService.ValidateToken(expiredToken);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void ValidateToken_MissingKey_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Role = User.UserRoles.USER
        };
        var token = _jwtService.GenerateToken(user);
        
        _configurationMock.Setup(c => c["Jwt:Key"]).Returns((string?)null);

        // Act
        var result = _jwtService.ValidateToken(token);

        // Assert
        result.Should().BeNull();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validación de token JWT fallida")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
