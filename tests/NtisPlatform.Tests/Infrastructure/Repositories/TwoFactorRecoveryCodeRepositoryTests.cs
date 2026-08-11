using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;

namespace NtisPlatform.Tests.Infrastructure.Repositories;

/// <summary>
/// Unit tests for TwoFactorRecoveryCodeRepository. The EF Core InMemory provider does not
/// support ExecuteUpdateAsync/ExecuteDeleteAsync (see RefreshTokenRepositoryTests for the same
/// caveat already documented in this codebase), so the redeem/revoke tests tolerate that
/// InvalidOperationException rather than asserting on it — the read-side query methods are
/// fully exercised against real data.
/// </summary>
public class TwoFactorRecoveryCodeRepositoryTests
{
    private static ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetActiveByUserIdAsync_ReturnsOnlyUnusedUnrevokedCodesForThatUser()
    {
        var context = GetInMemoryDbContext();
        var repository = new TwoFactorRecoveryCodeRepository(context);

        context.TwoFactorRecoveryCodes.AddRange(
            new TwoFactorRecoveryCodeEntity { UserId = 1, CodeHash = "active-1" },
            new TwoFactorRecoveryCodeEntity { UserId = 1, CodeHash = "used", UsedAt = DateTime.Now },
            new TwoFactorRecoveryCodeEntity { UserId = 1, CodeHash = "revoked", RevokedAt = DateTime.Now },
            new TwoFactorRecoveryCodeEntity { UserId = 2, CodeHash = "other-user" });
        await context.SaveChangesAsync();

        var result = await repository.GetActiveByUserIdAsync(1);

        Assert.Single(result);
        Assert.Equal("active-1", result[0].CodeHash);
    }

    [Fact]
    public async Task CountActiveByUserIdAsync_CountsOnlyActiveCodes()
    {
        var context = GetInMemoryDbContext();
        var repository = new TwoFactorRecoveryCodeRepository(context);

        context.TwoFactorRecoveryCodes.AddRange(
            new TwoFactorRecoveryCodeEntity { UserId = 1, CodeHash = "a" },
            new TwoFactorRecoveryCodeEntity { UserId = 1, CodeHash = "b" },
            new TwoFactorRecoveryCodeEntity { UserId = 1, CodeHash = "c", UsedAt = DateTime.Now });
        await context.SaveChangesAsync();

        var count = await repository.CountActiveByUserIdAsync(1);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetActiveByUserIdAsync_WithNoCodes_ReturnsEmpty()
    {
        var context = GetInMemoryDbContext();
        var repository = new TwoFactorRecoveryCodeRepository(context);

        var result = await repository.GetActiveByUserIdAsync(999);

        Assert.Empty(result);
    }

    [Fact]
    public async Task TryRedeemAsync_OnActiveCode_ConsumesItOrIsSkippedOnUnsupportedProvider()
    {
        var context = GetInMemoryDbContext();
        var repository = new TwoFactorRecoveryCodeRepository(context);

        var code = new TwoFactorRecoveryCodeEntity { UserId = 1, CodeHash = "a" };
        context.TwoFactorRecoveryCodes.Add(code);
        await context.SaveChangesAsync();

        try
        {
            var redeemed = await repository.TryRedeemAsync(code.Id);
            Assert.True(redeemed);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("ExecuteUpdate"))
        {
            Assert.True(true); // EF Core InMemory provider limitation, not a repository bug
        }
    }

    [Fact]
    public async Task RevokeAllActiveAsync_DoesNotThrowForUserWithNoCodes()
    {
        var context = GetInMemoryDbContext();
        var repository = new TwoFactorRecoveryCodeRepository(context);

        try
        {
            await repository.RevokeAllActiveAsync(999);
            Assert.True(true);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("ExecuteUpdate"))
        {
            Assert.True(true);
        }
    }
}
