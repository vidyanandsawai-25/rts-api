namespace NtisPlatform.Application.Utilities;

/// <summary>
/// Provides utility methods for pagination calculations.
/// </summary>
public static class PaginationHelper
{
    /// <summary>
    /// Calculates pagination parameters for query execution.
    /// </summary>
    /// <param name="requestedPageNumber">The requested page number (1-based).</param>
    /// <param name="requestedPageSize">The requested page size. Use -1 to fetch all records.</param>
    /// <param name="totalCount">The total count of records available.</param>
    /// <returns>A tuple containing (pageNumber, pageSize, skip, take) values.</returns>
    /// <remarks>
    /// When <paramref name="requestedPageSize"/> is -1, returns all records with pageNumber=1 and pageSize=totalCount.
    /// </remarks>  
    public static (int pageNumber, int pageSize, int skip, int take) Calculate(
        int requestedPageNumber,
        int requestedPageSize,
        int totalCount)
    {
        // If PageSize == -1, fetch all records and set pageNumber to 1, pageSize to totalCount
        if (requestedPageSize == -1)
            return (1, Math.Max(1, totalCount), 0, totalCount);

        // Otherwise, use the requested values
        return (
            requestedPageNumber,
            requestedPageSize,
            (requestedPageNumber - 1) * requestedPageSize,
            requestedPageSize
        );
    }
}