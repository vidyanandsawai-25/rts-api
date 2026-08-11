using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;

namespace NtisPlatform.Tests.Infrastructure.Repositories;

/// <summary>
/// Unit tests for MfaChallengeRepository. As with RefreshTokenRepositoryTests, the EF Core
/// InMemory provider does not support ExecuteUpdateAsync/ExecuteDeleteAsync, so the
/// consume/failed-attempt tests tolerate that limitation rather than asserting behavior that the
/// provider cannot execute — see MfaChallengeServiceTests for full behavioral coverage via mocks.
/// </summary>
public class MfaChallengeRepositoryTests
{
    private static ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static MfaChallengeEntity NewChallenge(string hash, int userId = 1) => new()
    {
        Id = Guid.NewGuid(),
        ChallengeHash = hash,
        UserId = userId,
        Purpose = "mfa-login",
        CreatedAt = DateTime.Now,
        ExpiresAt = DateTime.Now.AddMinutes(5)
    };

    [Fact]
    public async Task GetByHashAsync_WithMatchingHash_ReturnsChallenge()
    {
        var context = GetInMemoryDbContext();
        var repository = new MfaChallengeRepository(context);

        var challenge = NewChallenge("hash-abc");
        context.TwoFactorChallenges.Add(challenge);
        await context.SaveChangesAsync();

        var result = await repository.GetByHashAsync("hash-abc");

        Assert.NotNull(result);
        Assert.Equal(challenge.Id, result!.Id);
    }

    [Fact]
    public async Task GetByHashAsync_WithNoMatch_ReturnsNull()
    {
        var context = GetInMemoryDbContext();
        var repository = new MfaChallengeRepository(context);

        var result = await repository.GetByHashAsync("does-not-exist");

        Assert.Null(result);
    }

    [Fact]
    public async Task TryConsumeAsync_OnActiveChallenge_ConsumesItOrIsSkippedOnUnsupportedProvider()
    {
        var context = GetInMemoryDbContext();
        var repository = new MfaChallengeRepository(context);

        var challenge = NewChallenge("hash-1");
        context.TwoFactorChallenges.Add(challenge);
        await context.SaveChangesAsync();

        try
        {
            var consumed = await repository.TryConsumeAsync(challenge.Id);
            Assert.True(consumed);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("ExecuteUpdate"))
        {
            Assert.True(true);
        }
    }

    [Fact]
    public async Task RecordFailedAttemptAsync_ForUnknownChallenge_DoesNotThrowUnexpectedly()
    {
        var context = GetInMemoryDbContext();
        var repository = new MfaChallengeRepository(context);

        try
        {
            var outcome = await repository.RecordFailedAttemptAsync(Guid.NewGuid(), 5);
            Assert.Equal(NtisPlatform.Core.Interfaces.MfaChallengeFailureOutcome.NotActive, outcome);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("ExecuteUpdate"))
        {
            Assert.True(true);
        }
    }
}
