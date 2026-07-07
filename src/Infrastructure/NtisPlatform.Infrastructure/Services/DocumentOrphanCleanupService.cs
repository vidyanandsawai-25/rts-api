using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Background service for cleaning up orphaned files from soft-deleted documents.
/// Runs daily at 2 AM UTC.
/// Deletes the underlying storage objects and hard-deletes the corresponding document records
/// after a grace period (default: 7 days).
/// Includes audit logging for all cleanup operations.
/// </summary>
public class DocumentOrphanCleanupService : BackgroundService
{
    private readonly ILogger<DocumentOrphanCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _gracePeriodDays = TimeSpan.FromDays(7);
    private readonly TimeSpan _dailyExecutionTime = TimeSpan.FromHours(2); // 2 AM UTC

    public DocumentOrphanCleanupService(
        ILogger<DocumentOrphanCleanupService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DocumentOrphanCleanupService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Calculate time until next daily execution (2 AM)
                var now = DateTime.UtcNow;
                var next2Am = now.Date.AddDays(1).Add(_dailyExecutionTime);

                // If it's already past 2 AM today, next execution is tomorrow
                if (now.TimeOfDay >= _dailyExecutionTime)
                {
                    next2Am = now.Date.AddDays(1).Add(_dailyExecutionTime);
                }
                else
                {
                    next2Am = now.Date.Add(_dailyExecutionTime);
                }

                var delay = next2Am - now;
                _logger.LogInformation(
                    "DocumentOrphanCleanupService will run at {NextExecutionTime} UTC (in {Hours} hours {Minutes} minutes)",
                    next2Am, delay.Hours, delay.Minutes);

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                // Execute cleanup
                await CleanupOrphanedFilesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("DocumentOrphanCleanupService is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DocumentOrphanCleanupService cleanup loop");
                // Continue running even if one iteration fails
            }
        }

        _logger.LogInformation("DocumentOrphanCleanupService stopped");
    }

    private async Task CleanupOrphanedFilesAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting orphan cleanup task at {ExecutionTime}", DateTime.UtcNow);

            using (var scope = _serviceProvider.CreateScope())
            {
                var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                var fileStorageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

                // Find documents marked for deletion beyond grace period (UTC)
                var cutoffDate = DateTime.UtcNow.Subtract(_gracePeriodDays);
                _logger.LogInformation(
                    "Orphan cleanup: Looking for documents soft-deleted before {CutoffDate} (grace period: {GracePeriodDays} days)",
                    cutoffDate, _gracePeriodDays.Days);

                // Get all soft-deleted documents
                var softDeletedDocs = await documentService.GetSoftDeletedDocumentsAsync(cutoffDate, cancellationToken);
                _logger.LogInformation("Found {DocumentCount} documents eligible for cleanup", softDeletedDocs.Count);

                if (!softDeletedDocs.Any())
                {
                    _logger.LogInformation("No documents to cleanup");
                    return;
                }

                int successCount = 0;
                int failureCount = 0;

                // Process each soft-deleted document
                foreach (var document in softDeletedDocs)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(document.StoragePath))
                        {
                            _logger.LogWarning(
                                "Document {DocumentId} ({DocumentGuid}) marked for deletion but has no storage path",
                                document.Id, document.DocumentGuid);
                            failureCount++;
                            continue;
                        }

                        // Delete file from storage
                        var fileDeleted = await fileStorageService.DeleteFileAsync(document.StoragePath, cancellationToken);
                        if (!fileDeleted)
                        {
                            _logger.LogWarning(
                                "File storage returned false when deleting {StoragePath} for document {DocumentId}. Skipping DB hard-delete so cleanup can retry later.",
                                document.StoragePath, document.Id);
                            failureCount++;
                            continue;
                        }
                        // Delete thumbnail if exists
                        if (!string.IsNullOrWhiteSpace(document.ThumbnailPath))
                        {
                            try
                            {
                                await fileStorageService.DeleteFileAsync(document.ThumbnailPath, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex,
                                    "Failed to delete thumbnail {ThumbnailPath} for document {DocumentId}",
                                    document.ThumbnailPath, document.Id);
                                // Continue - main file was deleted
                            }
                        }

                        // Only hard-delete the document record once the underlying storage object has been deleted (already verified above).

                        // Hard delete the document record
                        await documentService.HardDeleteDocumentAsync(document.Id, cancellationToken);

                        _logger.LogInformation(
                            "Successfully deleted orphaned document {DocumentId} ({DocumentGuid})",
                            document.Id, document.DocumentGuid);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error deleting orphaned document {DocumentId} ({DocumentGuid})",
                            document.Id, document.DocumentGuid);
                        failureCount++;
                        // Continue processing other documents
                    }
                }

                _logger.LogInformation(
                    "Orphan cleanup completed: {SuccessCount} deleted, {FailureCount} failed",
                    successCount, failureCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during orphan cleanup");
            // Don't rethrow - let service continue running
        }
    }
}
