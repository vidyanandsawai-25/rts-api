using NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Application-facing contract for the ApartmentQC "top section" panel.
/// </summary>
public interface IApartmentQcTopSectionService
{
    /// <summary>
    /// Returns the full top-section payload for the property resolved from <paramref name="query"/>,
    /// or null if no identifier resolves to a property.
    /// </summary>
    Task<PropertyTopSectionDto?> GetTopSectionAsync(ApartmentQcTopSectionQueryParameters query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the editable fields of the ApartmentQC top section for the specified property.
    /// </summary>
    Task<TopSectionUpdateOutcome> UpdateTopSectionAsync(int propertyId, UpdateApartmentQcTopSectionDto dto, int updatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the editable fields of a specific wing in PTIS.WingDetailsMast.
    /// </summary>
    Task<TopSectionUpdateOutcome> UpdateWingDetailsAsync(int wingDetailId, UpdateApartmentQcWingDetailsDto dto, int updatedBy, CancellationToken cancellationToken = default);
}

