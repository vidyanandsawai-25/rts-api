using System.Linq.Expressions;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Generic repository interface for data access operations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
/// <typeparam name="TKey">Primary key type</typeparam>
public interface IRepository<T, TKey> where T : class
{
    Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Deletes an already-loaded entity. Use this overload to avoid redundant lookups
    /// when the entity has already been retrieved.
    /// </summary>
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);

    IQueryable<T> GetQueryable();
    Task<IEnumerable<T>> GetAsync(
        Expression<Func<T, bool>>? filter = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for BaseEntity with int key
/// </summary>
public interface IRepository<T> : IRepository<T, int> where T : BaseEntity
{
}
