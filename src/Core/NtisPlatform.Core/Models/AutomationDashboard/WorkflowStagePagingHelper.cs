namespace NtisPlatform.Core.Models.AutomationDashboard;

public static class WorkflowStagePagingHelper
{
    /// <summary>
    /// Normalizes nullable paging input and caps page size for stable API performance.
    /// </summary>
    public static (int PageNumber, int PageSize) NormalizePaging(int? pageNumber, int? pageSize)
    {
        var normalizedPageNumber = pageNumber.GetValueOrDefault(1);
        var normalizedPageSize = pageSize.GetValueOrDefault(10);

        if (normalizedPageNumber < 1) normalizedPageNumber = 1;
        if (normalizedPageSize < 1) normalizedPageSize = 10;
        if (normalizedPageSize > 500) normalizedPageSize = 500;

        return (normalizedPageNumber, normalizedPageSize);
    }

    /// <summary>
    /// Applies paging after totals are calculated from the complete ward result set.
    /// </summary>
    public static List<T> PageWardData<T>(IEnumerable<T> wardData, int pageNumber, int pageSize)
        => wardData
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
}
