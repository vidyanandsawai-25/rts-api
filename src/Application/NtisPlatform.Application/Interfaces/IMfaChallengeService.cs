using NtisPlatform.Application.DTOs.Auth;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// A newly created login MFA challenge. <see cref="ChallengeId"/> is the raw opaque token
/// handed to the client — only its hash is persisted server-side.
/// </summary>
public sealed record MfaLoginChallenge(string ChallengeId, DateTime ExpiresAtUtc);

/// <summary>
/// Why an MFA login-challenge verification attempt did not succeed. Lets the controller map to
/// the correct HTTP status code without leaking account-existence details.
/// </summary>
public enum MfaVerificationFailureReason
{
    /// <summary>Challenge id does not match any known challenge, or belongs to a different purpose.</summary>
    InvalidChallenge,

    /// <summary>Challenge existed but has expired.</summary>
    ChallengeExpired,

    /// <summary>Challenge was already consumed by a prior successful verification.</summary>
    ChallengeConsumed,

    /// <summary>Challenge was locked out after too many failed attempts.</summary>
    ChallengeLocked,

    /// <summary>Challenge is valid and still active, but the supplied code/recovery code was wrong.</summary>
    InvalidCode
}

/// <summary>
/// Result of an MFA login-challenge verification attempt.
/// </summary>
public sealed class MfaVerificationResult
{
    public bool Success { get; init; }
    public MfaVerificationFailureReason? FailureReason { get; init; }
    public LoginResponseDto? LoginResponse { get; init; }

    public static MfaVerificationResult Succeeded(LoginResponseDto response) =>
        new() { Success = true, LoginResponse = response };

    public static MfaVerificationResult Failed(MfaVerificationFailureReason reason) =>
        new() { Success = false, FailureReason = reason };
}

/// <summary>
/// Manages the short-lived, one-time-use MFA challenge issued after a successful password
/// check for a user with two-factor authentication enabled. Separate from
/// <see cref="ITwoFactorAuthenticationService"/>, which owns authenticator setup/enable/disable.
/// </summary>
public interface IMfaChallengeService
{
    /// <summary>
    /// Creates a new login MFA challenge for a user whose password has already been verified.
    /// </summary>
    Task<MfaLoginChallenge> CreateLoginChallengeAsync(
        int userId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a code or recovery code against a pending login challenge. On success, the
    /// challenge is consumed and a full login token pair is issued.
    /// </summary>
    Task<MfaVerificationResult> VerifyLoginChallengeAsync(
        string challengeId,
        string code,
        bool useRecoveryCode,
        CancellationToken cancellationToken = default);
}
