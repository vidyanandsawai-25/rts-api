using NtisPlatform.Application.DTOs.RetrospectiveTax;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Builds the read-only "Retrospective Tax Details" screen payload for a single property by
/// re-implementing the legacy year-wise tax-pending PIVOT SQL script in application code.
/// </summary>
public interface IRetrospectiveTaxService
{
    /// <summary>
    /// Resolves the property from Ward + PropertyNo (+ optional PartitionNo) and returns its
    /// year-wise retrospective pending tax amounts per tax head.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// No property matches, or (when PartitionNo is omitted) more than one property matches.
    /// </exception>
    Task<RetrospectiveTaxDto> GetRetrospectiveTaxAsync(
        RetrospectiveTaxQueryParameters query,
        CancellationToken cancellationToken = default);
}
