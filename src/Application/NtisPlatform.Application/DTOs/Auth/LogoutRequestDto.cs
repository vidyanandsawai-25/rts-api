using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Auth;

/// <summary>
/// Request DTO for logout
/// </summary>
public class LogoutRequestDto
{
    /// <summary>
    /// The refresh token to revoke
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}
