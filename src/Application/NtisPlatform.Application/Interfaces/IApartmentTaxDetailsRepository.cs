using NtisPlatform.Application.DTOs.Property.ApartmentTaxDetails;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Read-only repository for apartment tax-head totals. Aggregates PolicyTaxDetails across every
/// PropertyMast row that shares a Ward + PropertyNo (an apartment complex is split into one
/// PropertyMast row per unit), optionally narrowed to a single wing via WingMasterId.
/// All reads are AsNoTracking - this feature never writes.
/// </summary>
public interface IApartmentTaxDetailsRepository
{
    /// <summary>
    /// Tax-head totals (TaxAmount summed per TaxName) for the properties resolved from
    /// <paramref name="query"/>, or null if no property matches.
    /// </summary>
    Task<ApartmentTaxDetailsRawData?> GetTaxDetailsAsync(ApartmentTaxDetailsQueryParameters query, CancellationToken cancellationToken = default);
}
