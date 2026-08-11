using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Repository interface for two-factor recovery code operations.
/// </summary>
public interface ITwoFactorRecoveryCodeRepository : IRepository<TwoFactorRecoveryCodeEntity, int>
{
    /// <summary>
    /// Returns all currently redeemable (not used, not revoked) recovery codes for a user.
    /// </summary>
    Task<IReadOnlyList<TwoFactorRecoveryCodeEntity>> GetActiveByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts currently redeemable recovery codes for a user.
    /// </summary>
    Task<int> CountActiveByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically marks a specific code as used, but only if it is still active. Prevents the
    /// same recovery code from being redeemed twice under concurrent requests.
    /// </summary>
    /// <returns>True if this call won the race and consumed the code; false otherwise.</returns>
    Task<bool> TryRedeemAsync(int recoveryCodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all currently active recovery codes for a user (used on regeneration, disable,
    /// and reset) so previously issued codes can never be redeemed again.
    /// </summary>
    Task RevokeAllActiveAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
