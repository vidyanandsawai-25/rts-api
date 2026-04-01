using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Auth;

/// <summary>
/// Request DTO for refreshing an access token
/// </summary>
public class RefreshTokenRequestDto
{
    /// <summary>
    /// The refresh token to use for obtaining a new access token
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}
