using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// User repository implementation
/// </summary>
public class UserRepository : Repository<UserEntity, int>, IUserRepository
{
    private readonly ISecuritySettingsService _securitySettings;

    public UserRepository(ApplicationDbContext context, ISecuritySettingsService securitySettings) : base(context)
    {
        _securitySettings = securitySettings;
    }

    public async Task<UserEntity?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        // Case-insensitive comparison using ToUpperInvariant() on both sides
        // This approach:
        // - Avoids culture-sensitive issues (e.g., Turkish-I problem)
        // - Works consistently across all database providers
        // - Gets translated to SQL UPPER() comparison
        return await _context.UserMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName.ToUpper() == username.ToUpper(), cancellationToken);
    }

    public async Task UpdateLastLoginAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.UserMasters
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user != null)
        {
            user.LastLoginAt = DateTime.Now;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task IncrementFailedLoginCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Get lockout policy from security settings 
        var maxAttempts = await _securitySettings.GetAsync<int>("MaxFailedAttempts", 5, cancellationToken);
        var lockoutMinutes = await _securitySettings.GetAsync<int>("LockoutDurationMinutes", 30, cancellationToken);

        // Fetch user to update
        var user = await _context.UserMasters
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null) return;

        var newFailedCount = (user.FailedLoginCount ?? 0) + 1;
        var newLockedUntil = newFailedCount >= maxAttempts ? DateTime.Now.AddMinutes(lockoutMinutes) : (DateTime?)null;

        user.FailedLoginCount = newFailedCount;
        user.LockedUntilAt = newLockedUntil;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetFailedLoginCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.UserMasters
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user != null)
        {
            user.FailedLoginCount = 0;
            user.LockedUntilAt = null;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
