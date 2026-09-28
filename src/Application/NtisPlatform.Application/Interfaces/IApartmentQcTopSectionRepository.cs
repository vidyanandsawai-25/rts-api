using NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Read-only repository backing the ApartmentQC "top section" panel (property overview,
/// property info, and the inputs the performance-summary calculator needs).
/// All reads are AsNoTracking — this feature never writes.
/// </summary>
public interface IApartmentQcTopSectionRepository
{
    /// <summary>
    /// Core property + master-data fields for the property resolved from <paramref name="query"/>,
    /// or null if no identifier resolves to a property.
    /// </summary>
    Task<PropertyTopSectionRawData?> GetPropertyAsync(ApartmentQcTopSectionQueryParameters query, CancellationToken cancellationToken = default);

    /// <summary>Sum of active PropertyDetails carpet area (Ft, Mtr) rows for the property.</summary>
    Task<(double? Ft, double? Mtr)> GetCarpetAreaTotalsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sum of active PropertyDetails BuiltupArea (Ft, Mtr) for rows where IsOpenPlot is NOT true
    /// (i.e. actual constructed area, excluding the open-plot marker row).
    /// </summary>
    Task<(double? Ft, double? Mtr)> GetBuiltUpAreaTotalsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sum of active PropertyDetails BuiltupArea (Ft, Mtr) for rows where IsOpenPlot = true.
    /// PropertyDetails has no dedicated plot-area column — an open plot's area is recorded in the
    /// same BuiltupArea fields as a regular unit's constructed area, distinguished only by IsOpenPlot.
    /// </summary>
    Task<(double? Ft, double? Mtr)> GetPlotAreaTotalsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current tax, retrospective tax, pre-merge old current tax, and pending current tax details for the property.
    /// </summary>
    Task<(decimal? CurrentTax, decimal? RetroTax, decimal? OldCurrentTax, decimal? PendingCurrent)> GetAdditionalRevenueTaxDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the active child wings belonging to the property's society (from PTIS.WingDetailsMast).
    /// </summary>
    Task<List<SocietyWingSummaryDto>> GetSocietyWingsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the editable fields of the property top section in the database.
    /// </summary>
    Task<TopSectionUpdateOutcome> UpdateTopSectionAsync(int propertyId, UpdateApartmentQcTopSectionDto dto, int updatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the editable fields of a specific wing in PTIS.WingDetailsMast.
    /// </summary>
    Task<TopSectionUpdateOutcome> UpdateWingDetailsAsync(int wingDetailId, UpdateApartmentQcWingDetailsDto dto, int updatedBy, CancellationToken cancellationToken = default);
}


