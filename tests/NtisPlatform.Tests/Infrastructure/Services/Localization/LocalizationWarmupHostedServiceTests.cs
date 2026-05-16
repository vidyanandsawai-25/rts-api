using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Infrastructure.Services.Localization;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services.Localization;

public class LocalizationWarmupHostedServiceTests
{
    private readonly Mock<ILocalizationService> _mockLocalizationService;
    private readonly Mock<ILogger<LocalizationWarmupHostedService>> _mockLogger;
    private readonly LocalizationWarmupHostedService _service;

    public LocalizationWarmupHostedServiceTests()
    {
        _mockLocalizationService = new Mock<ILocalizationService>();
        _mockLogger = new Mock<ILogger<LocalizationWarmupHostedService>>();
        _service = new LocalizationWarmupHostedService(
            _mockLocalizationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task StartAsync_LoadsLocalizationData()
    {
        // Arrange
        var stats = new Dictionary<string, int>
        {
            { "ValidationMessages||en", 100 },
            { "ValidationMessages||hi", 95 },
            { "ValidationMessages||mr", 90 }
        };

        _mockLocalizationService
            .Setup(s => s.ReloadAsync(null, null, null, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockLocalizationService
            .Setup(s => s.GetCacheStats())
            .Returns(stats);

        // Act
        await _service.StartAsync(CancellationToken.None);

        // Assert
        _mockLocalizationService.Verify(
            s => s.ReloadAsync(null, null, null, true, It.IsAny<CancellationToken>()),
            Times.Once);
        _mockLocalizationService.Verify(
            s => s.GetCacheStats(),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_LogsStartMessage()
    {
        // Arrange
        var stats = new Dictionary<string, int>();
        _mockLocalizationService
            .Setup(s => s.ReloadAsync(null, null, null, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockLocalizationService
            .Setup(s => s.GetCacheStats())
            .Returns(stats);

        // Act
        await _service.StartAsync(CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Localization warmup started")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_LogsCompletionWithStats()
    {
        // Arrange
        var stats = new Dictionary<string, int>
        {
            { "ValidationMessages||en", 50 }
        };

        _mockLocalizationService
            .Setup(s => s.ReloadAsync(null, null, null, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockLocalizationService
            .Setup(s => s.GetCacheStats())
            .Returns(stats);

        // Act
        await _service.StartAsync(CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Localization warmup completed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenReloadFails_LogsErrorAndThrows()
    {
        // Arrange
        var exception = new Exception("Database connection failed");
        _mockLocalizationService
            .Setup(s => s.ReloadAsync(null, null, null, true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.StartAsync(CancellationToken.None));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Localization warmup failed")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_UsesExcludeGeneratedFlag()
    {
        // Arrange
        _mockLocalizationService
            .Setup(s => s.ReloadAsync(null, null, null, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockLocalizationService
            .Setup(s => s.GetCacheStats())
            .Returns(new Dictionary<string, int>());

        // Act
        await _service.StartAsync(CancellationToken.None);

        // Assert - Verify excludeGenerated parameter is true
        _mockLocalizationService.Verify(
            s => s.ReloadAsync(null, null, null, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_HandlesCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _mockLocalizationService
            .Setup(s => s.ReloadAsync(null, null, null, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockLocalizationService
            .Setup(s => s.GetCacheStats())
            .Returns(new Dictionary<string, int>());

        // Act
        await _service.StartAsync(cts.Token);

        // Assert
        _mockLocalizationService.Verify(
            s => s.ReloadAsync(null, null, null, true, cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_CompletesSuccessfully()
    {
        // Arrange & Act
        await _service.StopAsync(CancellationToken.None);

        // Assert - Should complete without error
        Assert.True(true);
    }

    [Fact]
    public async Task StopAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        // Act
        await _service.StopAsync(cts.Token);

        // Assert
        Assert.True(true);
    }

    [Fact]
    public async Task StartAsync_WithEmptyStats_LogsZeroBuckets()
    {
        // Arrange
        var emptyStats = new Dictionary<string, int>();
        _mockLocalizationService
            .Setup(s => s.ReloadAsync(null, null, null, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockLocalizationService
            .Setup(s => s.GetCacheStats())
            .Returns(emptyStats);

        // Act
        await _service.StartAsync(CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("0 buckets")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WithMultipleBuckets_LogsAllBuckets()
    {
        // Arrange
        var stats = new Dictionary<string, int>
        {
            { "ValidationMessages||en", 100 },
            { "ValidationMessages||hi", 95 },
            { "ValidationMessages||mr", 90 },
            { "UILabels||en", 50 }
        };

        _mockLocalizationService
            .Setup(s => s.ReloadAsync(null, null, null, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockLocalizationService
            .Setup(s => s.GetCacheStats())
            .Returns(stats);

        // Act
        await _service.StartAsync(CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("4 buckets")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
