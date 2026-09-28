using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.BackgroundTasks;

/// <summary>
/// Drains <see cref="IBackgroundTaskQueue"/> for the lifetime of the process. Each work item runs
/// in its own freshly-created DI scope (never the scope of whichever HTTP request enqueued it,
/// which is long disposed by the time this runs) and its own try/catch, so one item's failure
/// never stops the loop or takes down the process.
/// </summary>
public sealed class QueuedHostedService : BackgroundService
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<QueuedHostedService> _logger;

    public QueuedHostedService(IBackgroundTaskQueue queue, IServiceScopeFactory scopeFactory, ILogger<QueuedHostedService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Func<IServiceProvider, CancellationToken, Task> workItem;
            try
            {
                workItem = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                await workItem(scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background task queue: a queued work item threw and was dropped.");
            }
        }
    }
}
