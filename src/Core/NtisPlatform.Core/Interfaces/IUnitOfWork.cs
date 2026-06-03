namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Unit of work interface for managing database transactions
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discards all pending (unsaved) tracked changes. Use after a failed operation to stop those
    /// partial changes from being re-attempted by a later SaveChanges; changes already saved within
    /// an open transaction are untouched and remain subject to commit/rollback.
    /// </summary>
    void DiscardChanges();
}
