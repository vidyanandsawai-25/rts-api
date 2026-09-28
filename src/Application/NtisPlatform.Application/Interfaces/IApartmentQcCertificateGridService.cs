using NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Application-layer service for the ApartmentQC certificate grid.
/// </summary>
public interface IApartmentQcCertificateGridService
{
    /// <summary>
    /// The certificate grid for the apartment (all unit-properties sharing the same WardId+PropertyNo
    /// as the property resolved from <paramref name="query"/>), or null if no identifier resolves
    /// to a property.
    /// </summary>
    Task<ApartmentQcCertificateGridDto?> GetGridAsync(ApartmentQcTopSectionQueryParameters query, CancellationToken cancellationToken = default);
}
