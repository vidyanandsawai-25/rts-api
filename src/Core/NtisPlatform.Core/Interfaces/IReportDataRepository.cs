namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Read-only repository over the report DATA replica (ReportDataDbContext). Exposes only a
/// queryable — report data providers compose LINQ with AsNoTracking() against it, so heavy
/// reporting reads run on the read-only connection rather than the transactional database.
/// </summary>
public interface IReportDataRepository<T> where T : class
{
    IQueryable<T> GetQueryable();
}
