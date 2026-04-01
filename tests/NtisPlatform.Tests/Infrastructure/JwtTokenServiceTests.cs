using Microsoft.Extensions.Configuration;
using Moq;
using NtisPlatform.Infrastructure.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NtisPlatform.Tests.Infrastructure;

/// <summary>
/// Unit tests for JwtTokenService
/// Tests JWT token generation with various configurations
/// </summary>
public class JwtTokenServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly JwtTokenService _tokenService;

    public JwtTokenServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();

        // Setup default valid configuration
        _configurationMock.Setup(c => c["Jwt:Key"]).Returns("ThisIsASecretKeyForJwtTokenGeneration32BytesLong!");
        _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("NtisPlatform");
        _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("NtisPlatformUsers");
        _configurationMock.Setup(c => c["Jwt:ExpiresInMinutes"]).Returns("60");

        _tokenService = new JwtTokenService(_configurationMock.Object);
    }

    [Fact]
    public void GenerateToken_WithValidParameters_ReturnsJwtToken()
    {
        // Arrange
        int userId = 1;
        string username = "testuser";
        int? userRoleId = 5;

        // Act
        var token = _tokenService.GenerateToken(userId, username, userRoleId);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        // Verify it's a valid JWT token
        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(token));

        var jwtToken = handler.ReadJwtToken(token);
        Assert.NotNull(jwtToken);
    }

    [Fact]
    public void GenerateToken_TokenContainsCorrectClaims()
    {
        // Arrange
        int userId = 123;
        string username = "adminuser";
        int? userRoleId = 10;

        // Act
        var token = _tokenService.GenerateToken(userId, username, userRoleId);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Verify claims
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "123");
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Name && c.Value == "adminuser");
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "123");
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "10");
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateToken_WithNullRole_DoesNotIncludeRoleClaim()
    {
        // Arrange
        int userId = 1;
        string username = "testuser";
        int? userRoleId = null;

        // Act
        var token = _tokenService.GenerateToken(userId, username, userRoleId);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Verify role claim is not present
        Assert.DoesNotContain(jwtToken.Claims, c => c.Type == ClaimTypes.Role);
    }

    [Fact]
    public void GenerateToken_HasCorrectIssuerAndAudience()
    {
        // Arrange
        int userId = 1;
        string username = "testuser";

        // Act
        var token = _tokenService.GenerateToken(userId, username, null);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal("NtisPlatform", jwtToken.Issuer);
        Assert.Contains("NtisPlatformUsers", jwtToken.Audiences);
    }

    [Fact]
    public void GenerateToken_HasCorrectExpiration()
    {
        // Arrange
        int userId = 1;
        string username = "testuser";
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _tokenService.GenerateToken(userId, username, null);

        var afterGeneration = DateTime.UtcNow;

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var expectedExpiration = beforeGeneration.AddMinutes(60);
        
        // Allow 1 second tolerance for test execution time
        Assert.True(jwtToken.ValidTo >= expectedExpiration.AddSeconds(-1));
        Assert.True(jwtToken.ValidTo <= afterGeneration.AddMinutes(60).AddSeconds(1));
    }

    [Fact]
    public void GenerateToken_WithCustomExpiration_UsesConfiguredValue()
    {
        // Arrange
        _configurationMock.Setup(c => c["Jwt:ExpiresInMinutes"]).Returns("120");
        var customTokenService = new JwtTokenService(_configurationMock.Object);
        
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = customTokenService.GenerateToken(1, "testuser", null);

        var afterGeneration = DateTime.UtcNow;

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var expectedExpiration = beforeGeneration.AddMinutes(120);
        
        Assert.True(jwtToken.ValidTo >= expectedExpiration.AddSeconds(-1));
        Assert.True(jwtToken.ValidTo <= afterGeneration.AddMinutes(120).AddSeconds(1));
    }

    [Fact]
    public void GenerateToken_WithInvalidExpiration_UsesDefaultValue()
    {
        // Arrange
        _configurationMock.Setup(c => c["Jwt:ExpiresInMinutes"]).Returns("invalid");
        var customTokenService = new JwtTokenService(_configurationMock.Object);
        
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = customTokenService.GenerateToken(1, "testuser", null);

        var afterGeneration = DateTime.UtcNow;

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Should default to 60 minutes
        var expectedExpiration = beforeGeneration.AddMinutes(60);
        
        Assert.True(jwtToken.ValidTo >= expectedExpiration.AddSeconds(-1));
        Assert.True(jwtToken.ValidTo <= afterGeneration.AddMinutes(60).AddSeconds(1));
    }

    [Fact]
    public void GenerateToken_WithNullKey_ThrowsException()
    {
        // Arrange
        _configurationMock.Setup(c => c["Jwt:Key"]).Returns((string?)null);
        var invalidTokenService = new JwtTokenService(_configurationMock.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            invalidTokenService.GenerateToken(1, "testuser", null));

        Assert.Equal("JWT Key is not configured", exception.Message);
    }

    [Fact]
    public void GenerateToken_WithEmptyKey_ThrowsException()
    {
        // Arrange
        _configurationMock.Setup(c => c["Jwt:Key"]).Returns("");
        var invalidTokenService = new JwtTokenService(_configurationMock.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            invalidTokenService.GenerateToken(1, "testuser", null));

        Assert.Equal("JWT Key is not configured", exception.Message);
    }

    [Fact]
    public void GenerateToken_GeneratesUniqueJtiForEachToken()
    {
        // Act
        var token1 = _tokenService.GenerateToken(1, "testuser", null);
        var token2 = _tokenService.GenerateToken(1, "testuser", null);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken1 = handler.ReadJwtToken(token1);
        var jwtToken2 = handler.ReadJwtToken(token2);

        var jti1 = jwtToken1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = jwtToken2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        Assert.NotEqual(jti1, jti2);
    }
}
