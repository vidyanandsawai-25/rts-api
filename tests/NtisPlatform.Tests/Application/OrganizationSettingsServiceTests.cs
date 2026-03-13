using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class OrganizationSettingsServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly OrganizationSettingsService _service;
    private readonly IMemoryCache _memoryCache;

    public OrganizationSettingsServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _service = new OrganizationSettingsService(_context, _memoryCache);
    }

    [Fact]
    public async Task GetAllSettingsAsync_ReturnsAllSettings()
    {
        _context.OrganizationSettings.AddRange(new[]
        {
            new OrganizationSetting { Key = "A.K1", Value = "v1", Category = "Cat1", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now },
            new OrganizationSetting { Key = "B.K2", Value = "v2", Category = "Cat2", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now }
        });
        await _context.SaveChangesAsync();

        var all = await _service.GetAllSettingsAsync(CancellationToken.None);
        Assert.Equal(2, all.Count);
        Assert.Equal("v1", all["A.K1"]);
        Assert.Equal("v2", all["B.K2"]);

        // Calling again should still return the same values (cached)
        var all2 = await _service.GetAllSettingsAsync(CancellationToken.None);
        Assert.Equal(all.Count, all2.Count);
    }

    [Fact]
    public async Task GetSettingsByCategoryAsync_ReturnsCategorySettings()
    {
        _context.OrganizationSettings.AddRange(new[]
        {
            new OrganizationSetting { Key = "Branding.Logo", Value = "logo.png", Category = "Branding", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now },
            new OrganizationSetting { Key = "Security.Policy", Value = "strict", Category = "Security", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now }
        });
        await _context.SaveChangesAsync();

        var branding = await _service.GetSettingsByCategoryAsync("Branding", CancellationToken.None);
        Assert.Single(branding);
        Assert.Equal("logo.png", branding["Branding.Logo"]);
    }

    [Fact]
    public async Task GetSettingAsync_ReturnsValueAnd_SettingUpdateReflects()
    {
        _context.OrganizationSettings.Add(new OrganizationSetting { Key = "Test.Key", Value = "v1", Category = "Other", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now });
        await _context.SaveChangesAsync();

        var val = await _service.GetSettingAsync("Test.Key", CancellationToken.None);
        Assert.Equal("v1", val);

        // Update via service
        await _service.SetSettingAsync("Test.Key", "v2", CancellationToken.None);

        var val2 = await _service.GetSettingAsync("Test.Key", CancellationToken.None);
        Assert.Equal("v2", val2);
    }

    [Fact]
    public async Task SetSettingAsync_ThrowsWhenMissing()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.SetSettingAsync("Missing.Key", "x", CancellationToken.None)
        );
    }

    [Fact]
    public async Task GetSettingAsync_Generic_ParsesSimpleTypes()
    {
        _context.OrganizationSettings.AddRange(new[]
        {
            new OrganizationSetting { Key = "Bool.Key", Value = "true", Category = "Other", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now },
            new OrganizationSetting { Key = "Int.Key", Value = "42", Category = "Other", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now },
            new OrganizationSetting { Key = "Date.Key", Value = "2020-01-02T03:04:05", Category = "Other", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now }
        });
        await _context.SaveChangesAsync();

        var b = await _service.GetSettingAsync<bool>("Bool.Key", default, CancellationToken.None);
        Assert.True(b);

        var i = await _service.GetSettingAsync<int>("Int.Key", default, CancellationToken.None);
        Assert.Equal(42, i);

        var dt = await _service.GetSettingAsync<DateTime>("Date.Key", default, CancellationToken.None);
        Assert.Equal(new DateTime(2020,1,2,3,4,5), dt);
    }

    [Fact]
    public async Task UpdateSettingsAsync_UpdatesMultipleAndInvalidatesCache()
    {
        _context.OrganizationSettings.AddRange(new[]
        {
            new OrganizationSetting { Key = "U.A", Value = "1", Category = "Other", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now },
            new OrganizationSetting { Key = "U.B", Value = "2", Category = "Other", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now }
        });
        await _context.SaveChangesAsync();

        // Prime cache for one key
        var primed = await _service.GetSettingAsync("U.A", CancellationToken.None);
        Assert.Equal("1", primed);

        await _service.UpdateSettingsAsync(new Dictionary<string, string?>
        {
            ["U.A"] = "10",
            ["U.B"] = "20"
        }, CancellationToken.None);

        var a = await _service.GetSettingAsync("U.A", CancellationToken.None);
        var b = await _service.GetSettingAsync("U.B", CancellationToken.None);

        Assert.Equal("10", a);
        Assert.Equal("20", b);
    }

    [Fact]
    public async Task GetSettingEntityAsync_ReturnsEntity()
    {
        _context.OrganizationSettings.Add(new OrganizationSetting { Key = "E.Key", Value = "val", Category = "Other", CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now });
        await _context.SaveChangesAsync();

        var ent = await _service.GetSettingEntityAsync("E.Key", CancellationToken.None);
        Assert.NotNull(ent);
        Assert.Equal("val", ent!.Value);
    }

    public void Dispose()
    {
        _memoryCache?.Dispose();
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
    }
}
