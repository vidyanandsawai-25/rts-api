namespace NtisPlatform.Application.DTOs.PasswordReset;

public class VerifyForgotPasswordOtpResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    /// <summary>
    /// Opaque, one-time-use bearer token authorizing the actual password change. Only present
    /// when <see cref="Success"/> is true.
    /// </summary>
    public string? ResetToken { get; set; }

    /// <summary>
    /// UTC expiry of <see cref="ResetToken"/>.
    /// </summary>
    public DateTime? ResetTokenExpiresAt { get; set; }
}
