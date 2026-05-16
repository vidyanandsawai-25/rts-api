using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core;
using NtisPlatform.Core.Constants;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace NtisPlatform.Tests.Application.Services;

/// <summary>
/// Comprehensive test suite for the entire localization process system.
/// Covers: LocalizationProcessor, ILocalization, ILocalizedQueryService, and BaseCommonCrudService localization hooks.
/// </summary>
public class LocalizationProcessSystemTests
{
    #region Test DTOs and Entities

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class TestDto
    {
        public int Id { get; set; }

        [IsLocalizable("TestResource")]
        public string Name { get; set; } = string.Empty;

        [IsLocalizable("TestResource")]
        public string Description { get; set; } = string.Empty;
    }

    public class TestCreateDto
    {
        [IsLocalizable("TestResource")]
        public string Name { get; set; } = string.Empty;

        [IsLocalizable("TestResource")]
        public string Description { get; set; } = string.Empty;
    }

    public class NonLocalizableDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #endregion

    #region Test Infrastructure

    private static Mock<IHttpContextAccessor> CreateHttpContextAccessor(string language = "en")
    {
        var mockAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContext.Items[HttpContextKeys.CurrentLanguage] = language;
        mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        return mockAccessor;
    }

    private static IOptions<LocalizationOptions> CreateMockLocalizationOptions(string defaultLanguage = "en")
    {
        return Options.Create(new LocalizationOptions { DefaultLanguage = defaultLanguage });
    }

    private static Mock<ILocalization> CreateMockLocalizationService()
    {
        var mock = new Mock<ILocalization>();

        mock.Setup(x => x.SaveBatchAsync(It.IsAny<IEnumerable<LocalizationEntry>>()))
            .ReturnsAsync((IEnumerable<LocalizationEntry> entries) =>
            {
                var result = new Dictionary<string, string>();
                foreach (var entry in entries)
                {
                    result[entry.PropertyName] = entry.Key;
                }
                return result;
            });

        mock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
            .ReturnsAsync((string resource, IEnumerable<string> keys, string language) =>
            {
                var result = new Dictionary<string, string>();
                foreach (var key in keys)
                {
                    // Simulate translation: return "Translated: {key}"
                    result[key] = $"Translated_{language}_{key}";
                }
                return result;
            });

        mock.Setup(x => x.DeactivateByKeysAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(Task.CompletedTask);

        return mock;
    }

    #endregion

    #region LocalizationProcessor - ProcessSaveAsync Tests

    [Fact]
    public async Task ProcessSaveAsync_WithLocalizableProperties_GeneratesKeysAndSaves()
    {
        // Arrange
        var mockLocalization = CreateMockLocalizationService();
        var mockHttpContext = CreateHttpContextAccessor("en");
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        var dto = new TestCreateDto
        {
            Name = "Test Name",
            Description = "Test Description"
        };

        // Act
        await processor.ProcessSaveAsync(dto);

        // Assert: every entry uses a freshly-minted {Resource}_{GUID}_{PropertyName} key
        mockLocalization.Verify(x => x.SaveBatchAsync(It.Is<IEnumerable<LocalizationEntry>>(entries =>
            entries.Count() == 2 &&
            entries.Any(e => e.PropertyName == "Name" && e.Value == "Test Name"
                              && e.Key.StartsWith("TestResource_") && e.Key.EndsWith("_Name")) &&
            entries.Any(e => e.PropertyName == "Description" && e.Value == "Test Description"
                              && e.Key.StartsWith("TestResource_") && e.Key.EndsWith("_Description"))
        )), Times.Once);
    }

    [Fact]
    public async Task ProcessSaveAsync_WithExistingLocalizationKey_SkipsProperty()
    {
        // Arrange
        var mockLocalization = CreateMockLocalizationService();
        var mockHttpContext = CreateHttpContextAccessor("en");
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        var dto = new TestCreateDto
        {
            Name = "TestResource_123_Name", // Already a localization key
            Description = "New Description"
        };

        // Act
        await processor.ProcessSaveAsync(dto);

        // Assert - Only Description should be saved
        mockLocalization.Verify(x => x.SaveBatchAsync(It.Is<IEnumerable<LocalizationEntry>>(entries =>
            entries.Count() == 1 &&
            entries.Single().PropertyName == "Description"
        )), Times.Once);
    }

    [Fact]
    public async Task ProcessSaveAsync_WithNullDto_DoesNothing()
    {
        // Arrange
        var mockLocalization = CreateMockLocalizationService();
        var mockHttpContext = CreateHttpContextAccessor();
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        // Act
        await processor.ProcessSaveAsync<TestCreateDto>(null!);

        // Assert
        mockLocalization.Verify(x => x.SaveBatchAsync(It.IsAny<IEnumerable<LocalizationEntry>>()), Times.Never);
    }

    [Fact]
    public async Task ProcessSaveAsync_WithEmptyProperties_DoesNothing()
    {
        // Arrange
        var mockLocalization = CreateMockLocalizationService();
        var mockHttpContext = CreateHttpContextAccessor();
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        var dto = new TestCreateDto
        {
            Name = "",
            Description = "   " // Whitespace only
        };

        // Act
        await processor.ProcessSaveAsync(dto);

        // Assert
        mockLocalization.Verify(x => x.SaveBatchAsync(It.IsAny<IEnumerable<LocalizationEntry>>()), Times.Never);
    }

    [Fact]
    public async Task ProcessSaveAsync_WithNonLocalizableDto_DoesNothing()
    {
        // Arrange
        var mockLocalization = CreateMockLocalizationService();
        var mockHttpContext = CreateHttpContextAccessor();
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        var dto = new NonLocalizableDto
        {
            Id = 1,
            Name = "Test Name"
        };

        // Act
        await processor.ProcessSaveAsync(dto);

        // Assert
        mockLocalization.Verify(x => x.SaveBatchAsync(It.IsAny<IEnumerable<LocalizationEntry>>()), Times.Never);
    }

    [Fact]
    public async Task ProcessSaveAsync_WithoutExistingKeys_MintsGuidBasedKey()
    {
        // Arrange
        var mockLocalization = CreateMockLocalizationService();
        var mockHttpContext = CreateHttpContextAccessor();
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        var dto = new TestCreateDto { Name = "Test Name" };

        // Act
        await processor.ProcessSaveAsync(dto);

        // Assert: fresh key minted with the {Resource}_{GUID}_{PropertyName} shape
        mockLocalization.Verify(x => x.SaveBatchAsync(It.Is<IEnumerable<LocalizationEntry>>(entries =>
            entries.Any()
            && !string.IsNullOrEmpty(entries.First().Key)
            && entries.First().Key.StartsWith("TestResource_")
            && entries.First().Key.EndsWith("_Name")
        )), Times.Once);
    }

    [Fact]
    public async Task ProcessSaveAsync_WithExistingKeys_ReusesThemForUpdate()
    {
        // Arrange
        var mockLocalization = CreateMockLocalizationService();
        var mockHttpContext = CreateHttpContextAccessor();
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        var dto = new TestCreateDto { Name = "Updated Name" };
        const string existingKey = "TestResource_abc123def456_Name";
        var existingKeys = new Dictionary<string, string> { ["Name"] = existingKey };

        // Act
        await processor.ProcessSaveAsync(dto, existingKeys);

        // Assert: the existing key flowed through unchanged
        mockLocalization.Verify(x => x.SaveBatchAsync(It.Is<IEnumerable<LocalizationEntry>>(entries =>
            entries.Single().Key == existingKey
        )), Times.Once);
    }

    [Fact]
    public async Task ProcessSaveAsync_AlwaysUsesDefaultLanguage_RegardlessOfHttpContext()
    {
        // Arrange
        var mockLocalization = CreateMockLocalizationService();
        var mockHttpContext = CreateHttpContextAccessor("fr"); // User language is French
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        var dto = new TestCreateDto { Name = "Test Name" };

        // Act
        await processor.ProcessSaveAsync(dto);

        // Assert - Save should always use default language "en", not the user's language "fr"
        mockLocalization.Verify(x => x.SaveBatchAsync(It.Is<IEnumerable<LocalizationEntry>>(entries =>
            entries.All(e => e.Language == "en")
        )), Times.Once);
    }

    #endregion

    #region LocalizationProcessor - ProcessGetAsync Tests

    [Fact]
    public async Task ProcessGetAsync_WithLocalizableProperties_TranslatesKeys()
    {
        // Arrange
        var mockLocalization = CreateMockLocalizationService();
        var mockHttpContext = CreateHttpContextAccessor("en");
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        var dtos = new List<TestDto>
        {
            new() { Id = 1, Name = "TestResource_1_Name", Description = "TestResource_1_Description" },
            new() { Id = 2, Name = "TestResource_2_Name", Description = "TestResource_2_Description" }
        };

        // Act
        await processor.ProcessGetAsync(dtos);

        // Assert
        Assert.Equal("Translated_en_TestResource_1_Name", dtos[0].Name);
        Assert.Equal("Translated_en_TestResource_1_Description", dtos[0].Description);
        Assert.Equal("Translated_en_TestResource_2_Name", dtos[1].Name);
        Assert.Equal("Translated_en_TestResource_2_Description", dtos[1].Description);
    }

    [Fact]
    public async Task ProcessGetAsync_WithEmptyCollection_DoesNothing()
    {
        // Arrange
        var mockLocalization = CreateMockLocalizationService();
        var mockHttpContext = CreateHttpContextAccessor();
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        var dtos = new List<TestDto>();

        // Act
        await processor.ProcessGetAsync(dtos);

        // Assert
        mockLocalization.Verify(x => x.GetAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessGetAsync_WithNullCollection_DoesNothing()
    {
        // Arrange
        var mockLocalization = CreateMockLocalizationService();
        var mockHttpContext = CreateHttpContextAccessor();
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        // Act
        await processor.ProcessGetAsync<TestDto>(null!);

        // Assert
        mockLocalization.Verify(x => x.GetAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessGetAsync_WithNonLocalizableDto_DoesNothing()
    {
        // Arrange
        var mockLocalization = CreateMockLocalizationService();
        var mockHttpContext = CreateHttpContextAccessor();
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        var dtos = new List<NonLocalizableDto>
        {
            new() { Id = 1, Name = "Test" }
        };

        // Act
        await processor.ProcessGetAsync(dtos);

        // Assert
        mockLocalization.Verify(x => x.GetAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessGetAsync_WithNonKeyValues_DoesNotTranslate()
    {
        // Arrange
        var mockLocalization = CreateMockLocalizationService();
        var mockHttpContext = CreateHttpContextAccessor();
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        var dtos = new List<TestDto>
        {
            new() { Id = 1, Name = "Regular Name", Description = "Regular Description" }
        };

        // Act
        await processor.ProcessGetAsync(dtos);

        // Assert - Values should remain unchanged (not localization keys)
        Assert.Equal("Regular Name", dtos[0].Name);
        Assert.Equal("Regular Description", dtos[0].Description);
    }

    [Fact]
    public async Task ProcessGetAsync_BatchesKeysFetchByResource()
    {
        // Arrange
        var mockLocalization = CreateMockLocalizationService();
        var mockHttpContext = CreateHttpContextAccessor();
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        var dtos = new List<TestDto>
        {
            new() { Id = 1, Name = "TestResource_1_Name", Description = "TestResource_1_Description" },
            new() { Id = 2, Name = "TestResource_2_Name", Description = "TestResource_2_Description" },
            new() { Id = 3, Name = "TestResource_3_Name", Description = "TestResource_3_Description" }
        };

        // Act
        await processor.ProcessGetAsync(dtos);

        // Assert - Should batch all keys in single call per resource
        mockLocalization.Verify(x => x.GetAsync(
            "TestResource",
            It.Is<IEnumerable<string>>(keys => keys.Count() == 6),
            It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region LocalizationProcessor - ProcessDeactivateAsync Tests

    [Fact]
    public async Task ProcessDeactivateAsync_WithValidKeys_DeactivatesInBatch()
    {
        // Arrange
        var mockLocalization = CreateMockLocalizationService();
        var mockHttpContext = CreateHttpContextAccessor();
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        var keys = new[] { "TestResource_1_Name", "TestResource_1_Description" };

        // Act
        await processor.ProcessDeactivateAsync("TestResource", keys);

        // Assert
        mockLocalization.Verify(x => x.DeactivateByKeysAsync("TestResource", It.Is<IEnumerable<string>>(k =>
            k.SequenceEqual(keys)
        )), Times.Once);
    }

    [Fact]
    public async Task ProcessDeactivateAsync_WithEmptyKeys_DoesNothing()
    {
        // Arrange
        var mockLocalization = CreateMockLocalizationService();
        var mockHttpContext = CreateHttpContextAccessor();
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        // Act
        await processor.ProcessDeactivateAsync("TestResource", Array.Empty<string>());

        // Assert
        mockLocalization.Verify(x => x.DeactivateByKeysAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task ProcessDeactivateAsync_WithNullResource_DoesNothing()
    {
        // Arrange
        var mockLocalization = CreateMockLocalizationService();
        var mockHttpContext = CreateHttpContextAccessor();
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        // Act
        await processor.ProcessDeactivateAsync(null!, new[] { "key1" });

        // Assert
        mockLocalization.Verify(x => x.DeactivateByKeysAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    #endregion

    #region LocalizationProcessor - GetResource Tests

    [Fact]
    public void GetResource_WithLocalizableDto_ReturnsResource()
    {
        // Act
        var resource = LocalizationProcessor.GetResource<TestDto>();

        // Assert
        Assert.Equal("TestResource", resource);
    }

    [Fact]
    public void GetResource_WithNonLocalizableDto_ThrowsException()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            LocalizationProcessor.GetResource<NonLocalizableDto>());

        Assert.Contains("No [IsLocalizable] attribute found", exception.Message);
    }

    #endregion

    #region LocalizationEntry Key Tests

    [Fact]
    public void LocalizationEntry_Key_IsSettableAndReturnsValue()
    {
        // Arrange
        var entry = new LocalizationEntry
        {
            Resource = "TestResource",
            Key = "TestResource_abc123def456_Name",
            PropertyName = "Name",
            Value = "Test Value",
            Language = "en"
        };

        // Act & Assert
        Assert.Equal("TestResource_abc123def456_Name", entry.Key);
    }

    [Fact]
    public void LocalizationEntry_Key_AcceptsArbitraryIdentifierSegment()
    {
        // Arrange
        var entry = new LocalizationEntry
        {
            Resource = "Master",
            Key = "Master_abc-def-123_Description",
            PropertyName = "Description",
            Value = "Test",
            Language = "fr"
        };

        // Act & Assert
        Assert.Equal("Master_abc-def-123_Description", entry.Key);
    }

    #endregion

    #region IsLocalizableAttribute Tests

    [Fact]
    public void IsLocalizableAttribute_OnProperty_IsDetectable()
    {
        // Arrange
        var property = typeof(TestDto).GetProperty("Name");

        // Act
        var attr = property?.GetCustomAttribute<IsLocalizableAttribute>();

        // Assert
        Assert.NotNull(attr);
        Assert.Equal("TestResource", attr.Resource);
    }

    #endregion

    #region ILocalizedQueryService Mock Tests

    [Fact]
    public async Task LocalizedQueryService_SearchLocalizedKeysAsync_ReturnsMatchingKeys()
    {
        // Arrange
        var mockService = new Mock<ILocalizedQueryService>();
        mockService.Setup(x => x.SearchLocalizedKeysAsync(
            "TestResource",
            "search",
            "en",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "TestResource_1_Name", "TestResource_2_Name" });

        // Act
        var result = await mockService.Object.SearchLocalizedKeysAsync("TestResource", "search", "en");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("TestResource_1_Name", result);
        Assert.Contains("TestResource_2_Name", result);
    }

    [Fact]
    public async Task LocalizedQueryService_GetKeysByLocalizedValuesBatchAsync_ReturnsBatchedResults()
    {
        // Arrange
        var mockService = new Mock<ILocalizedQueryService>();
        mockService.Setup(x => x.GetKeysByLocalizedValuesBatchAsync(
            "TestResource",
            It.IsAny<IEnumerable<string>>(),
            "en",
            false,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, IReadOnlyList<string>>
            {
                ["Value1"] = new List<string> { "TestResource_1_Name" },
                ["Value2"] = new List<string> { "TestResource_2_Name", "TestResource_3_Name" }
            });

        // Act
        var result = await mockService.Object.GetKeysByLocalizedValuesBatchAsync(
            "TestResource",
            new[] { "Value1", "Value2" },
            "en",
            false);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Single(result["Value1"]);
        Assert.Equal(2, result["Value2"].Count);
    }

    #endregion

    #region ILocalizationService Mock Tests

    [Fact]
    public void LocalizationService_GetTranslation_WithFallback()
    {
        // Arrange
        var mockService = new Mock<ILocalizationService>();
        mockService.Setup(x => x.GetTranslation("TestResource", "fr", "TestResource_1_Name"))
            .Returns("Translated French Name");

        // Act
        var result = mockService.Object.GetTranslation("TestResource", "fr", "TestResource_1_Name");

        // Assert
        Assert.Equal("Translated French Name", result);
    }

    [Fact]
    public void LocalizationService_GetTranslations_BatchOperation()
    {
        // Arrange
        var mockService = new Mock<ILocalizationService>();
        var keys = new[] { "key1", "key2", "key3" };
        mockService.Setup(x => x.GetTranslations("TestResource", "en", keys))
            .Returns(new Dictionary<string, string>
            {
                ["key1"] = "Value 1",
                ["key2"] = "Value 2",
                ["key3"] = "Value 3"
            });

        // Act
        var result = mockService.Object.GetTranslations("TestResource", "en", keys);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Value 1", result["key1"]);
    }

    [Fact]
    public void LocalizationService_InvalidateKeys_RemovesFromCache()
    {
        // Arrange
        var mockService = new Mock<ILocalizationService>();

        // Act
        mockService.Object.InvalidateKeys("TestResource", new[] { "key1", "key2" });

        // Assert
        mockService.Verify(x => x.InvalidateKeys("TestResource", It.Is<IEnumerable<string>>(k =>
            k.Count() == 2)), Times.Once);
    }

    #endregion

    #region Multi-Language Support Tests

    [Theory]
    [InlineData("en", "English Translation")]
    [InlineData("fr", "French Translation")]
    [InlineData("es", "Spanish Translation")]
    [InlineData("th", "Thai Translation")]
    public async Task ProcessGetAsync_WithDifferentLanguages_TranslatesCorrectly(string language, string expectedPrefix)
    {
        // Arrange
        var mockLocalization = new Mock<ILocalization>();
        mockLocalization.Setup(x => x.GetAsync("TestResource", It.IsAny<IEnumerable<string>>(), language))
            .ReturnsAsync(new Dictionary<string, string>
            {
                ["TestResource_1_Name"] = expectedPrefix
            });

        var mockHttpContext = CreateHttpContextAccessor(language);
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        var dtos = new List<TestDto>
        {
            new() { Id = 1, Name = "TestResource_1_Name" }
        };

        // Act
        await processor.ProcessGetAsync(dtos);

        // Assert
        Assert.Equal(expectedPrefix, dtos[0].Name);
    }

    [Fact]
    public async Task ProcessSaveAsync_WithMultipleLanguages_AlwaysSavesInDefaultLanguage()
    {
        // Arrange
        var savedEntries = new List<LocalizationEntry>();
        var mockLocalization = new Mock<ILocalization>();
        mockLocalization.Setup(x => x.SaveBatchAsync(It.IsAny<IEnumerable<LocalizationEntry>>()))
            .Callback<IEnumerable<LocalizationEntry>>(entries => savedEntries.AddRange(entries))
            .ReturnsAsync(new Dictionary<string, string> { ["Name"] = "TestResource_1_Name" });

        var mockHttpContext = CreateHttpContextAccessor("th"); // User language is Thai
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        var dto = new TestCreateDto { Name = "Thai Name" };

        // Act
        await processor.ProcessSaveAsync(dto);

        // Assert - Should save as default language "en", not the user's language "th"
        Assert.Single(savedEntries);
        Assert.Equal("en", savedEntries[0].Language);
    }

    #endregion

    #region Edge Cases and Error Handling Tests

    [Fact]
    public async Task ProcessSaveAsync_WithVeryLongValue_HandlesCorrectly()
    {
        // Arrange
        var mockLocalization = CreateMockLocalizationService();
        var mockHttpContext = CreateHttpContextAccessor();
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        var longValue = new string('A', 10000);
        var dto = new TestCreateDto { Name = longValue };

        // Act
        await processor.ProcessSaveAsync(dto);

        // Assert
        mockLocalization.Verify(x => x.SaveBatchAsync(It.Is<IEnumerable<LocalizationEntry>>(entries =>
            entries.Single().Value == longValue
        )), Times.Once);
    }

    [Fact]
    public async Task ProcessSaveAsync_WithUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var mockLocalization = CreateMockLocalizationService();
        var mockHttpContext = CreateHttpContextAccessor();
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        var unicodeValue = "日本語 中文 한국어 ไทย العربية";
        var dto = new TestCreateDto { Name = unicodeValue };

        // Act
        await processor.ProcessSaveAsync(dto);

        // Assert
        mockLocalization.Verify(x => x.SaveBatchAsync(It.Is<IEnumerable<LocalizationEntry>>(entries =>
            entries.Single().Value == unicodeValue
        )), Times.Once);
    }

    [Fact]
    public async Task ProcessGetAsync_WhenTranslationMissing_RetainsOriginalKey()
    {
        // Arrange
        var mockLocalization = new Mock<ILocalization>();
        mockLocalization.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
            .ReturnsAsync(new Dictionary<string, string>()); // Empty result - no translations found

        var mockHttpContext = CreateHttpContextAccessor();
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        var originalKey = "TestResource_1_Name";
        var dtos = new List<TestDto>
        {
            new() { Id = 1, Name = originalKey }
        };

        // Act
        await processor.ProcessGetAsync(dtos);

        // Assert - Original key should be retained when no translation found
        Assert.Equal(originalKey, dtos[0].Name);
    }

    #endregion

    #region Concurrency and Thread Safety Tests

    [Fact]
    public async Task ProcessSaveAsync_ConcurrentCalls_HandlesSafely()
    {
        // Arrange
        var callCount = 0;
        var mockLocalization = new Mock<ILocalization>();
        mockLocalization.Setup(x => x.SaveBatchAsync(It.IsAny<IEnumerable<LocalizationEntry>>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref callCount);
                return new Dictionary<string, string> { ["Name"] = $"Key_{callCount}" };
            });

        var mockHttpContext = CreateHttpContextAccessor();
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        // Act - Run 10 concurrent save operations
        var tasks = Enumerable.Range(1, 10).Select(i =>
        {
            var dto = new TestCreateDto { Name = $"Name {i}" };
            return processor.ProcessSaveAsync(dto);
        });

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, callCount);
    }

    [Fact]
    public async Task ProcessGetAsync_ConcurrentCalls_HandlesSafely()
    {
        // Arrange
        var callCount = 0;
        var mockLocalization = new Mock<ILocalization>();
        mockLocalization.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref callCount);
                return new Dictionary<string, string> { ["TestResource_1_Name"] = "Translated" };
            });

        var mockHttpContext = CreateHttpContextAccessor();
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        // Act - Run 10 concurrent get operations
        var tasks = Enumerable.Range(1, 10).Select(_ =>
        {
            var dtos = new List<TestDto> { new() { Id = 1, Name = "TestResource_1_Name" } };
            return processor.ProcessGetAsync(dtos);
        });

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, callCount);
    }

    #endregion

    #region Integration Scenario Tests

    [Fact]
    public async Task FullCrudCycle_CreateReadUpdateDelete_LocalizationFlowWorks()
    {
        // Arrange
        var storedTranslations = new Dictionary<string, Dictionary<string, string>>();
        var mockLocalization = new Mock<ILocalization>();

        // Setup Save
        mockLocalization.Setup(x => x.SaveBatchAsync(It.IsAny<IEnumerable<LocalizationEntry>>()))
            .ReturnsAsync((IEnumerable<LocalizationEntry> entries) =>
            {
                var result = new Dictionary<string, string>();
                foreach (var entry in entries)
                {
                    if (!storedTranslations.ContainsKey(entry.Key))
                        storedTranslations[entry.Key] = new Dictionary<string, string>();
                    storedTranslations[entry.Key][entry.Language] = entry.Value;
                    result[entry.PropertyName] = entry.Key;
                }
                return result;
            });

        // Setup Get
        mockLocalization.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
            .ReturnsAsync((string _, IEnumerable<string> keys, string language) =>
            {
                var result = new Dictionary<string, string>();
                foreach (var key in keys)
                {
                    if (storedTranslations.TryGetValue(key, out var translations) &&
                        translations.TryGetValue(language, out var value))
                    {
                        result[key] = value;
                    }
                }
                return result;
            });

        // Setup Deactivate
        mockLocalization.Setup(x => x.DeactivateByKeysAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Callback<string, IEnumerable<string>>((_, keys) =>
            {
                foreach (var key in keys)
                    storedTranslations.Remove(key);
            })
            .Returns(Task.CompletedTask);

        var mockHttpContext = CreateHttpContextAccessor("en");
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        // Act - CREATE
        var createDto = new TestCreateDto { Name = "Original Name", Description = "Original Description" };
        await processor.ProcessSaveAsync(createDto);

        // Assert CREATE - Keys should be generated with {Resource}_{GUID}_{PropertyName} shape
        Assert.StartsWith("TestResource_", createDto.Name);
        Assert.EndsWith("_Name", createDto.Name);
        Assert.StartsWith("TestResource_", createDto.Description);
        Assert.EndsWith("_Description", createDto.Description);

        // Capture generated keys for the subsequent operations
        var nameKey = createDto.Name;
        var descKey = createDto.Description;

        // Act - READ
        var readDtos = new List<TestDto>
        {
            new() { Id = 1, Name = nameKey, Description = descKey }
        };
        await processor.ProcessGetAsync(readDtos);

        // Assert READ - Should get translated values
        Assert.Equal("Original Name", readDtos[0].Name);
        Assert.Equal("Original Description", readDtos[0].Description);

        // Act - UPDATE: reuse the entity's existing keys so the same rows get updated
        var updateDto = new TestCreateDto { Name = "Updated Name", Description = "Updated Description" };
        var existingKeys = new Dictionary<string, string>
        {
            ["Name"] = nameKey,
            ["Description"] = descKey
        };
        await processor.ProcessSaveAsync(updateDto, existingKeys);

        Assert.Equal(nameKey, updateDto.Name);
        Assert.Equal(descKey, updateDto.Description);

        // Assert UPDATE - Same keys, new values
        var readAfterUpdate = new List<TestDto>
        {
            new() { Id = 1, Name = nameKey, Description = descKey }
        };
        await processor.ProcessGetAsync(readAfterUpdate);
        Assert.Equal("Updated Name", readAfterUpdate[0].Name);
        Assert.Equal("Updated Description", readAfterUpdate[0].Description);

        // Act - DELETE
        await processor.ProcessDeactivateAsync("TestResource", new[] { nameKey, descKey });

        // Assert DELETE - Translations should be removed
        Assert.Empty(storedTranslations);
    }

    [Fact]
    public async Task BulkOperations_WithMultipleEntities_ProcessesAllCorrectly()
    {
        // Arrange
        var savedEntries = new List<LocalizationEntry>();
        var mockLocalization = new Mock<ILocalization>();
        mockLocalization.Setup(x => x.SaveBatchAsync(It.IsAny<IEnumerable<LocalizationEntry>>()))
            .Callback<IEnumerable<LocalizationEntry>>(entries => savedEntries.AddRange(entries))
            .ReturnsAsync((IEnumerable<LocalizationEntry> entries) =>
                entries.ToDictionary(e => e.PropertyName, e => e.Key));

        var mockHttpContext = CreateHttpContextAccessor();
        var processor = new LocalizationProcessor(mockLocalization.Object, mockHttpContext.Object, CreateMockLocalizationOptions());

        // Act - Save multiple DTOs
        for (int i = 1; i <= 5; i++)
        {
            var dto = new TestCreateDto { Name = $"Name {i}", Description = $"Description {i}" };
            await processor.ProcessSaveAsync(dto);
        }

        // Assert
        Assert.Equal(10, savedEntries.Count); // 5 DTOs * 2 properties each
        Assert.Equal(5, savedEntries.Count(e => e.PropertyName == "Name"));
        Assert.Equal(5, savedEntries.Count(e => e.PropertyName == "Description"));
    }

    #endregion
}

