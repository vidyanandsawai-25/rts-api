using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.AssetCapitalValue;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Services
{
    public partial class AssetCapitalValueService
    {
        #region CV Calculation History Methods

        /// <summary>
        /// Get CV calculation history for an asset
        /// </summary>
        public async Task<List<AssetCVCalculationHistoryDto>> GetCalculationHistoryAsync(long assetId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Retrieving CV calculation history for AssetId: {AssetId}", assetId);

            var historyEntities = await _historyRepository.GetQueryable()
                .Where(x => x.AssetId == (int)assetId)
                .Include(x => x.AssetMaster)
                .OrderByDescending(x => x.CalculationDate)
                .ToListAsync(cancellationToken);

            // Fetch descriptions in-memory for safety and simplicity
            var floorIds = historyEntities.Where(x => x.FloorId.HasValue).Select(x => x.FloorId!.Value).Distinct().ToList();
            var floorDescriptions = await _assetFloorRepository.GetQueryable()
                .Where(x => floorIds.Contains(x.FloorId))
                .Select(x => new { x.FloorId, Description = x.Floor != null ? x.Floor.Description : string.Empty })
                .Distinct()
                .ToDictionaryAsync(x => x.FloorId, x => x.Description, cancellationToken);

            var dtos = _mapper.Map<List<AssetCVCalculationHistoryDto>>(historyEntities);

            foreach (var dto in dtos)
            {
                if (dto.FloorId.HasValue && floorDescriptions.TryGetValue(dto.FloorId.Value, out var floorDesc))
                {
                    dto.FloorDescription = floorDesc;
                }
            }

            return dtos;
        }

        private async Task SaveCalculationHistoryAsync(
            int assetId,
            string financialYear,
            int? floorId,
            decimal baseRate,
            decimal area,
            decimal natureFactor,
            decimal useFactor,
            decimal ageFactor,
            decimal floorFactor,
            decimal capitalValue,
            string formula,
            CancellationToken cancellationToken)
        {
            // History no longer tracks a "latest" flag (IsLatest column removed from schema);
            // simply insert the new history record.
            // Insert new history record
            var newHistory = new AssetCVCalculationHistoryEntity
            {
                AssetId = assetId,
                CalculationDate = DateTime.Now,
                FinancialYear = financialYear,
                FloorId = floorId,
                BuiltUpAreaSqMeter = area,
                BaseRate = baseRate,
                AgeFactor = ageFactor,
                FloorFactor = floorFactor,
                NatureFactor = natureFactor,
                UseFactor = useFactor,
                CapitalValue = capitalValue,
                IsActive = true,
                CreatedDate = DateTime.Now,
                CreatedBy = null // CreatedBy is INT in the DB; no user context in the CV calc flow
            };

            await _historyRepository.AddAsync(newHistory, cancellationToken);
        }

        #endregion
    }
}
