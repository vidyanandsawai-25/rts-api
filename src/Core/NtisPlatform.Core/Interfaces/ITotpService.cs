namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Standards-compliant (RFC 6238) TOTP operations. Deliberately independent of any particular
/// user-management framework — the secret is passed in explicitly on every call, and this
/// service never touches storage.
/// </summary>
public interface ITotpService
{
    /// <summary>
    /// Generates a new random base32-encoded shared secret suitable for authenticator apps.
    /// </summary>
    string GenerateSecret();

    /// <summary>
    /// Builds an otpauth:// URI for the given issuer/account/secret, with issuer and account
    /// name properly URI-escaped. This is the value QR-code renderers on the frontend expect.
    /// </summary>
    string BuildAuthenticatorUri(string issuer, string accountName, string secret);

    /// <summary>
    /// Validates a 6-digit code against the given secret at the given point in time, allowing
    /// the configured number of adjacent time-step drift.
    /// </summary>
    bool ValidateCode(string secret, string code, DateTimeOffset timestamp);

    /// <summary>
    /// Computes the TOTP code for a secret at a specific point in time. Exposed only so tests
    /// can exercise <see cref="ValidateCode"/> deterministically without waiting on the real
    /// 30-second time step.
    /// </summary>
    string ComputeCode(string secret, DateTimeOffset timestamp);
}
