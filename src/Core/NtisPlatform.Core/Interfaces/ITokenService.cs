namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generate a JWT access token for authenticated user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="username">Username</param>
    /// <returns>JWT token string</returns>
    string GenerateToken(int userId, string username);

    /// <summary>
    /// Generate a cryptographically secure refresh token
    /// </summary>
    /// <returns>Refresh token string</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validate a JWT token and extract claims
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>Token validation result with user information</returns>
    JwtValidationResult ValidateToken(string token);
}

/// <summary>
/// Result of JWT token validation
/// </summary>
public class JwtValidationResult
{
    public bool IsValid { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
}
