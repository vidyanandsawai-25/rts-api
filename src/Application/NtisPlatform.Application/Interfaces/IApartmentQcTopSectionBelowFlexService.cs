using NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Application-layer service for the ApartmentQC "below flex" status-badge strip.
/// </summary>
public interface IApartmentQcTopSectionBelowFlexService
{
    /// <summary>
    /// Every configured workflow stage and certificate type for the property resolved from
    /// <paramref name="query"/>, or null if no identifier resolves to a property.
    /// </summary>
    Task<ApartmentQcBelowFlexDto?> GetBelowFlexAsync(ApartmentQcTopSectionQueryParameters query, CancellationToken cancellationToken = default);
}
