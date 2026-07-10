using Microsoft.EntityFrameworkCore.Storage;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// Unit of work bound to <see cref="ReportingDbContext"/> (the report queue database).
/// </summary>
public class ReportingUnitOfWork : IReportingUnitOfWork
{
    private readonly ReportingDbContext _context;
    private IDbContextTransaction? _transaction;

    public ReportingUnitOfWork(ReportingDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            if (_transaction != null)
                await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void DiscardChanges() => _context.ChangeTracker.Clear();

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
