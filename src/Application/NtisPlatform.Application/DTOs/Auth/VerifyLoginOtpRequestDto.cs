using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Auth;

/// <summary>
/// Completes a login that returned RequiresTwoFactor with TwoFactorMethod "otp" by presenting the
/// emailed/texted one-time code for the pending challenge.
/// </summary>
public class VerifyLoginOtpRequestDto
{
    [Required(ErrorMessage = "ChallengeId is required")]
    [MaxLength(200, ErrorMessage = "ChallengeId is too long")]
    public string ChallengeId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Code is required")]
    [MaxLength(20, ErrorMessage = "Code is too long")]
    public string Code { get; set; } = string.Empty;
}
