using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Repository interface for UserMaster operations
/// </summary>
public interface IUserRepository : IRepository<UserEntity, int>
{
    /// <summary>
    /// Find user by username (case-insensitive)
    /// </summary>
    Task<UserEntity?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find an active user by username OR email (case-insensitive). Used by the self-service
    /// forgot-password flow, where the caller doesn't know which identifier type they were given.
    /// </summary>
    Task<UserEntity?> GetByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update last login timestamp
    /// </summary>
    Task UpdateLastLoginAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increment failed login count
    /// </summary>
    Task IncrementFailedLoginCountAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset failed login count
    /// </summary>
    Task ResetFailedLoginCountAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a newly generated (or reset) TOTP secret as pending — not yet enabled.
    /// Used during authenticator setup, before the first code is verified.
    /// </summary>
    Task SetPendingTwoFactorSecretAsync(int userId, string encryptedSecret, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks two-factor authentication as enabled for the user and rotates the security stamp.
    /// Requires a pending secret to already be set. Returns false if the user does not exist.
    /// </summary>
    Task<bool> EnableTwoFactorAsync(int userId, string newSecurityStamp, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables two-factor authentication, clears the stored secret, and rotates the security
    /// stamp. Returns false if the user does not exist.
    /// </summary>
    Task<bool> DisableTwoFactorAsync(int userId, string newSecurityStamp, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates the security stamp without changing the two-factor enabled/secret state.
    /// </summary>
    Task UpdateSecurityStampAsync(int userId, string newSecurityStamp, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fast, no-tracking read of just the current security stamp — used by JWT validation to
    /// detect tokens issued before a sensitive security change.
    /// </summary>
    Task<string?> GetSecurityStampAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a new password hash, clears <c>MustChangePassword</c>, and rotates the security stamp
    /// (invalidating any live access tokens). Used by the self-service forgot-password flow.
    /// Returns false if the user does not exist.
    /// </summary>
    Task<bool> ResetPasswordAsync(int userId, string newPasswordHash, string newSecurityStamp, CancellationToken cancellationToken = default);
}
