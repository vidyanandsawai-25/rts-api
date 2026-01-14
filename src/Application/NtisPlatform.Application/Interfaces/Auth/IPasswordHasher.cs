using System.Security.Cryptography;

namespace NtisPlatform.Application.Interfaces.Auth;

/// <summary>
/// Service for password hashing and verification
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hash password using secure algorithm
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verify password against stored hash (constant-time comparison)
    /// </summary>
    bool VerifyPassword(string password, string passwordHash);

    /// <summary>
    /// Check if password hash needs to be rehashed (algorithm upgrade)
    /// </summary>
    bool NeedsRehash(string passwordHash);
}

/// <summary>
/// Default implementation using PBKDF2
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 32;
    private const int HashSize = 32;
    private const int Iterations = 100000;

    public string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[SaltSize];
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashSize);

        var hashBytes = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        var hashBytes = Convert.FromBase64String(passwordHash);

        if (hashBytes.Length != SaltSize + HashSize)
            return false;

        var salt = new byte[SaltSize];
        Array.Copy(hashBytes, 0, salt, 0, SaltSize);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashSize);

        var storedHash = new byte[HashSize];
        Array.Copy(hashBytes, SaltSize, storedHash, 0, HashSize);

        // Constant-time comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(hash, storedHash);
    }

    public bool NeedsRehash(string passwordHash)
    {
        // Check if hash format is outdated (can be extended for algorithm upgrades)
        try
        {
            var hashBytes = Convert.FromBase64String(passwordHash);
            return hashBytes.Length != SaltSize + HashSize;
        }
        catch
        {
            return true;
        }
    }
}
