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
    /// Gets all units (children) of a given apartment or structure property.
    /// If propertyId refers to an apartment, returns all structures.
    /// If propertyId refers to a structure, returns all units of that structure.
    /// </summary>
    /// <param name="propertyId">Parent property ID (apartment or structure)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of child properties (structures or units)</returns>
    Task<List<PropertySearchResponseDto>> GetApartmentUnitListAsync(int propertyId, CancellationToken cancellationToken = default);
}
