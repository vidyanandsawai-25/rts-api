using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces.TaxEngine;

/// <summary>
/// Handles the persistence phase of an RV calculation: replacing old result rows,
/// building PolicyTaxDetails records, and writing TransMastRV entries.
/// Extracted from <c>RateableValueService</c> to satisfy the Single Responsibility Principle.
/// Callers are responsible for managing the surrounding transaction.
/// </summary>
public interface IRVPersistenceService
{
    /// <summary>
    /// Soft-deletes any active RV result rows for <paramref name="propertyId"/> that
    /// are not yet marked for deletion, then bulk-inserts <paramref name="newRows"/>.
    /// Does NOT call SaveChanges — the caller commits the transaction.
    /// </summary>
    Task ReplaceExistingResultsAsync(
        int propertyId,
        List<PropertyTaxCalculationRVResultsEntity> newRows);

    /// <summary>
    /// Soft-deletes stale PolicyTaxDetails and TransMastRV rows for the property/year,
    /// then inserts fresh aggregated policy and transaction records.
    /// Does NOT call SaveChanges — the caller commits the transaction.
    /// </summary>
    /// <returns>The newly created <see cref="PolicyTaxDetailsEntity"/> records.</returns>
    Task<List<PolicyTaxDetailsEntity>> SavePolicyAndTransmastRVAsync(
        int propertyId,
        int financeYear,
        int yearMasterId,
        List<PropertyTaxCalculationRVResultsEntity> detailRows,
        decimal totalRv,
        int? educationTaxId,
        int? employmentTaxId);
}
