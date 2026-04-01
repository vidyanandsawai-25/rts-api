using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Bcrypt password hasher implementation
/// Requires BCrypt.Net-Next NuGet package
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        // Use BCrypt with work factor of 12 (recommended for security)
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch
        {
            // Invalid hash format or other errors
            return false;
        }
    }
}
