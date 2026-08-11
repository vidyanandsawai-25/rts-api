namespace NtisPlatform.Application.DTOs.PasswordReset;

/// <summary>
/// Response to a forgot-password request. <see cref="Message"/> is deliberately generic
/// regardless of whether the account exists, to avoid leaking account existence.
/// </summary>
public class ForgotPasswordResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    /// <summary>
    /// Opaque, one-time-use OTP challenge id. Only present when an OTP was actually sent (i.e.
    /// a matching, deliverable account was found).
    /// </summary>
    public string? ChallengeId { get; set; }

    /// <summary>
    /// UTC expiry of the OTP challenge. Only present when <see cref="ChallengeId"/> is present.
    /// </summary>
    public DateTime? ChallengeExpiresAtUtc { get; set; }
}
