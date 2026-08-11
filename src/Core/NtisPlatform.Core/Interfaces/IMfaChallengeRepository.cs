using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Outcome of attempting to record a failed MFA challenge verification attempt.
/// </summary>
public enum MfaChallengeFailureOutcome
{
    /// <summary>The challenge no longer exists or was already consumed/revoked/expired.</summary>
    NotActive,

    /// <summary>The attempt was recorded and the challenge remains usable.</summary>
    AttemptRecorded,

    /// <summary>The attempt was recorded and pushed the challenge over its attempt limit; it is now revoked.</summary>
    NowLocked
}

/// <summary>
/// Repository interface for MFA login-challenge operations.
/// </summary>
public interface IMfaChallengeRepository : IRepository<MfaChallengeEntity, Guid>
{
    /// <summary>
    /// Looks up a challenge by the SHA-256 hash of its opaque token.
    /// </summary>
    Task<MfaChallengeEntity?> GetByHashAsync(string challengeHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up the most recent still-active (not consumed/revoked/expired) challenge for a user
    /// and purpose. Used when the presented code must be compared against a specific row so a
    /// wrong guess can still have its failed-attempt count recorded (unlike <see cref="GetByHashAsync"/>,
    /// which only finds a row when the guess is already correct).
    /// </summary>
    Task<MfaChallengeEntity?> GetActiveByUserIdAndPurposeAsync(int userId, string purpose, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically marks the challenge as consumed, but only if it is currently active
    /// (not expired, not already consumed, not revoked). Prevents the same login challenge
    /// from completing two concurrent login flows.
    /// </summary>
    /// <returns>True if this call won the race and consumed the challenge; false otherwise.</returns>
    Task<bool> TryConsumeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically increments the failed-attempt counter and revokes the challenge once the
    /// configured maximum is reached.
    /// </summary>
    Task<MfaChallengeFailureOutcome> RecordFailedAttemptAsync(Guid id, int maximumAttempts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes challenges that expired or were resolved (consumed/revoked) more than the given
    /// retention window ago. Intended for a periodic cleanup task.
    /// </summary>
    Task DeleteStaleAsync(TimeSpan retention, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
