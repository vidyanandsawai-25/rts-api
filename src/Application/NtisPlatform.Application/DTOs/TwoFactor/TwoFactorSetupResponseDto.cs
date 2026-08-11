namespace NtisPlatform.Application.DTOs.TwoFactor;

/// <summary>
/// Data the frontend needs to render a QR code and let the user manually enter the key into
/// their authenticator app. Returned by setup and reset; never persisted or logged.
/// </summary>
public class TwoFactorSetupResponseDto
{
    /// <summary>
    /// The shared secret, formatted in space-separated groups for manual entry.
    /// </summary>
    public string SharedKey { get; set; } = string.Empty;

    /// <summary>
    /// otpauth:// URI — the frontend renders this directly as a QR code.
    /// </summary>
    public string AuthenticatorUri { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;
}
