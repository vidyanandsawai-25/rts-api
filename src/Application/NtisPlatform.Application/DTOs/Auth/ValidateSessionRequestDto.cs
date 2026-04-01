using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Auth;

/// <summary>
/// Request DTO for validating a session/token
/// </summary>
public class ValidateSessionRequestDto
{
    /// <summary>
    /// The access token to validate
    /// </summary>
    [Required(ErrorMessage = "Access token is required")]
    public string AccessToken { get; set; } = string.Empty;
}
