using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Worker.Services;
using Xunit;

namespace NtisPlatform.Tests.Worker.Services;

/// <summary>
/// Comprehensive tests for HardDeleteCleanupWorker to achieve better coverage
/// </summary>
public class HardDeleteCleanupWorkerComprehensiveTests
{
    private readonly Mock<ILogger<HardDeleteCleanupWorker>> _loggerMock;
    private readonly IServiceProvider _serviceProvider;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IHardDeleteCleanupService> _cleanupServiceMock;

    public HardDeleteCleanupWorkerComprehensiveTests()
    {
        _loggerMock = new Mock<ILogger<HardDeleteCleanupWorker>>();
        _configurationMock = new Mock<IConfiguration>();
        _cleanupServiceMock = new Mock<IHardDeleteCleanupService>();

        // Setup real service provider with mock service
        var services = new ServiceCollection();
        services.AddSingleton(_cleanupServiceMock.Object);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void SetupConfiguration(string intervalHours, string retentionDays, string runOnStartup)
    {
        var intervalSection = new Mock<IConfigurationSection>();
        intervalSection.Setup(x => x.Value).Returns(intervalHours);

        var retentionSection = new Mock<IConfigurationSection>();
        retentionSection.Setup(x => x.Value).Returns(retentionDays);

        var startupSection = new Mock<IConfigurationSection>();
        startupSection.Setup(x => x.Value).Returns(runOnStartup);

        _configurationMock.Setup(x => x.GetSection("CleanupWorker:IntervalHours")).Returns(intervalSection.Object);
        _configurationMock.Setup(x => x.GetSection("CleanupWorker:RetentionDays")).Returns(retentionSection.Object);
        _configurationMock.Setup(x => x.GetSection("CleanupWorker:RunOnStartup")).Returns(startupSection.Object);
    }

    private HardDeleteCleanupWorker CreateWorker()
    {
        return new HardDeleteCleanupWorker(
            _loggerMock.Object,
            _serviceProvider,
            _configurationMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var worker = CreateWorker();

        // Assert
        Assert.NotNull(worker);
    }

    #endregion

    #region ExecuteAsync - Configuration Tests

    [Fact]
    public async Task ExecuteAsync_WithRunOnStartup_ExecutesImmediately()
    {
        // Arrange
        SetupConfiguration("24", "30", "true");

        var executedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _cleanupServiceMock
            .Setup(x => x.CleanupMarkedEntitiesAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10)
            .Callback(() => executedTcs.TrySetResult());

        var worker = CreateWorker();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        // Act
        await worker.StartAsync(timeoutCts.Token);

        // Assert (deterministic wait for actual execution)
        var completed = await Task.WhenAny(
            executedTcs.Task,
            Task.Delay(TimeSpan.FromSeconds(10), timeoutCts.Token));

        Assert.Same(executedTcs.Task, completed);

        _cleanupServiceMock.Verify(
            x => x.CleanupMarkedEntitiesAsync(30, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);

        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomIntervalHours_UsesConfiguration()
    {
        // Arrange - Set RunOnStartup to "true" to ensure immediate execution
        SetupConfiguration("6", "15", "true");

        var tcs = new TaskCompletionSource<bool>();
        _cleanupServiceMock
            .Setup(x => x.CleanupMarkedEntitiesAsync(15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3)
            .Callback(() => tcs.TrySetResult(true));

        var worker = CreateWorker();
        var cts = new CancellationTokenSource();

        // Act
        var executeTask = worker.StartAsync(cts.Token);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5)); // Fail fast if callback never happens
        cts.Cancel();
        await executeTask;
        await worker.StopAsync(CancellationToken.None);

        // Assert - Verify the cleanup service was called with the configured retention days
        _cleanupServiceMock.Verify(
            x => x.CleanupMarkedEntitiesAsync(15, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExecuteAsync_WhenCleanupThrowsException_ContinuesWithoutCrashing()
    {
        // Arrange
        SetupConfiguration("1", "0", "true");

        var tcs = new TaskCompletionSource<bool>();
        _cleanupServiceMock
            .Setup(x => x.CleanupMarkedEntitiesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cleanup failed"))
            .Callback(() => tcs.TrySetResult(true));

        var worker = CreateWorker();
        var cts = new CancellationTokenSource();

        // Act
        var executeTask = worker.StartAsync(cts.Token);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5)); // Fail fast if callback never happens
        cts.Cancel();
        await executeTask;
        await worker.StopAsync(CancellationToken.None);

        // Assert - Worker should not crash and should complete gracefully
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("stopped")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    // Removed: CreateScope extension method cannot be mocked - this is by design
    // The error handling is tested through the cleanup service throwing exceptions

    #endregion

    #region CalculateNextRun Tests (via Reflection)

    [Fact]
    public void CalculateNextRun_With24HourInterval_ReturnsNext2AM()
    {
        // Arrange
        var worker = CreateWorker();
        var method = typeof(HardDeleteCleanupWorker).GetMethod(
            "CalculateNextRun",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (DateTime)method!.Invoke(worker, new object[] { 24 })!;

        // Assert
        Assert.Equal(2, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.True(result > DateTime.Now);
    }

    [Fact]
    public void CalculateNextRun_WithCustomInterval_ReturnsCurrentPlusInterval()
    {
        // Arrange
        var worker = CreateWorker();
        var method = typeof(HardDeleteCleanupWorker).GetMethod(
            "CalculateNextRun",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var before = DateTime.Now;

        // Act
        var result = (DateTime)method!.Invoke(worker, new object[] { 6 })!;
        var after = DateTime.Now;

        // Assert
        var expectedMin = before.AddHours(6);
        var expectedMax = after.AddHours(6);
        Assert.True(result >= expectedMin && result <= expectedMax);
    }

    [Fact]
    public void CalculateNextRun_When2AMPassed_SchedulesTomorrow()
    {
        // Arrange
        var worker = CreateWorker();
        var method = typeof(HardDeleteCleanupWorker).GetMethod(
            "CalculateNextRun",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (DateTime)method!.Invoke(worker, new object[] { 24 })!;

        // Assert
        if (DateTime.Now.Hour >= 2)
        {
            // If it's past 2 AM, should schedule for tomorrow
            Assert.True(result.Date > DateTime.Now.Date);
        }
        else
        {
            // If before 2 AM, should schedule for today
            Assert.True(result.Date >= DateTime.Now.Date);
        }
    }

    #endregion

    #region StartAsync and StopAsync Tests

    [Fact]
    public async Task StartAsync_LogsInformation()
    {
        // Arrange
        _configurationMock.Setup(x => x.GetSection("CleanupWorker:IntervalHours").Value).Returns("24");
        _configurationMock.Setup(x => x.GetSection("CleanupWorker:RetentionDays").Value).Returns("0");
        _configurationMock.Setup(x => x.GetSection("CleanupWorker:RunOnStartup").Value).Returns("false");

        var worker = CreateWorker();

        // Act
        await worker.StartAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("started")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_LogsInformation()
    {
        // Arrange
        _configurationMock.Setup(x => x.GetSection("CleanupWorker:IntervalHours").Value).Returns("24");
        _configurationMock.Setup(x => x.GetSection("CleanupWorker:RetentionDays").Value).Returns("0");
        _configurationMock.Setup(x => x.GetSection("CleanupWorker:RunOnStartup").Value).Returns("false");

        var worker = CreateWorker();
        await worker.StartAsync(CancellationToken.None);

        // Act
        await worker.StopAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("stopped") || v.ToString()!.Contains("stopping")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequested_StopsGracefully()
    {
        // Arrange
        _configurationMock.Setup(x => x.GetSection("CleanupWorker:IntervalHours").Value).Returns("24");
        _configurationMock.Setup(x => x.GetSection("CleanupWorker:RetentionDays").Value).Returns("0");
        _configurationMock.Setup(x => x.GetSection("CleanupWorker:RunOnStartup").Value).Returns("false");

        var worker = CreateWorker();
        var cts = new CancellationTokenSource();

        // Act
        var startTask = worker.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        await startTask;
        await worker.StopAsync(CancellationToken.None);

        // Assert - Worker logs stopped not stopping
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("stopped")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task ExecuteAsync_LogsConfigurationOnStartup()
    {
        // Arrange
        SetupConfiguration("12", "45", "true");

        var tcs = new TaskCompletionSource<bool>();

        _cleanupServiceMock
            .Setup(x => x.CleanupMarkedEntitiesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5)
            .Callback(() => tcs.TrySetResult(true));

        var worker = CreateWorker();
        var cts = new CancellationTokenSource();

        // Act
        var executeTask = worker.StartAsync(cts.Token);

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5)); // Fail fast if callback never happens

        cts.Cancel();
        await executeTask;
        await worker.StopAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString() != null &&
                    v.ToString()!.Contains("Configuration", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
    [Fact]
    public async Task ExecuteAsync_LogsDeletedEntityCount()
    {
        // Arrange
        _configurationMock.Setup(x => x.GetSection("CleanupWorker:IntervalHours").Value).Returns("1");
        _configurationMock.Setup(x => x.GetSection("CleanupWorker:RetentionDays").Value).Returns("0");
        _configurationMock.Setup(x => x.GetSection("CleanupWorker:RunOnStartup").Value).Returns("true");

        _cleanupServiceMock
            .Setup(x => x.CleanupMarkedEntitiesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var worker = CreateWorker();
        var cts = new CancellationTokenSource();

        // Act
        var executeTask = worker.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();
        await executeTask;
        await worker.StopAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("42")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion
}
