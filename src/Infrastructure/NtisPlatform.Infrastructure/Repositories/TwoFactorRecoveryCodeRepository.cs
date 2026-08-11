using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// Two-factor recovery code repository implementation.
/// </summary>
public class TwoFactorRecoveryCodeRepository : Repository<TwoFactorRecoveryCodeEntity, int>, ITwoFactorRecoveryCodeRepository
{
    public TwoFactorRecoveryCodeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<TwoFactorRecoveryCodeEntity>> GetActiveByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TwoFactorRecoveryCodeEntity>()
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.UsedAt == null && c.RevokedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountActiveByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TwoFactorRecoveryCodeEntity>()
            .AsNoTracking()
            .CountAsync(c => c.UserId == userId && c.UsedAt == null && c.RevokedAt == null, cancellationToken);
    }

    public async Task<bool> TryRedeemAsync(int recoveryCodeId, CancellationToken cancellationToken = default)
    {
        var rowsAffected = await _context.Set<TwoFactorRecoveryCodeEntity>()
            .Where(c => c.Id == recoveryCodeId && c.UsedAt == null && c.RevokedAt == null)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(c => c.UsedAt, DateTime.UtcNow),
                cancellationToken);

        return rowsAffected == 1;
    }

    public async Task RevokeAllActiveAsync(int userId, CancellationToken cancellationToken = default)
    {
        await _context.Set<TwoFactorRecoveryCodeEntity>()
            .Where(c => c.UserId == userId && c.UsedAt == null && c.RevokedAt == null)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(c => c.RevokedAt, DateTime.UtcNow),
                cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
