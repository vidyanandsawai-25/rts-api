using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.Options;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services.Localization;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services.Localization;

public class LocalizedQueryServiceTests
{
    private static IDbContextFactory<ApplicationDbContext> CreateFactory()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<ApplicationDbContext>(opts =>
            opts.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        return services.BuildServiceProvider().GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    }

    private static async Task SeedAsync(IDbContextFactory<ApplicationDbContext> factory, params MultilingualResourceEntity[] entities)
    {
        await using var ctx = await factory.CreateDbContextAsync();
        ctx.MultilingualResourceEntity.AddRange(entities);
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task DefaultConstructor_UsesEnglishAsDefault()
    {
        var factory = CreateFactory();
        var service = new LocalizedQueryService(factory);

        await SeedAsync(factory, new MultilingualResourceEntity
        {
            Resource = "R", Key = "K", en_US = "English", IsActive = true
        });

        var result = await service.GetLocalizedValueAsync("R", "K", "en");
        Assert.Equal("English", result);
    }

    [Fact]
    public async Task GetLocalizedValueAsync_ReturnsRequestedLanguage()
    {
        var factory = CreateFactory();
        var service = new LocalizedQueryService(factory, Options.Create(new LocalizationOptions { DefaultLanguage = "en" }));

        await SeedAsync(factory, new MultilingualResourceEntity
        {
            Resource = "R", Key = "K1", en_US = "Hello", hi_IN = "नमस्ते", mr_IN = "नमस्कार", IsActive = true
        });

        Assert.Equal("Hello", await service.GetLocalizedValueAsync("R", "K1", "en"));
        Assert.Equal("नमस्ते", await service.GetLocalizedValueAsync("R", "K1", "hi"));
        Assert.Equal("नमस्कार", await service.GetLocalizedValueAsync("R", "K1", "mr"));
    }

    [Fact]
    public async Task GetLocalizedValueAsync_FallsBackToDefaultLanguage()
    {
        var factory = CreateFactory();
        var service = new LocalizedQueryService(factory, Options.Create(new LocalizationOptions { DefaultLanguage = "en" }));

        await SeedAsync(factory, new MultilingualResourceEntity
        {
            Resource = "R", Key = "K1", en_US = "Hello", hi_IN = "", mr_IN = "", IsActive = true
        });

        var result = await service.GetLocalizedValueAsync("R", "K1", "hi");
        Assert.Equal("Hello", result);
    }

    [Fact]
    public async Task GetLocalizedValueAsync_ReturnsNull_WhenEntityMissing()
    {
        var factory = CreateFactory();
        var service = new LocalizedQueryService(factory);

        var result = await service.GetLocalizedValueAsync("R", "Missing", "en");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLocalizedValueAsync_ReturnsNull_WhenInactive()
    {
        var factory = CreateFactory();
        var service = new LocalizedQueryService(factory);
        await SeedAsync(factory, new MultilingualResourceEntity
        {
            Resource = "R", Key = "K1", en_US = "Hello", IsActive = false
        });

        var result = await service.GetLocalizedValueAsync("R", "K1", "en");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLocalizedValuesAsync_ReturnsEmpty_ForEmptyKeyList()
    {
        var factory = CreateFactory();
        var service = new LocalizedQueryService(factory);

        var result = await service.GetLocalizedValuesAsync("R", Array.Empty<string>(), "en");
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLocalizedValuesAsync_ReturnsMappedDictionary()
    {
        var factory = CreateFactory();
        var service = new LocalizedQueryService(factory);
        await SeedAsync(factory,
            new MultilingualResourceEntity { Resource = "R", Key = "A", en_US = "Apple", IsActive = true },
            new MultilingualResourceEntity { Resource = "R", Key = "B", en_US = "Banana", IsActive = true });

        var result = await service.GetLocalizedValuesAsync("R", new[] { "A", "B", "MissingKey" }, "en");

        Assert.Equal(2, result.Count);
        Assert.Equal("Apple", result["A"]);
        Assert.Equal("Banana", result["B"]);
    }

    [Fact]
    public async Task GetLocalizedValuesAsync_ReturnsEmptyValue_WhenStored()
    {
        var factory = CreateFactory();
        var service = new LocalizedQueryService(factory);
        await SeedAsync(factory,
            new MultilingualResourceEntity { Resource = "R", Key = "A", en_US = "", IsActive = true });

        var result = await service.GetLocalizedValuesAsync("R", new[] { "A" }, "en");

        Assert.True(result.ContainsKey("A"));
    }

    [Fact]
    public async Task SearchLocalizedKeysAsync_ReturnsEmpty_ForBlankSearch()
    {
        var factory = CreateFactory();
        var service = new LocalizedQueryService(factory);

        var result = await service.SearchLocalizedKeysAsync("R", "  ", "en");
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("hi")]
    [InlineData("mr")]
    [InlineData("unknown")]
    public async Task GetLocalizedValueAsync_HandlesAllLanguages(string language)
    {
        var factory = CreateFactory();
        var service = new LocalizedQueryService(factory);
        await SeedAsync(factory, new MultilingualResourceEntity
        {
            Resource = "R", Key = "K", en_US = "E", hi_IN = "H", mr_IN = "M", IsActive = true
        });

        var result = await service.GetLocalizedValueAsync("R", "K", language);
        Assert.NotNull(result);
    }
}
