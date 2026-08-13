namespace NtisPlatform.Application.Helpers;

/// <summary>
/// Normalizes user-supplied TOTP and recovery codes before validation. Authenticator apps
/// commonly render codes with a space (e.g. "123 456"); recovery codes are displayed with a
/// hyphen (e.g. "ABCDE-FGHJK") for readability. Neither separator carries meaning for
/// verification.
/// </summary>
public static class TwoFactorCodeNormalizer
{
    /// <summary>
    /// Strips everything but digits. Does not validate length — use <see cref="IsSixDigits"/>.
    /// </summary>
    public static string NormalizeTotpCode(string raw)
    {
        return new string(raw.Where(char.IsDigit).ToArray());
    }

    public static bool IsSixDigits(string normalizedCode)
    {
        return normalizedCode.Length == 6 && normalizedCode.All(char.IsDigit);
    }

    /// <summary>
    /// Strips whitespace and hyphens and upper-cases the result, matching the alphabet recovery
    /// codes are generated from.
    /// </summary>
    public static string NormalizeRecoveryCode(string raw)
    {
        return new string(raw.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray()).ToUpperInvariant();
    }
}
