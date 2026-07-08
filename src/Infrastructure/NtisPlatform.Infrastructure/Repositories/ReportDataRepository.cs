using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// Read-only repository bound to <see cref="ReportDataDbContext"/> (the report data replica).
/// Queries are returned with AsNoTracking() by default — callers compose further LINQ on top.
/// </summary>
public class ReportDataRepository<T> : IReportDataRepository<T> where T : class
{
    private readonly ReportDataDbContext _context;

    public ReportDataRepository(ReportDataDbContext context)
    {
        _context = context;
    }

    public IQueryable<T> GetQueryable() => _context.Set<T>().AsNoTracking();
}
