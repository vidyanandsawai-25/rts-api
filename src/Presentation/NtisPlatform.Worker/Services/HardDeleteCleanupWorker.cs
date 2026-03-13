using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Worker.Services;

/// <summary>
/// Hosted service that runs the hard delete cleanup task on a schedule
/// </summary>
public class HardDeleteCleanupWorker : BackgroundService
{
    private readonly ILogger<HardDeleteCleanupWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public HardDeleteCleanupWorker(
        ILogger<HardDeleteCleanupWorker> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Hard Delete Cleanup Worker starting at: {time}", DateTimeOffset.Now);

        // Get configuration settings
        var intervalHours = _configuration.GetValue<int>("CleanupWorker:IntervalHours", 24);
        var retentionDays = _configuration.GetValue<int>("CleanupWorker:RetentionDays", 0);
        var runOnStartup = _configuration.GetValue<bool>("CleanupWorker:RunOnStartup", false);

        _logger.LogInformation(
            "Cleanup Worker Configuration - Interval: {IntervalHours}h, Retention: {RetentionDays} days, Run on Startup: {RunOnStartup}",
            intervalHours, retentionDays, runOnStartup);

        // Optionally run immediately on startup
        if (runOnStartup)
        {
            await RunCleanupTask(retentionDays, stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Calculate delay until next run
                var nextRun = CalculateNextRun(intervalHours);
                var delay = nextRun - DateTime.Now;

                if (delay.TotalMilliseconds > 0)
                {
                    _logger.LogInformation("Next cleanup scheduled for: {NextRun} (in {Hours:F2} hours)",
                        nextRun, delay.TotalHours);
                    
                    await Task.Delay(delay, stoppingToken);
                }

                // Run the cleanup task
                await RunCleanupTask(retentionDays, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Expected when service is stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Hard Delete Cleanup Worker main loop");
                
                // Wait before retrying to avoid rapid failure loops
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Hard Delete Cleanup Worker stopping at: {time}", DateTimeOffset.Now);
    }

    private async Task RunCleanupTask(int retentionDays, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting hard delete cleanup task at: {time}", DateTimeOffset.Now);

        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var cleanupService = scope.ServiceProvider.GetRequiredService<IHardDeleteCleanupService>();
                var deletedCount = await cleanupService.CleanupMarkedEntitiesAsync(retentionDays, stoppingToken);
                
                _logger.LogInformation(
                    "Hard delete cleanup completed successfully. Deleted {Count} entities at: {time}",
                    deletedCount, DateTimeOffset.Now);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute hard delete cleanup task");
        }
    }

    private DateTime CalculateNextRun(int intervalHours)
    {
        // If interval is 24 hours, run at 2 AM
        if (intervalHours == 24)
        {
            var now = DateTime.Now;
            var scheduledTime = new DateTime(now.Year, now.Month, now.Day, 2, 0, 0);
            
            // If 2 AM today has passed, schedule for tomorrow
            if (scheduledTime <= now)
            {
                scheduledTime = scheduledTime.AddDays(1);
            }
            
            return scheduledTime;
        }
        
        // For other intervals, just add the hours
        return DateTime.Now.AddHours(intervalHours);
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Hard Delete Cleanup Worker started");
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Hard Delete Cleanup Worker stopped");
        return base.StopAsync(cancellationToken);
    }
}
