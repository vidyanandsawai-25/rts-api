namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service responsible for handling tax-related operations during property combination.
/// Manages pending taxes aggregation and current year tax recalculation using RateableValueService.
/// </summary>
public interface ICombinePropertyTaxService
{
    /// <summary>
    /// Aggregates pending taxes from combined properties to the source property.
    /// For all pending tax records (year-wise, tax-wise):
    /// - Sums pending amounts by TaxId and PendingYearId from combined properties
    /// - Updates or creates pending tax records on source property
    /// - Sets PendingFixed = true for all affected records
    /// - Zeroes out combined properties' PendingAmount while keeping IsActive = true (preserves historical records)
    /// </summary>
    /// <param name="sourcePropertyId">The main property ID receiving aggregated taxes</param>
    /// <param name="combinePropertyIds">List of property IDs being combined</param>
    /// <param name="createdBy">User ID who initiated the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if aggregation was successful</returns>
    Task<bool> AggregatePendingTaxesAsync(
        int sourcePropertyId,
        List<int> combinePropertyIds,
        int? createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculates Rateable Value (RV) tax for the source property using RateableValueService.CalculateAndSaveAsync().
    /// Uses the combined PropertyDetails to calculate fresh tax amounts.
    /// Saves results to PolicyTaxDetailsEntity for the current financial year.
    /// </summary>
    /// <param name="sourcePropertyId">The property ID to recalculate taxes for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if recalculation was successful</returns>
    Task<bool> RecalculateCurrentYearTaxAsync(
        int sourcePropertyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current financial year based on the fiscal calendar (April to March).
    /// </summary>
    /// <returns>The current financial year (e.g., 2024 for FY 2024-25)</returns>
    int GetCurrentFinanceYear();

    /// <summary>
    /// Handles all tax-related operations for property combination:
    /// 1. Aggregate pending taxes from previous years
    /// 2. Recalculate current year RV tax using RateableValueService.CalculateAndSaveAsync()
    /// </summary>
    /// <param name="sourcePropertyId">The main property ID</param>
    /// <param name="combinePropertyIds">List of property IDs being combined</param>
    /// <param name="createdBy">User ID who initiated the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the tax-processing flow completed without throwing (sub-step failures are logged as warnings)</returns>
    Task<bool> ProcessCombinePropertyTaxesAsync(
        int sourcePropertyId,
        List<int> combinePropertyIds,
        int? createdBy,
        CancellationToken cancellationToken = default);
}
