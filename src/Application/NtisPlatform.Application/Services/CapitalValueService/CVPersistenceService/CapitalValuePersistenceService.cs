using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.CapitalValue;
using NtisPlatform.Application.Interfaces.ICapitalValueService;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Persistence;
using NtisPlatform.Core.Entities.Master;

using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.CapitalValue.CVPersistenceService;

/// <summary>
/// Encapsulates all bulk persistence operations for CV calculation.
/// Separates persistence concerns from orchestration logic.
/// </summary>
public class CapitalValuePersistenceService : ICapitalValuePersistenceService
{
    private readonly IPropertyTaxCalculationCVResultsService _cvResultsService;
    private readonly IPolicyTaxDetailsCVService _policyTaxService;
    private readonly ITransMastService _transMastService;
    private readonly IRepository<PropertyRuleApplicationLogEntity, int> _ruleLogRepo;
    private readonly ILogger<CapitalValuePersistenceService> _logger;

    public CapitalValuePersistenceService(
        IPropertyTaxCalculationCVResultsService cvResultsService,
        IPolicyTaxDetailsCVService policyTaxService,
        ITransMastService transMastService,
        IRepository<PropertyRuleApplicationLogEntity, int> ruleLogRepo,
        ILogger<CapitalValuePersistenceService> logger)
    {
        _cvResultsService = cvResultsService;
        _policyTaxService = policyTaxService;
        _transMastService = transMastService;
        _ruleLogRepo = ruleLogRepo;
        _logger = logger;
    }

    public async Task<BulkResult<PropertyTaxCalculationCVResultsDto>> PersistCVResultsAsync( List<CreatePropertyTaxCalculationCVResultsDto> cvResults, CancellationToken cancellationToken = default)
    {
        if (!cvResults.Any())
            return new BulkResult<PropertyTaxCalculationCVResultsDto>(0, 0, []);

        var bulkResult = await _cvResultsService.BulkCreateAsync( cvResults.ToArray(), cancellationToken);

        if (bulkResult.FailedCount > 0)
        {
            _logger.LogWarning("Bulk CV insert had {FailedCount} failures. Errors: {Errors}",
                bulkResult.FailedCount, string.Join("; ", bulkResult.Errors ?? []));
        }

        _logger.LogDebug("Bulk inserted {SuccessCount} CV result records", bulkResult.SuccessCount);
        return bulkResult;
    }

    public async Task PersistAggregatedDataAsync(
        int propertyId, YearMasterEntity financeYear, Dictionary<int, (decimal TotalTax, decimal TotalCV)> aggregatedTaxes, Dictionary<int, PolicyTaxDetailsCVDto> existingPolicies,
        Dictionary<(int PropertyId, int FinanceYearId, int TaxId), TransMastDto> existingTransMast, string policyCode, DateTime policyDate, int policyYear, string? policyReason,
        int createdBy, CancellationToken cancellationToken = default)
    {
        if (!aggregatedTaxes.Any())
            return;

        // Prepare bulk operations
        var policyUpdates = new List<BulkUpdateItem<int, UpdatePolicyTaxDetailsCVDto>>();
        var policyCreates = new List<CreatePolicyTaxDetailsCVDto>();
        var transMastUpdates = new List<BulkUpdateItem<int, UpdateTransMastDto>>();
        var transMastCreates = new List<CreateTransMastDto>();

        foreach (var (taxId, (totalTax, totalCV)) in aggregatedTaxes)
        {
            // Prepare PolicyTaxDetailsCV upserts
            if (existingPolicies.TryGetValue(taxId, out var existingPolicy))
            {
                policyUpdates.Add(new BulkUpdateItem<int, UpdatePolicyTaxDetailsCVDto>(
                    existingPolicy.Id,
                    new UpdatePolicyTaxDetailsCVDto
                    {
                        PolicyCode = policyCode,
                        PolicyDate = policyDate,
                        PolicyYear = (short)policyYear,
                        PolicyReason = policyReason,
                        PolicyRVorCVvalue = totalCV,
                         TaxAmount = totalTax,
                        UpdatedBy = createdBy,
                        UpdatedDate = DateTime.Now

                    }
                ));
            }
            else
            {
                policyCreates.Add(new CreatePolicyTaxDetailsCVDto
                {
                    PropertyId = propertyId,
                    PolicyCode = policyCode,
                    PolicyDate = policyDate,
                    PolicyYear = (short)policyYear,
                    PolicyReason = policyReason,
                    PolicyRVorCVvalue = totalCV,
                    TaxId = taxId,
                    TaxAmount = totalTax,
                     CreatedBy = createdBy,
                    IsActive=true,
                    CreatedDate=DateTime.Now
                });
            }

            // Prepare TransMast upserts
            var transKey = (propertyId, financeYear.Id, taxId);
            if (existingTransMast.TryGetValue(transKey, out var existingTrans))
            {
                transMastUpdates.Add(new BulkUpdateItem<int, UpdateTransMastDto>(
                    existingTrans.Id,
                    new UpdateTransMastDto
                    {
                        CalculationType = "CV",
                        CalculationValue = totalCV,
                        TaxAmount = totalTax,
                        IsActive = true,
                        UpdatedBy = createdBy,                        
                        UpdatedDate = DateTime.Now

                    }
                ));
            }
            else
            {
                transMastCreates.Add(new CreateTransMastDto
                {
                    PropertyId = propertyId,
                    FinanceYearId = financeYear.Id,
                    CalculationType = "CV",
                    CalculationValue = totalCV,
                    TaxId = taxId,
                    TaxAmount = totalTax,
                    CreatedBy = createdBy,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                });
            }
        }

        // Execute bulk operations
        await ExecutePolicyBulkOperationsAsync(policyCreates, policyUpdates, cancellationToken);
        await ExecuteTransMastBulkOperationsAsync(transMastCreates, transMastUpdates, cancellationToken);
    }

    private async Task ExecutePolicyBulkOperationsAsync(List<CreatePolicyTaxDetailsCVDto> creates,List<BulkUpdateItem<int, UpdatePolicyTaxDetailsCVDto>> updates,CancellationToken cancellationToken)
    {
        if (creates.Any())
        {
            var result = await _policyTaxService.BulkCreateAsync(creates.ToArray(), cancellationToken);
            _logger.LogDebug("Bulk created {Count} PolicyTaxDetails records", result.SuccessCount);
        }

        if (updates.Any())
        {
            var result = await _policyTaxService.BulkUpdateAsync(updates.ToArray(), cancellationToken);
            _logger.LogDebug("Bulk updated {Count} PolicyTaxDetails records", result.SuccessCount);
        }
    }

    private async Task ExecuteTransMastBulkOperationsAsync( List<CreateTransMastDto> creates, List<BulkUpdateItem<int, UpdateTransMastDto>> updates, CancellationToken cancellationToken)
    {
        if (creates.Any())
        {
            var result = await _transMastService.BulkCreateAsync(creates.ToArray(), cancellationToken);
            _logger.LogDebug("Bulk created {Count} TransMast records", result.SuccessCount);
        }

        if (updates.Any())
        {
            var result = await _transMastService.BulkUpdateAsync(updates.ToArray(), cancellationToken);
            _logger.LogDebug("Bulk updated {Count} TransMast records", result.SuccessCount);
        }
    }

    public async Task SaveRuleApplicationLogAsync(
        int propertyId,
        int financeYear,
        int propertyDetailsId,
        List<RuleApplicationTraceEntry> appliedRules,
        string category,
        DateTime appliedAt,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Saving rule application log for PropertyId={PropertyId}, DetailsId={DetailsId}, Year={Year}, Category={Category}",
            propertyId, propertyDetailsId, financeYear, category);

        var now = DateTime.Now;

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
                .SetProperty(x => x.UpdatedDate,           now),
                cancellationToken);

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

        await _ruleLogRepo.AddRangeAsync(entities, cancellationToken);
        _logger.LogInformation("Saved {Count} rule application logs for PropertyId={PropertyId}, DetailsId={DetailsId}",
            entities.Count, propertyId, propertyDetailsId);
    }
}
