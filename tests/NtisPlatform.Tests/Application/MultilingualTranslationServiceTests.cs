using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Master.MultilingualDetail;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class MultilingualTranslationServiceTests
{
    private readonly Mock<IRepository<MultilingualResourceEntity, int>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ITranslationService> _translationServiceMock;
    private readonly Mock<ILogger<MultilingualTranslationService>> _loggerMock;
    private readonly TranslationServiceOptions _options;
    private readonly MultilingualTranslationService _service;

    public MultilingualTranslationServiceTests()
    {
        _repositoryMock = new Mock<IRepository<MultilingualResourceEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _translationServiceMock = new Mock<ITranslationService>();
        _loggerMock = new Mock<ILogger<MultilingualTranslationService>>();

        _options = new TranslationServiceOptions { IsActive = true };
        var optionsMock = new Mock<IOptions<TranslationServiceOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);

        var localizationOptions = Options.Create(new LocalizationOptions { DefaultLanguage = "en" });

        _service = new MultilingualTranslationService(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            optionsMock.Object,
            _translationServiceMock.Object,
            _loggerMock.Object,
            localizationOptions);
    }

    [Fact]
    public async Task GetAllAsync_FilterEmptyHindi_ReturnsOnlyRecordsWithEmptyHindi()
    {
        // Arrange
        var entities = new List<MultilingualResourceEntity>
        {
            new MultilingualResourceEntity { Id = 1, en_US = "Hello", hi_IN = "नमस्ते", mr_IN = "नमस्कार" },
            new MultilingualResourceEntity { Id = 2, en_US = "Goodbye", hi_IN = "", mr_IN = "निरोप" },
            new MultilingualResourceEntity { Id = 3, en_US = "Thank you", hi_IN = null, mr_IN = "धन्यवाद" }
        };

        var mockQueryable = entities.BuildMock();
        _repositoryMock.Setup(x => x.GetQueryable()).Returns(mockQueryable);

        var queryParams = new MultilingualTranslationQueryParameters { FilterEmptyLanguages = ["hi"] };

        var mockMapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<MultilingualResourceEntity, MultilingualTranslationDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapperMock.Setup(x => x.ConfigurationProvider).Returns(mockMapperConfig);

        // Act
        var result = await _service.GetAllAsync(queryParams);

        // Assert
        Assert.Equal(2, result.Items.Count());
        Assert.All(result.Items, item => Assert.True(string.IsNullOrEmpty(item.hi_IN)));
    }

    [Fact]
    public async Task GetAllAsync_FilterEmptyMarathi_ReturnsOnlyRecordsWithEmptyMarathi()
    {
        // Arrange
        var entities = new List<MultilingualResourceEntity>
        {
            new MultilingualResourceEntity { Id = 1, en_US = "Hello", hi_IN = "नमस्ते", mr_IN = "नमस्कार" },
            new MultilingualResourceEntity { Id = 2, en_US = "Goodbye", hi_IN = "अलविदा", mr_IN = "" },
            new MultilingualResourceEntity { Id = 3, en_US = "Thank you", hi_IN = "धन्यवाद", mr_IN = null }
        };

        var mockQueryable = entities.BuildMock();
        _repositoryMock.Setup(x => x.GetQueryable()).Returns(mockQueryable);

        var queryParams = new MultilingualTranslationQueryParameters { FilterEmptyLanguages = ["mr"] };

        var mockMapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<MultilingualResourceEntity, MultilingualTranslationDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapperMock.Setup(x => x.ConfigurationProvider).Returns(mockMapperConfig);

        // Act
        var result = await _service.GetAllAsync(queryParams);

        // Assert
        Assert.Equal(2, result.Items.Count());
        Assert.All(result.Items, item => Assert.True(string.IsNullOrEmpty(item.mr_IN)));
    }

    [Fact]
    public async Task GetAllAsync_FilterBothEmpty_ReturnsOnlyRecordsWithBothEmpty()
    {
        // Arrange
        var entities = new List<MultilingualResourceEntity>
        {
            new MultilingualResourceEntity { Id = 1, en_US = "One", hi_IN = "एक", mr_IN = "" },
            new MultilingualResourceEntity { Id = 2, en_US = "Two", hi_IN = "", mr_IN = "दोन" },
            new MultilingualResourceEntity { Id = 3, en_US = "Three", hi_IN = "", mr_IN = null }
        };

        var mockQueryable = entities.BuildMock();
        _repositoryMock.Setup(x => x.GetQueryable()).Returns(mockQueryable);

        var queryParams = new MultilingualTranslationQueryParameters
        {
            FilterEmptyLanguages = ["hi", "mr"]
        };

        var mockMapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<MultilingualResourceEntity, MultilingualTranslationDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapperMock.Setup(x => x.ConfigurationProvider).Returns(mockMapperConfig);

        // Act
        var result = await _service.GetAllAsync(queryParams);

        // Assert
        Assert.Single(result.Items);
        var item = result.Items.First();
        Assert.Equal("Three", item.en_US);
        Assert.True(string.IsNullOrEmpty(item.hi_IN));
        Assert.True(string.IsNullOrEmpty(item.mr_IN));
    }

    [Fact]
    public async Task GetAllAsync_AutoTranslateEnabled_CallsTranslationService()
    {
        // Arrange
        var entities = new List<MultilingualResourceEntity>
        {
            new MultilingualResourceEntity { Id = 1, en_US = "Apple", hi_IN = "", mr_IN = "" }
        };

        var mockQueryable = entities.BuildMock();
        _repositoryMock.Setup(x => x.GetQueryable()).Returns(mockQueryable);

        var queryParams = new MultilingualTranslationQueryParameters { IsAutoTranslate = true };

        var mockMapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<MultilingualResourceEntity, MultilingualTranslationDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapperMock.Setup(x => x.ConfigurationProvider).Returns(mockMapperConfig);

        var hindiTranslations = new Dictionary<string, string> { { "Apple", "सेब" } };
        var marathiTranslations = new Dictionary<string, string> { { "Apple", "सफरचंद" } };

        _translationServiceMock.Setup(x => x.TranslateBatchAsync(It.IsAny<List<string>>(), "en", "hi", It.IsAny<CancellationToken>()))
            .ReturnsAsync(hindiTranslations);
        _translationServiceMock.Setup(x => x.TranslateBatchAsync(It.IsAny<List<string>>(), "en", "mr", It.IsAny<CancellationToken>()))
            .ReturnsAsync(marathiTranslations);

        // Act
        var result = await _service.GetAllAsync(queryParams);

        // Assert
        var item = result.Items.First();
        Assert.Equal("सेब", item.hi_IN);
        Assert.Equal("सफरचंद", item.mr_IN);
        _translationServiceMock.Verify(x => x.TranslateBatchAsync(It.IsAny<List<string>>(), "en", "hi", It.IsAny<CancellationToken>()), Times.Once);
        _translationServiceMock.Verify(x => x.TranslateBatchAsync(It.IsAny<List<string>>(), "en", "mr", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_AutoTranslateDisabled_DoesNotCallTranslationService()
    {
        // Arrange
        var entities = new List<MultilingualResourceEntity>
        {
            new MultilingualResourceEntity { Id = 1, en_US = "Apple", hi_IN = "", mr_IN = "" }
        };

        var mockQueryable = entities.BuildMock();
        _repositoryMock.Setup(x => x.GetQueryable()).Returns(mockQueryable);

        var queryParams = new MultilingualTranslationQueryParameters { IsAutoTranslate = false };

        var mockMapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<MultilingualResourceEntity, MultilingualTranslationDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapperMock.Setup(x => x.ConfigurationProvider).Returns(mockMapperConfig);

        // Act
        await _service.GetAllAsync(queryParams);

        // Assert
        _translationServiceMock.Verify(x => x.TranslateBatchAsync(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_TranslationServiceInactive_DoesNotCallTranslationService()
    {
        // Arrange
        _options.IsActive = false;
        var entities = new List<MultilingualResourceEntity>
        {
            new MultilingualResourceEntity { Id = 1, en_US = "Apple", hi_IN = "", mr_IN = "" }
        };

        var mockQueryable = entities.BuildMock();
        _repositoryMock.Setup(x => x.GetQueryable()).Returns(mockQueryable);

        var queryParams = new MultilingualTranslationQueryParameters { IsAutoTranslate = true };

        var mockMapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<MultilingualResourceEntity, MultilingualTranslationDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapperMock.Setup(x => x.ConfigurationProvider).Returns(mockMapperConfig);

        // Act
        await _service.GetAllAsync(queryParams);

        // Assert
        _translationServiceMock.Verify(x => x.TranslateBatchAsync(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void IsAutoTranslationEnabled_ReturnsTrue_WhenOptionsIsActive()
    {
        // Arrange
        _options.IsActive = true;

        // Act
        var result = _service.IsAutoTranslationEnabled();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAutoTranslationEnabled_ReturnsFalse_WhenOptionsIsInactive()
    {
        // Arrange
        _options.IsActive = false;

        // Act
        var result = _service.IsAutoTranslationEnabled();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAutoTranslationEnabled_ReflectsOptionsAcrossCalls()
    {
        // Arrange — start enabled (constructor set IsActive = true)
        Assert.True(_service.IsAutoTranslationEnabled());

        // Act — flip the underlying option
        _options.IsActive = false;

        // Assert — service reads the live value, no caching
        Assert.False(_service.IsAutoTranslationEnabled());

        // Flip back
        _options.IsActive = true;
        Assert.True(_service.IsAutoTranslationEnabled());
    }

    [Fact]
    public void IsAutoTranslationEnabled_DoesNotHitRepository()
    {
        // Arrange — fresh mock to verify zero calls
        _repositoryMock.Invocations.Clear();

        // Act
        _ = _service.IsAutoTranslationEnabled();

        // Assert
        _repositoryMock.Verify(x => x.GetQueryable(), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_TranslationFails_LogsErrorAndReturnsOriginalItems()
    {
        // Arrange
        var entities = new List<MultilingualResourceEntity>
        {
            new MultilingualResourceEntity { Id = 1, en_US = "Apple", hi_IN = "", mr_IN = "" }
        };

        var mockQueryable = entities.BuildMock();
        _repositoryMock.Setup(x => x.GetQueryable()).Returns(mockQueryable);

        var queryParams = new MultilingualTranslationQueryParameters { IsAutoTranslate = true };

        var mockMapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<MultilingualResourceEntity, MultilingualTranslationDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapperMock.Setup(x => x.ConfigurationProvider).Returns(mockMapperConfig);

        _translationServiceMock.Setup(x => x.TranslateBatchAsync(It.IsAny<List<string>>(), "en", "hi", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Translation service down"));

        // Act
        var result = await _service.GetAllAsync(queryParams);

        // Assert
        Assert.Single(result.Items);
        Assert.True(string.IsNullOrEmpty(result.Items.First().hi_IN));
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Translation failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
