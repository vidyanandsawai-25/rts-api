using Microsoft.Extensions.Configuration;
using Moq;
using NtisPlatform.Infrastructure.Services.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;
    private const string TestJwtKey = "ThisIsAVerySecureSecretKeyForJWTTokenGenerationAndValidation123456";
    private const string TestIssuer = "NtisPlatform";
    private const string TestAudience = "NtisPlatformUsers";

    public JwtTokenServiceTests()
    {
        var configurationData = new Dictionary<string, string>
        {
            { "Jwt:Key", TestJwtKey },
            { "Jwt:ExpiresInMinutes", "15" },
            { "Jwt:Issuer", TestIssuer },
            { "Jwt:Audience", TestAudience }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData!)
            .Build();

        _jwtTokenService = new JwtTokenService(_configuration);
    }

    [Fact]
    public void GenerateAccessToken_WithValidParameters_ShouldReturnToken()
    {
        // Arrange
        var userId = 1;
        var organizationId = "org123";
        var roles = new List<string> { "Admin", "User" };

        // Act
        var token = _jwtTokenService.GenerateAccessToken(userId, organizationId, roles);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Contains(".", token); // JWT format has dots
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeUserIdClaim()
    {
        // Arrange
        var userId = 42;
        var organizationId = "org123";
        var roles = new List<string> { "User" };

        // Act
        var token = _jwtTokenService.GenerateAccessToken(userId, organizationId, roles);
        var principal = _jwtTokenService.ValidateToken(token);

        // Assert
        Assert.NotNull(principal);
        var userIdClaim = principal.FindFirst("user_id");
        Assert.NotNull(userIdClaim);
        Assert.Equal(userId.ToString(), userIdClaim.Value);
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeOrganizationIdClaim()
    {
        // Arrange
        var userId = 1;
        var organizationId = "testOrg456";
        var roles = new List<string> { "User" };

        // Act
        var token = _jwtTokenService.GenerateAccessToken(userId, organizationId, roles);
        var principal = _jwtTokenService.ValidateToken(token);

        // Assert
        Assert.NotNull(principal);
        var orgClaim = principal.FindFirst("organization_id");
        Assert.NotNull(orgClaim);
        Assert.Equal(organizationId, orgClaim.Value);
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeRoleClaims()
    {
        // Arrange
        var userId = 1;
        var organizationId = "org123";
        var roles = new List<string> { "Admin", "Manager", "User" };

        // Act
        var token = _jwtTokenService.GenerateAccessToken(userId, organizationId, roles);
        var principal = _jwtTokenService.ValidateToken(token);

        // Assert
        Assert.NotNull(principal);
        var roleClaims = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Equal(3, roleClaims.Count);
        Assert.Contains("Admin", roleClaims);
        Assert.Contains("Manager", roleClaims);
        Assert.Contains("User", roleClaims);
    }

    [Fact]
    public void GenerateAccessToken_WithAdditionalClaims_ShouldIncludeThemInToken()
    {
        // Arrange
        var userId = 1;
        var organizationId = "org123";
        var roles = new List<string> { "User" };
        var additionalClaims = new Dictionary<string, string>
        {
            { "email", "test@example.com" },
            { "department", "IT" },
            { "custom_claim", "custom_value" }
        };

        // Act
        var token = _jwtTokenService.GenerateAccessToken(userId, organizationId, roles, additionalClaims);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.NotNull(jwtToken);
        Assert.Contains(jwtToken.Claims, c => c.Type == "email" && c.Value == "test@example.com");
        Assert.Contains(jwtToken.Claims, c => c.Type == "department" && c.Value == "IT");
        Assert.Contains(jwtToken.Claims, c => c.Type == "custom_claim" && c.Value == "custom_value");
    }

    [Fact]
    public void GenerateAccessToken_WithNullAdditionalClaims_ShouldNotThrowException()
    {
        // Arrange
        var userId = 1;
        var organizationId = "org123";
        var roles = new List<string> { "User" };

        // Act
        var token = _jwtTokenService.GenerateAccessToken(userId, organizationId, roles, null);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateAccessToken_WithEmptyRoles_ShouldGenerateTokenWithoutRoleClaims()
    {
        // Arrange
        var userId = 1;
        var organizationId = "org123";
        var roles = new List<string>();

        // Act
        var token = _jwtTokenService.GenerateAccessToken(userId, organizationId, roles);
        var principal = _jwtTokenService.ValidateToken(token);

        // Assert
        Assert.NotNull(principal);
        var roleClaims = principal.FindAll(ClaimTypes.Role).ToList();
        Assert.Empty(roleClaims);
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeStandardJwtClaims()
    {
        // Arrange
        var userId = 1;
        var organizationId = "org123";
        var roles = new List<string> { "User" };

        // Act
        var token = _jwtTokenService.GenerateAccessToken(userId, organizationId, roles);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.NotNull(jwtToken);
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Sub);
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Jti);
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Iat);
    }

    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnClaimsPrincipal()
    {
        // Arrange
        var userId = 1;
        var organizationId = "org123";
        var roles = new List<string> { "User" };
        var token = _jwtTokenService.GenerateAccessToken(userId, organizationId, roles);

        // Act
        var principal = _jwtTokenService.ValidateToken(token);

        // Assert
        Assert.NotNull(principal);
        Assert.NotNull(principal.Identity);
        Assert.True(principal.Identity.IsAuthenticated);
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var invalidToken = "invalid.token.string";

        // Act
        var principal = _jwtTokenService.ValidateToken(invalidToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_WithExpiredToken_ShouldReturnNull()
    {
        // Arrange - Create a token with very short expiration
        var configurationData = new Dictionary<string, string>
        {
            { "Jwt:Key", TestJwtKey },
            { "Jwt:ExpiresInMinutes", "0" }, // Expires immediately
            { "Jwt:Issuer", TestIssuer },
            { "Jwt:Audience", TestAudience }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData!)
            .Build();

        var service = new JwtTokenService(config);
        var token = service.GenerateAccessToken(1, "org123", new List<string> { "User" });

        // Wait a moment to ensure token expires
        Thread.Sleep(1000);

        // Act
        var principal = _jwtTokenService.ValidateToken(token);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_WithTamperedToken_ShouldReturnNull()
    {
        // Arrange
        var token = _jwtTokenService.GenerateAccessToken(1, "org123", new List<string> { "User" });
        var tamperedToken = token[..^5] + "XXXXX"; // Modify the signature

        // Act
        var principal = _jwtTokenService.ValidateToken(tamperedToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_WithEmptyToken_ShouldReturnNull()
    {
        // Act
        var principal = _jwtTokenService.ValidateToken(string.Empty);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_WithWrongIssuer_ShouldReturnNull()
    {
        // Arrange - Create token with different issuer
        var configurationData = new Dictionary<string, string>
        {
            { "Jwt:Key", TestJwtKey },
            { "Jwt:ExpiresInMinutes", "15" },
            { "Jwt:Issuer", "WrongIssuer" },
            { "Jwt:Audience", TestAudience }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData!)
            .Build();

        var differentService = new JwtTokenService(config);
        var token = differentService.GenerateAccessToken(1, "org123", new List<string> { "User" });

        // Act
        var principal = _jwtTokenService.ValidateToken(token);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_WithWrongAudience_ShouldReturnNull()
    {
        // Arrange - Create token with different audience
        var configurationData = new Dictionary<string, string>
        {
            { "Jwt:Key", TestJwtKey },
            { "Jwt:ExpiresInMinutes", "15" },
            { "Jwt:Issuer", TestIssuer },
            { "Jwt:Audience", "WrongAudience" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData!)
            .Build();

        var differentService = new JwtTokenService(config);
        var token = differentService.GenerateAccessToken(1, "org123", new List<string> { "User" });

        // Act
        var principal = _jwtTokenService.ValidateToken(token);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnNonEmptyString()
    {
        // Act
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // Assert
        Assert.NotNull(refreshToken);
        Assert.NotEmpty(refreshToken);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnBase64String()
    {
        // Act
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // Assert
        Assert.NotNull(refreshToken);
        var isBase64 = IsBase64String(refreshToken);
        Assert.True(isBase64);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnDifferentTokensOnMultipleCalls()
    {
        // Act
        var token1 = _jwtTokenService.GenerateRefreshToken();
        var token2 = _jwtTokenService.GenerateRefreshToken();
        var token3 = _jwtTokenService.GenerateRefreshToken();

        // Assert
        Assert.NotEqual(token1, token2);
        Assert.NotEqual(token2, token3);
        Assert.NotEqual(token1, token3);
    }

    [Fact]
    public void GetAccessTokenExpirationSeconds_ShouldReturnCorrectValue()
    {
        // Act
        var expirationSeconds = _jwtTokenService.GetAccessTokenExpirationSeconds();

        // Assert
        Assert.Equal(900, expirationSeconds); // 15 minutes * 60 seconds
    }

    [Fact]
    public void GetAccessTokenExpirationSeconds_WithCustomConfiguration_ShouldReturnConfiguredValue()
    {
        // Arrange
        var configurationData = new Dictionary<string, string>
        {
            { "Jwt:Key", TestJwtKey },
            { "Jwt:ExpiresInMinutes", "30" },
            { "Jwt:Issuer", TestIssuer },
            { "Jwt:Audience", TestAudience }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData!)
            .Build();

        var service = new JwtTokenService(config);

        // Act
        var expirationSeconds = service.GetAccessTokenExpirationSeconds();

        // Assert
        Assert.Equal(1800, expirationSeconds); // 30 minutes * 60 seconds
    }

    [Fact]
    public void GenerateCsrfToken_ShouldReturnNonEmptyString()
    {
        // Act
        var csrfToken = _jwtTokenService.GenerateCsrfToken();

        // Assert
        Assert.NotNull(csrfToken);
        Assert.NotEmpty(csrfToken);
    }

    [Fact]
    public void GenerateCsrfToken_ShouldReturnBase64String()
    {
        // Act
        var csrfToken = _jwtTokenService.GenerateCsrfToken();

        // Assert
        Assert.NotNull(csrfToken);
        var isBase64 = IsBase64String(csrfToken);
        Assert.True(isBase64);
    }

    [Fact]
    public void GenerateCsrfToken_ShouldReturnDifferentTokensOnMultipleCalls()
    {
        // Act
        var token1 = _jwtTokenService.GenerateCsrfToken();
        var token2 = _jwtTokenService.GenerateCsrfToken();
        var token3 = _jwtTokenService.GenerateCsrfToken();

        // Assert
        Assert.NotEqual(token1, token2);
        Assert.NotEqual(token2, token3);
        Assert.NotEqual(token1, token3);
    }

    [Fact]
    public void ValidateCsrfToken_WithMatchingTokens_ShouldReturnTrue()
    {
        // Arrange
        var originalToken = _jwtTokenService.GenerateCsrfToken();

        // Act
        var isValid = _jwtTokenService.ValidateCsrfToken(originalToken, originalToken);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateCsrfToken_WithDifferentTokens_ShouldReturnFalse()
    {
        // Arrange
        var token1 = _jwtTokenService.GenerateCsrfToken();
        var token2 = _jwtTokenService.GenerateCsrfToken();

        // Act
        var isValid = _jwtTokenService.ValidateCsrfToken(token1, token2);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateCsrfToken_WithEmptyToken_ShouldReturnFalse()
    {
        // Arrange
        var storedToken = _jwtTokenService.GenerateCsrfToken();

        // Act
        var isValid = _jwtTokenService.ValidateCsrfToken(string.Empty, storedToken);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateCsrfToken_WithEmptyStoredToken_ShouldReturnFalse()
    {
        // Arrange
        var token = _jwtTokenService.GenerateCsrfToken();

        // Act
        var isValid = _jwtTokenService.ValidateCsrfToken(token, string.Empty);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateCsrfToken_WithNullToken_ShouldReturnFalse()
    {
        // Arrange
        var storedToken = _jwtTokenService.GenerateCsrfToken();

        // Act
        var isValid = _jwtTokenService.ValidateCsrfToken(null!, storedToken);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateCsrfToken_WithNullStoredToken_ShouldReturnFalse()
    {
        // Arrange
        var token = _jwtTokenService.GenerateCsrfToken();

        // Act
        var isValid = _jwtTokenService.ValidateCsrfToken(token, null!);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateCsrfToken_WithInvalidBase64Token_ShouldReturnFalse()
    {
        // Arrange
        var storedToken = _jwtTokenService.GenerateCsrfToken();
        var invalidToken = "not-a-valid-base64-string!!!";

        // Act
        var isValid = _jwtTokenService.ValidateCsrfToken(invalidToken, storedToken);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateCsrfToken_WithInvalidBase64StoredToken_ShouldReturnFalse()
    {
        // Arrange
        var token = _jwtTokenService.GenerateCsrfToken();
        var invalidStoredToken = "not-a-valid-base64-string!!!";

        // Act
        var isValid = _jwtTokenService.ValidateCsrfToken(token, invalidStoredToken);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void Constructor_WithMissingJwtKey_ShouldThrowException()
    {
        // Arrange
        var configurationData = new Dictionary<string, string>
        {
            { "Jwt:ExpiresInMinutes", "15" },
            { "Jwt:Issuer", TestIssuer },
            { "Jwt:Audience", TestAudience }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData!)
            .Build();

        var service = new JwtTokenService(config);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            service.GenerateAccessToken(1, "org123", new List<string> { "User" }));
    }

    [Fact]
    public void Constructor_WithMissingOptionalConfiguration_ShouldUseDefaults()
    {
        // Arrange
        var configurationData = new Dictionary<string, string>
        {
            { "Jwt:Key", TestJwtKey }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData!)
            .Build();

        // Act
        var service = new JwtTokenService(config);
        var token = service.GenerateAccessToken(1, "org123", new List<string> { "User" });

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Equal(900, service.GetAccessTokenExpirationSeconds()); // Default 15 minutes
    }

    [Fact]
    public void GenerateAccessToken_TokenStructure_ShouldBeValidJwt()
    {
        // Arrange
        var userId = 1;
        var organizationId = "org123";
        var roles = new List<string> { "Admin" };

        // Act
        var token = _jwtTokenService.GenerateAccessToken(userId, organizationId, roles);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.NotNull(jwtToken);
        Assert.Equal(TestIssuer, jwtToken.Issuer);
        Assert.Contains(TestAudience, jwtToken.Audiences);
        Assert.True(jwtToken.ValidTo > DateTime.UtcNow);
    }

    [Fact]
    public void GenerateAccessToken_MultipleRoles_ShouldAllBeAccessibleInToken()
    {
        // Arrange
        var userId = 1;
        var organizationId = "org123";
        var roles = new List<string> { "Admin", "SuperUser", "Moderator", "User" };

        // Act
        var token = _jwtTokenService.GenerateAccessToken(userId, organizationId, roles);
        var principal = _jwtTokenService.ValidateToken(token);

        // Assert
        Assert.NotNull(principal);
        var extractedRoles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Equal(4, extractedRoles.Count);
        foreach (var role in roles)
        {
            Assert.Contains(role, extractedRoles);
        }
    }

    [Fact]
    public void GenerateAccessToken_WithSpecialCharactersInClaims_ShouldHandleCorrectly()
    {
        // Arrange
        var userId = 1;
        var organizationId = "org-123-test";
        var roles = new List<string> { "User" };
        var additionalClaims = new Dictionary<string, string>
        {
            { "email", "test.user+tag@example.com" },
            { "name", "John O'Brien" }
        };

        // Act
        var token = _jwtTokenService.GenerateAccessToken(userId, organizationId, roles, additionalClaims);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.NotNull(jwtToken);
        Assert.Contains(jwtToken.Claims, c => c.Type == "email" && c.Value == "test.user+tag@example.com");
        Assert.Contains(jwtToken.Claims, c => c.Type == "name" && c.Value == "John O'Brien");
    }

    private static bool IsBase64String(string str)
    {
        try
        {
            Convert.FromBase64String(str);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
