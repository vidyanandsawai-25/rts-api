using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Enums;
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
    /// <b>Query</b> — Returns per-status property counts for the dashboard overview panel.
    /// </summary>
    Task<PropertyDashboardStatsDto> GetPropertyDashboardStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of available scope categories and their input options.
    /// </summary>
    /// <param name="category">Optional scope category filter</param>
    /// <b>Query</b> — Returns the 3 main dashboard cards with structure/unit/demand breakdown.
    /// Accepts optional filters: property type, type of use, zone, ward, category.
    /// </summary>
    Task<MainCardsResponseDto> GetMainCardsAsync(
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <b>Query</b> — Returns workflow stage cards with structure/unit counts per stage.
    /// Accepts the same optional filters as GetMainCardsAsync.
    /// </summary>
    Task<List<WorkflowStageCardDto>> GetWorkflowCardsAsync(
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <b>Query</b> — Returns scope category options for the property search scope selector.
    /// When <paramref name="category"/> is provided, returns only that category's options;
    /// otherwise returns all categories.
    /// </summary>
    List<ScopeCategoryDto> GetScopeOptions(ScopeCategory? category);

    /// <summary>
    /// <b>Query</b> — Returns all units (children) of a given apartment or structure property.
    /// If propertyId refers to an apartment, returns all structures.
    /// If propertyId refers to a structure, returns all units of that structure.
    /// </summary>
    /// <param name="propertyId">Parent property ID (apartment or structure)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of child properties (structures or units)</returns>
    Task<List<PropertySearchResponseDto>> GetApartmentUnitListAsync(int propertyId, CancellationToken cancellationToken = default);
}
