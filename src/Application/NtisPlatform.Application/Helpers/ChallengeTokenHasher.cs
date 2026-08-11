using System.Security.Cryptography;
using System.Text;

namespace NtisPlatform.Application.Helpers;

/// <summary>
/// Generates and hashes opaque bearer tokens and numeric one-time codes for OTP challenges.
/// Same technique already used privately inside <c>MfaChallengeService</c> and
/// <c>TwoFactorAuthenticationService</c> — pulled out here so new OTP-based challenge types don't
/// need their own copy.
/// </summary>
public static class ChallengeTokenHasher
{
    /// <summary>
    /// Generates a cryptographically random opaque bearer token (base64-encoded).
    /// </summary>
    public static string GenerateToken(int byteLength = 32)
    {
        var bytes = new byte[byteLength];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Generates a 6-digit numeric one-time code, suitable for emailing/texting to a user.
    /// </summary>
    public static string GenerateNumericCode()
    {
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return value.ToString("D6");
    }

    /// <summary>
    /// SHA-256 hash (hex) of a raw token or code, for at-rest storage.
    /// </summary>
    public static string HashToken(string raw)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }
}
