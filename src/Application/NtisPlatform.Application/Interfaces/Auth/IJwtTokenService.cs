using System.Security.Claims;

namespace NtisPlatform.Application.Interfaces.Auth;

/// <summary>
/// Service for JWT token operations
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generate access token for authenticated user
    /// </summary>
    string GenerateAccessToken(int userId, string organizationId, List<string> roles, Dictionary<string, string>? additionalClaims = null);

    /// <summary>
    /// Generate refresh token
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Validate token and extract claims
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Get token expiration time in seconds
    /// </summary>
    int GetAccessTokenExpirationSeconds();

    /// <summary>
    /// Generate CSRF token
    /// </summary>
    string GenerateCsrfToken();

    /// <summary>
    /// Validate CSRF token
    /// </summary>
    bool ValidateCsrfToken(string token, string storedToken);
}
