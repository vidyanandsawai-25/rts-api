using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Worker.Services;
using Xunit;

namespace NtisPlatform.Tests.Worker.Services;

public class PropertyTaxJobProcessorWorkerTests
{
    private readonly Mock<ILogger<PropertyTaxJobProcessorWorker>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IRepository<PropertyTaxJobEntity, int>> _jobRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPropertyTaxOperationsService> _operationsServiceMock;
    private readonly IServiceProvider _serviceProvider;

    public PropertyTaxJobProcessorWorkerTests()
    {
        _loggerMock = new Mock<ILogger<PropertyTaxJobProcessorWorker>>();
        _configurationMock = new Mock<IConfiguration>();
        _jobRepoMock = new Mock<IRepository<PropertyTaxJobEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _operationsServiceMock = new Mock<IPropertyTaxOperationsService>();

        var services = new ServiceCollection();
        services.AddSingleton(_jobRepoMock.Object);
        services.AddSingleton(_unitOfWorkMock.Object);
        services.AddSingleton(_operationsServiceMock.Object);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void SetupConfiguration(string pollInterval)
    {
        var intervalSection = new Mock<IConfigurationSection>();
        intervalSection.Setup(x => x.Value).Returns(pollInterval);
        _configurationMock.Setup(x => x.GetSection("PropertyTaxJobProcessor:PollIntervalSeconds")).Returns(intervalSection.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var worker = new PropertyTaxJobProcessorWorker(_loggerMock.Object, _serviceProvider, _configurationMock.Object);
        Assert.NotNull(worker);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoJobs_DoesNotCallProcessJob()
    {
        SetupConfiguration("1");
        _jobRepoMock.Setup(r => r.GetQueryable()).Returns(new List<PropertyTaxJobEntity>().BuildMock());

        var worker = new PropertyTaxJobProcessorWorker(_loggerMock.Object, _serviceProvider, _configurationMock.Object);
        var cts = new CancellationTokenSource();

        var startTask = worker.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        await startTask;
        await worker.StopAsync(CancellationToken.None);

        _operationsServiceMock.Verify(s => s.ProcessJobAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithPendingJob_ProcessesJobSuccessfully()
    {
        SetupConfiguration("1");

        var pendingJob = new PropertyTaxJobEntity
        {
            Id = 123,
            JobCode = "JOB-123",
            Status = nameof(JobStatus.Pending),
            IsActive = true,
            MarkedForDeletion = false,
            CreatedDate = DateTime.Now.AddMinutes(-5)
        };

        var jobs = new List<PropertyTaxJobEntity> { pendingJob };
        _jobRepoMock.Setup(r => r.GetQueryable()).Returns(jobs.BuildMock());

        var tcs = new TaskCompletionSource<bool>();
        _operationsServiceMock
            .Setup(s => s.ProcessJobAsync(123, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() => tcs.TrySetResult(true));

        var worker = new PropertyTaxJobProcessorWorker(_loggerMock.Object, _serviceProvider, _configurationMock.Object);
        var cts = new CancellationTokenSource();

        var startTask = worker.StartAsync(cts.Token);
        await Task.WhenAny(tcs.Task, Task.Delay(2000));
        cts.Cancel();
        await startTask;
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(nameof(JobStatus.InProgress), pendingJob.Status);
        _operationsServiceMock.Verify(s => s.ProcessJobAsync(123, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
