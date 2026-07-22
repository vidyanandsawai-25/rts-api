using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using NtisPlatform.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NtisPlatform.Application.Interfaces.TaxEngine;

/// <summary>
/// Handles the persistence phase of an RV calculation: replacing old result rows,
/// building PolicyTaxDetails records, and writing TransMast (CalculationType = "RV") entries.
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
        List<RVCalculationResultsEntity> newResultsRows,
        List<RVCalculationTaxDetailsEntity> newTaxDetailRows);

    /// <summary>
    /// Soft-deletes stale PolicyTaxDetails and TransMast (CalculationType = "RV") rows for the property/year,
    /// then inserts fresh aggregated policy and transaction records.
    /// Does NOT call SaveChanges — the caller commits the transaction.
    /// </summary>
    /// <returns>The newly created <see cref="PolicyTaxDetailsEntity"/> records.</returns>
    Task<List<PolicyTaxDetailsEntity>> SavePolicyAndTransmastRVAsync(
        int propertyId,
        int financeYear,
        int yearMasterId,
        List<RVCalculationResultsEntity> resultsRows,
        List<RVCalculationTaxDetailsEntity> taxDetailRows,
        decimal totalRv,
        int? educationTaxId,
        int? employmentTaxId);

    /// <summary>
    /// Soft-deletes any existing active rule application logs for this property details ID and finance year,
    /// and inserts the new trace entries.
    /// </summary>
    Task SaveRuleApplicationLogAsync(
        int propertyId,
        int financeYear,
        int propertyDetailsId,
        List<RuleApplicationTraceEntry> appliedRules,
        string category,
        DateTime appliedAt);
}
