using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Auth;

/// <summary>
/// Completes a login that returned RequiresTwoFactor by presenting either a TOTP code or a
/// recovery code for the pending challenge.
/// </summary>
public class VerifyTwoFactorRequestDto
{
    [Required(ErrorMessage = "ChallengeId is required")]
    [MaxLength(200, ErrorMessage = "ChallengeId is too long")]
    public string ChallengeId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Code is required")]
    [MaxLength(20, ErrorMessage = "Code is too long")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// True if <see cref="Code"/> is a recovery code rather than a TOTP code.
    /// </summary>
    public bool UseRecoveryCode { get; set; }
}
