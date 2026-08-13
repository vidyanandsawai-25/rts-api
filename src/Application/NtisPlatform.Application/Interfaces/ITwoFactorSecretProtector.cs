namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Encrypts/decrypts TOTP shared secrets at rest via ASP.NET Core Data Protection. The secret is
/// never hashed — it must remain decryptable to validate future codes.
/// </summary>
public interface ITwoFactorSecretProtector
{
    string Protect(string plaintextSecret);
    string Unprotect(string encryptedSecret);
}
