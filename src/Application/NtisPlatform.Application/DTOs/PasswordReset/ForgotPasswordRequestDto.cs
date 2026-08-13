using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PasswordReset;

/// <summary>
/// Starts the self-service forgot-password flow.
/// </summary>
public class ForgotPasswordRequestDto
{
    [Required(ErrorMessage = "UsernameOrEmail is required")]
    [MaxLength(100, ErrorMessage = "UsernameOrEmail is too long")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    /// <summary>
    /// Which delivery method the user picked, from the list returned by
    /// <c>POST /Auth/forgot-password/methods</c>: "Email", "Sms", or "Authenticator".
    /// Re-validated server-side against the account's actual availability — never trusted as-is.
    /// </summary>
    [Required(ErrorMessage = "Method is required")]
    [MaxLength(20, ErrorMessage = "Method is too long")]
    public string Method { get; set; } = string.Empty;
}
