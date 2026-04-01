namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Service for password hashing and verification using bcrypt
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hash a plain text password using bcrypt
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>Bcrypt hashed password</returns>
    string HashPassword(string password);
    
    /// <summary>
    /// Verify a plain text password against a bcrypt hash
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <param name="passwordHash">Bcrypt hashed password</param>
    /// <returns>True if password matches, false otherwise</returns>
    bool VerifyPassword(string password, string passwordHash);
}
