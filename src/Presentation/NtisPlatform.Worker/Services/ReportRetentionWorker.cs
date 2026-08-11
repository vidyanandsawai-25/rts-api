using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Worker.Services;

/// <summary>
/// Deletes terminal report requests (Completed/Failed/Cancelled) and their stored PDFs once they
/// pass the retention window, so the report queue DB and file storage don't grow unbounded.
/// </summary>
public class ReportRetentionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReportRetentionWorker> _logger;

    public ReportRetentionWorker(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<ReportRetentionWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var retentionDays = _configuration.GetValue<int>("Reporting:RetentionDays", 1);
        var sweepHours = _configuration.GetValue<int>("Reporting:RetentionSweepHours", 24);
        var batchSize = _configuration.GetValue<int>("Reporting:RetentionBatchSize", 200);

        _logger.LogInformation(
            "Report retention worker started (keep {Days}d, sweep every {Hours}h).", retentionDays, sweepHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(retentionDays, batchSize, stoppingToken);
                await Task.Delay(TimeSpan.FromHours(sweepHours), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in report retention sweep.");
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }

        _logger.LogInformation("Report retention worker stopping.");
    }

    private async Task SweepAsync(int retentionDays, int batchSize, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
        var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();

        var cutoff = DateTime.Now.AddDays(-retentionDays);

        var expired = await db.ReportRequests
            .Where(r => (r.Status == ReportRequestStatus.Completed
                         || r.Status == ReportRequestStatus.Failed
                         || r.Status == ReportRequestStatus.Cancelled)
                        && (r.CompletedDate ?? r.CreatedDate) < cutoff)
            .OrderBy(r => r.CreatedDate)
            .Take(batchSize)
            .ToListAsync(ct);

        if (expired.Count == 0)
            return;

        var toDelete = new List<Core.Entities.Reporting.ReportRequestEntity>();

        foreach (var request in expired)
        {
            // Remove the stored PDF (file + Document row) before the queue row.
            if (request.OutputDocumentGuid.HasValue)
            {
                try
                {
                    var doc = await documentService.GetDocumentByGuidAsync(request.OutputDocumentGuid.Value, ct);
                    if (doc is not null)
                    {
                        //await fileStorage.DeleteFileAsync(doc.StoragePath, ct);
                        await documentService.DeleteDocumentAsync(request.OutputDocumentGuid.Value, request.RequestedByUserId, ct);
                        // File deletion is handled by DocumentOrphanCleanupService when the document is soft-deleted.
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to delete document {DocumentGuid} for expired request {RequestId}; skipping request deletion so cleanup can be retried.",
                        request.OutputDocumentGuid, request.ReportRequestId);
                    continue;
                }
            }

            toDelete.Add(request);
        }

        if (toDelete.Count == 0)
            return;

        // Single set-based delete for all expired requests' logs, instead of one round-trip per request.
        var idsToDelete = toDelete.Select(r => r.ReportRequestId).ToList();
        await db.ReportRequestLogs
            .Where(l => idsToDelete.Contains(l.ReportRequestId))
            .ExecuteDeleteAsync(ct);

        db.ReportRequests.RemoveRange(toDelete);

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Report retention removed {Count} expired request(s).", toDelete.Count);
    }
}
