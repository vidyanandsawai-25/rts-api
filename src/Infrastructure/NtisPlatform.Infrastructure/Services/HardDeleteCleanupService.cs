using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Service implementation for cleaning up entities marked for hard deletion
/// </summary>
public class HardDeleteCleanupService : IHardDeleteCleanupService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HardDeleteCleanupService> _logger;

    public HardDeleteCleanupService(
        ApplicationDbContext context,
        ILogger<HardDeleteCleanupService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> CleanupMarkedEntitiesAsync(int retentionDays = 0, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.Now.AddDays(-retentionDays);
        var totalDeleted = 0;

        _logger.LogInformation("Starting hard delete cleanup task. Retention days: {RetentionDays}, Cutoff date: {CutoffDate}", 
            retentionDays, cutoffDate);

        try
        {
            // Process each entity type that implements IHardDeletable
            totalDeleted += await CleanupEntityType<Core.Entities.PropertyEntity>(cutoffDate, cancellationToken);

            // Add more entity types here as they implement IHardDeletable
            // totalDeleted += await CleanupEntityType<OtherEntity>(cutoffDate, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Hard delete cleanup completed. Total entities deleted: {TotalDeleted}", totalDeleted);
            return totalDeleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during hard delete cleanup");
            throw;
        }
    }

    private async Task<int> CleanupEntityType<TEntity>(DateTime cutoffDate, CancellationToken cancellationToken) 
        where TEntity : class, IHardDeletable
    {
        try
        {
            var entitiesToDelete = await _context.Set<TEntity>()
                .Where(e => e.MarkedForDeletion && 
                           e.MarkedForDeletionDate.HasValue && 
                           e.MarkedForDeletionDate.Value <= cutoffDate)
                .ToListAsync(cancellationToken);

            if (entitiesToDelete.Any())
            {
                _context.Set<TEntity>().RemoveRange(entitiesToDelete);
                
                _logger.LogInformation("Marked {Count} {EntityType} entities for permanent deletion", 
                    entitiesToDelete.Count, typeof(TEntity).Name);
                
                return entitiesToDelete.Count;
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public async Task MarkForHardDeleteAsync<TEntity>(int id, CancellationToken cancellationToken = default) 
        where TEntity : class
    {
        // TODO: Implement mark for hard delete logic
        await Task.CompletedTask;
    }

    public async Task UnmarkForHardDeleteAsync<TEntity>(int id, CancellationToken cancellationToken = default) 
        where TEntity : class
    {
        // TODO: Implement unmark for hard delete logic
        await Task.CompletedTask;
    }

    public async Task ForceHardDeleteAsync<TEntity>(int id, CancellationToken cancellationToken = default) 
        where TEntity : class
    {
        // TODO: Implement force hard delete logic
        await Task.CompletedTask;
    }
}
