using Microsoft.Extensions.Options;
using NtisPlatform.Application.Options;
using NtisPlatform.Core.Interfaces;
using OtpNet;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// RFC 6238 TOTP implementation backed by Otp.NET, using the SHA1/6-digit/30-second defaults
/// every mainstream authenticator app (Google Authenticator, Microsoft Authenticator, Authy)
/// expects.
/// </summary>
public class TotpService : ITotpService
{
    private const int SecretLengthBytes = 20; // 160-bit, the de-facto authenticator-app default
    private const int TimeStepSeconds = 30;
    private const int Digits = 6;

    private readonly int _allowedDriftSteps;

    public TotpService(IOptions<TwoFactorAuthenticationOptions> options)
    {
        _allowedDriftSteps = options.Value.AllowedDriftSteps;
    }

    public string GenerateSecret()
    {
        var secretBytes = KeyGeneration.GenerateRandomKey(SecretLengthBytes);
        return Base32Encoding.ToString(secretBytes);
    }

    public string BuildAuthenticatorUri(string issuer, string accountName, string secret)
    {
        var label = Uri.EscapeDataString($"{issuer}:{accountName}");
        var encodedIssuer = Uri.EscapeDataString(issuer);
        return $"otpauth://totp/{label}?secret={secret}&issuer={encodedIssuer}&algorithm=SHA1&digits={Digits}&period={TimeStepSeconds}";
    }

    public bool ValidateCode(string secret, string code, DateTimeOffset timestamp)
    {
        var totp = CreateTotp(secret);
        var window = Math.Max(2, _allowedDriftSteps);

        return totp.VerifyTotp(
            timestamp.UtcDateTime,
            code,
            out _,
            new VerificationWindow(previous: window, future: window));
    }

    public string ComputeCode(string secret, DateTimeOffset timestamp)
    {
        var totp = CreateTotp(secret);
        return totp.ComputeTotp(timestamp.UtcDateTime);
    }

    private static Totp CreateTotp(string secret)
    {
        var secretBytes = Base32Encoding.ToBytes(secret);
        return new Totp(secretBytes, step: TimeStepSeconds, mode: OtpHashMode.Sha1, totpSize: Digits);
    }
}
