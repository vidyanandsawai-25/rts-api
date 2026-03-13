using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation using EF Core
/// Handles both BaseEntity (int keys, soft delete) and BaseEntity (string keys, hard delete)
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
/// <typeparam name="TKey">Primary key type</typeparam>
public class Repository<T, TKey> : IRepository<T, TKey> where T : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
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
        // Set CreatedDate for BaseEntity
        if (entity is BaseEntity commonEntity)
        {
            commonEntity.CreatedDate = DateTime.Now;
        }
        
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        // Set UpdatedDate for BaseEntity
        if (entity is BaseEntity commonEntity)
        {
            commonEntity.UpdatedDate = DateTime.Now;
        }
        
        _dbSet.Update(entity);
        await Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            // For entities implementing IHardDeletable, perform soft delete and set deletion timestamp
            // The entity will be permanently deleted by the nightly cleanup task
            if (entity is IHardDeletable hardDeletable && entity is BaseEntity baseEntityForHardDelete)
            {
                baseEntityForHardDelete.IsActive = false;
                // Only set deletion date if not already set (preserves original deletion timestamp)
                if (!hardDeletable.MarkedForDeletionDate.HasValue)
                {
                    hardDeletable.MarkedForDeletionDate = DateTime.Now;
                }
                await UpdateAsync(entity, cancellationToken);
            }
            // Soft delete for regular BaseEntity
            else if (entity is BaseEntity baseEntity)
            {
                baseEntity.IsActive = false;
                await UpdateAsync(entity, cancellationToken);
            }
            // Hard delete for entities that don't inherit from BaseEntity
            else
            {
                _dbSet.Remove(entity);
            }
        }
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
        {
            query = query.Where(filter);
        }

        return await query.ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Repository implementation for BaseEntity with int key
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public class Repository<T> : Repository<T, int>, IRepository<T> where T : BaseEntity
{
    public Repository(ApplicationDbContext context) : base(context)
    {
    }
}
