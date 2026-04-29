using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Bulk;
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

    public HardDeleteCleanupService(ApplicationDbContext context,ILogger<HardDeleteCleanupService> logger)
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
            // Note: PropertyEntity implements IHardDeletable but MarkedForDeletionDate column 
            // doesn't exist in database yet (EF Core ignores it), so this won't find any records 
            // to delete until column is added to database
            totalDeleted += await CleanupEntityType<Core.Entities.PropertyEntity>(cutoffDate, cancellationToken);

            // UserEntity cleanup - removes users marked for deletion after retention period
            totalDeleted += await CleanupEntityType<Core.Entities.Master.UserEntity>(cutoffDate, cancellationToken);

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

    public async Task MarkForHardDeleteAsync<TEntity, TKey>(TKey id, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        // TODO: Implement mark for hard delete logic
        await Task.CompletedTask;
    }

    public async Task UnmarkForHardDeleteAsync<TEntity, TKey>(TKey id, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        // TODO: Implement unmark for hard delete logic
        await Task.CompletedTask;
    }

    public async Task<bool> ForceHardDeleteAsync<TEntity, TKey>(TKey id, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        try
        {
            var entity = await _context.Set<TEntity>().FindAsync(new object[] { id }, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Force hard delete failed - {EntityType} with ID {Id} not found",
                    typeof(TEntity).Name, id);
                return false;
            }

            _context.Set<TEntity>().Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Force hard delete completed for {EntityType} with ID {Id}",
                typeof(TEntity).Name, id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during force hard delete of {EntityType} with ID {Id}",
                typeof(TEntity).Name, id);
            throw;
        }
    }

    public async Task<BulkResult<TKey>> BulkForceHardDeleteAsync<TEntity, TKey>(TKey[] ids, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        if (ids == null || ids.Length == 0)
        {
            return new BulkResult<TKey>(0, 0, []);
        }

        _logger.LogInformation("Starting bulk force hard delete for {Count} {EntityType} entities",
            ids.Length, typeof(TEntity).Name);

        var deletedIds = new List<TKey>();
        var errors = new List<string>();
        var failedCount = 0;

        foreach (var id in ids)
        {
            try
            {
                var entity = await _context.Set<TEntity>().FindAsync(new object[] { id }, cancellationToken);

                if (entity == null)
                {
                    failedCount++;
                    errors.Add($"{typeof(TEntity).Name} with ID {id} not found");
                    _logger.LogWarning("Bulk force hard delete - {EntityType} with ID {Id} not found",
                        typeof(TEntity).Name, id);
                }
                else
                {
                    _context.Set<TEntity>().Remove(entity);
                    deletedIds.Add(id);
                }
            }
            catch (Exception ex)
            {
                failedCount++;
                errors.Add($"Error deleting {typeof(TEntity).Name} with ID {id}: {ex.Message}");
                _logger.LogError(ex, "Error during bulk force hard delete of {EntityType} with ID {Id}",
                    typeof(TEntity).Name, id);
            }
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Bulk force hard delete completed. Success: {SuccessCount}, Failed: {FailedCount}",
                deletedIds.Count, failedCount);
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Constraint violation or database error during bulk force hard delete. No entities deleted.");
            // All deletes failed, so clear deletedIds and add a global error
            errors.Add("Bulk delete failed due to database constraint violation. No entities were deleted.");
            return new BulkResult<TKey>(0, ids.Length, [], errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes during bulk force hard delete");
            throw;
        }

        return new BulkResult<TKey>(
            deletedIds.Count,
            failedCount,
            deletedIds,
            errors.Count > 0 ? errors : null);
    }
}