using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.Options;

/// <summary>
/// Configuration for authenticator-app (TOTP) two-factor authentication.
/// Bound from the "Authentication:TwoFactor" section of configuration.
/// </summary>
public sealed class TwoFactorAuthenticationOptions
{
    public const string SectionName = "Authentication:TwoFactor";

    /// <summary>
    /// Issuer name shown inside authenticator apps (e.g. "NtisPlatform"). Also used as the
    /// otpauth:// URI issuer parameter.
    /// </summary>
    //[Required]
    public required string Issuer { get; init; }

    /// <summary>
    /// Lifetime of a login MFA challenge, in minutes.
    /// </summary>
    [Range(1, 30)]
    public int ChallengeLifetimeMinutes { get; init; } = 5;

    /// <summary>
    /// Maximum failed verification attempts allowed against a single MFA challenge before it is
    /// locked out.
    /// </summary>
    [Range(1, 20)]
    public int MaximumVerificationAttempts { get; init; } = 5;

    /// <summary>
    /// Number of one-time recovery codes generated when 2FA is enabled or regenerated.
    /// </summary>
    [Range(1, 20)]
    public int RecoveryCodeCount { get; init; } = 10;

    /// <summary>
    /// Number of adjacent 30-second time steps (before and after) tolerated when validating a
    /// TOTP code, to absorb clock drift between the server and the user's device.
    /// </summary>
    [Range(0, 5)]
    public int AllowedDriftSteps { get; init; } = 1;

    /// <summary>
    /// Lifetime of the one-time code emailed during admin-assisted enrollment, in minutes.
    /// Longer than <see cref="ChallengeLifetimeMinutes"/> since checking email realistically
    /// takes longer than checking a phone that's already in hand.
    /// </summary>
    [Range(1, 60)]
    public int EmailVerificationLifetimeMinutes { get; init; } = 15;
}
