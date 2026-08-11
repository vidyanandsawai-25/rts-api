using NtisPlatform.Application.DTOs.TwoFactor;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Reason a two-factor state-changing operation could not proceed. Lets the controller map to
/// the correct HTTP status code (409 for an invalid state transition, 401 for a bad code).
/// </summary>
public enum TwoFactorOperationError
{
    /// <summary>2FA is already enabled and the caller asked to run setup/enable again (not a reset).</summary>
    AlreadyEnabled,

    /// <summary>2FA is not enabled, so disable/regenerate/etc. cannot proceed.</summary>
    NotEnabled,

    /// <summary>Setup was never started, so there is no pending secret to confirm.</summary>
    SetupNotStarted,

    /// <summary>The supplied code failed validation (bad format, wrong TOTP, or unknown recovery code).</summary>
    InvalidCode,

    /// <summary>The target user could not be found.</summary>
    UserNotFound,

    /// <summary>
    /// The target user has no email address on file. Enrollment always ends with a code emailed
    /// to that address, so it can never be gated on proving access to it — see
    /// <see cref="ITwoFactorAuthenticationService.EnableAsync"/>.
    /// </summary>
    EmailNotOnFile
}

/// <summary>
/// Result of a two-factor operation that can fail for a business reason (as opposed to an
/// unexpected exception).
/// </summary>
public sealed class TwoFactorOperationResult<T>
{
    public bool Success { get; init; }
    public TwoFactorOperationError? Error { get; init; }
    public T? Value { get; init; }

    public static TwoFactorOperationResult<T> Succeeded(T value) => new() { Success = true, Value = value };

    public static TwoFactorOperationResult<T> Failed(TwoFactorOperationError error) =>
        new() { Success = false, Error = error };
}

/// <summary>
/// Owns authenticator setup, enable, disable, reset, and recovery-code lifecycle for a user's
/// own account. Does not handle login-time MFA challenges — see <see cref="IMfaChallengeService"/>.
/// </summary>
public interface ITwoFactorAuthenticationService
{
    /// <summary>
    /// Returns the current 2FA status for a user. Never includes the authenticator secret.
    /// </summary>
    Task<TwoFactorStatusResponseDto> GetStatusAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts (or, if 2FA is already enabled, restarts as part of an explicit reset) authenticator
    /// setup: generates a new secret, stores it encrypted as pending, and returns the
    /// otpauth:// URI and manual key for the frontend to render. Fails fast with
    /// <see cref="TwoFactorOperationError.EmailNotOnFile"/> if the target has no email address on
    /// file — completing setup always ends with an emailed verification code (see
    /// <see cref="EnableAsync"/>), so there is no point starting a setup that can never finish.
    /// </summary>
    /// <param name="isReset">
    /// True when this call is part of an explicit reset of an already-enabled authenticator.
    /// False setup calls fail with <see cref="TwoFactorOperationError.AlreadyEnabled"/> if 2FA is
    /// already on, to avoid silently replacing a working authenticator.
    /// </param>
    Task<TwoFactorOperationResult<TwoFactorSetupResponseDto>> BeginSetupAsync(
        int userId,
        bool isReset,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enrollment, step 2 of 3: confirms the first code from whatever authenticator app was used
    /// to scan the QR from <see cref="BeginSetupAsync"/>. Proves the caller can operate <em>some</em>
    /// authenticator app, but not that it belongs to this user's account — whether the caller is
    /// the account owner (self-service) or an admin setting it up on someone else's behalf, so
    /// this does NOT enable 2FA yet. On success it emails a one-time code to the user's registered
    /// address and leaves enrollment pending; call <see cref="ConfirmEnableAsync"/> with that code
    /// to finish.
    /// </summary>
    Task<TwoFactorOperationResult<TwoFactorEmailVerificationPendingResponseDto>> EnableAsync(
        int userId,
        string totpCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enrollment, step 3 of 3: confirms the one-time code emailed by <see cref="EnableAsync"/>.
    /// Only on success does this actually enable 2FA and issue recovery codes — proving whoever is
    /// completing setup has access to the account's real inbox, not just an authenticator app.
    /// </summary>
    Task<TwoFactorOperationResult<EnableTwoFactorResponseDto>> ConfirmEnableAsync(
        int userId,
        string emailCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerates recovery codes after re-verifying a TOTP code. Invalidates all previously
    /// issued, unused recovery codes.
    /// </summary>
    Task<TwoFactorOperationResult<RecoveryCodesResponseDto>> RegenerateRecoveryCodesAsync(
        int userId,
        string verificationCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables 2FA after re-verifying a TOTP or recovery code: clears the authenticator secret,
    /// invalidates recovery codes, rotates the security stamp, and revokes existing refresh
    /// tokens.
    /// </summary>
    Task<TwoFactorOperationResult<bool>> DisableAsync(
        int userId,
        string verificationCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the authenticator after re-verifying a TOTP or recovery code: disables 2FA,
    /// invalidates recovery codes, rotates the security stamp, revokes existing refresh tokens,
    /// and immediately begins a new setup so the caller can re-enroll.
    /// </summary>
    Task<TwoFactorOperationResult<TwoFactorSetupResponseDto>> ResetAsync(
        int userId,
        string verificationCode,
        CancellationToken cancellationToken = default);
}
