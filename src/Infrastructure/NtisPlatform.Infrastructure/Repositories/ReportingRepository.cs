using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// Generic repository bound to <see cref="ReportingDbContext"/> (the report queue database).
/// Separate from <see cref="Repository{T, TKey}"/> so reporting entities resolve against the
/// reporting context, not ApplicationDbContext.
/// </summary>
public class ReportingRepository<T, TKey> : IReportingRepository<T, TKey> where T : class
{
    protected readonly ReportingDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public ReportingRepository(ReportingDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        => await _dbSet.FindAsync(new object[] { id! }, cancellationToken);

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual IQueryable<T> GetQueryable() => _dbSet.AsQueryable();

    public virtual async Task<IEnumerable<T>> GetAsync(
        Expression<Func<T, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = _dbSet;
        if (filter != null)
            query = query.Where(filter);
        return await query.ToListAsync(cancellationToken);
    }
}
