namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service for cleaning up entities marked for hard deletion
/// </summary>
public interface IHardDeleteCleanupService
{
    /// <summary>
    /// Permanently deletes all entities marked for deletion that meet the retention criteria
    /// </summary>
    /// <param name="retentionDays">Number of days to wait before permanently deleting marked entities</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entities permanently deleted</returns>
    Task<int> CleanupMarkedEntitiesAsync(int retentionDays = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a specific entity for hard deletion
    /// </summary>
    /// <typeparam name="TEntity">Entity type that implements IHardDeletable</typeparam>
    /// <typeparam name="TKey">Type of the entity's primary key</typeparam>
    /// <param name="id">Entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task MarkForHardDeleteAsync<TEntity, TKey>(TKey id, CancellationToken cancellationToken = default) where TEntity : class;

    /// <summary>
    /// Unmarks an entity from hard deletion (recovery)
    /// </summary>
    /// <typeparam name="TEntity">Entity type that implements IHardDeletable</typeparam>
    /// <typeparam name="TKey">Type of the entity's primary key</typeparam>
    /// <param name="id">Entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UnmarkForHardDeleteAsync<TEntity, TKey>(TKey id, CancellationToken cancellationToken = default) where TEntity : class;

    /// <summary>
    /// Immediately performs hard delete on a specific entity (bypasses retention period)
    /// This is an irreversible operation and should be used with extreme caution.
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Type of the entity's primary key</typeparam>
    /// <param name="id">Entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if entity not found</returns>
    Task<bool> ForceHardDeleteAsync<TEntity, TKey>(TKey id, CancellationToken cancellationToken = default) where TEntity : class;
}
