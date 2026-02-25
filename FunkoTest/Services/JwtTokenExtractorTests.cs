using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using FunkoApi.Services.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace FunkoTest.Services;

[TestFixture]
public class JwtTokenExtractorTests
{
    private Mock<ILogger<JwtTokenExtractor>> _loggerMock = null!;
    private JwtTokenExtractor _extractor = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<JwtTokenExtractor>>();
        _extractor = new JwtTokenExtractor(_loggerMock.Object);
    }

    private string GenerateTestToken(List<Claim> claims)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Test]
    public void ExtractUserId_ValidToken_ReturnsUserId()
    {
        // Arrange
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "123") };
        var token = GenerateTestToken(claims);

        // Act
        var result = _extractor.ExtractUserId(token);

        // Assert
        result.Should().Be(123);
    }

    [Test]
    public void ExtractUserId_InvalidToken_ReturnsNull()
    {
        // Arrange
        var token = "invalid.token.string";

        // Act
        var result = _extractor.ExtractUserId(token);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void ExtractUserId_TokenWithoutIdClaim_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim> { new Claim(ClaimTypes.Email, "test@test.com") };
        var token = GenerateTestToken(claims);

        // Act
        var result = _extractor.ExtractUserId(token);

        // Assert
        result.Should().BeNull();
    }
    
    [Test]
    public void ExtractUserId_NonNumericId_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "abc") };
        var token = GenerateTestToken(claims);

        // Act
        var result = _extractor.ExtractUserId(token);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void ExtractRole_ValidToken_ReturnsRole()
    {
        // Arrange
        var claims = new List<Claim> { new Claim(ClaimTypes.Role, "ADMIN") };
        var token = GenerateTestToken(claims);

        // Act
        var result = _extractor.ExtractRole(token);

        // Assert
        result.Should().Be("ADMIN");
    }

    [Test]
    public void ExtractRole_InvalidToken_ReturnsNull()
    {
        // Arrange
        var token = "invalid.token";

        // Act
        var result = _extractor.ExtractRole(token);

        // Assert
        result.Should().BeNull();
    }
    
    [Test]
    public void IsAdmin_AdminRole_ReturnsTrue()
    {
        // Arrange
        var claims = new List<Claim> { new Claim(ClaimTypes.Role, "admin") };
        var token = GenerateTestToken(claims);

        // Act
        var result = _extractor.IsAdmin(token);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsAdmin_UserRole_ReturnsFalse()
    {
        // Arrange
        var claims = new List<Claim> { new Claim(ClaimTypes.Role, "user") };
        var token = GenerateTestToken(claims);

        // Act
        var result = _extractor.IsAdmin(token);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void ExtractEmail_ValidToken_ReturnsEmail()
    {
        // Arrange
        var claims = new List<Claim> { new Claim(ClaimTypes.Email, "test@example.com") };
        var token = GenerateTestToken(claims);

        // Act
        var result = _extractor.ExtractEmail(token);

        // Assert
        result.Should().Be("test@example.com");
    }

    [Test]
    public void ExtractEmail_InvalidToken_ReturnsNull()
    {
        // Arrange
        var token = "invalid";

        // Act
        var result = _extractor.ExtractEmail(token);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void ExtractUserInfo_ValidToken_ReturnsTuple()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim(ClaimTypes.Role, "admin")
        };
        var token = GenerateTestToken(claims);

        // Act
        var (userId, isAdmin, role) = _extractor.ExtractUserInfo(token);

        // Assert
        userId.Should().Be(42);
        isAdmin.Should().BeTrue();
        role.Should().Be("admin");
    }

    [Test]
    public void IsValidTokenFormat_ValidFormat_ReturnsTrue()
    {
        // Arrange
        // A minimal valid JWT structure: header.payload.signature
        var header = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9";
        var payload = "eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ";
        var signature = "SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var token = $"{header}.{payload}.{signature}";

        // Act
        var result = _extractor.IsValidTokenFormat(token);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsValidTokenFormat_InvalidFormat_ReturnsFalse()
    {
        // Arrange
        var token = "invalid.token";

        // Act
        var result = _extractor.IsValidTokenFormat(token);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void ExtractClaims_ValidToken_ReturnsClaimsPrincipal()
    {
        // Arrange
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, "testname") };
        var token = GenerateTestToken(claims);

        // Act
        var result = _extractor.ExtractClaims(token);

        // Assert
        result.Should().NotBeNull();
        result!.Identity!.IsAuthenticated.Should().BeTrue();
        result.Claims.Should().Contain(c => c.Value == "testname");
        var claim = result.Claims.First(c => c.Value == "testname");
        claim.Type.Should().BeOneOf(ClaimTypes.Name, "unique_name", "name");
    }
    
    [Test]
    public void ExtractClaims_MalformedToken_ReturnsNull()
    {
         // Arrange
         var token = "malformed.token";

         // Act
         var result = _extractor.ExtractClaims(token);

         // Assert
         result.Should().BeNull();
    }

    [Test]
    public void ExtractClaims_FallbackParsing_ReturnsPrincipal()
    {
        // Arrange
        var header = "eyJhbGciOiJub25lIn0"; // {"alg":"none"}
        var payload = "eyJuYW1lIjoidGVzdCJ9"; // {"name":"test"}
        var token = $"{header}.{payload}.";
        var badHeader = "not_base64_header";
        var payloadJson = "{\"name\":\"fallback\"}";
        var payloadEncoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payloadJson))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
        var tokenValues = $"{badHeader}.{payloadEncoded}.sig";
        
        // Act
        var result = _extractor.ExtractClaims(tokenValues);

        // Assert
        result.Should().NotBeNull();
        result!.Claims.First(c => c.Type == ClaimTypes.Name).Value.Should().Be("fallback");
    }
}
