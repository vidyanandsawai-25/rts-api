using NtisPlatform.Application.Services.TaxEngine.OccupationTax;

namespace NtisPlatform.Application.Interfaces.TaxEngine;

/// <summary>
/// Pure calculation engine for Occupation Tax. Given certificate dates and approved configuration,
/// it computes the correct tax amounts for the current finance year plus retrospective years.
/// The engine is stateless and dependency-light; it outputs an OccupationTaxResult without
/// touching persistence or external systems.
/// </summary>
public interface IOccupationTaxEngine
{
    /// <summary>
    /// Computes Occupation Tax amounts for a property over current and retrospective years.
    /// </summary>
    /// <param name="input">Certificate dates, NETTAX, and configuration.</param>
    /// <param name="currentFinanceYear">The active finance year for proraton basis.</param>
    /// <returns>Computation result (valid or rejected with reason).</returns>
    OccupationTaxResult Compute(OccupationTaxInput input, FinanceYear currentFinanceYear);
}
