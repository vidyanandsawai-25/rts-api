using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PasswordReset;

/// <summary>
/// Completes the forgot-password flow by setting a new password, authorized by the reset token
/// obtained from <c>VerifyForgotPasswordOtpResponseDto</c>.
/// </summary>
public class ResetPasswordRequestDto
{
    [Required(ErrorMessage = "ResetToken is required")]
    [MaxLength(200, ErrorMessage = "ResetToken is too long")]
    public string ResetToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "NewPassword is required")]
    [MinLength(8, ErrorMessage = "NewPassword must be at least 8 characters")]
    [MaxLength(100, ErrorMessage = "NewPassword is too long")]
    public string NewPassword { get; set; } = string.Empty;
}
