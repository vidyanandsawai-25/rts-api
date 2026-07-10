using System.Linq.Expressions;

namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Generic repository for entities in the separate report queue database (ReportingDbContext).
/// Mirrors <see cref="IRepository{T, TKey}"/> but is bound to the reporting context, so reporting
/// entities never resolve against ApplicationDbContext. Use this for ReportRequest/ReportRequestLog.
/// </summary>
public interface IReportingRepository<T, TKey> where T : class
{
    Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    IQueryable<T> GetQueryable();
    Task<IEnumerable<T>> GetAsync(
        Expression<Func<T, bool>>? filter = null,
        CancellationToken cancellationToken = default);
}
