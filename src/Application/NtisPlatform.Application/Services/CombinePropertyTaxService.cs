using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service responsible for handling tax-related operations during property combination.
/// Implements the combine property tax handling flow:
/// 1. Aggregate pending taxes from combined properties (year-wise, tax-wise)
/// 2. Recalculate current year RV tax using RateableValueService.CalculateAndSaveAsync()
/// 
/// FUTURE WORK:
/// - Currently, only Rateable Value (RV) taxes are calculated and updated during property combination
/// - Capital Value (CV) tax calculation and update will be implemented in a future PR
/// </summary>
public class CombinePropertyTaxService : ICombinePropertyTaxService
{
    private readonly IRepository<TaxPendingDetailsEntity> _taxPendingRepository;
    private readonly IRepository<YearMasterEntity, int> _yearMasterRepository;
    private readonly IRateableValueService _rateableValueService;
    private readonly ILogger<CombinePropertyTaxService> _logger;

    public CombinePropertyTaxService(
        IRepository<TaxPendingDetailsEntity> taxPendingRepository,
        IRepository<YearMasterEntity, int> yearMasterRepository,
        IRateableValueService rateableValueService,
        ILogger<CombinePropertyTaxService> logger)
    {
        _taxPendingRepository = taxPendingRepository;
        _yearMasterRepository = yearMasterRepository;
        _rateableValueService = rateableValueService;
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
            // Step 1: Aggregate pending taxes from combined properties (year-wise, tax-wise)
            var pendingTaxResult = await AggregatePendingTaxesAsync(
                sourcePropertyId,
                combinePropertyIds,
                createdBy,
                cancellationToken);

            if (!pendingTaxResult)
            {
                _logger.LogWarning("Failed to aggregate pending taxes for SourcePropertyId={SourcePropertyId}", sourcePropertyId);
                // Continue with recalculation even if pending tax aggregation fails
            }

            // Step 2: Recalculate current year RV tax using RateableValueService.CalculateAndSaveAsync()
            var recalculationResult = await RecalculateCurrentYearTaxAsync(sourcePropertyId, cancellationToken);

            if (!recalculationResult)
            {
                _logger.LogWarning("Failed to recalculate current year tax for SourcePropertyId={SourcePropertyId}", sourcePropertyId);
                // Log warning but don't fail the entire operation
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
            // Get all pending tax records from combined properties.
            // Note: Do NOT filter by IsActive here; pending records may be inactive in the database,
            // but we still need to aggregate the pending amounts and then zero them out.
            var combinedPendingTaxes = await _taxPendingRepository.GetQueryable()
                .Where(t => combinePropertyIds.Contains(t.PropertyId) &&
                            !t.MarkedForDeletion &&
                            !t.PendingFixed)
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

            // Group by PendingYearId and TaxId to aggregate amounts
            var aggregatedTaxes = combinedPendingTaxes
                .GroupBy(t => new { t.PendingYearId, t.TaxId })
                .Select(g => new
                {
                    g.Key.PendingYearId,
                    g.Key.TaxId,
                    TotalAmount = g.Sum(t => t.PendingAmount ?? 0)
                })
                .ToList();

            _logger.LogDebug(
                "Aggregated into {Count} unique PendingYearId+TaxId combinations",
                aggregatedTaxes.Count);

            // Get existing pending tax records for source property
            var distinctPendingYearIds = aggregatedTaxes.Select(a => a.PendingYearId).Distinct().ToList();
            var distinctTaxIds = aggregatedTaxes.Select(a => a.TaxId).Distinct().ToList();

            var sourcePendingTaxes = await _taxPendingRepository.GetQueryable()
                .Where(t => t.PropertyId == sourcePropertyId &&
                            distinctPendingYearIds.Contains(t.PendingYearId) &&
                            distinctTaxIds.Contains(t.TaxId) &&
                            t.IsActive &&
                            !t.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            var sourceTaxLookup = sourcePendingTaxes
                .ToDictionary(t => (t.PendingYearId, t.TaxId), t => t);

            // Update or insert pending tax records for source property
            var newRecords = new List<TaxPendingDetailsEntity>();

            foreach (var aggregated in aggregatedTaxes)
            {
                var key = (aggregated.PendingYearId, aggregated.TaxId);

                if (sourceTaxLookup.TryGetValue(key, out var existingRecord))
                {
                    // Update existing record: Add aggregated amount
                    existingRecord.PendingAmount = (existingRecord.PendingAmount ?? 0) + aggregated.TotalAmount;
                    existingRecord.PendingFixed = true; // Mark to skip in future calculations
                    existingRecord.UpdatedBy = createdBy;
                    existingRecord.UpdatedDate = DateTime.Now;

                    _logger.LogDebug(
                        "Updated source pending tax: PropertyId={PropertyId}, PendingYearId={PendingYearId}, TaxId={TaxId}, NewAmount={Amount}",
                        sourcePropertyId,
                        aggregated.PendingYearId,
                        aggregated.TaxId,
                        existingRecord.PendingAmount);
                }
                else
                {
                    // Create new record for source property
                    var newRecord = new TaxPendingDetailsEntity
                    {
                        PropertyId = sourcePropertyId,
                        PendingYearId = aggregated.PendingYearId,
                        TaxId = aggregated.TaxId,
                        PendingAmount = aggregated.TotalAmount,
                        PendingFixed = true, // Mark to skip in future calculations
                        IsActive = true,
                        MarkedForDeletion = false,
                        CreatedBy = createdBy,
                        CreatedDate = DateTime.Now
                    };
                    newRecords.Add(newRecord);

                    _logger.LogDebug(
                        "Created new source pending tax: PropertyId={PropertyId}, PendingYearId={PendingYearId}, TaxId={TaxId}, Amount={Amount}",
                        sourcePropertyId,
                        aggregated.PendingYearId,
                        aggregated.TaxId,
                        aggregated.TotalAmount);
                }
            }

            // Add new records
            if (newRecords.Count > 0)
            {
                await _taxPendingRepository.AddRangeAsync(newRecords, cancellationToken);
            }

            // Set PendingFixed = true for any existing source property records not in aggregated list
            var allSourcePendingTaxes = await _taxPendingRepository.GetQueryable()
                .Where(t => t.PropertyId == sourcePropertyId &&
                            t.IsActive &&
                            !t.MarkedForDeletion &&
                            !t.PendingFixed)
                .ToListAsync(cancellationToken);

            foreach (var sourceRecord in allSourcePendingTaxes)
            {
                sourceRecord.PendingFixed = true;
                sourceRecord.UpdatedBy = createdBy;
                sourceRecord.UpdatedDate = DateTime.Now;
            }

            // Zero out combined properties' pending tax records and mark as fixed
            // Keep IsActive = true (do not deactivate), only set PendingAmount = 0 and PendingFixed = true
            // This ensures historical records are preserved while preventing double-counting
            foreach (var combinedTax in combinedPendingTaxes)
            {
                combinedTax.PendingAmount = 0;
                combinedTax.PendingFixed = true;
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
                    "SourcePropertyId={SourcePropertyId}, TotalRV={TotalRV}, TotalTax={TotalTax}",
                    sourcePropertyId,
                    result.TotalRateableValue,
                    result.TotalTax);
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
