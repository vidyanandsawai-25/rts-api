using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Repository interface for refresh token operations
/// </summary>
public interface IRefreshTokenRepository : IRepository<RefreshTokenEntity, int>
{
    /// <summary>
    /// Get an active refresh token by token value
    /// </summary>
    Task<RefreshTokenEntity?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a refresh token
    /// </summary>
    Task RevokeTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically consume a refresh token (mark as revoked) only if it is still active.
    /// This prevents concurrent replay attacks during token rotation.
    /// </summary>
    /// <param name="tokenId">The ID of the token to consume</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the token was successfully consumed, false if already consumed or invalid</returns>
    Task<bool> ConsumeTokenAsync(int tokenId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke all refresh tokens for a user
    /// </summary>
    Task RevokeAllUserTokensAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up expired tokens (background job task)
    /// </summary>
    Task DeleteExpiredTokensAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Save changes to the database
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
