using NtisPlatform.Application.DTOs.Property.ApartmentTaxDetails;

namespace NtisPlatform.Application.Interfaces;

/// <summary>Application-facing contract for the apartment tax-details panel.</summary>
public interface IApartmentTaxDetailsService
{
    /// <summary>
    /// Returns tax-head totals for the properties resolved from <paramref name="query"/>,
    /// or null if no property matches.
    /// </summary>
    Task<ApartmentTaxDetailsDto?> GetTaxDetailsAsync(ApartmentTaxDetailsQueryParameters query, CancellationToken cancellationToken = default);
}
