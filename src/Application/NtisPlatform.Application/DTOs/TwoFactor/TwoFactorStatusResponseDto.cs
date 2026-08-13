namespace NtisPlatform.Application.DTOs.TwoFactor;

/// <summary>
/// Current two-factor authentication status for the calling user. Never includes the
/// authenticator secret.
/// </summary>
public class TwoFactorStatusResponseDto
{
    public bool IsEnabled { get; set; }
    public int RecoveryCodesRemaining { get; set; }
    public bool HasAuthenticatorKey { get; set; }
}
