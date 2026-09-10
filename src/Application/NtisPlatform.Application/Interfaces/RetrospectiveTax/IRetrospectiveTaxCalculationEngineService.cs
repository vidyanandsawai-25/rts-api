using NtisPlatform.Application.DTOs.RetrospectiveTax;

namespace NtisPlatform.Application.Interfaces.RetrospectiveTax;

/// <summary>
/// The "Calculate Retrospective Tax" action: given a property, reads the configured
/// Retrospective Rule Builder rules (evidence conditions, date condition, action, penalty rule),
/// finds the one whose conditions match this property's actual evidence, and calculates + saves
/// the year-wise retrospective tax breakup per that matched rule's configuration. Never invents
/// rule behavior — every value it produces traces back to a field on the matched
/// RetrospectiveRuleMaster row.
/// </summary>
public interface IRetrospectiveTaxCalculationEngineService
{
    /// <summary>
    /// Runs the calculation for <paramref name="propertyId"/> and persists a
    /// RetrospectiveTaxCalculation header + one RetrospectiveTaxCalculationDetail row per
    /// retrospective financial year. Returns null if no active, published rule's evidence
    /// conditions match this property (including no matching fallback rule).
    /// </summary>
    Task<RetrospectiveTaxEngineResultDto?> CalculateAndSaveAsync(
        int propertyId, int? calculatedBy, CancellationToken cancellationToken = default);
}
