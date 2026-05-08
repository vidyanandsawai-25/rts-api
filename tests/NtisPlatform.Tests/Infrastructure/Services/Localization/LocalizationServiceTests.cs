using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services.Localization;

namespace NtisPlatform.Tests.Infrastructure.Services.Localization;

/// <summary>
/// Comprehensive unit tests for LocalizationService
/// </summary>
public class LocalizationServiceTests
{
    private IDbContextFactory<ApplicationDbContext> CreateDbContextFactory()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    }

    #region GetTranslation Tests

    [Fact]
    public async Task GetTranslation_WithCachedKey_ReturnsTranslation()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        await using (var context = await factory.CreateDbContextAsync())
        {
            var resource = new MultilingualResourceEntity
            {
                Resource = "ValidationMessages",
                Key = "RequiredField",
                en_US = "Field is required",
                hi_IN = "?????? ?????? ??",
                mr_IN = "????? ?????? ???",
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now
            };
            context.MultilingualResourceEntity.Add(resource);
            await context.SaveChangesAsync();
        }

        var service = new LocalizationService(factory);
        await service.ReloadAsync();

        // Act
        var result = service.GetTranslation("ValidationMessages", "en", "RequiredField");

        // Assert
        Assert.Equal("Field is required", result);
    }

    [Fact]
    public async Task GetTranslation_WithHindiLanguage_ReturnsHindiTranslation()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        await using (var context = await factory.CreateDbContextAsync())
        {
            var resource = new MultilingualResourceEntity
            {
                Resource = "ValidationMessages",
                Key = "RequiredField",
                en_US = "Field is required",
                hi_IN = "?????? ?????? ??",
                mr_IN = "????? ?????? ???",
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now
            };
            context.MultilingualResourceEntity.Add(resource);
            await context.SaveChangesAsync();
        }

        var service = new LocalizationService(factory);
        await service.ReloadAsync();

        // Act
        var result = service.GetTranslation("ValidationMessages", "hi", "RequiredField");

        // Assert
        Assert.Equal("?????? ?????? ??", result);
    }

    [Fact]
    public async Task GetTranslation_WithMarathiLanguage_ReturnsMarathiTranslation()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        await using (var context = await factory.CreateDbContextAsync())
        {
            var resource = new MultilingualResourceEntity
            {
                Resource = "ValidationMessages",
                Key = "RequiredField",
                en_US = "Field is required",
                hi_IN = "?????? ?????? ??",
                mr_IN = "????? ?????? ???",
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now
            };
            context.MultilingualResourceEntity.Add(resource);
            await context.SaveChangesAsync();
        }

        var service = new LocalizationService(factory);
        await service.ReloadAsync();

        // Act
        var result = service.GetTranslation("ValidationMessages", "mr", "RequiredField");

        // Assert
        Assert.Equal("????? ?????? ???", result);
    }

    [Fact]
    public async Task GetTranslation_WithMissingKey_ReturnsKey()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        var service = new LocalizationService(factory);
        await service.ReloadAsync();

        // Act
        var result = service.GetTranslation("ValidationMessages", "en", "NonExistentKey");

        // Assert
        Assert.Equal("NonExistentKey", result);
    }

    [Fact]
    public async Task GetTranslation_WithMissingTranslationInLanguage_FallsBackToEnglish()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        await using (var context = await factory.CreateDbContextAsync())
        {
            var resource = new MultilingualResourceEntity
            {
                Resource = "ValidationMessages",
                Key = "RequiredField",
                en_US = "Field is required",
                hi_IN = string.Empty, // Empty instead of null
                mr_IN = string.Empty, // Empty instead of null
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now
            };
            context.MultilingualResourceEntity.Add(resource);
            await context.SaveChangesAsync();
        }

        var service = new LocalizationService(factory);
        await service.ReloadAsync();

        // Act
        var result = service.GetTranslation("ValidationMessages", "hi", "RequiredField");

        // Assert
        Assert.Equal("Field is required", result); // Falls back to English
    }

    [Fact]
    public async Task GetTranslation_WithLanguageCode_WithDash_NormalizesCorrectly()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        await using (var context = await factory.CreateDbContextAsync())
        {
            var resource = new MultilingualResourceEntity
            {
                Resource = "ValidationMessages",
                Key = "RequiredField",
                en_US = "Field is required",
                hi_IN = "?????? ?????? ??",
                mr_IN = "????? ?????? ???",
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now
            };
            context.MultilingualResourceEntity.Add(resource);
            await context.SaveChangesAsync();
        }

        var service = new LocalizationService(factory);
        await service.ReloadAsync();

        // Act
        var result = service.GetTranslation("ValidationMessages", "hi-IN", "RequiredField");

        // Assert
        Assert.Equal("?????? ?????? ??", result);
    }

    [Fact]
    public async Task GetTranslation_CaseInsensitive_Resource()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        await using (var context = await factory.CreateDbContextAsync())
        {
            var resource = new MultilingualResourceEntity
            {
                Resource = "ValidationMessages",
                Key = "RequiredField",
                en_US = "Field is required",
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now
            };
            context.MultilingualResourceEntity.Add(resource);
            await context.SaveChangesAsync();
        }

        var service = new LocalizationService(factory);
        await service.ReloadAsync();

        // Act
        var result = service.GetTranslation("validationmessages", "en", "RequiredField");

        // Assert
        Assert.Equal("Field is required", result);
    }

    #endregion

    #region GetTranslations Tests

    [Fact]
    public async Task GetTranslations_WithMultipleKeys_ReturnsAllTranslations()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        await using (var context = await factory.CreateDbContextAsync())
        {
            context.MultilingualResourceEntity.AddRange(
                new MultilingualResourceEntity
                {
                    Resource = "ValidationMessages",
                    Key = "RequiredField",
                    en_US = "Field is required",
                    IsActive = true,
                    CreatedBy = 1,
                    CreatedDate = DateTime.Now
                },
                new MultilingualResourceEntity
                {
                    Resource = "ValidationMessages",
                    Key = "InvalidEmail",
                    en_US = "Invalid email",
                    IsActive = true,
                    CreatedBy = 1,
                    CreatedDate = DateTime.Now
                }
            );
            await context.SaveChangesAsync();
        }

        var service = new LocalizationService(factory);
        await service.ReloadAsync();

        // Act
        var result = service.GetTranslations("ValidationMessages", "en", new[] { "RequiredField", "InvalidEmail" });

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Field is required", result["RequiredField"]);
        Assert.Equal("Invalid email", result["InvalidEmail"]);
    }

    [Fact]
    public async Task GetTranslations_WithMissingKeys_ReturnsKeyAsValue()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        var service = new LocalizationService(factory);
        await service.ReloadAsync();

        // Act
        var result = service.GetTranslations("ValidationMessages", "en", new[] { "NonExistentKey1", "NonExistentKey2" });

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("NonExistentKey1", result["NonExistentKey1"]);
        Assert.Equal("NonExistentKey2", result["NonExistentKey2"]);
    }

    [Fact]
    public async Task GetTranslations_WithMixedExistingAndMissing_ReturnsCorrectValues()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        await using (var context = await factory.CreateDbContextAsync())
        {
            var resource = new MultilingualResourceEntity
            {
                Resource = "ValidationMessages",
                Key = "RequiredField",
                en_US = "Field is required",
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now
            };
            context.MultilingualResourceEntity.Add(resource);
            await context.SaveChangesAsync();
        }

        var service = new LocalizationService(factory);
        await service.ReloadAsync();

        // Act
        var result = service.GetTranslations("ValidationMessages", "en", new[] { "RequiredField", "NonExistentKey" });

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Field is required", result["RequiredField"]);
        Assert.Equal("NonExistentKey", result["NonExistentKey"]);
    }

    #endregion

    #region TryGetTranslation Tests

    [Fact]
    public async Task TryGetTranslation_WithExistingKey_ReturnsTrueAndValue()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        await using (var context = await factory.CreateDbContextAsync())
        {
            var resource = new MultilingualResourceEntity
            {
                Resource = "ValidationMessages",
                Key = "RequiredField",
                en_US = "Field is required",
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now
            };
            context.MultilingualResourceEntity.Add(resource);
            await context.SaveChangesAsync();
        }

        var service = new LocalizationService(factory);
        await service.ReloadAsync();

        // Act
        var result = service.TryGetTranslation("ValidationMessages", "en", "RequiredField", out var value);

        // Assert
        Assert.True(result);
        Assert.Equal("Field is required", value);
    }

    [Fact]
    public async Task TryGetTranslation_WithNonExistingKey_ReturnsFalse()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        var service = new LocalizationService(factory);
        await service.ReloadAsync();

        // Act
        var result = service.TryGetTranslation("ValidationMessages", "en", "NonExistentKey", out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public async Task TryGetTranslation_WithNonExistingResource_ReturnsFalse()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        var service = new LocalizationService(factory);
        await service.ReloadAsync();

        // Act
        var result = service.TryGetTranslation("NonExistentResource", "en", "SomeKey", out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }

    #endregion

    #region GetTranslationsExact Tests

    [Fact]
    public async Task GetTranslationsExact_WithExistingKeys_ReturnsOnlyExistingTranslations()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        await using (var context = await factory.CreateDbContextAsync())
        {
            var resource = new MultilingualResourceEntity
            {
                Resource = "ValidationMessages",
                Key = "RequiredField",
                en_US = "Field is required",
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now
            };
            context.MultilingualResourceEntity.Add(resource);
            await context.SaveChangesAsync();
        }

        var service = new LocalizationService(factory);
        await service.ReloadAsync();

        // Act
        var result = service.GetTranslationsExact("ValidationMessages", "en", new[] { "RequiredField", "NonExistentKey" });

        // Assert
        Assert.Single(result);
        Assert.Equal("Field is required", result["RequiredField"]);
        Assert.False(result.ContainsKey("NonExistentKey"));
    }

    [Fact]
    public async Task GetTranslationsExact_WithNonCachedLanguage_ReturnsEmpty()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        var service = new LocalizationService(factory);
        await service.ReloadAsync();

        // Act
        var result = service.GetTranslationsExact("ValidationMessages", "en", new[] { "SomeKey" });

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region SetTranslation Tests

    [Fact]
    public void SetTranslation_CreatesNewBucket()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        var service = new LocalizationService(factory);

        // Act
        service.SetTranslation("ValidationMessages", "en", "RequiredField", "Field is required");

        // Assert
        var result = service.GetTranslation("ValidationMessages", "en", "RequiredField");
        Assert.Equal("Field is required", result);
    }

    [Fact]
    public void SetTranslation_UpdatesExistingKey()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        var service = new LocalizationService(factory);
        service.SetTranslation("ValidationMessages", "en", "RequiredField", "Field is required");

        // Act
        service.SetTranslation("ValidationMessages", "en", "RequiredField", "Updated value");

        // Assert
        var result = service.GetTranslation("ValidationMessages", "en", "RequiredField");
        Assert.Equal("Updated value", result);
    }

    [Fact]
    public void SetTranslation_AddsNewKeyToExistingBucket()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        var service = new LocalizationService(factory);
        service.SetTranslation("ValidationMessages", "en", "RequiredField", "Field is required");

        // Act
        service.SetTranslation("ValidationMessages", "en", "InvalidEmail", "Invalid email");

        // Assert
        var result1 = service.GetTranslation("ValidationMessages", "en", "RequiredField");
        var result2 = service.GetTranslation("ValidationMessages", "en", "InvalidEmail");
        Assert.Equal("Field is required", result1);
        Assert.Equal("Invalid email", result2);
    }

    #endregion

    #region Invalidate Tests

    [Fact]
    public async Task Invalidate_WithoutParameters_ClearsAllCache()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        await using (var context = await factory.CreateDbContextAsync())
        {
            var resource = new MultilingualResourceEntity
            {
                Resource = "ValidationMessages",
                Key = "RequiredField",
                en_US = "Field is required",
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now
            };
            context.MultilingualResourceEntity.Add(resource);
            await context.SaveChangesAsync();
        }

        var service = new LocalizationService(factory);
        await service.ReloadAsync();

        // Act
        service.Invalidate();

        // Assert
        var stats = service.GetCacheStats();
        Assert.Empty(stats);
    }

    [Fact]
    public async Task Invalidate_WithResource_ClearsOnlyThatResource()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        await using (var context = await factory.CreateDbContextAsync())
        {
            context.MultilingualResourceEntity.AddRange(
                new MultilingualResourceEntity
                {
                    Resource = "ValidationMessages",
                    Key = "RequiredField",
                    en_US = "Field is required",
                    IsActive = true,
                    CreatedBy = 1,
                    CreatedDate = DateTime.Now
                },
                new MultilingualResourceEntity
                {
                    Resource = "UILabels",
                    Key = "Submit",
                    en_US = "Submit",
                    IsActive = true,
                    CreatedBy = 1,
                    CreatedDate = DateTime.Now
                }
            );
            await context.SaveChangesAsync();
        }

        var service = new LocalizationService(factory);
        await service.ReloadAsync();

        // Act
        service.Invalidate(resource: "ValidationMessages");

        // Assert
        var result1 = service.TryGetTranslation("ValidationMessages", "en", "RequiredField", out _);
        var result2 = service.TryGetTranslation("UILabels", "en", "Submit", out _);
        Assert.False(result1); // Invalidated
        Assert.True(result2); // Still cached
    }

    [Fact]
    public async Task Invalidate_WithResourceAndLanguage_ClearsOnlyThatBucket()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        await using (var context = await factory.CreateDbContextAsync())
        {
            var resource = new MultilingualResourceEntity
            {
                Resource = "ValidationMessages",
                Key = "RequiredField",
                en_US = "Field is required",
                hi_IN = "?????? ?????? ??",
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now
            };
            context.MultilingualResourceEntity.Add(resource);
            await context.SaveChangesAsync();
        }

        var service = new LocalizationService(factory);
        await service.ReloadAsync();

        // Act
        service.Invalidate(resource: "ValidationMessages", language: "hi");

        // Assert
        var resultEn = service.TryGetTranslation("ValidationMessages", "en", "RequiredField", out _);
        var resultHi = service.TryGetTranslation("ValidationMessages", "hi", "RequiredField", out _);
        Assert.True(resultEn); // Still cached
        Assert.False(resultHi); // Invalidated
    }

    [Fact]
    public async Task Invalidate_WithResourceLanguageAndKey_RemovesOnlyThatKey()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        await using (var context = await factory.CreateDbContextAsync())
        {
            context.MultilingualResourceEntity.AddRange(
                new MultilingualResourceEntity
                {
                    Resource = "ValidationMessages",
                    Key = "RequiredField",
                    en_US = "Field is required",
                    IsActive = true,
                    CreatedBy = 1,
                    CreatedDate = DateTime.Now
                },
                new MultilingualResourceEntity
                {
                    Resource = "ValidationMessages",
                    Key = "InvalidEmail",
                    en_US = "Invalid email",
                    IsActive = true,
                    CreatedBy = 1,
                    CreatedDate = DateTime.Now
                }
            );
            await context.SaveChangesAsync();
        }

        var service = new LocalizationService(factory);
        await service.ReloadAsync();

        // Act
        service.Invalidate(resource: "ValidationMessages", language: "en", key: "RequiredField");

        // Assert
        var result1 = service.TryGetTranslation("ValidationMessages", "en", "RequiredField", out _);
        var result2 = service.TryGetTranslation("ValidationMessages", "en", "InvalidEmail", out _);
        Assert.False(result1); // Invalidated
        Assert.True(result2); // Still cached
    }

    #endregion

    #region RefreshAsync Tests

    [Fact]
    public async Task RefreshAsync_InvalidatesAndReloads()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        await using (var context = await factory.CreateDbContextAsync())
        {
            var resource = new MultilingualResourceEntity
            {
                Resource = "ValidationMessages",
                Key = "RequiredField",
                en_US = "Field is required",
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now
            };
            context.MultilingualResourceEntity.Add(resource);
            await context.SaveChangesAsync();
        }

        var service = new LocalizationService(factory);
        await service.ReloadAsync();

        // Update the database
        await using (var context = await factory.CreateDbContextAsync())
        {
            var resource = await context.MultilingualResourceEntity.FirstAsync();
            resource.en_US = "Updated value";
            await context.SaveChangesAsync();
        }

        // Act
        await service.RefreshAsync();

        // Assert
        var result = service.GetTranslation("ValidationMessages", "en", "RequiredField");
        Assert.Equal("Updated value", result);
    }

    #endregion

    #region ReloadAsync Tests

    [Fact]
    public async Task ReloadAsync_LoadsAllActiveResources()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        await using (var context = await factory.CreateDbContextAsync())
        {
            context.MultilingualResourceEntity.AddRange(
                new MultilingualResourceEntity
                {
                    Resource = "ValidationMessages",
                    Key = "RequiredField",
                    en_US = "Field is required",
                    IsActive = true,
                    CreatedBy = 1,
                    CreatedDate = DateTime.Now
                },
                new MultilingualResourceEntity
                {
                    Resource = "ValidationMessages",
                    Key = "InvalidEmail",
                    en_US = "Invalid email",
                    IsActive = false, // Inactive
                    CreatedBy = 1,
                    CreatedDate = DateTime.Now
                }
            );
            await context.SaveChangesAsync();
        }

        var service = new LocalizationService(factory);

        // Act
        await service.ReloadAsync();

        // Assert
        var result1 = service.TryGetTranslation("ValidationMessages", "en", "RequiredField", out _);
        var result2 = service.TryGetTranslation("ValidationMessages", "en", "InvalidEmail", out _);
        Assert.True(result1); // Active, loaded
        Assert.False(result2); // Inactive, not loaded
    }

    [Fact]
    public async Task ReloadAsync_WithResource_LoadsOnlyThatResource()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        await using (var context = await factory.CreateDbContextAsync())
        {
            context.MultilingualResourceEntity.AddRange(
                new MultilingualResourceEntity
                {
                    Resource = "ValidationMessages",
                    Key = "RequiredField",
                    en_US = "Field is required",
                    IsActive = true,
                    CreatedBy = 1,
                    CreatedDate = DateTime.Now
                },
                new MultilingualResourceEntity
                {
                    Resource = "UILabels",
                    Key = "Submit",
                    en_US = "Submit",
                    IsActive = true,
                    CreatedBy = 1,
                    CreatedDate = DateTime.Now
                }
            );
            await context.SaveChangesAsync();
        }

        var service = new LocalizationService(factory);

        // Act
        await service.ReloadAsync(resource: "ValidationMessages");

        // Assert
        var result1 = service.TryGetTranslation("ValidationMessages", "en", "RequiredField", out _);
        var result2 = service.TryGetTranslation("UILabels", "en", "Submit", out _);
        Assert.True(result1); // Loaded
        Assert.False(result2); // Not loaded
    }

    [Fact]
    public async Task ReloadAsync_WithExcludeGenerated_ExcludesGeneratedResources()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        await using (var context = await factory.CreateDbContextAsync())
        {
            context.MultilingualResourceEntity.AddRange(
                new MultilingualResourceEntity
                {
                    Resource = "ValidationMessages",
                    Key = "RequiredField",
                    en_US = "Field is required",
                    IsGenerated = false,
                    IsActive = true,
                    CreatedBy = 1,
                    CreatedDate = DateTime.Now
                },
                new MultilingualResourceEntity
                {
                    Resource = "ValidationMessages",
                    Key = "GeneratedKey",
                    en_US = "Generated",
                    IsGenerated = true,
                    IsActive = true,
                    CreatedBy = 1,
                    CreatedDate = DateTime.Now
                }
            );
            await context.SaveChangesAsync();
        }

        var service = new LocalizationService(factory);

        // Act
        await service.ReloadAsync(excludeGenerated: true);

        // Assert
        var result1 = service.TryGetTranslation("ValidationMessages", "en", "RequiredField", out _);
        var result2 = service.TryGetTranslation("ValidationMessages", "en", "GeneratedKey", out _);
        Assert.True(result1); // Not generated, loaded
        Assert.False(result2); // Generated, excluded
    }

    [Fact]
    public async Task ReloadAsync_WithCancellationToken_PropagatesToken()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        var service = new LocalizationService(factory);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => 
            service.ReloadAsync(ct: cts.Token));
    }

    #endregion

    #region GetCacheStats Tests

    [Fact]
    public async Task GetCacheStats_ReturnsCorrectStats()
    {
        // Arrange
        var factory = CreateDbContextFactory();
        await using (var context = await factory.CreateDbContextAsync())
        {
            context.MultilingualResourceEntity.AddRange(
                new MultilingualResourceEntity
                {
                    Resource = "ValidationMessages",
                    Key = "RequiredField",
                    en_US = "Field is required",
                    hi_IN = "?????? ?????? ??",
                    IsActive = true,
                    CreatedBy = 1,
                    CreatedDate = DateTime.Now
                },
                new MultilingualResourceEntity
                {
                    Resource = "ValidationMessages",
                    Key = "InvalidEmail",
                    en_US = "Invalid email",
                    hi_IN = "?????? ????",
                    IsActive = true,
                    CreatedBy = 1,
                    CreatedDate = DateTime.Now
                }
            );
            await context.SaveChangesAsync();
        }

        var service = new LocalizationService(factory);
        await service.ReloadAsync();

        // Act
        var stats = service.GetCacheStats();

        // Assert
        Assert.True(stats.Count >= 2); // At least en and hi buckets (may include mr if non-empty values)
        Assert.True(stats.ContainsKey("ValidationMessages||en"));
        Assert.True(stats.ContainsKey("ValidationMessages||hi"));
        Assert.Equal(2, stats["ValidationMessages||en"]); // 2 keys
        Assert.Equal(2, stats["ValidationMessages||hi"]); // 2 keys
    }

    #endregion
}
