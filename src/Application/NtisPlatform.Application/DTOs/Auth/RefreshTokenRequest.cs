using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Auth;

/// <summary>
/// Request to refresh an access token
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// Refresh token obtained during login
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Device information for security validation
    /// </summary>
    public DeviceInfo? Device { get; set; }
}

/// <summary>
/// Response containing new access token
/// </summary>
public class RefreshTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
}
