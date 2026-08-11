using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.TwoFactor;

/// <summary>
/// Request to enable 2FA by confirming the first code from the authenticator app.
/// </summary>
public class EnableTwoFactorRequestDto
{
    /// <summary>
    /// The 6-digit authenticator code. May contain spaces (e.g. "123 456") as typically
    /// displayed by authenticator apps — normalized server-side before validation.
    /// </summary>
    [Required(ErrorMessage = "Code is required")]
    [MaxLength(20, ErrorMessage = "Code is too long")]
    public string Code { get; set; } = string.Empty;
}
