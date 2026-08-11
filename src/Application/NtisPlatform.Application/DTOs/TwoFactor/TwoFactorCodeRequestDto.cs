using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.TwoFactor;

/// <summary>
/// Shared reauthentication request shape for sensitive 2FA state changes (disable, reset,
/// recovery code regeneration) that accept either a TOTP code or a recovery code.
/// </summary>
public class TwoFactorCodeRequestDto
{
    [Required(ErrorMessage = "Code is required")]
    [MaxLength(20, ErrorMessage = "Code is too long")]
    public string Code { get; set; } = string.Empty;
}
