using NtisPlatform.Application.DTOs.PropertyReassessment;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Builds the read-only "Property Re-Assessment" screen payload for a single property by
/// re-implementing the legacy re-assessment SQL (property lookup, photos, floor details and the
/// old-vs-new tax-head summary) in application code.
/// </summary>
public interface IPropertyReassessmentService
{
    /// <summary>
    /// Resolves the property from Ward + PropertyNo (+ optional PartitionNo) and returns its
    /// old/new photos, floor details and tax-head comparison in a single payload.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// No property matches, or (when PartitionNo is omitted) more than one property matches.
    /// </exception>
    Task<PropertyReassessmentDto> GetReassessmentAsync(
        PropertyReassessmentQueryParameters query,
        CancellationToken cancellationToken = default);
}
