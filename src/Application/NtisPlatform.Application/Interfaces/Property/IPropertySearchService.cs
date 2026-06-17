using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Interfaces.Property;

/// <summary>
/// Use-case boundary for the "Property Discovery and Dashboard Statistics" capability —
/// two read-only operations that serve the search and monitoring needs of the
/// property management system:
/// <list type="bullet">
///   <item><description><b>SearchPropertiesAsync</b> — a validated multi-criteria query
///   (Quick Search or KYC Search) that returns a paged result set.</description></item>
///   <item><description><b>GetPropertyDashboardStatsAsync</b> — a count-summary query
///   returning per-status aggregates for the dashboard.</description></item>
/// </list>
/// <para>
/// Both operations are pure queries (no mutation, no transaction). They are deliberately
/// separated from the per-property mutation use-cases so the read path carries no
/// write-side transaction overhead and can be scaled or cached independently.
/// </para>
/// </summary>
public interface IPropertySearchService
{
    /// <summary>
    /// <b>Query</b> — Searches properties using Quick Search or KYC Search criteria with
    /// server-side pagination. Both search modes are selected via <c>queryParameters.SearchType</c>.
    /// </summary>
    /// <exception cref="NtisPlatform.Application.Exceptions.PropertyValidationException">
    /// Thrown when amount-filter parameters (min/max tax amount) are invalid.
    /// </exception>
    Task<PagedResult<PropertySearchResponseDto>> SearchPropertiesAsync(PropertySearchQueryParameters queryParameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// <b>Query</b> — Returns per-status property counts (Registered, Geo-Sequenced, Assessed,
    /// Unassessed, Survey) for the dashboard overview panel.
    /// </summary>
    Task<PropertyDashboardStatsDto> GetPropertyDashboardStatsAsync(CancellationToken cancellationToken = default);
}
