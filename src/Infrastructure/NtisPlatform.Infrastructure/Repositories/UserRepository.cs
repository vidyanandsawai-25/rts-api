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
        // Case-insensitive comparison using ToUpper() on both sides
        // This approach:
        // - Gets translated to SQL UPPER() comparison by EF Core
        // - Works consistently across all database providers
        // Note: ToUpperInvariant() cannot be translated by EF Core
        return await _context.UserMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName.ToUpper() == username.ToUpper(), cancellationToken);
    }

    public async Task<UserEntity?> GetByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default)
    {
        var normalized = usernameOrEmail.ToUpper();
        return await _context.UserMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.IsActive &&
                (u.UserName.ToUpper() == normalized || (u.Email != null && u.Email.ToUpper() == normalized)),
                cancellationToken);
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

    public async Task<FailedLoginIncrementResult> IncrementFailedLoginCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Get lockout policy from security settings
        var maxAttempts = await _securitySettings.GetAsync<int>("MAXFAILEDATTEMPTS", 5, cancellationToken);
        var lockoutMinutes = await _securitySettings.GetAsync<int>("LOCKOUTDURATIONMINUTES", 30, cancellationToken);

        // Fetch user to update
        var user = await _context.UserMasters
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null) return new FailedLoginIncrementResult(0, null);

        var newFailedCount = (user.FailedLoginCount ?? 0) + 1;
        var newLockedUntil = newFailedCount >= maxAttempts ? DateTime.Now.AddMinutes(lockoutMinutes) : (DateTime?)null;

        user.FailedLoginCount = newFailedCount;
        user.LockedUntilAt = newLockedUntil;
        await _context.SaveChangesAsync(cancellationToken);

        return new FailedLoginIncrementResult(Math.Max(0, maxAttempts - newFailedCount), newLockedUntil);
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

    public async Task SetPendingTwoFactorSecretAsync(int userId, string encryptedSecret, CancellationToken cancellationToken = default)
    {
        var user = await _context.UserMasters
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null) return;

        user.TwoFactorSecretEncrypted = encryptedSecret;
        user.TwoFactorEnabled = false;
        user.TwoFactorEnabledAt = null;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> EnableTwoFactorAsync(int userId, string newSecurityStamp, CancellationToken cancellationToken = default)
    {
        var user = await _context.UserMasters
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null) return false;

        user.TwoFactorEnabled = true;
        user.TwoFactorEnabledAt = DateTime.Now;
        user.SecurityStamp = newSecurityStamp;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DisableTwoFactorAsync(int userId, string newSecurityStamp, CancellationToken cancellationToken = default)
    {
        var user = await _context.UserMasters
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null) return false;

        user.TwoFactorEnabled = false;
        user.TwoFactorEnabledAt = null;
        user.TwoFactorSecretEncrypted = null;
        user.SecurityStamp = newSecurityStamp;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task UpdateSecurityStampAsync(int userId, string newSecurityStamp, CancellationToken cancellationToken = default)
    {
        var user = await _context.UserMasters
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null) return;

        user.SecurityStamp = newSecurityStamp;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> GetSecurityStampAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserMasters
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.SecurityStamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ResetPasswordAsync(int userId, string newPasswordHash, string newSecurityStamp, CancellationToken cancellationToken = default)
    {
        var user = await _context.UserMasters
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null) return false;

        user.PasswordHash = newPasswordHash;
        user.MustChangePassword = false;
        user.PasswordChangedAt = DateTime.Now;
        user.SecurityStamp = newSecurityStamp;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task IncrementOtpChallengeLockoutAsync(int userId, CancellationToken cancellationToken = default)
    {
        var maxLockouts = await _securitySettings.GetAsync<int>("MAXOTPCHALLENGELOCKOUTS", 3, cancellationToken);
        var lockoutMinutes = await _securitySettings.GetAsync<int>("OTPCHALLENGELOCKOUTDURATIONMINUTES", 15, cancellationToken);
        var now = DateTime.Now;

        await _context.UserMasters
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(u => u.OtpChallengeFailCount, u => (u.OtpChallengeFailCount ?? 0) + 1)
                .SetProperty(u => u.OtpChallengeLockedUntilAt, u =>
                    (u.OtpChallengeFailCount ?? 0) + 1 >= maxLockouts ? now.AddMinutes(lockoutMinutes) : u.OtpChallengeLockedUntilAt),
                cancellationToken);
    }

    public async Task ResetOtpChallengeLockoutAsync(int userId, CancellationToken cancellationToken = default)
    {
        await _context.UserMasters
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(u => u.OtpChallengeFailCount, 0)
                .SetProperty(u => u.OtpChallengeLockedUntilAt, (DateTime?)null),
                cancellationToken);
    }
}
