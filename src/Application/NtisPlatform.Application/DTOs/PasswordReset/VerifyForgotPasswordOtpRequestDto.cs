using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PasswordReset;

/// <summary>
/// Verifies the OTP sent by <c>ForgotPasswordRequestDto</c> and, on success, obtains a short-lived
/// reset token used to actually change the password.
/// </summary>
public class VerifyForgotPasswordOtpRequestDto
{
    [Required(ErrorMessage = "ChallengeId is required")]
    [MaxLength(200, ErrorMessage = "ChallengeId is too long")]
    public string ChallengeId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Code is required")]
    [MaxLength(20, ErrorMessage = "Code is too long")]
    public string Code { get; set; } = string.Empty;
}
