using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.CapitalValue;
using NtisPlatform.Application.Interfaces.ICapitalValueService;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Persistence;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Services.CapitalValue.CVPersistenceService;

/// <summary>
/// Encapsulates all bulk persistence operations for CV calculation.
/// Separates persistence concerns from orchestration logic.
/// </summary>
public class CapitalValuePersistenceService : ICapitalValuePersistenceService
{
    private readonly IPropertyTaxCalculationCVResultsService _cvResultsService;
    private readonly IPolicyTaxDetailsService _policyTaxService;
    private readonly ITransMastService _transMastService;
    private readonly ILogger<CapitalValuePersistenceService> _logger;

    public CapitalValuePersistenceService(
        IPropertyTaxCalculationCVResultsService cvResultsService,
        IPolicyTaxDetailsService policyTaxService,
        ITransMastService transMastService,
        ILogger<CapitalValuePersistenceService> logger)
    {
        _cvResultsService = cvResultsService;
        _policyTaxService = policyTaxService;
        _transMastService = transMastService;
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
        int propertyId, YearMasterEntity financeYear, Dictionary<int, (decimal TotalTax, decimal TotalCV)> aggregatedTaxes, Dictionary<int, PolicyTaxDetailsDto> existingPolicies,
        Dictionary<(int PropertyId, int FinanceYearId, int TaxId), TransMastDto> existingTransMast, string policyCode, DateTime policyDate, int policyYear, string? policyReason,
        int createdBy, CancellationToken cancellationToken = default)
    {
        if (!aggregatedTaxes.Any())
            return;

        // Prepare bulk operations
        var policyUpdates = new List<BulkUpdateItem<int, UpdatePolicyTaxDetailsDto>>();
        var policyCreates = new List<CreatePolicyTaxDetailsDto>();
        var transMastUpdates = new List<BulkUpdateItem<int, UpdateTransMastDto>>();
        var transMastCreates = new List<CreateTransMastDto>();

        foreach (var (taxId, (totalTax, totalCV)) in aggregatedTaxes)
        {
            // Prepare PolicyTaxDetailsCV upserts
            if (existingPolicies.TryGetValue(taxId, out var existingPolicy))
            {
                policyUpdates.Add(new BulkUpdateItem<int, UpdatePolicyTaxDetailsDto>(
                    existingPolicy.Id,
                    new UpdatePolicyTaxDetailsDto
                    {
                        PolicyCode = policyCode,
                        PolicyDate = policyDate,
                        PolicyYear = (short)policyYear,
                        PolicyReason = policyReason,
                        PolicyRVorCVvalue = totalCV,
                        TaxAmount = totalTax,
                        UpdatedBy = createdBy,
                        UpdatedDate = DateTime.UtcNow

                    }
                ));
            }
            else
            {
                policyCreates.Add(new CreatePolicyTaxDetailsDto
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
                    CreatedDate=DateTime.UtcNow
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
                        RVorCV = "CV",
                        RVorCVValue = totalCV,
                        TaxAmount = totalTax,
                        IsActive = true,
                        UpdatedBy = createdBy,                        
                        UpdatedDate = DateTime.UtcNow

                    }
                ));
            }
            else
            {
                transMastCreates.Add(new CreateTransMastDto
                {
                    PropertyId = propertyId,
                    FinanceYearId = financeYear.Id,
                    RVorCV = "CV",
                    RVorCVValue = totalCV,
                    TaxId = taxId,
                    TaxAmount = totalTax,
                    CreatedBy = createdBy,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                });
            }
        }

        // Execute bulk operations
        await ExecutePolicyBulkOperationsAsync(policyCreates, policyUpdates, cancellationToken);
        await ExecuteTransMastBulkOperationsAsync(transMastCreates, transMastUpdates, cancellationToken);
    }

    private async Task ExecutePolicyBulkOperationsAsync(List<CreatePolicyTaxDetailsDto> creates,List<BulkUpdateItem<int, UpdatePolicyTaxDetailsDto>> updates,CancellationToken cancellationToken)
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
}
