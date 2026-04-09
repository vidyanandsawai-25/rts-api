using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// Refresh token repository implementation
/// </summary>
public class RefreshTokenRepository : Repository<RefreshTokenEntity, int>, IRefreshTokenRepository
{
    private readonly IPasswordHasher _passwordHasher;

    public RefreshTokenRepository(ApplicationDbContext context, IPasswordHasher passwordHasher) : base(context)
    {
        _passwordHasher = passwordHasher;
    }

    public async Task<RefreshTokenEntity?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        // Since tokens are hashed, we need to fetch active tokens and verify against the hash
        // Fetch only non-revoked, non-expired tokens to minimize verification attempts
        var activeTokens = await _context.Set<RefreshTokenEntity>()
            .AsNoTracking()
            .Include(rt => rt.User)
            .Where(rt => !rt.IsRevoked && rt.ExpiresAt > DateTime.Now)
            .ToListAsync(cancellationToken);

        // Verify the plaintext token against each stored hash
        foreach (var tokenEntity in activeTokens)
        {
            if (_passwordHasher.VerifyPassword(token, tokenEntity.Token))
            {
                return tokenEntity;
            }
        }

        return null;
    }

    public async Task RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        // Since tokens are hashed, find the matching token first by verifying hashes
        var tokenEntity = await GetByTokenAsync(token, cancellationToken);
        
        if (tokenEntity != null && !tokenEntity.IsRevoked)
        {
            // Revoke the specific token by its ID
            await _context.Set<RefreshTokenEntity>()
                .Where(rt => rt.Id == tokenEntity.Id && !rt.IsRevoked)
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(rt => rt.IsRevoked, true)
                    .SetProperty(rt => rt.RevokedAt, DateTime.Now),
                    cancellationToken);
        }
    }

    public async Task<bool> ConsumeTokenAsync(int tokenId, CancellationToken cancellationToken = default)
    {
        // Atomically consume the token only if it's still active (not revoked and not expired)
        // This prevents concurrent replay attacks - only one request will successfully consume the token
        var rowsAffected = await _context.Set<RefreshTokenEntity>()
            .Where(rt => rt.Id == tokenId 
                && !rt.IsRevoked 
                && rt.ExpiresAt > DateTime.Now)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(rt => rt.IsRevoked, true)
                .SetProperty(rt => rt.RevokedAt, DateTime.Now),
                cancellationToken);

        // Return true if exactly one row was updated (token successfully consumed)
        return rowsAffected == 1;
    }

    public async Task RevokeAllUserTokensAsync(int userId, CancellationToken cancellationToken = default)
    {
        await _context.Set<RefreshTokenEntity>()
            .Where(rt => rt.Id == userId && !rt.IsRevoked)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(rt => rt.IsRevoked, true)
                .SetProperty(rt => rt.RevokedAt, DateTime.Now),
                cancellationToken);
    }

    public async Task DeleteExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.Now.AddDays(-30); // Keep revoked tokens for 30 days for audit
        
        await _context.Set<RefreshTokenEntity>()
            .Where(rt => rt.ExpiresAt < DateTime.Now || (rt.IsRevoked && rt.RevokedAt < cutoffDate))
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// Save changes to the database
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
