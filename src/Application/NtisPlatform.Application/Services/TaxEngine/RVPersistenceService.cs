using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.TaxEngine;

/// <summary>
/// Handles the write phase of an RV calculation: replacing result rows and persisting
/// the aggregated PolicyTaxDetails + TransMast (CalculationType = "RV") records.
/// Extracted from <c>RateableValueService</c> to satisfy the Single Responsibility Principle.
/// </summary>
public sealed class RVPersistenceService : IRVPersistenceService
{
    private readonly IRepository<RVCalculationResultsEntity, int> _taxResultsRepo;
    private readonly IRepository<RVCalculationTaxDetailsEntity, int> _taxDetailsRepo;
    private readonly IRepository<PolicyTaxDetailsEntity, int> _policyTaxRepo;
    private readonly IRepository<TransMastEntity, int> _transmastRVRepo;
    private readonly IRepository<PropertyRuleApplicationLogEntity, int> _ruleLogRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RVPersistenceService> _logger;
    private readonly TimeProvider _timeProvider;

    public RVPersistenceService(
        IRepository<RVCalculationResultsEntity, int> taxResultsRepo,
        IRepository<RVCalculationTaxDetailsEntity, int> taxDetailsRepo,
        IRepository<PolicyTaxDetailsEntity, int> policyTaxRepo,
        IRepository<TransMastEntity, int> transmastRVRepo,
        IRepository<PropertyRuleApplicationLogEntity, int> ruleLogRepo,
        IUnitOfWork unitOfWork,
        ILogger<RVPersistenceService> logger,
        TimeProvider timeProvider)
    {
        _taxResultsRepo = taxResultsRepo;
        _taxDetailsRepo = taxDetailsRepo;
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
        List<RVCalculationResultsEntity> newResultsRows,
        List<RVCalculationTaxDetailsEntity> newTaxDetailRows)
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

        // Bulk soft-delete tax detail rows for the property
        await _taxDetailsRepo.GetQueryable()
            .Where(x => x.RVCalculationResults!.PropertyId == propertyId &&
                        !x.MarkedForDeletion)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsActive,
                             false)
                .SetProperty(x => x.MarkedForDeletion,
                             true)
                .SetProperty(x => x.MarkedForDeletionDate, now)
                .SetProperty(x => x.UpdatedDate,
                             now));

        // Store PropertyDetailsId mapping BEFORE saving (navigation property becomes detached after save)
        var taxDetailToPropertyDetailsMap = new Dictionary<RVCalculationTaxDetailsEntity, int>();
        foreach (var detail in newTaxDetailRows)
        {
            if (detail.RVCalculationResults != null)
            {
                taxDetailToPropertyDetailsMap[detail] = detail.RVCalculationResults.PropertyDetailsId;
            }
        }

        foreach (var row in newResultsRows)
        {
            row.IsActive = true;
            row.MarkedForDeletion = false;
            row.MarkedForDeletionDate = null;
        }

        await _taxResultsRepo.AddRangeAsync(newResultsRows);

        // Save results rows first so they get database-generated IDs.
        // Required to set RVCalculationResultsId on tax detail rows before inserting them.
        await _unitOfWork.SaveChangesAsync();

        // Now set the FK on tax details using the results row IDs that were just assigned
        foreach (var detail in newTaxDetailRows)
        {
            detail.IsActive = true;
            detail.MarkedForDeletion = false;
            detail.MarkedForDeletionDate = null;
            detail.CreatedDate ??= now;
            detail.UpdatedDate = now;

            // Find the corresponding results row using the stored PropertyDetailsId
            // (navigation property is detached after SaveChangesAsync, so we use the map)
            if (taxDetailToPropertyDetailsMap.TryGetValue(detail, out var propertyDetailsId))
            {
                var correspondingResultsRow = newResultsRows.FirstOrDefault(r =>
                    r.PropertyDetailsId == propertyDetailsId);

                if (correspondingResultsRow != null)
                {
                    detail.RVCalculationResultsId = correspondingResultsRow.Id;
                }
            }
        }

        await _taxDetailsRepo.AddRangeAsync(newTaxDetailRows);

        _logger.LogInformation(
            "Replaced results for PropertyId={PropertyId}: {ResultsRowCount} results rows and {TaxDetailCount} tax details",
            propertyId, newResultsRows.Count, newTaxDetailRows.Count);
    }

    /// <inheritdoc/>
    public async Task<List<PolicyTaxDetailsEntity>> SavePolicyAndTransmastRVAsync(
        int propertyId,
        int financeYear,
        int yearMasterId,
        List<RVCalculationResultsEntity> resultsRows,
        List<RVCalculationTaxDetailsEntity> taxDetailRows,
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

        // Deactivate stale TransMast (RV) rows — bulk SQL UPDATE
        int oldTransmastCount = await _transmastRVRepo.GetQueryable()
            .Where(x => x.PropertyId == propertyId &&
                        x.FinanceYearId == yearMasterId &&
                        x.CalculationType == "RV" &&
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
        var taxGroups = taxDetailRows
            .Where(x => x.TaxId > 0 && x.IsActive && !x.MarkedForDeletion)
            .OrderBy(x => x.TaxId)
            .GroupBy(x => x.TaxId)
            .ToList();

        var newPolicyRecords  = new List<PolicyTaxDetailsEntity>();
        var newTransmastRecords = new List<TransMastEntity>();

        foreach (var taxGroup in taxGroups)
        {
            var taxId = taxGroup.Key;

            // Skip education and employment taxes  they're handled separately below
            bool isSpecial = (educationTaxId.HasValue && taxId == educationTaxId.Value) ||
                             (employmentTaxId.HasValue  && taxId == employmentTaxId.Value);
            if (isSpecial)
                continue;

            decimal taxAmount = taxGroup.Sum(x => x.TaxAmount ?? 0m);

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

            newTransmastRecords.Add(new TransMastEntity
            {
                PropertyId        = propertyId,
                FinanceYearId     = yearMasterId,
                TaxId             = taxId,
                TaxAmount         = taxAmount,
                CalculationType   = "RV",
                CalculationValue  = totalRv,
                IsActive          = true,
                MarkedForDeletion = false,
                CreatedDate       = now,
                UpdatedDate       = now
            });
        }

        // Handle education tax: MAX(REducationTax) + MAX(CEducationTax)
        // (Each type group has one shared value, MAX prevents duplicates)
        if (educationTaxId.HasValue)
        {
            decimal rEducationTax = resultsRows
                .Where(x => x.IsActive && !x.MarkedForDeletion && x.REducationTax.HasValue)
                .Max(x => x.REducationTax) ?? 0m;

            decimal cEducationTax = resultsRows
                .Where(x => x.IsActive && !x.MarkedForDeletion && x.CEducationTax.HasValue)
                .Max(x => x.CEducationTax) ?? 0m;

            decimal educationTaxAmount = rEducationTax + cEducationTax;

            if (educationTaxAmount > 0)
            {
                newPolicyRecords.Add(new PolicyTaxDetailsEntity
                {
                    PropertyId           = propertyId,
                    PolicyCode           = "NETTAX",
                    PolicyDate           = now,
                    PolicyYear           = (short)financeYear,
                    PolicyRVorCVvalue    = totalRv,
                    TaxId                = educationTaxId.Value,
                    TaxAmount            = educationTaxAmount,
                    IsActive             = true,
                    MarkedForDeletion    = false,
                    MarkedForDeletionDate = null,
                    CreatedDate          = now,
                    UpdatedDate          = now
                });

                newTransmastRecords.Add(new TransMastEntity
                {
                    PropertyId        = propertyId,
                    FinanceYearId     = yearMasterId,
                    TaxId             = educationTaxId.Value,
                    TaxAmount         = educationTaxAmount,
                    CalculationType   = "RV",
                    CalculationValue  = totalRv,
                    IsActive          = true,
                    MarkedForDeletion = false,
                    CreatedDate       = now,
                    UpdatedDate       = now
                });
            }
        }

        // Handle employment tax: MAX(CEmploymentTax)
        // (All C-type details share one value, MAX prevents duplicates)
        if (employmentTaxId.HasValue)
        {
            decimal employmentTaxAmount = resultsRows
                .Where(x => x.IsActive && !x.MarkedForDeletion && x.CEmploymentTax.HasValue)
                .Max(x => x.CEmploymentTax) ?? 0m;

            if (employmentTaxAmount > 0)
            {
                newPolicyRecords.Add(new PolicyTaxDetailsEntity
                {
                    PropertyId           = propertyId,
                    PolicyCode           = "NETTAX",
                    PolicyDate           = now,
                    PolicyYear           = (short)financeYear,
                    PolicyRVorCVvalue    = totalRv,
                    TaxId                = employmentTaxId.Value,
                    TaxAmount            = employmentTaxAmount,
                    IsActive             = true,
                    MarkedForDeletion    = false,
                    MarkedForDeletionDate = null,
                    CreatedDate          = now,
                    UpdatedDate          = now
                });

                newTransmastRecords.Add(new TransMastEntity
                {
                    PropertyId        = propertyId,
                    FinanceYearId     = yearMasterId,
                    TaxId             = employmentTaxId.Value,
                    TaxAmount         = employmentTaxAmount,
                    CalculationType   = "RV",
                    CalculationValue  = totalRv,
                    IsActive          = true,
                    MarkedForDeletion = false,
                    CreatedDate       = now,
                    UpdatedDate       = now
                });
            }
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
