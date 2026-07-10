using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// Generic repository over <see cref="ReportingDbContext"/> (the report database). Behaviourally
/// identical to <see cref="Repository{T, TKey}"/> but bound to a different context, so the report
/// catalogue entities (ReportDefinition / ReportParameterDefinition) can be served through the
/// standard <see cref="IRepository{T, TKey}"/> contract that <c>BaseCommonCrudService</c> expects.
/// Registered as a closed generic in DI for those specific entity types.
/// </summary>
public class ReportDbRepository<T, TKey> : IRepository<T, TKey> where T : class
{
    protected readonly ReportingDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public ReportDbRepository(ReportingDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id! }, cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity is BaseEntity commonEntity)
            commonEntity.CreatedDate = DateTime.UtcNow;

        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var entityList = entities as IList<T> ?? entities.ToList();
        var now = DateTime.UtcNow;

        foreach (var entity in entityList)
        {
            if (entity is BaseEntity commonEntity)
                commonEntity.CreatedDate = now;
        }

        await _dbSet.AddRangeAsync(entityList, cancellationToken);
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity is BaseEntity commonEntity)
            commonEntity.UpdatedDate = DateTime.UtcNow;

        _dbSet.Update(entity);
        await Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
            await DeleteAsync(entity, cancellationToken);
    }

    public virtual async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        // Soft delete for BaseEntity; hard delete otherwise. Mirrors Repository<T, TKey>.
        if (entity is IHardDeletable hardDeletable && entity is BaseEntity baseEntityForHardDelete)
        {
            baseEntityForHardDelete.IsActive = false;
            hardDeletable.MarkedForDeletion = true;
            if (!hardDeletable.MarkedForDeletionDate.HasValue)
                hardDeletable.MarkedForDeletionDate = DateTime.UtcNow;
            await UpdateAsync(entity, cancellationToken);
        }
        else if (entity is BaseEntity baseEntity)
        {
            baseEntity.IsActive = false;
            await UpdateAsync(entity, cancellationToken);
        }
        else
        {
            _dbSet.Remove(entity);
        }
    }

    public virtual Task HardDeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public virtual async Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        return entity != null;
    }

    public virtual IQueryable<T> GetQueryable()
    {
        return _dbSet.AsQueryable();
    }

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
