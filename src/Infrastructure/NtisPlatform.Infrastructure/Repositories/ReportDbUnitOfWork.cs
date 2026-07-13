using Microsoft.EntityFrameworkCore.Storage;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// <see cref="IUnitOfWork"/> bound to <see cref="ReportingDbContext"/> so the report catalogue
/// CRUD services (ReportDefinition / ReportParameterDefinition) commit to the report database.
/// Resolves the same scoped <see cref="ReportingDbContext"/> as <see cref="ReportDbRepository{T, TKey}"/>,
/// so tracked changes from the repository are persisted by SaveChanges here.
/// </summary>
public class ReportDbUnitOfWork : IUnitOfWork
{
    private readonly ReportingDbContext _context;
    private IDbContextTransaction? _transaction;

    public ReportDbUnitOfWork(ReportingDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

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

    public void DiscardChanges()
    {
        _context.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        // The ReportingDbContext is owned by the DI scope (created from the pooled factory),
        // so it is not disposed here — disposing it would return it to the pool prematurely.
    }
}
