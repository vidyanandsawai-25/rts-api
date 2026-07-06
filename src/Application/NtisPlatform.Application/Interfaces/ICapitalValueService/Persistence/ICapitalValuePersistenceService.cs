using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.CapitalValue;
using NtisPlatform.Core.Entities.Master;

using NtisPlatform.Application.DTOs.Rules.RuleExecution;

namespace NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Persistence;

/// <summary>
/// Abstraction for bulk CV persistence operations.
/// Encapsulates complex persistence logic away from orchestrator.
/// </summary>
public interface ICapitalValuePersistenceService
{
    /// <summary>
    /// Persists CV calculation results in bulk.
    /// </summary>
    Task<BulkResult<PropertyTaxCalculationCVResultsDto>> PersistCVResultsAsync(
        List<CreatePropertyTaxCalculationCVResultsDto> cvResults,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists aggregated policy and transaction master data in bulk.
    /// </summary>
    Task PersistAggregatedDataAsync(
        int propertyId,
        YearMasterEntity financeYear,
        Dictionary<int, (decimal TotalTax, decimal TotalCV)> aggregatedTaxes,
        Dictionary<int, PolicyTaxDetailsCVDto> existingPolicies,
        Dictionary<(int PropertyId, int FinanceYearId, int TaxId), TransMastDto> existingTransMast,
        string policyCode,
        DateTime policyDate,
        int policyYear,
        string? policyReason,
        int createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists CV rule execution trace logs.
    /// </summary>
    Task SaveRuleApplicationLogAsync(
        int propertyId,
        int financeYear,
        int propertyDetailsId,
        List<RuleApplicationTraceEntry> appliedRules,
        string category,
        DateTime appliedAt,
        CancellationToken cancellationToken = default);
}
