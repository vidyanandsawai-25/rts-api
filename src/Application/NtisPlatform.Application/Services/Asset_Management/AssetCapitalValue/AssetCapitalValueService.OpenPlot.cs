using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.AssetCapitalValue;

namespace NtisPlatform.Application.Services
{
    public partial class AssetCapitalValueService
    {
        #region Open Plot CV Methods

        /// <summary>
        /// Calculate CV for an open plot asset using LandAreaSqMeter stored on AssetMaster.
        /// Formula: CV = Rate × LandAreaSqMeter × UseFactor
        /// Only UseFactor is applied for open plots (Nature/Age/Floor factors = 1.0, not applicable).
        /// </summary>
        public async Task<PlotCVSummaryDto> CalculatePlotCVAsync(CalculatePlotCVRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting plot CV calculation for AssetId: {AssetId}", request.AssetId);

            var asset = await _assetRepository.GetQueryable()
                .Where(x => x.Id == (int)request.AssetId && !x.MarkedForDeletion)
                .FirstOrDefaultAsync(cancellationToken);

            if (asset == null)
                throw new InvalidOperationException($"Asset with ID {request.AssetId} not found");

            // Land area is stored on AMS.AssetDetails.LandAreaSqMeter (AssetMaster no longer carries it).
            var details = await _detailsRepository.GetQueryable().AsNoTracking()
                .FirstOrDefaultAsync(d => d.AssetId == asset.Id, cancellationToken);
            decimal area = details?.LandAreaSqMeter ?? 0m;
            if (area <= 0)
            {
                _logger.LogWarning("No plot area (LandAreaSqMeter) found for AssetId {AssetId}; returning uncalculated plot CV.", request.AssetId);
                var emptySummary = new PlotCVSummaryDto
                {
                    AssetId = asset.Id,
                    AssetNo = asset.AssetNo,
                    AssetName = asset.AssetName,
                    TotalPlots = 1
                };
                emptySummary.PlotDetails.Add(new PlotCVDetailDto
                {
                    PlotId = asset.Id,
                    IsCalculated = false,
                    CalculationMessage = "No land area found for this plot. Enter Total Plot Area on Basic Info."
                });
                return emptySummary;
            }

            var masterData = await LoadMasterDataAsync(cancellationToken);

            // Resolve active year range
            var yearRange = masterData.YearRanges.FirstOrDefault(x => x.IsActive)
                ?? masterData.YearRanges.FirstOrDefault();
            if (yearRange == null)
                throw new InvalidOperationException("No active assessment year range found in master data");

            // Resolve rate from master: TypeOfUseId as TypeOfUseGroupCVId, FloorGroupId = 0 for open plots
            int typeOfUseGroupId = 0;
            var rateMaster = masterData.AllRateMasters.FirstOrDefault(x =>
                x.AssessmentYearRangeId == yearRange.Id &&
                x.TypeOfUseGroupCVId == typeOfUseGroupId &&
                x.FloorGroupId == 0)
                ?? masterData.AllRateMasters.FirstOrDefault(x => x.AssessmentYearRangeId == yearRange.Id);

            decimal rate = rateMaster?.RateAmount ?? 0m;

            // Open plots have no TypeOfUse/SubTypeOfUse to key a UseFactor lookup off, so it's fixed at 1.
            decimal useFactor = 1m;

            var summary = new PlotCVSummaryDto
            {
                AssetId = asset.Id,
                AssetNo = asset.AssetNo,
                AssetName = asset.AssetName,
                TotalPlots = 1
            };

            var detail = new PlotCVDetailDto
            {
                PlotId = asset.Id,
                PlotAreaSqMtr = (double)area,
                PlotTaxableAreaSqMtr = (double)area,
                BaseRate = rate
            };

            if (rate <= 0)
            {
                detail.IsCalculated = false;
                detail.CalculationMessage = $"No rate found for TypeOfUseGroupId: {typeOfUseGroupId}, YearRange: {yearRange.Id}";
                summary.PlotDetails.Add(detail);
                return summary;
            }

            // NatureFactor = 1, AgeFactor = 1, FloorFactor = 1 — not applicable for open plots
            var (cv, _, formula) = CapitalValueCalculationEngine.Calculate(rate, area, 1m, useFactor, 1m, 1m);
            detail.CapitalValue = cv;
            detail.CVCalculationFormula = formula;
            detail.IsCalculated = true;
            detail.CalculationMessage = "CV calculated successfully";
            summary.CalculatedPlots = 1;
            summary.TotalPlotAreaSqMtr = (double)area;
            summary.TotalCapitalValue = cv;
            summary.LastCVCalculationDate = DateTime.Now;
            summary.PlotDetails.Add(detail);

            await SaveCalculationHistoryAsync(
                asset.Id, $"{yearRange.FromYear}-{yearRange.ToYear % 100:D2}", null,
                rate, area, 1m, useFactor, 1m, 1m,
                cv, formula, cancellationToken);

            // Bumps AssetDetails.UpdatedDate. AssetDetailsEntity.CapitalValue itself is Ignore()'d in
            // the EF model (compatibility shim only) and is NOT what the Asset Register reads — open
            // plots have no SubUnitsDetails rows either, so there is currently no persisted "official"
            // read path for a plot's CV besides recalculating it (this method) or reading the
            // AssetCVCalculationHistory audit trail directly.
            await PersistAssetCapitalValueAsync(asset.Id, cv, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Plot CV calculation completed for AssetId: {AssetId}, TotalCV: {TotalCV}, UseFactor: {UseFactor}",
                request.AssetId, cv, useFactor);

            return summary;
        }

        /// <summary>
        /// Return the current stored CV for an open plot asset without recalculating
        /// </summary>
        public async Task<PlotCVSummaryDto?> GetPlotCVAsync(long assetId, CancellationToken cancellationToken = default)
        {
            var asset = await _assetRepository.GetQueryable()
                .Where(x => x.Id == (int)assetId && !x.MarkedForDeletion)
                .FirstOrDefaultAsync(cancellationToken);

            if (asset == null)
                return null;

            double area = (double)(0m);
            bool hasCV = false;

            var summary = new PlotCVSummaryDto
            {
                AssetId = asset.Id,
                AssetNo = asset.AssetNo,
                AssetName = asset.AssetName,
                TotalPlots = 1,
                TotalPlotAreaSqMtr = area,
                CalculatedPlots = hasCV ? 1 : 0
            };

            summary.PlotDetails.Add(new PlotCVDetailDto
            {
                PlotId = asset.Id,
                PlotAreaSqMtr = area,
                PlotTaxableAreaSqMtr = area,
                IsCalculated = hasCV,
                CalculationMessage = hasCV ? "CV previously calculated" : "CV not calculated"
            });

            return summary;
        }

        #endregion
    }
}
