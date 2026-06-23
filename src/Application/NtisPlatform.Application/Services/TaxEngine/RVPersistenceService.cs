using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.TaxEngine;

/// <summary>
/// Handles the write phase of an RV calculation: replacing result rows and persisting
/// the aggregated PolicyTaxDetails + TransMastRV records.
/// Extracted from <c>RateableValueService</c> to satisfy the Single Responsibility Principle.
/// </summary>
public sealed class RVPersistenceService : IRVPersistenceService
{
    private readonly IRepository<PropertyTaxCalculationRVResultsEntity, int> _taxResultsRepo;
    private readonly IRepository<PolicyTaxDetailsEntity, int> _policyTaxRepo;
    private readonly IRepository<TransMastRVEntity, int> _transmastRVRepo;
    private readonly IRepository<PropertyRuleApplicationLogEntity, int> _ruleLogRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RVPersistenceService> _logger;
    private readonly TimeProvider _timeProvider;

    public RVPersistenceService(
        IRepository<PropertyTaxCalculationRVResultsEntity, int> taxResultsRepo,
        IRepository<PolicyTaxDetailsEntity, int> policyTaxRepo,
        IRepository<TransMastRVEntity, int> transmastRVRepo,
        IRepository<PropertyRuleApplicationLogEntity, int> ruleLogRepo,
        IUnitOfWork unitOfWork,
        ILogger<RVPersistenceService> logger,
        TimeProvider timeProvider)
    {
        _taxResultsRepo = taxResultsRepo;
        _policyTaxRepo = policyTaxRepo;
        _transmastRVRepo = transmastRVRepo;
        _ruleLogRepo = ruleLogRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public async Task ReplaceExistingResultsAsync(
        int propertyId,
        List<PropertyTaxCalculationRVResultsEntity> newRows)
    {
        var now = _timeProvider.GetLocalNow().DateTime;

        // Bulk soft-delete — single SQL UPDATE instead of load + per-row UpdateAsync
        await _taxResultsRepo.GetQueryable()
            .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsActive,              false)
                .SetProperty(x => x.MarkedForDeletion,     true)
                .SetProperty(x => x.MarkedForDeletionDate, now)
                .SetProperty(x => x.UpdatedDate,           now));

        foreach (var row in newRows)
        {
            row.IsActive = true;
            row.MarkedForDeletion = false;
            row.MarkedForDeletionDate = null;
        }

        await _taxResultsRepo.AddRangeAsync(newRows);
    }

    /// <inheritdoc/>
    public async Task<List<PolicyTaxDetailsEntity>> SavePolicyAndTransmastRVAsync(
        int propertyId,
        int financeYear,
        int yearMasterId,
        List<PropertyTaxCalculationRVResultsEntity> detailRows,
        decimal totalRv,
        int? educationTaxId,
        int? employmentTaxId)
    {
        _logger.LogDebug("Saving policy and TransmastRV for PropertyId={PropertyId}, Year={Year}",
            propertyId, financeYear);

        var now = _timeProvider.GetLocalNow().DateTime;

        // Deactivate stale PolicyTaxDetails rows — bulk SQL UPDATE
        int oldPolicyCount = await _policyTaxRepo.GetQueryable()
            .Where(x => x.PropertyId == propertyId &&
                        x.PolicyYear == financeYear &&
                        x.PolicyCode == "NETTAX" &&
                        x.IsActive &&
                        !x.MarkedForDeletion)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsActive,              false)
                .SetProperty(x => x.MarkedForDeletion,     true)
                .SetProperty(x => x.MarkedForDeletionDate, now)
                .SetProperty(x => x.UpdatedDate,           now));

        // Deactivate stale TransmastRV rows — bulk SQL UPDATE
        int oldTransmastCount = await _transmastRVRepo.GetQueryable()
            .Where(x => x.PropertyId == propertyId &&
                        x.FinanceYearId == yearMasterId &&
                        x.IsActive &&
                        !x.MarkedForDeletion)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsActive,              false)
                .SetProperty(x => x.MarkedForDeletion,     true)
                .SetProperty(x => x.MarkedForDeletionDate, now)
                .SetProperty(x => x.UpdatedDate,           now));

        _logger.LogDebug("Deactivated {PolicyCount} policy and {TransCount} transmast records",
            oldPolicyCount, oldTransmastCount);

        // Aggregate by tax
        var taxGroups = detailRows
            .Where(x => x.TaxId > 0)
            .OrderBy(x => x.TaxId)
            .GroupBy(x => x.TaxId)
            .ToList();

        var newPolicyRecords  = new List<PolicyTaxDetailsEntity>();
        var newTransmastRecords = new List<TransMastRVEntity>();

        foreach (var taxGroup in taxGroups)
        {
            var taxId = taxGroup.Key;

            // Education/employment tax rows are written once per detail in the type group.
            // MAX prevents double-counting; SUM is correct for all other taxes.
            bool isSpecial = (educationTaxId.HasValue && taxId == educationTaxId.Value) ||
                             (employmentTaxId.HasValue  && taxId == employmentTaxId.Value);

            decimal taxAmount = isSpecial
                ? taxGroup.Max(x => x.TaxAmount ?? 0m)
                : taxGroup.Sum(x => x.TaxAmount ?? 0m);

            newPolicyRecords.Add(new PolicyTaxDetailsEntity
            {
                PropertyId           = propertyId,
                PolicyCode           = "NETTAX",
                PolicyDate           = now,
                PolicyYear           = (short)financeYear,
                PolicyRVorCVvalue    = totalRv,
                TaxId                = taxId,
                TaxAmount            = taxAmount,
                IsActive             = true,
                MarkedForDeletion    = false,
                MarkedForDeletionDate = null,
                CreatedDate          = now,
                UpdatedDate          = now
            });

            newTransmastRecords.Add(new TransMastRVEntity
            {
                PropertyId        = propertyId,
                FinanceYearId     = yearMasterId,
                TaxId             = taxId,
                TaxAmount         = taxAmount,
                RateableValue     = totalRv,
                IsActive          = true,
                MarkedForDeletion = false,
                CreatedDate       = now,
                UpdatedDate       = now
            });
        }

        if (newPolicyRecords.Any())
            await _policyTaxRepo.AddRangeAsync(newPolicyRecords);

        if (newTransmastRecords.Any())
            await _transmastRVRepo.AddRangeAsync(newTransmastRecords);

        _logger.LogInformation(
            "Saved {PolicyCount} policy and {TransCount} transmast records for PropertyId={PropertyId}, Year={Year}",
            newPolicyRecords.Count, newTransmastRecords.Count, propertyId, financeYear);

        return newPolicyRecords;
    }

    /// <inheritdoc/>
    public async Task SaveRuleApplicationLogAsync(
        int propertyId,
        int financeYear,
        int propertyDetailsId,
        List<RuleApplicationTraceEntry> appliedRules,
        string category,
        DateTime appliedAt)
    {
        _logger.LogDebug("Saving rule application log for PropertyId={PropertyId}, DetailsId={DetailsId}, Year={Year}, Category={Category}",
            propertyId, propertyDetailsId, financeYear, category);

        var now = _timeProvider.GetLocalNow().DateTime;

        // Bulk soft-delete any existing rule application logs for this details ID, finance year, and category
        await _ruleLogRepo.GetQueryable()
            .Where(x => x.PropertyDetailsId == propertyDetailsId &&
                        x.FinanceYear == financeYear &&
                        x.RuleCategory == category &&
                        x.IsActive &&
                        !x.MarkedForDeletion)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsActive,              false)
                .SetProperty(x => x.MarkedForDeletion,     true)
                .SetProperty(x => x.MarkedForDeletionDate, now)
                .SetProperty(x => x.UpdatedDate,           now));

        if (appliedRules == null || !appliedRules.Any())
            return;

        var entities = new List<PropertyRuleApplicationLogEntity>();

        foreach (var rule in appliedRules)
        {
            entities.Add(new PropertyRuleApplicationLogEntity
            {
                PropertyId = propertyId,
                PropertyDetailsId = propertyDetailsId,
                FinanceYear = financeYear,
                RuleCategory = category,
                RuleCode = rule.RuleCode,
                RuleName = rule.RuleName,
                EffectType = rule.EffectType,
                EffectValue = rule.EffectValue,
                ApplyRate = rule.ApplyRate,
                BaseValue = rule.BaseValue,
                ComputedValue = rule.ComputedValue,
                CumulativeValue = rule.CumulativeValue,
                ApplyOrder = rule.ApplyOrder,
                StopProcessing = rule.StopProcessing,
                AppliedAt = appliedAt,
                RuleScopeId = rule.RuleScopeId,
                RuleScopeName = rule.RuleScopeName,
                Name = rule.Name,
                IsActive = true,
                MarkedForDeletion = false,
                MarkedForDeletionDate = null,
                CreatedDate = now,
                UpdatedDate = now
            });
        }

        await _ruleLogRepo.AddRangeAsync(entities);
        _logger.LogInformation("Saved {Count} rule application logs for PropertyId={PropertyId}, DetailsId={DetailsId}",
            entities.Count, propertyId, propertyDetailsId);
    }
}
