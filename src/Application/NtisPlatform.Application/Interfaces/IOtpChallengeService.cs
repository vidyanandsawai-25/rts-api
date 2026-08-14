namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Canonical set of purposes recorded on <c>CORE.TwoFactorChallenge</c> rows created for
/// emailed/texted one-time codes (as opposed to TOTP-based purposes like "mfa-login").
/// </summary>
public static class OtpChallengePurpose
{
    /// <summary>Global config-driven OTP required at login for users without TOTP enabled.</summary>
    public const string LoginOtp = "login-otp";

    /// <summary>OTP sent to confirm ownership of the account during self-service forgot password.</summary>
    public const string ForgotPasswordOtp = "forgot-password-otp";

    /// <summary>Short-lived bearer token issued after a forgot-password OTP is verified, used to authorize the actual password change.</summary>
    public const string PasswordReset = "password-reset";
}

/// <summary>
/// A newly created OTP challenge. <see cref="ChallengeId"/> is the raw opaque bearer token handed
/// to the client — only its hash is persisted server-side. The one-time code itself is never
/// returned here; it was already sent to the user via email/SMS.
/// </summary>
public sealed record OtpChallengeResult(string ChallengeId, DateTime ExpiresAt);

/// <summary>
/// Why an OTP verification attempt did not succeed.
/// </summary>
public enum OtpVerificationFailureReason
{
    /// <summary>Challenge id does not match any known challenge, or belongs to a different purpose.</summary>
    InvalidChallenge,

    /// <summary>Challenge existed but has expired.</summary>
    ChallengeExpired,

    /// <summary>Challenge was already consumed by a prior successful verification.</summary>
    ChallengeConsumed,

    /// <summary>Challenge was locked out after too many failed attempts.</summary>
    ChallengeLocked,

    /// <summary>Challenge is valid and still active, but the supplied code was wrong.</summary>
    InvalidCode
}

/// <summary>
/// Result of an OTP verification attempt.
/// </summary>
public sealed class OtpVerificationResult
{
    public bool Success { get; init; }
    public OtpVerificationFailureReason? FailureReason { get; init; }
    public int UserId { get; init; }

    public static OtpVerificationResult Succeeded(int userId) =>
        new() { Success = true, UserId = userId };

    public static OtpVerificationResult Failed(OtpVerificationFailureReason reason) =>
        new() { Success = false, FailureReason = reason };
}

/// <summary>
/// Generic primitive for issuing and verifying short-lived, one-time numeric codes delivered by
/// email and/or SMS — used for config-driven login OTP and forgot-password OTP. Distinct from
/// <see cref="IMfaChallengeService"/>, which owns TOTP/recovery-code login challenges.
/// </summary>
public interface IOtpChallengeService
{
    /// <summary>
    /// Generates a one-time code, persists a hashed challenge under the given purpose, and sends
    /// the raw code via the requested channel(s). At least one of <paramref name="sendEmail"/> /
    /// <paramref name="sendSms"/> must be true.
    /// </summary>
    Task<OtpChallengeResult> CreateAsync(
        NtisPlatform.Core.Entities.Master.UserEntity user,
        string purpose,
        bool sendEmail,
        bool sendSms,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a code against a pending OTP challenge for the given purpose. On success, the
    /// challenge is consumed.
    /// </summary>
    Task<OtpVerificationResult> VerifyAsync(
        string challengeId,
        string purpose,
        string code,
        CancellationToken cancellationToken = default);
}
