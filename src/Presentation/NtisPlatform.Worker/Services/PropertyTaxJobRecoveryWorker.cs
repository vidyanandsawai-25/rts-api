using NtisPlatform.Application.Enums;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace NtisPlatform.Worker.Services;

/// <summary>
/// Background worker service to detect and reset stuck InProgress property tax jobs
/// back to Pending status so they can be processed again.
/// </summary>
public class PropertyTaxJobRecoveryWorker : BackgroundService
{
    private readonly ILogger<PropertyTaxJobRecoveryWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public PropertyTaxJobRecoveryWorker(
        ILogger<PropertyTaxJobRecoveryWorker> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Property Tax Job Recovery Worker starting at: {time}", DateTimeOffset.Now);

        // Load configurations
        var thresholdMinutes = _configuration.GetValue<int>("PropertyTaxJobRecovery:StuckJobThresholdMinutes", 30);
        var intervalMinutes = _configuration.GetValue<int>("PropertyTaxJobRecovery:RecoveryCheckIntervalMinutes", 15);
        var runOnStartup = _configuration.GetValue<bool>("PropertyTaxJobRecovery:RunOnStartup", true);

        _logger.LogInformation(
            "Job Recovery Config - Threshold: {ThresholdMinutes}m, Check Interval: {IntervalMinutes}m, Run on Startup: {RunOnStartup}",
            thresholdMinutes, intervalMinutes, runOnStartup);

        // Run immediately on startup if configured
        if (runOnStartup)
        {
            await RunRecoveryTask(thresholdMinutes, stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Sleep for the configured interval
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);

                // Run the stuck job check
                await RunRecoveryTask(thresholdMinutes, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Property Tax Job Recovery Worker main loop");
                // Sleep for a shorter duration before retrying in case of an error
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }

        _logger.LogInformation("Property Tax Job Recovery Worker stopping at: {time}", DateTimeOffset.Now);
    }

    private async Task RunRecoveryTask(int thresholdMinutes, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting stuck property tax job scan at: {time}", DateTimeOffset.Now);

        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var jobRepo = scope.ServiceProvider.GetRequiredService<IRepository<PropertyTaxJobEntity, int>>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var now = DateTime.Now;
            var localThresholdTime = now.AddMinutes(-thresholdMinutes);

            var stuckJobsList = await jobRepo.GetQueryable()
                .Where(j => j.Status == nameof(JobStatus.InProgress)
                    && j.IsActive
                    && !j.MarkedForDeletion
                    && ((j.UpdatedDate.HasValue && j.UpdatedDate.Value < localThresholdTime)
                        || (!j.UpdatedDate.HasValue && j.StartTime < localThresholdTime)))
                .ToListAsync(stoppingToken);

            var recoveredCount = 0;

            foreach (var job in stuckJobsList)
            {
                _logger.LogWarning(
                    "Found stuck job {JobCode} (ID: {JobId}). Last updated: {LastUpdated}, started: {Started}. Resetting to Pending.",
                    job.JobCode, job.Id, job.UpdatedDate, job.StartTime);

                job.Status = nameof(JobStatus.Pending);
                job.Remarks = $"Job reset to Pending automatically by Recovery Worker due to inactivity (over {thresholdMinutes} minutes).";
                job.ErrorMessage = null;
                job.StartTime = DateTime.Now; // Will be overwritten by main worker when reclaimed
                job.UpdatedDate = DateTime.Now;

                await jobRepo.UpdateAsync(job, stoppingToken);
                recoveredCount++;
            }

            if (recoveredCount > 0)
            {
                await unitOfWork.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Successfully recovered {Count} stuck jobs back to Pending status.", recoveredCount);
            }
            else
            {
                _logger.LogInformation("Stuck job scan completed. No stuck jobs found.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run stuck property tax job recovery scan");
        }
    }
}
