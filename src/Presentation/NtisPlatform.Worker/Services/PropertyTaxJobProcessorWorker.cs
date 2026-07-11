using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace NtisPlatform.Worker.Services;

/// <summary>
/// Background worker service for processing property tax jobs asynchronously
/// </summary>
public class PropertyTaxJobProcessorWorker : BackgroundService
{
    private readonly ILogger<PropertyTaxJobProcessorWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public PropertyTaxJobProcessorWorker(
        ILogger<PropertyTaxJobProcessorWorker> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Property Tax Job Processor Worker starting at: {time}", DateTimeOffset.Now);

        var pollIntervalSeconds = _configuration.GetValue<int>("PropertyTaxJobProcessor:PollIntervalSeconds", 2);
        _logger.LogInformation("Property Tax Job Processor Config - Poll Interval: {PollIntervalSeconds}s", pollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var jobRepo = scope.ServiceProvider.GetRequiredService<IRepository<PropertyTaxJobEntity, int>>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                // Activate any scheduled jobs whose execution time has arrived
                var scheduledJobsToActivate = await jobRepo.GetQueryable()
                    .Where(j => j.Status == nameof(JobStatus.Scheduled)
                        && j.IsActive
                        && !j.MarkedForDeletion
                        && j.StartTime <= DateTime.Now)
                    .ToListAsync(stoppingToken);

                if (scheduledJobsToActivate.Any())
                {
                    foreach (var scheduledJob in scheduledJobsToActivate)
                    {
                        _logger.LogInformation("Scheduled job {JobCode} execution time has arrived. Activating...", scheduledJob.JobCode);
                        scheduledJob.Status = nameof(JobStatus.Pending);
                        scheduledJob.UpdatedDate = DateTime.Now;
                        await jobRepo.UpdateAsync(scheduledJob, stoppingToken);
                    }
                    await unitOfWork.SaveChangesAsync(stoppingToken);
                }

                // Poll for the oldest Pending active job (ordered by CreatedDate)
                var job = await jobRepo.GetQueryable()
                    .Where(j => j.Status == nameof(JobStatus.Pending) 
                        && j.IsActive 
                        && !j.MarkedForDeletion)
                    .OrderBy(j => j.CreatedDate)
                    .FirstOrDefaultAsync(stoppingToken);

                if (job != null)
                {
                    _logger.LogInformation("Found pending job {JobCode}. Attempting to lock...", job.JobCode);

                    // Lock the job to InProgress state so no other worker picks it up
                    job.Status = nameof(JobStatus.InProgress);
                    job.StartTime = DateTime.Now;
                    job.UpdatedDate = DateTime.Now; // Triggers the concurrency token check

                    try
                    {
                        await jobRepo.UpdateAsync(job, stoppingToken);
                        await unitOfWork.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Successfully claimed job {JobCode}. Starting execution...", job.JobCode);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        // Concurrency conflict: Another worker instance claimed this job first
                        _logger.LogWarning("Job {JobCode} was already claimed by another worker instance. Skipping.", job.JobCode);
                        unitOfWork.DiscardChanges();
                        continue; // Proceed to the next loop iteration to look for a different job
                    }

                    // Resolve the scoped service and run calculations
                    var operationsService = scope.ServiceProvider.GetRequiredService<IPropertyTaxOperationsService>();
                    
                    try
                    {
                        await operationsService.ProcessJobAsync(job.Id, stoppingToken);
                        _logger.LogInformation("Job {JobCode} processed successfully.", job.JobCode);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing job {JobCode}", job.JobCode);
                        
                        // Mark job as Failed in case of catastrophic error
                        job.Status = nameof(JobStatus.Failed);
                        job.CompleteTime = DateTime.Now;
                        job.DurationMs = (long)(job.CompleteTime.Value - job.StartTime).TotalMilliseconds;
                        job.Remarks = "This job operation failed.";
                        job.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                        job.UpdatedDate = DateTime.Now;
                        await jobRepo.UpdateAsync(job, stoppingToken);
                        await unitOfWork.SaveChangesAsync(stoppingToken);
                    }
                }

                // Poll interval from configuration
                await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Property Tax Job Processor Worker main loop");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("Property Tax Job Processor Worker stopping at: {time}", DateTimeOffset.Now);
    }
}
