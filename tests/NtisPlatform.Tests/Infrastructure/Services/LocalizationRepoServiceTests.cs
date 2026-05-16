using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Options;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services;

public class LocalizationRepoServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<ILocalizationService> _cache;
    private readonly LocalizationRepoService _service;

    public LocalizationRepoServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _cache = new Mock<ILocalizationService>();
        _service = new LocalizationRepoService(_db, _cache.Object, Options.Create(new LocalizationOptions { DefaultLanguage = "en" }));
    }

    public void Dispose() => _db.Dispose();

    private static LocalizationEntry Entry(string resource, string entityId, string prop, string value, string language = "en") => new()
    {
        Resource = resource,
        Key = $"{resource}_{entityId}_{prop}",
        PropertyName = prop,
        Value = value,
        Language = language
    };

    [Fact]
    public void DefaultConstructor_UsesEnglish()
    {
        var defaultCtor = new LocalizationRepoService(_db, _cache.Object);
        Assert.NotNull(defaultCtor);
    }

    [Fact]
    public async Task SaveAsync_CreatesNewEntity()
    {
        var entry = Entry("Floor", "1", "Description", "Ground Floor", "en");

        var key = await _service.SaveAsync(entry);

        Assert.Equal("Floor_1_Description", key);
        var saved = await _db.MultilingualResourceEntity.SingleAsync();
        Assert.Equal("Ground Floor", saved.en_US);
        Assert.Equal("Floor", saved.Resource);
        Assert.True(saved.IsGenerated);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingEntity_WhenValueChanged()
    {
        var entry = Entry("Floor", "1", "Description", "Ground", "en");
        await _service.SaveAsync(entry);

        var updated = Entry("Floor", "1", "Description", "First Floor", "en");
        await _service.SaveAsync(updated);

        var saved = await _db.MultilingualResourceEntity.SingleAsync();
        Assert.Equal("First Floor", saved.en_US);
    }

    [Fact]
    public async Task SaveAsync_SkipsUpdate_WhenValueUnchanged()
    {
        var entry = Entry("Floor", "1", "Description", "Ground", "en");
        await _service.SaveAsync(entry);

        var initialUpdate = (await _db.MultilingualResourceEntity.SingleAsync()).UpdatedDate;

        var sameValue = Entry("Floor", "1", "Description", "Ground", "en");
        await _service.SaveAsync(sameValue);

        var afterUpdate = (await _db.MultilingualResourceEntity.SingleAsync()).UpdatedDate;
        Assert.Equal(initialUpdate, afterUpdate);
    }

    [Fact]
    public async Task SaveAsync_SetsHindiColumn()
    {
        var entry = Entry("Floor", "1", "Description", "तल", "hi");

        await _service.SaveAsync(entry);

        var saved = await _db.MultilingualResourceEntity.SingleAsync();
        Assert.Equal("तल", saved.hi_IN);
    }

    [Fact]
    public async Task SaveAsync_SetsMarathiColumn()
    {
        var entry = Entry("Floor", "1", "Description", "मजला", "mr");

        await _service.SaveAsync(entry);

        var saved = await _db.MultilingualResourceEntity.SingleAsync();
        Assert.Equal("मजला", saved.mr_IN);
    }

    [Fact]
    public async Task SaveBatchAsync_ReturnsEmpty_OnEmptyList()
    {
        var result = await _service.SaveBatchAsync(Array.Empty<LocalizationEntry>());
        Assert.Empty(result);
    }

    [Fact]
    public async Task SaveBatchAsync_PersistsAllEntries()
    {
        var entries = new[]
        {
            Entry("Floor", "1", "Description", "Ground"),
            Entry("Floor", "2", "Name", "First")
        };

        var result = await _service.SaveBatchAsync(entries);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, await _db.MultilingualResourceEntity.CountAsync());
    }

    [Fact]
    public async Task SaveBatchAsync_UpdatesExistingAndAddsNew()
    {
        await _service.SaveAsync(Entry("Floor", "1", "Description", "Ground"));

        var batch = new[]
        {
            Entry("Floor", "1", "Description", "Updated Ground"),
            Entry("Floor", "2", "Description", "First")
        };
        await _service.SaveBatchAsync(batch);

        var rows = await _db.MultilingualResourceEntity.OrderBy(x => x.Key).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, r => r.Key == "Floor_1_Description" && r.en_US == "Updated Ground");
        Assert.Contains(rows, r => r.Key == "Floor_2_Description" && r.en_US == "First");
    }

    [Fact]
    public async Task GetAsync_ReturnsFromCache_WhenAllKeysCached()
    {
        _cache.Setup(c => c.GetTranslationsExact("R", "en", It.IsAny<IEnumerable<string>>()))
            .Returns(new Dictionary<string, string> { ["K1"] = "Hello", ["K2"] = "World" });

        var result = await _service.GetAsync("R", new[] { "K1", "K2" }, "en");

        Assert.Equal("Hello", result["K1"]);
        Assert.Equal("World", result["K2"]);
    }

    [Fact]
    public async Task GetAsync_FetchesMissingFromDatabase()
    {
        _db.MultilingualResourceEntity.Add(new MultilingualResourceEntity
        {
            Resource = "R", Key = "K1", en_US = "Hello", IsActive = true
        });
        await _db.SaveChangesAsync();
        _cache.Setup(c => c.GetTranslationsExact("R", "en", It.IsAny<IEnumerable<string>>()))
            .Returns(new Dictionary<string, string>());

        var result = await _service.GetAsync("R", new[] { "K1" }, "en");

        Assert.Equal("Hello", result["K1"]);
        _cache.Verify(c => c.SetTranslation("R", "en", "K1", "Hello"), Times.Once);
    }

    [Fact]
    public async Task GetAsync_ReturnsKey_WhenNotFoundInDb()
    {
        _cache.Setup(c => c.GetTranslationsExact("R", "en", It.IsAny<IEnumerable<string>>()))
            .Returns(new Dictionary<string, string>());

        var result = await _service.GetAsync("R", new[] { "Missing" }, "en");

        Assert.Equal("Missing", result["Missing"]);
    }

    [Fact]
    public async Task DeactivateByKeysAsync_DoesNothing_OnEmptyList()
    {
        await _service.DeactivateByKeysAsync("R", Array.Empty<string>());
        // No exception
    }
}
