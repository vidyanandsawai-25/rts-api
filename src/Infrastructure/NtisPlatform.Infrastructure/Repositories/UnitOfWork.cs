using Microsoft.EntityFrameworkCore.Storage;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// Unit of work implementation for managing database transactions
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    /// <summary>
    /// Counts nested Begin/Commit calls sharing the one physical transaction below. UnitOfWork is
    /// scoped (one instance per request), so a caller that opens a transaction and then triggers
    /// other application code synchronously in-process (e.g. certificate save publishing
    /// PropertyCertificateChangedEvent, which the RV recalculation pipeline handles inline via
    /// MediatR) can end up calling BeginTransactionAsync a second time on the SAME connection --
    /// which EF Core's underlying ADO.NET connection does not support and throws
    /// InvalidOperationException("The connection is already in a transaction..."). Only the
    /// outermost Begin/Commit pair actually opens/commits the physical transaction; nested callers
    /// join it and just flush their own changes.
    /// </summary>
    private int _transactionDepth;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            // Already inside an ambient transaction on this scoped UnitOfWork -- join it instead
            // of attempting a second physical BeginTransactionAsync.
            _transactionDepth++;
            return;
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        _transactionDepth = 1;
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transactionDepth > 1)
        {
            // Nested caller: flush its own changes so later code sharing this ambient
            // transaction can see them, but leave the actual commit to the outermost caller.
            await SaveChangesAsync(cancellationToken);
            _transactionDepth--;
            return;
        }

        try
        {
            await SaveChangesAsync(cancellationToken);
            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
            }
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
                _transactionDepth = 0;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        // A rollback always tears down the whole ambient transaction, even when requested by a
        // nested caller -- a nested failure (e.g. RV recalculation failing mid-way through a
        // certificate bulk-save) must not let the outermost caller's eventual commit proceed as
        // if nothing happened, since that would commit a certificate row whose tax recalculation
        // never actually completed.
        _transactionDepth = 0;

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
        _context.Dispose();
    }
}
