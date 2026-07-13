namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Unit of work for the separate report queue database (ReportingDbContext).
/// Kept distinct from <see cref="IUnitOfWork"/> (which is bound to ApplicationDbContext) so that
/// saves to reporting entities commit against the correct context.
/// </summary>
public interface IReportingUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    void DiscardChanges();
}
