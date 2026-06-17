using NtisPlatform.Core.Models;

namespace NtisPlatform.Core.Interfaces.Property;

/// <summary>
/// Data-access for the Property Search screen: the multi-criteria property search
/// (Quick Search / KYC Search / Values &amp; Dues) and the dashboard count statistics.
/// </summary>
public interface IPropertySearchRepository
{
    /// <summary>
    /// Searches properties based on Quick Search or KYC Search criteria with pagination.
    /// </summary>
    /// <param name="searchRequest">Search parameters from either Quick Search or KYC Search tab</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination (-1 returns all matching rows)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total count and list of properties matching search criteria</returns>
    Task<(int TotalCount, List<PropertySearchResponseDto> Items)> SearchPropertiesAsync(PropertySearchRequestDto searchRequest, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets property dashboard statistics for the property search screen.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dashboard statistics with various property counts</returns>
    Task<PropertyDashboardStatsDto> GetPropertyDashboardStatsAsync(CancellationToken cancellationToken = default);
}
