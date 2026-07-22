using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Interfaces.Property;

/// <summary>
/// Use-case boundary for the "Property Discovery and Dashboard Statistics" capability -
/// two read-only operations that serve the search and monitoring needs of the
/// property management system:
/// <list type="bullet">
///   <item><description><b>SearchPropertiesAsync</b> - a validated multi-criteria query
///   (Quick Search or KYC Search) that returns a paged result set.</description></item>
///   <item><description><b>GetPropertyDashboardStatsAsync</b> - a count-summary query
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
    /// Query - Searches properties using Quick Search or KYC Search criteria with
    /// server-side pagination. Both search modes are selected via queryParameters.SearchType.
    /// Supports Values and Dues filtering via ValuationMethod, FilterType, and amount parameters.
    /// </summary>
    /// <exception cref="NtisPlatform.Application.Exceptions.PropertyValidationException">
    /// Thrown when ValuationMethod/FilterType/amount parameters are invalid or incomplete (e.g., FilterType='Between' without AmountTo).
    /// </exception>
    Task<PagedResult<PropertySearchResponseDto>> SearchPropertiesAsync(PropertySearchQueryParameters queryParameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Query - Returns per-status property counts for the dashboard overview panel.
    /// </summary>
    Task<PropertyDashboardStatsDto> GetPropertyDashboardStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Query - Returns the 3 main dashboard cards with structure/unit/demand breakdown.
    /// Accepts optional filters: property type, type of use, zone, ward, category.
    /// </summary>
    Task<MainCardsResponseDto> GetMainCardsAsync(
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Query - Returns workflow stage cards with structure/unit counts per stage.
    /// Accepts the same optional filters as GetMainCardsAsync.
    /// </summary>
    Task<List<WorkflowStageCardDto>> GetWorkflowCardsAsync(
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Query - Returns scope category options for the property search scope selector.
    /// When category parameter is provided, returns only that category's options;
    /// otherwise returns all categories.
    /// </summary>
    /// <param name="category">Optional scope category filter</param>
    /// <returns>List of scope category DTOs with options</returns>
    List<ScopeCategoryDto> GetScopeOptions(ScopeCategory? category);

    /// <summary>
    /// Query - Returns all units (children) of a given apartment or structure property with optional filtering.
    /// If propertyId refers to an apartment (empty PartitionNo), returns all units (non-empty PartitionNo) and structures.
    /// If propertyId refers to a structure (non-empty PartitionNo), returns all units with the same PartitionNo.
    /// Response displays all results as Units with total count across all matching properties.
    /// Supports the same filters as SearchPropertiesAsync (RV, CV, Total Tax, property type, zone, ward, etc.)
    /// </summary>
    /// <param name="propertyId">Parent property ID (apartment or structure)</param>
    /// <param name="searchRequest">Optional filter request DTO with grid filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Apartment unit list with all properties displayed as units and total count</returns>
    Task<ApartmentUnitListResponseDto> GetApartmentUnitListAsync(int propertyId, PropertySearchRequestDto? searchRequest = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Query - Searches properties scoped by SearchCategory (Zone-wise, Ward-wise, Building-wise,
    /// or a From/To property-number range within a ward), with server-side pagination.
    /// </summary>
    /// <exception cref="NtisPlatform.Application.Exceptions.PropertyValidationException">
    /// Thrown when SearchCategory is invalid, or when the fields required by the selected
    /// category (ZoneId/WardId/PropertyNo/PropertyFrom) are missing or malformed.
    /// </exception>
    Task<PagedResult<PropertySearchByCategoryResponseDto>> SearchByCategoryAsync(PropertySearchByCategoryQueryParameters queryParameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Query - Validates the SearchCategory scope (same rules as SearchByCategoryAsync) and
    /// resolves the bare PropertyIds matching it, without paging/sorting/mapping - for bulk
    /// actions over "every property matching this scope" (e.g. bulk lock/unlock by category).
    /// </summary>
    /// <exception cref="NtisPlatform.Application.Exceptions.PropertyValidationException">
    /// Thrown when SearchCategory is invalid, or when the fields required by the selected
    /// category (ZoneId/WardId/PropertyNo/PropertyFrom) are missing or malformed.
    /// </exception>
    Task<List<int>> ResolvePropertyIdsByCategoryAsync(PropertySearchByCategoryQueryParameters queryParameters, CancellationToken cancellationToken = default);
}
