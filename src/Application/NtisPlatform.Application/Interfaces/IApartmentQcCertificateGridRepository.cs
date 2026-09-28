using NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Read-only repository backing the ApartmentQC certificate grid. All reads are AsNoTracking —
/// this feature never writes.
/// </summary>
public interface IApartmentQcCertificateGridRepository
{
    /// <summary>
    /// Builds the certificate grid for every unit-property sharing (<paramref name="wardId"/>,
    /// <paramref name="propertyNo"/>) — i.e. every partition of that property number (the whole
    /// apartment). A certificate uploaded at any level applies to everything under it, so per
    /// certificate type this returns, independently in three separate lists (none suppresses
    /// another): the Society-level record if one exists
    /// (<see cref="ApartmentQcCertificateGridDto.SocietyCertificates"/>, applies to every wing/unit),
    /// one row per wing that has its own Wing-level record
    /// (<see cref="ApartmentQcCertificateGridDto.WingCertificates"/>), and Unit-level records
    /// aggregated across every unit in the apartment that has its own record
    /// (<see cref="ApartmentQcCertificateGridDto.UnitCertificates"/>). Returns null when no active,
    /// non-deleted property matches.
    /// </summary>
    Task<ApartmentQcCertificateGridDto?> GetGridAsync(int wardId, string propertyNo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the certificate grid scoped to a single wing: units counted are only this wing's
    /// units. Same independent-lists behavior as <see cref="GetGridAsync"/>, narrowed to this one
    /// wing: <see cref="ApartmentQcCertificateGridDto.SocietyCertificates"/> (still applies, since
    /// Society covers this wing too), <see cref="ApartmentQcCertificateGridDto.WingCertificates"/>
    /// (this wing's own record), and <see cref="ApartmentQcCertificateGridDto.UnitCertificates"/>
    /// (aggregated across only this wing's units). Returns null when the wing has no active units.
    /// </summary>
    Task<ApartmentQcCertificateGridDto?> GetGridForWingAsync(int wingDetailsId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the certificate grid scoped to a single unit-property. Same independent-lists
    /// behavior as <see cref="GetGridAsync"/>, narrowed to this one property:
    /// <see cref="ApartmentQcCertificateGridDto.SocietyCertificates"/> (still applies),
    /// <see cref="ApartmentQcCertificateGridDto.WingCertificates"/> (this property's wing's own
    /// record, still applies), <see cref="ApartmentQcCertificateGridDto.UnitCertificates"/> (this
    /// exact property's own property-wise record), and
    /// <see cref="ApartmentQcCertificateGridDto.FloorCertificates"/> (one per floor). Returns null
    /// when the property is not found/active.
    /// </summary>
    Task<ApartmentQcCertificateGridDto?> GetGridForUnitAsync(int propertyId, CancellationToken cancellationToken = default);
}
