namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Canonical set of security audit event types recorded for 2FA/MFA activity.
/// </summary>
public static class SecurityAuditEventType
{
    public const string TwoFactorSetupStarted = "TwoFactorSetupStarted";
    public const string TwoFactorEnabled = "TwoFactorEnabled";
    public const string TwoFactorDisabled = "TwoFactorDisabled";
    public const string TwoFactorReset = "TwoFactorReset";
    public const string RecoveryCodesRegenerated = "RecoveryCodesRegenerated";
    public const string RecoveryCodeUsed = "RecoveryCodeUsed";
    public const string MfaVerificationSucceeded = "MfaVerificationSucceeded";
    public const string MfaVerificationFailed = "MfaVerificationFailed";
    public const string MfaChallengeExpired = "MfaChallengeExpired";
    public const string MfaChallengeLocked = "MfaChallengeLocked";
    public const string SuspiciousRepeatedAttempts = "SuspiciousRepeatedAttempts";

    // Admin-initiated (User Management screen)
    public const string TwoFactorRequiredByAdmin = "TwoFactorRequiredByAdmin";
    public const string TwoFactorUnrequiredByAdmin = "TwoFactorUnrequiredByAdmin";
    public const string TwoFactorAdminReset = "TwoFactorAdminReset";

    // Admin-assisted enrollment (in-person setup) — email ownership gate
    public const string TwoFactorEmailVerificationSent = "TwoFactorEmailVerificationSent";
    public const string TwoFactorEmailVerificationConfirmed = "TwoFactorEmailVerificationConfirmed";

    // Self-service forgot password (config-driven OTP)
    public const string ForgotPasswordOtpRequested = "ForgotPasswordOtpRequested";
    public const string ForgotPasswordOtpVerified = "ForgotPasswordOtpVerified";
    public const string PasswordResetCompleted = "PasswordResetCompleted";

    // Self-service authenticated password change
    public const string PasswordChanged = "PasswordChanged";
}

/// <summary>
/// Records structured security audit events. Implementations must never persist or log secrets,
/// OTP codes, recovery codes, passwords, or tokens — only the event metadata itself.
/// </summary>
public interface ISecurityAuditService
{
    Task RecordAsync(
        string eventType,
        int? userId,
        bool success,
        string? correlationId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? detail = null,
        CancellationToken cancellationToken = default);
}
