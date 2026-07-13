using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Application.Enums;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Worker.Services;
using Xunit;

namespace NtisPlatform.Tests.Worker.Services;

public class PropertyTaxJobRecoveryWorkerTests
{
    private readonly Mock<ILogger<PropertyTaxJobRecoveryWorker>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IRepository<PropertyTaxJobEntity, int>> _jobRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IServiceProvider _serviceProvider;

    public PropertyTaxJobRecoveryWorkerTests()
    {
        _loggerMock = new Mock<ILogger<PropertyTaxJobRecoveryWorker>>();
        _configurationMock = new Mock<IConfiguration>();
        _jobRepoMock = new Mock<IRepository<PropertyTaxJobEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        var services = new ServiceCollection();
        services.AddSingleton(_jobRepoMock.Object);
        services.AddSingleton(_unitOfWorkMock.Object);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void SetupConfiguration(string thresholdMin, string checkIntervalMin, string runOnStartup)
    {
        var thresholdSection = new Mock<IConfigurationSection>();
        thresholdSection.Setup(x => x.Value).Returns(thresholdMin);
        _configurationMock.Setup(x => x.GetSection("PropertyTaxJobRecovery:StuckJobThresholdMinutes")).Returns(thresholdSection.Object);

        var intervalSection = new Mock<IConfigurationSection>();
        intervalSection.Setup(x => x.Value).Returns(checkIntervalMin);
        _configurationMock.Setup(x => x.GetSection("PropertyTaxJobRecovery:RecoveryCheckIntervalMinutes")).Returns(intervalSection.Object);

        var startupSection = new Mock<IConfigurationSection>();
        startupSection.Setup(x => x.Value).Returns(runOnStartup);
        _configurationMock.Setup(x => x.GetSection("PropertyTaxJobRecovery:RunOnStartup")).Returns(startupSection.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var worker = new PropertyTaxJobRecoveryWorker(_loggerMock.Object, _serviceProvider, _configurationMock.Object);
        Assert.NotNull(worker);
    }

    [Fact]
    public async Task ExecuteAsync_WithRunOnStartupAndStuckJob_ResetsJobToPending()
    {
        SetupConfiguration("30", "15", "true");

        var stuckJob = new PropertyTaxJobEntity
        {
            Id = 999,
            JobCode = "JOB-999",
            Status = nameof(JobStatus.InProgress),
            IsActive = true,
            MarkedForDeletion = false,
            StartTime = DateTime.Now.AddMinutes(-40),
            UpdatedDate = DateTime.Now.AddMinutes(-40)
        };

        var jobs = new List<PropertyTaxJobEntity> { stuckJob };
        _jobRepoMock.Setup(r => r.GetQueryable()).Returns(jobs.BuildMock());

        var tcs = new TaskCompletionSource<bool>();
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Callback(() => tcs.TrySetResult(true));

        var worker = new PropertyTaxJobRecoveryWorker(_loggerMock.Object, _serviceProvider, _configurationMock.Object);
        var cts = new CancellationTokenSource();

        var startTask = worker.StartAsync(cts.Token);
        await Task.WhenAny(tcs.Task, Task.Delay(2000));
        cts.Cancel();
        await startTask;
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(nameof(JobStatus.Pending), stuckJob.Status);
        _jobRepoMock.Verify(r => r.UpdateAsync(stuckJob, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
