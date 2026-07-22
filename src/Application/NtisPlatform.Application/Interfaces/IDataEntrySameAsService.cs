using NtisPlatform.Application.DTOs.DataEntrySameAs;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Re-implements the legacy [PTIS].[DataEntrySameAS] stored procedure in application code:
/// makes one or more destination properties' data-entry the same as a source property.
/// </summary>
public interface IDataEntrySameAsService
{
    /// <summary>
    /// Copies the source property's data-entry to each valid destination (replace semantics),
    /// honouring the requested filter mode. Runs in a single transaction.
    /// </summary>
    /// <param name="request">Source, destinations, filter mode and optional manual Type.</param>
    /// <param name="updatedBy">Acting user id (audit), from the authenticated principal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentException">Invalid filter type, missing source, or no valid destinations.</exception>
    Task<DataEntrySameAsResultDto> ExecuteAsync(
        DataEntrySameAsRequestDto request,
        int updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds sibling properties (same Ward + PropertyNo, a different non-empty partition that also
    /// differs from its wing number) that are candidate destinations for a "Same As" copy.
    /// </summary>
    Task<List<DataEntrySameAsPropertyDto>> GetSiblingPropertiesAsync(
        DataEntrySameAsQueryParameters query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the assessable property units under a building (same Ward + PropertyNo, optionally one
    /// partition), joining ward/zone/type/category master data and summing carpet areas per property.
    /// Always excludes amenity part-types, building/wing-level rows, and inactive/deleted properties;
    /// the query parameters may further filter/search on PartType, PropertyCategoryName and Type.
    /// </summary>
    Task<List<DataEntrySameAsUnitDto>> GetPropertyUnitsAsync(
        DataEntrySameAsUnitsQueryParameters query,
        CancellationToken cancellationToken = default);
}
