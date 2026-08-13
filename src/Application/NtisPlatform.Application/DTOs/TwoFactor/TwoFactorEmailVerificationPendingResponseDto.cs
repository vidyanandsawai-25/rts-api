namespace NtisPlatform.Application.DTOs.TwoFactor;

/// <summary>
/// Returned by <see cref="Interfaces.ITwoFactorAuthenticationService.EnableAsync"/> once the TOTP
/// code is confirmed and a one-time verification code has been emailed to the account's
/// registered address. 2FA is not enabled yet — that happens only once the emailed code is
/// confirmed via <see cref="Interfaces.ITwoFactorAuthenticationService.ConfirmEnableAsync"/>.
/// </summary>
public class TwoFactorEmailVerificationPendingResponseDto
{
    /// <summary>
    /// The account's email address, partially masked (e.g. "jo***@example.com") — safe to show in
    /// the UI without fully exposing the address.
    /// </summary>
    public string MaskedEmail { get; set; } = string.Empty;
}
