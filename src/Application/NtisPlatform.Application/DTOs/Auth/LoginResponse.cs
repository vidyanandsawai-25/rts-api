namespace NtisPlatform.Application.DTOs.Auth;

/// <summary>
/// Response model for successful authentication
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT access token for API authorization
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token for obtaining new access tokens (null for web clients using cookies)
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Token type (Bearer for mobile/desktop, Cookie for web)
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Token expiration time in seconds
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// CSRF token for web clients
    /// </summary>
    public string? CsrfToken { get; set; }

    /// <summary>
    /// Authenticated user information
    /// </summary>
    public UserInfo User { get; set; } = new();

    /// <summary>
    /// Whether two-factor authentication is required
    /// </summary>
    public bool RequiresTwoFactor { get; set; }
}

/// <summary>
/// User information returned after authentication
/// </summary>
public class UserInfo
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}

