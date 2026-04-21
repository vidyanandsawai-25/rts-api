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
}
