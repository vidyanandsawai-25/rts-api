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
    Task<PropertyDashboardStatsDto> GetPropertyDashboardStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the 3 main dashboard cards: Previously Registered, Assessment Approved, Additional Revenue Generated.
    /// Supports optional filters: property type, type of use, zone, ward, category.
    /// </summary>
    Task<MainCardsResponseDto> GetMainCardsAsync(
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets workflow stage cards showing structure/unit counts at each stage.
    /// Supports the same optional filters as GetMainCardsAsync.
    /// </summary>
    Task<List<WorkflowStageCardDto>> GetWorkflowCardsAsync(
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all units (children) of a given apartment or structure property with optional filtering.
    /// If propertyId refers to an apartment (empty PartitionNo), returns all units (non-empty PartitionNo) and structures.
    /// If propertyId refers to a structure (non-empty PartitionNo), returns all units with the same PartitionNo.
    /// Response displays all results as "Units" with total count across all matching properties.
    /// Supports the same filters as SearchPropertiesAsync (RV, CV, Total Tax, property type, zone, ward, etc.)
    /// </summary>
    /// <param name="propertyId">Parent property ID (apartment or structure)</param>
    /// <param name="searchRequest">Optional filter request DTO with grid filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Apartment unit list with all properties displayed as units and total count</returns>
    Task<ApartmentUnitListResponseDto> GetApartmentUnitListAsync(int propertyId, PropertySearchRequestDto? searchRequest = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs an independent unified search using pattern-matching heuristics and multi-term keyword matching.
    /// </summary>
    Task<(int TotalCount, List<PropertySearchResponseDto> Items)> UnifiedSearchPropertiesAsync(string query, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches properties scoped by <see cref="Core.Enums.PropertySearchCategory"/> - Zone-wise,
    /// Ward-wise, Building-wise, or a From/To property-number range within a ward.
    /// </summary>
    /// <param name="request">Category and its associated scope parameters</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination (-1 returns all matching rows)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total count and list of properties matching the category scope</returns>
    Task<(int TotalCount, List<PropertySearchByCategoryResponseDto> Items)> SearchByCategoryAsync(
        PropertySearchByCategoryRequestDto request, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the bare PropertyIds matching a SearchCategory scope (same category-switch and
    /// optional filters as <see cref="SearchByCategoryAsync"/>), without the response-DTO mapping,
    /// wing lookup, or natural-sort ordering - for bulk actions over "every property matching this
    /// scope" (e.g. bulk lock/unlock by category).
    /// </summary>
    Task<List<int>> GetPropertyIdsByCategoryAsync(
        PropertySearchByCategoryRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns PropertyNo/PartitionNo suggestions for properties in the given ward whose
    /// PropertyNo and/or PartitionNo contain the supplied terms (SQL LIKE '%term%' semantics),
    /// for a typeahead/autocomplete UI. The full active-property list for the ward is cached
    /// in memory (see implementation) so repeated keystrokes only re-filter, they don't re-query.
    /// </summary>
    /// <param name="wardId">Ward to scope the suggestions to (required).</param>
    /// <param name="propertyNo">Partial PropertyNo term to match anywhere in the value, or null/empty to skip this filter.</param>
    /// <param name="partitionNo">Partial PartitionNo term to match anywhere in the value, or null/empty to skip this filter.</param>
    /// <param name="maxResults">Maximum number of suggestions to return.</param>
    Task<List<PropertySuggestionDto>> GetPropertySuggestionsAsync(
        int wardId, string? propertyNo, string? partitionNo, int maxResults, CancellationToken cancellationToken = default);
}
