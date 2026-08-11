using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PasswordReset;

/// <summary>
/// Looks up which OTP delivery methods are actually available for an account, before the user
/// commits to one.
/// </summary>
public class ForgotPasswordAvailableMethodsRequestDto
{
    [Required(ErrorMessage = "UsernameOrEmail is required")]
    [MaxLength(100, ErrorMessage = "UsernameOrEmail is too long")]
    public string UsernameOrEmail { get; set; } = string.Empty;
}
