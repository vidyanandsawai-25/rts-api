using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;

namespace NtisPlatform.Tests.Infrastructure.Repositories;

/// <summary>
/// Unit tests for SecurityAuditLogRepository — a thin append-only wrapper.
/// </summary>
public class SecurityAuditLogRepositoryTests
{
    private static ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddAsync_PersistsEntryWithoutSensitiveFields()
    {
        var context = GetInMemoryDbContext();
        var repository = new SecurityAuditLogRepository(context);

        var entry = new SecurityAuditLogEntity
        {
            EventType = "TwoFactorEnabled",
            UserId = 1,
            Success = true,
            CreatedAt = DateTime.Now
        };

        await repository.AddAsync(entry);

        var saved = await context.SecurityAuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal("TwoFactorEnabled", saved!.EventType);
        Assert.True(saved.Success);
        Assert.True(saved.Id > 0);
    }

    [Fact]
    public async Task AddAsync_MultipleEntries_AreAllPersisted()
    {
        var context = GetInMemoryDbContext();
        var repository = new SecurityAuditLogRepository(context);

        await repository.AddAsync(new SecurityAuditLogEntity { EventType = "A", Success = true, CreatedAt = DateTime.Now });
        await repository.AddAsync(new SecurityAuditLogEntity { EventType = "B", Success = false, CreatedAt = DateTime.Now });

        var count = await context.SecurityAuditLogs.CountAsync();
        Assert.Equal(2, count);
    }
}
