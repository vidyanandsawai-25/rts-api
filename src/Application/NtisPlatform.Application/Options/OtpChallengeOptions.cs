using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.Options;

/// <summary>
/// Configuration for emailed/texted one-time-code challenges (login OTP, forgot-password OTP).
/// Bound from the "Authentication:Otp" section of configuration.
/// </summary>
public sealed class OtpChallengeOptions
{
    public const string SectionName = "Authentication:Otp";

    /// <summary>
    /// Lifetime of a login-OTP or forgot-password-OTP challenge, in minutes.
    /// </summary>
    [Range(1, 30)]
    public int LifetimeMinutes { get; init; } = 5;

    /// <summary>
    /// Maximum failed verification attempts allowed against a single OTP challenge before it is
    /// locked out.
    /// </summary>
    [Range(1, 20)]
    public int MaximumVerificationAttempts { get; init; } = 5;

    /// <summary>
    /// Lifetime of the short-lived bearer token issued after a forgot-password OTP is verified,
    /// during which the client must submit the new password.
    /// </summary>
    [Range(1, 60)]
    public int PasswordResetTokenLifetimeMinutes { get; init; } = 10;
}
