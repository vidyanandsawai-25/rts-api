using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service responsible for handling tax-related operations during property combination.
/// Implements the combine property tax handling flow:
/// 1. Aggregate migrated-ULB-arrears TransMast rows (PolicyCode = OLD_ARREARS) from combined
///    properties onto the source property (year-wise, tax-wise)
/// 2. Recalculate current year RV tax using RateableValueService.CalculateAndSaveAsync()
///
/// FUTURE WORK:
/// - Currently, only Rateable Value (RV) taxes are calculated and updated during property combination
/// - Capital Value (CV) tax calculation and update will be implemented in a future PR
/// </summary>
public class CombinePropertyTaxService : ICombinePropertyTaxService
{
    private readonly IRepository<TransMastEntity> _transMastRepository;
    private readonly IRepository<YearMasterEntity, int> _yearMasterRepository;
    private readonly IPolicyCodeLookupService _policyCodeLookup;
    private readonly IRateableValueService _rateableValueService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CombinePropertyTaxService> _logger;

    public CombinePropertyTaxService(
        IRepository<TransMastEntity> transMastRepository,
        IRepository<YearMasterEntity, int> yearMasterRepository,
        IPolicyCodeLookupService policyCodeLookup,
        IRateableValueService rateableValueService,
        IUnitOfWork unitOfWork,
        ILogger<CombinePropertyTaxService> logger)
    {
        _transMastRepository = transMastRepository;
        _yearMasterRepository = yearMasterRepository;
        _policyCodeLookup = policyCodeLookup;
        _rateableValueService = rateableValueService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> ProcessCombinePropertyTaxesAsync(
        int sourcePropertyId,
        List<int> combinePropertyIds,
        int? createdBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting tax processing for combine property. SourcePropertyId={SourcePropertyId}, CombinePropertyIds={CombinePropertyIds}",
            sourcePropertyId,
            string.Join(",", combinePropertyIds));

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Step 1: Aggregate pending taxes from combined properties (year-wise, tax-wise)
                var pendingTaxResult = await AggregatePendingTaxesAsync(
                    sourcePropertyId,
                    combinePropertyIds,
                    createdBy,
                    cancellationToken);

                if (!pendingTaxResult)
                {
                    _logger.LogWarning("Failed to aggregate pending taxes for SourcePropertyId={SourcePropertyId}", sourcePropertyId);
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return false;
                }

                // Save pending tax changes before recalculation
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Pending tax aggregation committed for SourcePropertyId={SourcePropertyId}", sourcePropertyId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Failed to aggregate pending taxes for SourcePropertyId={SourcePropertyId}. Transaction rolled back.", sourcePropertyId);
                throw;
            }

            // Step 2: Recalculate current year RV tax using RateableValueService.CalculateAndSaveAsync()
            // This runs in its own transaction
            var recalculationResult = await RecalculateCurrentYearTaxAsync(sourcePropertyId, cancellationToken);

            if (!recalculationResult)
            {
                _logger.LogWarning("Failed to recalculate current year tax for SourcePropertyId={SourcePropertyId}", sourcePropertyId);
                return false;
            }

            _logger.LogInformation(
                "Tax processing completed for combine property. SourcePropertyId={SourcePropertyId}",
                sourcePropertyId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing taxes for combine property. SourcePropertyId={SourcePropertyId}",
                sourcePropertyId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> AggregatePendingTaxesAsync(
        int sourcePropertyId,
        List<int> combinePropertyIds,
        int? createdBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Aggregating pending taxes. SourcePropertyId={SourcePropertyId}, CombinePropertyIds={CombinePropertyIds}",
            sourcePropertyId,
            string.Join(",", combinePropertyIds));

        try
        {
            var oldArrearsPolicyCodeId = await _policyCodeLookup.GetIdAsync(PolicyCodes.OldArrears, cancellationToken);

            // Get all migrated-ULB-arrears TransMast rows from combined properties.
            // Note: Do NOT filter by IsActive here; rows may be inactive in the database,
            // but we still need to aggregate their amounts and then zero them out.
            var combinedPendingTaxes = await _transMastRepository.GetQueryable()
                .Where(t => combinePropertyIds.Contains(t.PropertyId) &&
                            !t.MarkedForDeletion &&
                            t.PolicyCodeId == oldArrearsPolicyCodeId)
                .ToListAsync(cancellationToken);

            if (combinedPendingTaxes.Count == 0)
            {
                _logger.LogInformation(
                    "No pending taxes found for combined properties. CombinePropertyIds={CombinePropertyIds}",
                    string.Join(",", combinePropertyIds));
                return true;
            }

            _logger.LogInformation(
                "Found {Count} pending tax records from combined properties",
                combinedPendingTaxes.Count);

            // Group by FinanceYearId and TaxId to aggregate amounts
            var aggregatedTaxes = combinedPendingTaxes
                .GroupBy(t => new { t.FinanceYearId, t.TaxId })
                .Select(g => new
                {
                    g.Key.FinanceYearId,
                    g.Key.TaxId,
                    TotalAmount = g.Sum(t => t.TaxAmount)
                })
                .ToList();

            _logger.LogDebug(
                "Aggregated into {Count} unique FinanceYearId+TaxId combinations",
                aggregatedTaxes.Count);

            // Get existing pending tax records for source property
            var distinctFinanceYearIds = aggregatedTaxes.Select(a => a.FinanceYearId).Distinct().ToList();
            var distinctTaxIds = aggregatedTaxes.Select(a => a.TaxId).Distinct().ToList();

            var sourcePendingTaxes = await _transMastRepository.GetQueryable()
                .Where(t => t.PropertyId == sourcePropertyId &&
                            distinctFinanceYearIds.Contains(t.FinanceYearId) &&
                            distinctTaxIds.Contains(t.TaxId) &&
                            t.PolicyCodeId == oldArrearsPolicyCodeId &&
                            t.IsActive &&
                            !t.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            var sourceTaxLookup = sourcePendingTaxes
                .ToDictionary(t => (t.FinanceYearId, t.TaxId), t => t);

            // Update or insert pending tax records for source property
            var newRecords = new List<TransMastEntity>();

            foreach (var aggregated in aggregatedTaxes)
            {
                var key = (aggregated.FinanceYearId, aggregated.TaxId);

                if (sourceTaxLookup.TryGetValue(key, out var existingRecord))
                {
                    // Update existing record: Add aggregated amount
                    existingRecord.TaxAmount += aggregated.TotalAmount;
                    existingRecord.CalculationValue = existingRecord.TaxAmount;
                    existingRecord.UpdatedBy = createdBy;
                    existingRecord.UpdatedDate = DateTime.Now;

                    _logger.LogDebug(
                        "Updated source pending tax: PropertyId={PropertyId}, FinanceYearId={FinanceYearId}, TaxId={TaxId}, NewAmount={Amount}",
                        sourcePropertyId,
                        aggregated.FinanceYearId,
                        aggregated.TaxId,
                        existingRecord.TaxAmount);
                }
                else
                {
                    // Create new record for source property
                    var newRecord = new TransMastEntity
                    {
                        PropertyId = sourcePropertyId,
                        FinanceYearId = aggregated.FinanceYearId,
                        CalculationType = "RV",
                        CalculationValue = aggregated.TotalAmount,
                        TaxId = aggregated.TaxId,
                        PolicyCodeId = oldArrearsPolicyCodeId,
                        TaxAmount = aggregated.TotalAmount,
                        IsActive = true,
                        MarkedForDeletion = false,
                        CreatedBy = createdBy,
                        CreatedDate = DateTime.Now
                    };
                    newRecords.Add(newRecord);

                    _logger.LogDebug(
                        "Created new source pending tax: PropertyId={PropertyId}, FinanceYearId={FinanceYearId}, TaxId={TaxId}, Amount={Amount}",
                        sourcePropertyId,
                        aggregated.FinanceYearId,
                        aggregated.TaxId,
                        aggregated.TotalAmount);
                }
            }

            // Add new records
            if (newRecords.Count > 0)
            {
                await _transMastRepository.AddRangeAsync(newRecords, cancellationToken);
            }

            // Zero out combined properties' arrears records.
            // Keep IsActive = true (do not deactivate), only set the amount to 0.
            // This ensures historical records are preserved while preventing double-counting.
            foreach (var combinedTax in combinedPendingTaxes)
            {
                combinedTax.TaxAmount = 0;
                combinedTax.CalculationValue = 0;
                combinedTax.IsActive = true; // Explicitly keep IsActive = true
                combinedTax.UpdatedBy = createdBy;
                combinedTax.UpdatedDate = DateTime.Now;
            }

            _logger.LogInformation(
                "Pending tax aggregation completed. SourcePropertyId={SourcePropertyId}, RecordsUpdated={Updated}, RecordsCreated={Created}, RecordsZeroed={Zeroed}",
                sourcePropertyId,
                sourcePendingTaxes.Count,
                newRecords.Count,
                combinedPendingTaxes.Count);


            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error aggregating pending taxes. SourcePropertyId={SourcePropertyId}",
                sourcePropertyId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RecalculateCurrentYearTaxAsync(
        int sourcePropertyId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Recalculating current year RV tax for SourcePropertyId={SourcePropertyId}",
            sourcePropertyId);

        try
        {
            // Call RateableValueService.CalculateAndSaveAsync() to recalculate taxes
            // This will:
            // 1. Load combined PropertyDetails (all floors from merged properties)
            // 2. Calculate fresh RV and tax amounts based on combined data
            // 3. Save to PolicyTaxDetailsEntity for current financial year
            var result = await _rateableValueService.CalculateAndSaveAsync(sourcePropertyId);

            if (result != null)
            {
                _logger.LogInformation(
                    "RV tax recalculation completed using RateableValueService.CalculateAndSaveAsync(). " +
                    "SourcePropertyId={SourcePropertyId}, TotalRV={TotalRV}",
                    sourcePropertyId,
                    result.TotalRateableValue);
                return true;
            }

            _logger.LogWarning(
                "RV tax recalculation returned null result for SourcePropertyId={SourcePropertyId}",
                sourcePropertyId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error recalculating RV tax for SourcePropertyId={SourcePropertyId}",
                sourcePropertyId);
            // Don't rethrow - allow combine operation to continue
            return false;
        }
    }

    /// <inheritdoc />
    public int GetCurrentFinanceYear()
    {
        // Financial year runs April to March
        // If current month >= April, current year is the finance year
        // Otherwise, previous year is the finance year
        var today = DateTime.Today;
        return today.Month >= 4 ? today.Year : today.Year - 1;
    }
}
