using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// MFA login-challenge repository implementation.
/// </summary>
public class MfaChallengeRepository : Repository<MfaChallengeEntity, Guid>, IMfaChallengeRepository
{
    public MfaChallengeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<MfaChallengeEntity?> GetByHashAsync(string challengeHash, CancellationToken cancellationToken = default)
    {
        return await _context.Set<MfaChallengeEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ChallengeHash == challengeHash, cancellationToken);
    }

    public async Task<MfaChallengeEntity?> GetActiveByUserIdAndPurposeAsync(int userId, string purpose, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Set<MfaChallengeEntity>()
            .AsNoTracking()
            .Where(c => c.UserId == userId
                && c.Purpose == purpose
                && c.ConsumedAt == null
                && c.RevokedAt == null
                && c.ExpiresAt > now)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> TryConsumeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var rowsAffected = await _context.Set<MfaChallengeEntity>()
            .Where(c => c.Id == id
                && c.ConsumedAt == null
                && c.RevokedAt == null
                && c.ExpiresAt > now)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(c => c.ConsumedAt, now),
                cancellationToken);

        return rowsAffected == 1;
    }

    public async Task<MfaChallengeFailureOutcome> RecordFailedAttemptAsync(Guid id, int maximumAttempts, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var rowsAffected = await _context.Set<MfaChallengeEntity>()
            .Where(c => c.Id == id
                && c.ConsumedAt == null
                && c.RevokedAt == null
                && c.ExpiresAt > now)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(c => c.FailedAttemptCount, c => c.FailedAttemptCount + 1)
                .SetProperty(c => c.RevokedAt, c => (c.FailedAttemptCount + 1) >= maximumAttempts ? now : c.RevokedAt),
                cancellationToken);

        if (rowsAffected != 1)
        {
            return MfaChallengeFailureOutcome.NotActive;
        }

        var challenge = await _context.Set<MfaChallengeEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return challenge?.RevokedAt != null
            ? MfaChallengeFailureOutcome.NowLocked
            : MfaChallengeFailureOutcome.AttemptRecorded;
    }

    public async Task DeleteStaleAsync(TimeSpan retention, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.Subtract(retention);

        await _context.Set<MfaChallengeEntity>()
            .Where(c => c.ExpiresAt < cutoff
                || (c.ConsumedAt != null && c.ConsumedAt < cutoff)
                || (c.RevokedAt != null && c.RevokedAt < cutoff))
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
