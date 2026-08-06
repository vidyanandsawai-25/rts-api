using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.AssetCapitalValue;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Services
{
    public partial class AssetCapitalValueService
    {
        #region Movable Assets Methods

        /// <summary>
        /// Calculate CV for a movable asset based on purchase value and depreciation
        /// Formula varies by valuation method:
        /// - DepreciatedValue: CV = PurchaseValue × (1 - AccumulatedDepreciation) × ConditionFactor
        /// - MarketValue: CV = CurrentMarketValue
        /// - BookValue: CV = CurrentBookValue
        /// - ReplacementCost: CV = PurchaseValue × InflationFactor × ConditionFactor
        /// </summary>
        public async Task<MovableAssetCVResultDto> CalculateMovableAssetCVAsync(
            CalculateMovableAssetCVRequestDto request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting movable asset CV calculation for AssetId: {AssetId}, Method: {Method}",
                request.AssetId, request.ValuationMethod);

            var asset = await _assetRepository.GetQueryable()
                .Where(x => x.Id == (int)request.AssetId && !x.MarkedForDeletion)
                .FirstOrDefaultAsync(cancellationToken);

            if (asset == null)
            {
                throw new InvalidOperationException($"Asset with ID {request.AssetId} not found");
            }

            // Pure computation — CalculateMovableAssetCV only builds the result DTO and never mutates
            // asset or writes a history row, so there's nothing to persist here.
            var result = CalculateMovableAssetCV(asset, request.ValuationMethod, request.CustomDepreciationRate, request.ConditionFactor);

            _logger.LogInformation("Movable asset CV calculated for AssetId: {AssetId}, CV: {CV}",
                request.AssetId, result.CapitalValue);

            return result;
        }

        /// <summary>
        /// Calculate CV for multiple movable assets in bulk
        /// </summary>
        public async Task<MovableAssetsCVSummaryDto> CalculateBulkMovableAssetsCVAsync(
            CalculateBulkMovableAssetsCVRequestDto request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting bulk movable assets CV calculation for {Count} assets",
                request.AssetIds.Count);

            var query = _assetRepository.GetQueryable()
                .Where(x => !x.MarkedForDeletion);

            if (request.AssetIds.Any())
            {
                query = query.Where(x => request.AssetIds.Contains(x.Id));
            }

            if (request.CategoryId.HasValue)
            {
                query = query.Where(x => x.AssetCategoryId == request.CategoryId.Value);
            }

            if (request.AssetTypeId.HasValue)
            {
                query = query.Where(x => x.AssetTypeId == request.AssetTypeId.Value);
            }

            var assets = await query.ToListAsync(cancellationToken);

            var result = new MovableAssetsCVSummaryDto
            {
                TotalAssets = assets.Count
            };

            foreach (var asset in assets)
            {
                var cvResult = CalculateMovableAssetCV(asset, request.ValuationMethod, null, 1.0m);

                // Update asset
                result.Assets.Add(cvResult);

                if (cvResult.IsCalculated)
                {
                    result.CalculatedAssets++;
                    result.TotalCapitalValue += cvResult.CapitalValue ?? 0;
                    result.TotalPurchaseValue += cvResult.PurchaseValue ?? 0;
                    result.TotalAccumulatedDepreciation += cvResult.AccumulatedDepreciation ?? 0;
                }
            }

            result.LastCVCalculationDate = DateTime.Now;

            _logger.LogInformation("Bulk movable assets CV calculated: {Calculated}/{Total} assets, TotalCV: {TotalCV}",
                result.CalculatedAssets, result.TotalAssets, result.TotalCapitalValue);

            return result;
        }

        /// <summary>
        /// Get CV for a movable asset
        /// </summary>
        public async Task<MovableAssetCVResultDto?> GetMovableAssetCVAsync(
            long assetId,
            CancellationToken cancellationToken = default)
        {
            var asset = await _assetRepository.GetQueryable()
                .Where(x => x.Id == (int)assetId && !x.MarkedForDeletion)
                .FirstOrDefaultAsync(cancellationToken);

            if (asset == null)
                return null;

            return MapToMovableAssetDto(asset);
        }

        /// <summary>
        /// Get CV summary for all movable assets by category or type
        /// </summary>
        public async Task<MovableAssetsCVSummaryDto> GetMovableAssetsCVByCategoryAsync(
            int? categoryId,
            int? assetTypeId,
            CancellationToken cancellationToken = default)
        {
            var query = _assetRepository.GetQueryable()
                .Where(x => !x.MarkedForDeletion);

            if (categoryId.HasValue)
            {
                query = query.Where(x => x.AssetCategoryId == categoryId.Value);
            }

            if (assetTypeId.HasValue)
            {
                query = query.Where(x => x.AssetTypeId == assetTypeId.Value);
            }

            var assets = await query.ToListAsync(cancellationToken);

            var result = new MovableAssetsCVSummaryDto
            {
                TotalAssets = assets.Count
            };

            foreach (var asset in assets)
            {
                var cvDto = MapToMovableAssetDto(asset);
                result.Assets.Add(cvDto);

                if (cvDto.IsCalculated)
                {
                    result.CalculatedAssets++;
                    result.TotalCapitalValue += cvDto.CapitalValue ?? 0;
                    result.TotalPurchaseValue += cvDto.PurchaseValue ?? 0;
                    result.TotalAccumulatedDepreciation += cvDto.AccumulatedDepreciation ?? 0;
                }
            }

            result.LastCVCalculationDate = null;

            return result;
        }

        /// <summary>
        /// Core calculation logic for movable asset CV
        /// </summary>
        private MovableAssetCVResultDto CalculateMovableAssetCV(
            AssetMasterEntity asset,
            MovableAssetValuationMethod method,
            decimal? customDepreciationRate,
            decimal conditionFactor)
        {
            var result = new MovableAssetCVResultDto
            {
                AssetId = asset.Id,
                AssetNo = asset.AssetNo,
                AssetName = asset.AssetName,
                PurchaseValue = asset.PurchaseValue,
                PurchaseDate = asset.PurchaseDate,
                DepreciationRate = customDepreciationRate ?? null,
                ConditionFactor = conditionFactor,
                ValuationMethod = method
            };

            // Calculate age
            if (asset.PurchaseDate.HasValue)
            {
                var age = DateTime.Now - asset.PurchaseDate.Value;
                result.AgeInYears = (int)(age.TotalDays / 365.25);
                result.AgeInMonths = (int)(age.TotalDays / 30.44);
            }

            // Set condition description
            result.ConditionDescription = conditionFactor switch
            {
                >= 0.9m => "Excellent",
                >= 0.7m => "Good",
                >= 0.5m => "Fair",
                >= 0.3m => "Poor",
                _ => "Very Poor"
            };

            // Validate purchase value for depreciation-based methods
            if (!asset.PurchaseValue.HasValue || asset.PurchaseValue.Value <= 0)
            {
                if (method == MovableAssetValuationMethod.DepreciatedValue ||
                    method == MovableAssetValuationMethod.ReplacementCost)
                {
                    result.IsCalculated = false;
                    result.CalculationMessage = "Purchase value is required for this valuation method";
                    return result;
                }
            }

            decimal capitalValue = 0;
            string formula = string.Empty;

            switch (method)
            {
                case MovableAssetValuationMethod.DepreciatedValue:
                    // CV = PurchaseValue × (1 - AnnualDepreciation × Years) × ConditionFactor
                    var depreciationRate = result.DepreciationRate ?? 0.1m; // Default 10% per year
                    var totalDepreciation = depreciationRate * result.AgeInYears;

                    // Cap depreciation at 90% (minimum 10% residual value)
                    if (totalDepreciation > 0.9m) totalDepreciation = 0.9m;

                    result.DepreciationFactor = 1 - totalDepreciation;
                    result.AccumulatedDepreciation = asset.PurchaseValue!.Value * totalDepreciation;

                    capitalValue = asset.PurchaseValue.Value * result.DepreciationFactor.Value * conditionFactor;
                    formula = $"CV = {asset.PurchaseValue:F2} × (1 - {depreciationRate:P0} × {result.AgeInYears}y) × {conditionFactor:F2} = {capitalValue:F2}";
                    break;

                case MovableAssetValuationMethod.MarketValue:
                    // AssetMasterEntity has no market-value/appraisal column to read from yet.
                    result.IsCalculated = false;
                    result.CalculationMessage = "Market value is not set for this asset";
                    return result;

                case MovableAssetValuationMethod.BookValue:
                    // AssetMasterEntity has no recorded book-value column, so derive it from purchase
                    // value and depreciation instead (same shape as DepreciatedValue).
                    if (asset.PurchaseValue.HasValue)
                    {
                        var bookDepRate = result.DepreciationRate ?? 0.1m;
                        var bookTotalDep = bookDepRate * result.AgeInYears;
                        if (bookTotalDep > 0.9m) bookTotalDep = 0.9m;
                        capitalValue = asset.PurchaseValue.Value * (1 - bookTotalDep);
                        formula = $"CV = BookValue = {asset.PurchaseValue:F2} × (1 - {bookTotalDep:P0}) = {capitalValue:F2}";
                    }
                    else
                    {
                        result.IsCalculated = false;
                        result.CalculationMessage = "Neither book value nor purchase value is set";
                        return result;
                    }
                    break;

                case MovableAssetValuationMethod.ReplacementCost:
                    // Simple replacement cost with condition adjustment
                    // In real world, this would use inflation indices
                    var inflationFactor = 1.0m + (0.03m * result.AgeInYears); // Assume 3% inflation per year
                    capitalValue = asset.PurchaseValue!.Value * inflationFactor * conditionFactor;
                    formula = $"CV = {asset.PurchaseValue:F2} × {inflationFactor:F2} (inflation) × {conditionFactor:F2} (condition) = {capitalValue:F2}";
                    break;
            }

            result.CapitalValue = Math.Round(capitalValue, 2);
            result.CurrentBookValue = result.CapitalValue; // Update book value
            result.CVCalculationFormula = formula;
            result.LastCVCalculationDate = DateTime.Now;
            result.IsCalculated = true;
            result.CalculationMessage = "CV calculated successfully";

            return result;
        }

        /// <summary>
        /// Map asset entity to movable asset DTO
        /// </summary>
        private MovableAssetCVResultDto MapToMovableAssetDto(AssetMasterEntity asset)
        {
            var result = new MovableAssetCVResultDto
            {
                AssetId = asset.Id,
                AssetNo = asset.AssetNo,
                AssetName = asset.AssetName,
                PurchaseValue = asset.PurchaseValue,
                PurchaseDate = asset.PurchaseDate,
                IsCalculated = false,
                CalculationMessage = "CV not calculated"
            };

            // Calculate age
            if (asset.PurchaseDate.HasValue)
            {
                var age = DateTime.Now - asset.PurchaseDate.Value;
                result.AgeInYears = (int)(age.TotalDays / 365.25);
                result.AgeInMonths = (int)(age.TotalDays / 30.44);
            }

            // Calculate accumulated depreciation
            if (asset.PurchaseValue.HasValue && false)
            {
                result.AccumulatedDepreciation = asset.PurchaseValue.Value - 0m;
                if (result.AccumulatedDepreciation < 0) result.AccumulatedDepreciation = 0;
            }

            return result;
        }

        #endregion
    }
}
