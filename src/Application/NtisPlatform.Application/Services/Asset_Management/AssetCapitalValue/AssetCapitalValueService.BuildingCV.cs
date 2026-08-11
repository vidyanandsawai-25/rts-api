using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.AssetCapitalValue;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Services
{
    public partial class AssetCapitalValueService
    {
        #region Public Methods

        /// <summary>
        /// Calculate and store capital value for a single asset (shop/unit)
        /// </summary>
        public async Task<AssetCVSummaryDto> CalculateAsync(CalculateAssetCVRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting CV calculation for AssetId: {AssetId}, IncludeChildren: {IncludeChildren}",
                request.AssetId, request.IncludeChildAssets);

            // Get asset master
            var asset = await _assetRepository.GetQueryable()
                .Where(x => x.Id == (int)request.AssetId && !x.MarkedForDeletion)
                .FirstOrDefaultAsync(cancellationToken);

            if (asset == null)
            {
                _logger.LogWarning("Asset not found for AssetId: {AssetId}", request.AssetId);
                throw new InvalidOperationException($"Asset with ID {request.AssetId} not found");
            }

            // Load master data once for efficiency
            var masterData = await LoadMasterDataAsync(cancellationToken);

            // Calculate CV for this asset
            var result = await CalculateAssetCVInternalAsync(asset, masterData, request.SubUnitsDetailsId, cancellationToken);

            // If including child assets, calculate them too
            if (request.IncludeChildAssets)
            {
                var childAssets = await _assetRepository.GetQueryable()
                    .Where(x => x.ParentAssetId == (int)request.AssetId && !x.MarkedForDeletion)
                    .ToListAsync(cancellationToken);

                foreach (var childAsset in childAssets)
                {
                    await CalculateAssetCVInternalAsync(childAsset, masterData, 0, cancellationToken);
                }
            }

            // Save all changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully calculated CV for AssetId: {AssetId}, TotalCV: {TotalCV}",
                request.AssetId, result.TotalCapitalValue);

            return result;
        }

        /// <summary>
        /// Calculate CV for entire building including all child assets (shops/units)
        /// </summary>
        public async Task<BuildingCVSummaryDto> CalculateBuildingCVAsync(CalculateBuildingCVRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting Building CV calculation for BuildingAssetId: {BuildingAssetId}",
                request.BuildingAssetId);

            // Get building (parent) asset
            var building = await _assetRepository.GetQueryable()
                .Where(x => x.Id == (int)request.BuildingAssetId && !x.MarkedForDeletion)
                .FirstOrDefaultAsync(cancellationToken);

            if (building == null)
            {
                _logger.LogWarning("Building asset not found for BuildingAssetId: {BuildingAssetId}", request.BuildingAssetId);
                throw new InvalidOperationException($"Building asset with ID {request.BuildingAssetId} not found");
            }

            // Load master data once
            var masterData = await LoadMasterDataAsync(cancellationToken);

            // Initialize result
            var result = new BuildingCVSummaryDto
            {
                BuildingAssetId = building.Id,
                BuildingAssetNo = building.AssetNo,
                BuildingName = building.AssetName
            };

            // Calculate CV for building's own floor details (if any)
            var buildingCVSummary = await CalculateAssetCVInternalAsync(building, masterData, 0, cancellationToken);
            result.BuildingOwnCapitalValue = buildingCVSummary.TotalCapitalValue;
            result.BuildingOwnFloorDetailsCount = buildingCVSummary.FloorDetailsCount;
            result.BuildingFloorDetails = buildingCVSummary.FloorDetails;

            // Get all child assets (shops/units)
            var childAssets = await _assetRepository.GetQueryable()
                .Where(x => x.ParentAssetId == (int)request.BuildingAssetId && !x.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            result.TotalChildAssets = childAssets.Count;

            // Bumps AssetDetails.UpdatedDate for the building's own CV. AssetDetailsEntity.CapitalValue
            // itself is Ignore()'d in the EF model (compatibility shim only) — the Asset Register actually
            // sums CV from SubUnitsDetailsEntity.CapitalValue (already persisted per floor detail above),
            // not from this call. See PersistAssetCapitalValueAsync.
            await PersistAssetCapitalValueAsync(building.Id, buildingCVSummary.TotalCapitalValue, cancellationToken);

            // Calculate CV for each child asset
            foreach (var childAsset in childAssets)
            {
                var childCVSummary = await CalculateAssetCVInternalAsync(childAsset, masterData, 0, cancellationToken);
                result.ChildAssets.Add(childCVSummary);

                // Each child (shop/unit) is already its own row in the register via its own
                // SubUnitsDetailsEntity.CapitalValue (persisted above); this just bumps its
                // AssetDetails.UpdatedDate the same way the building's own call above does.
                await PersistAssetCapitalValueAsync(childAsset.Id, childCVSummary.TotalCapitalValue, cancellationToken);

                if (childCVSummary.IsFullyCalculated)
                    result.CalculatedChildAssets++;
            }

            // Calculate totals
            result.ChildAssetsCapitalValue = result.ChildAssets.Sum(x => x.TotalCapitalValue);
            result.TotalBuildingCapitalValue = result.BuildingOwnCapitalValue + result.ChildAssetsCapitalValue;
            result.TotalBuildingCarpetAreaSqMeter = buildingCVSummary.TotalCarpetAreaSqMeter +
                result.ChildAssets.Sum(x => x.TotalCarpetAreaSqMeter);

            // Update building's total CV in database
            result.LastCVCalculationDate = DateTime.Now;
            result.IsFullyCalculated = result.CalculatedChildAssets == result.TotalChildAssets &&
                buildingCVSummary.IsFullyCalculated;
            result.CalculationMessage = result.IsFullyCalculated
                ? "All CVs calculated successfully"
                : $"Calculated {result.CalculatedChildAssets}/{result.TotalChildAssets} child assets";

            // Save all changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Building CV calculation completed for BuildingAssetId: {BuildingAssetId}, TotalCV: {TotalCV}, Children: {ChildCount}",
                request.BuildingAssetId, result.TotalBuildingCapitalValue, result.TotalChildAssets);

            return result;
        }

        /// <summary>
        /// Get capital value summary for a specific asset (shop/unit)
        /// </summary>
        public async Task<AssetCVSummaryDto?> GetAssetCVSummaryAsync(long assetId, CancellationToken cancellationToken = default)
        {
            var asset = await _assetRepository.GetQueryable()
                .Where(x => x.Id == (int)assetId && !x.MarkedForDeletion)
                .FirstOrDefaultAsync(cancellationToken);

            if (asset == null)
                return null;

            var floorDetails = await GetByAssetIdAsync(assetId, cancellationToken);

            return new AssetCVSummaryDto
            {
                AssetId = asset.Id,
                AssetNo = asset.AssetNo,
                AssetName = asset.AssetName,
                ParentAssetId = asset.ParentAssetId,
                HierarchyLevel = asset.HierarchyLevel,
                TotalCapitalValue = floorDetails.Where(x => x.IsCalculated).Sum(x => x.CapitalValue ?? 0),
                TotalCarpetAreaSqMeter = floorDetails.Sum(x => x.CarpetAreaSqMeter ?? 0),
                FloorDetailsCount = floorDetails.Count,
                CalculatedFloorDetailsCount = floorDetails.Count(x => x.IsCalculated),
                FloorDetails = floorDetails
            };
        }

        /// <summary>
        /// Get building-level CV summary with breakdown by child assets
        /// </summary>
        public async Task<BuildingCVSummaryDto?> GetBuildingCVSummaryAsync(long buildingAssetId, CancellationToken cancellationToken = default)
        {
            var building = await _assetRepository.GetQueryable()
                .Where(x => x.Id == (int)buildingAssetId && !x.MarkedForDeletion)
                .FirstOrDefaultAsync(cancellationToken);

            if (building == null)
                return null;

            var result = new BuildingCVSummaryDto
            {
                BuildingAssetId = building.Id,
                BuildingAssetNo = building.AssetNo,
                BuildingName = building.AssetName
            };

            // Get building's own floor details
            var buildingFloorDetails = await GetByAssetIdAsync(buildingAssetId, cancellationToken);
            result.BuildingFloorDetails = buildingFloorDetails;
            result.BuildingOwnFloorDetailsCount = buildingFloorDetails.Count;
            result.BuildingOwnCapitalValue = buildingFloorDetails.Where(x => x.IsCalculated).Sum(x => x.CapitalValue ?? 0);

            // Get all child assets
            result.ChildAssets = await GetChildAssetsCVAsync(buildingAssetId, cancellationToken);
            result.TotalChildAssets = result.ChildAssets.Count;
            result.CalculatedChildAssets = result.ChildAssets.Count(x => x.IsFullyCalculated);
            result.ChildAssetsCapitalValue = result.ChildAssets.Sum(x => x.TotalCapitalValue);

            // Calculate totals
            result.TotalBuildingCapitalValue = result.BuildingOwnCapitalValue + result.ChildAssetsCapitalValue;
            result.TotalBuildingCarpetAreaSqMeter = buildingFloorDetails.Sum(x => x.CarpetAreaSqMeter ?? 0) +
                result.ChildAssets.Sum(x => x.TotalCarpetAreaSqMeter);

            result.IsFullyCalculated = result.CalculatedChildAssets == result.TotalChildAssets &&
                buildingFloorDetails.All(x => x.IsCalculated);

            return result;
        }

        /// <summary>
        /// Get capital value for a specific asset floor detail
        /// </summary>
        public async Task<AssetCapitalValueResultDto?> GetByAssetFloorIdAsync(long assetFloorDetailId, CancellationToken cancellationToken = default)
        {
            var floorDetail = await _assetFloorRepository.GetQueryable()
                .Where(x => x.Id == (int)assetFloorDetailId && !x.MarkedForDeletion)
                .Include(x => x.Floor)
                .Include(x => x.SubFloor)
                .Include(x => x.ConstructionType)
                .Include(x => x.TypeOfUse)
                .Include(x => x.SubTypeOfUse)
                .FirstOrDefaultAsync(cancellationToken);

            if (floorDetail == null)
                return null;

            return MapToDto(floorDetail);
        }

        /// <summary>
        /// Get all capital values for an asset
        /// </summary>
        public async Task<List<AssetCapitalValueResultDto>> GetByAssetIdAsync(long assetId, CancellationToken cancellationToken = default)
        {
            var floorDetails = await _assetFloorRepository.GetQueryable()
                .Where(x => x.AssetId == (int)assetId && !x.MarkedForDeletion)
                .Include(x => x.Floor)
                .Include(x => x.SubFloor)
                .Include(x => x.ConstructionType)
                .Include(x => x.TypeOfUse)
                .Include(x => x.SubTypeOfUse)
                .ToListAsync(cancellationToken);

            return floorDetails.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Get all child assets (shops/units) CV for a building
        /// </summary>
        public async Task<List<AssetCVSummaryDto>> GetChildAssetsCVAsync(long parentAssetId, CancellationToken cancellationToken = default)
        {
            var childAssets = await _assetRepository.GetQueryable()
                .Where(x => x.ParentAssetId == (int)parentAssetId && !x.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            var result = new List<AssetCVSummaryDto>();

            foreach (var childAsset in childAssets)
            {
                var floorDetails = await GetByAssetIdAsync(childAsset.Id, cancellationToken);

                result.Add(new AssetCVSummaryDto
                {
                    AssetId = childAsset.Id,
                    AssetNo = childAsset.AssetNo,
                    AssetName = childAsset.AssetName,
                    ParentAssetId = childAsset.ParentAssetId,
                    HierarchyLevel = childAsset.HierarchyLevel,
                    TotalCapitalValue = floorDetails.Where(x => x.IsCalculated).Sum(x => x.CapitalValue ?? 0),
                    TotalCarpetAreaSqMeter = floorDetails.Sum(x => x.CarpetAreaSqMeter ?? 0),
                    FloorDetailsCount = floorDetails.Count,
                    CalculatedFloorDetailsCount = floorDetails.Count(x => x.IsCalculated),
                    FloorDetails = floorDetails
                });
            }

            return result;
        }

        /// <summary>
        /// Read-only rollup: parent asset's own base value + already-calculated CV of all sub-units
        /// (child assets) + already-calculated CV of all inventory batches under it. Composed entirely
        /// from existing read-only lookups (<see cref="GetAssetCVSummaryAsync"/>,
        /// <see cref="GetChildAssetsCVAsync"/>) plus a batched inventory query — nothing is calculated
        /// or written back here.
        /// </summary>
        public async Task<ParentAssetValuationDto?> GetParentAssetValuationAsync(long parentAssetId, CancellationToken cancellationToken = default)
        {
            var parentAsset = await _assetRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.Id == (int)parentAssetId && !x.MarkedForDeletion)
                .FirstOrDefaultAsync(cancellationToken);

            if (parentAsset == null)
                return null;

            // Parent's own base value — its own floor details CV, excluding sub-units/inventory.
            var parentSummary = await GetAssetCVSummaryAsync(parentAssetId, cancellationToken);
            var baseValue = parentSummary?.TotalCapitalValue ?? 0m;

            // Sub-units (child assets) — each one's already-calculated CV.
            // EF Core cannot parse strings to integers in SQL, so Life is computed in-memory below.
            var childAssets = await _assetRepository.GetQueryable()
                .AsNoTracking()
                .Where(a =>
                    a.ParentAssetId == (int)parentAssetId &&
                    !a.MarkedForDeletion &&
                    !_inventoryAssetDetailRepository.GetQueryable().Any(iad => iad.AssetId == a.Id))
                .ToListAsync(cancellationToken);

            var childAssetSummaries = new List<AssetCVSummaryDto>();
            foreach (var childAsset in childAssets)
            {
                var floorDetails = await GetByAssetIdAsync(childAsset.Id, cancellationToken);

                childAssetSummaries.Add(new AssetCVSummaryDto
                {
                    AssetId = childAsset.Id,
                    AssetNo = childAsset.AssetNo,
                    AssetName = childAsset.AssetName,
                    ParentAssetId = childAsset.ParentAssetId,
                    HierarchyLevel = childAsset.HierarchyLevel,
                    TotalCapitalValue = floorDetails.Where(x => x.IsCalculated).Sum(x => x.CapitalValue ?? 0),
                    TotalCarpetAreaSqMeter = floorDetails.Sum(x => x.CarpetAreaSqMeter ?? 0),
                    FloorDetailsCount = floorDetails.Count,
                    CalculatedFloorDetailsCount = floorDetails.Count(x => x.IsCalculated),
                    FloorDetails = floorDetails
                });
            }
            var subUnitsCapitalValue = childAssetSummaries.Sum(x => x.TotalCapitalValue);

            // Floor count: a parent building with sub-units never has floor-detail rows of its own
            // (SubUnitsDetailsService.CreateAsync deliberately skips inserting one — see that class'
            // comments), so parentSummary.FloorDetailsCount alone is always 0 for such buildings. The
            // building's real floors live on its children's own rows instead — count distinct FloorIds
            // across the parent's own floor details (if any) AND every child's, using data already
            // fetched above rather than an extra query.
            var floorIds = new HashSet<int>();
            if (parentSummary?.FloorDetails != null)
            {
                foreach (var fd in parentSummary.FloorDetails) floorIds.Add(fd.FloorId);
            }
            foreach (var child in childAssetSummaries)
            {
                foreach (var fd in child.FloorDetails) floorIds.Add(fd.FloorId);
            }

            // Inventory batches owned directly by this parent asset.
            var (inventoryCapitalValue, inventoryBatchesCount, totalInventoryCount) =
                await GetInventoryCapitalValueAsync((int)parentAssetId, cancellationToken);

            return new ParentAssetValuationDto
            {
                ParentAssetId = parentAsset.Id,
                ParentAssetNo = parentAsset.AssetNo,
                ParentAssetName = parentAsset.AssetName,
                BaseValue = baseValue,
                FloorCount = floorIds.Count,
                SubUnitsCapitalValue = subUnitsCapitalValue,
                SubUnitsCount = childAssetSummaries.Count,
                InventoryCapitalValue = inventoryCapitalValue,
                InventoryBatchesCount = inventoryBatchesCount,
                TotalInventoryCount = totalInventoryCount,
                TotalCapitalValue = baseValue + subUnitsCapitalValue + inventoryCapitalValue
            };
        }

        /// <summary>
        /// Sums already-calculated CV across all active inventory batches owned directly by
        /// <paramref name="parentAssetId"/> (AMS.InventoryBatch / AMS.InventoryAssetDetail). Prefers
        /// each batch's own TotalBatchCV; falls back to summing its InventoryAssetDetail rows'
        /// UnitCapitalValue when TotalBatchCV isn't set. TotalInventoryCount is the sum of each
        /// batch's own Quantity (total physical items across all batches, not the batch count).
        /// Read-only — never calculates or persists.
        /// </summary>
        private async Task<(decimal TotalCapitalValue, int BatchesCount, int TotalInventoryCount)> GetInventoryCapitalValueAsync(
            int parentAssetId, CancellationToken cancellationToken)
        {
            var inventoryBatches = await _inventoryBatchRepository.GetQueryable()
                .AsNoTracking()
                .Where(b => b.ParentAssetId == parentAssetId && b.IsActive && !b.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            var batchIds = inventoryBatches.Select(b => b.Id).ToList();
            var unitCVTotalsByBatch = batchIds.Count == 0
                ? new Dictionary<int, decimal>()
                : await _inventoryAssetDetailRepository.GetQueryable()
                    .AsNoTracking()
                    .Where(d => batchIds.Contains(d.BatchId) && d.IsActive && !d.MarkedForDeletion)
                    .GroupBy(d => d.BatchId)
                    .Select(g => new { BatchId = g.Key, Total = g.Sum(d => d.UnitCapitalValue ?? 0m) })
                    .ToDictionaryAsync(x => x.BatchId, x => x.Total, cancellationToken);

            var totalCapitalValue = inventoryBatches
                .Sum(b => b.TotalBatchCV ?? unitCVTotalsByBatch.GetValueOrDefault(b.Id));
            var totalInventoryCount = inventoryBatches.Sum(b => b.Quantity);

            return (totalCapitalValue, inventoryBatches.Count, totalInventoryCount);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Load all master data required for CV calculations
        /// </summary>
        private async Task<CVMasterData> LoadMasterDataAsync(CancellationToken cancellationToken)
        {
            // Read-only reference data reused across every floor detail / child asset in this request
            // (avoids one query per floor/child) — AsNoTracking since nothing here is ever updated.
            var yearRanges = await _assessmentYearRangeRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken);

            var floorFactors = await _floorFactorRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

            var natureFactors = await _natureFactorRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

            var useFactors = await _useFactorRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

            var ageFactors = await _ageFactorRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

            var typeOfUses = await _typeOfUseRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

            var typeOfUseGroups = await _typeOfUseGroupRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

            // Load rate masters directly by SubZoneId — no CSN dependency
            // Not filterable by AssessmentYearRangeId here — CalculateFloorDetailCV resolves the year
            // range per floor detail (parsed from that floor's own AssessmentYear), which varies across
            // the floor details/child assets this data is reused for.
            var allRateMasters = await _rateRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Select(rm => new RateMasterLookup
                {
                    Id = rm.Id,
                    SubZoneId = rm.SubZoneId,
                    TypeOfUseGroupCVId = rm.TypeOfUseGroupCVId,
                    FloorGroupId = rm.FloorGroupId,
                    AssessmentYearRangeId = rm.AssessmentYearRangeId,
                    RateAmount = rm.RateAmount
                })
                .ToListAsync(cancellationToken);

            // --- Precomputed lookups: DSA optimization ---
            // CalculateFloorDetailCV runs once per floor detail and previously re-scanned these same
            // lists with FirstOrDefault on every call (O(n) per floor detail). Building the lookups
            // once here, per request, turns each of those scans into an O(1) (or small-bucket O(k))
            // dictionary lookup. See CVMasterData for the field declarations.

            // PK-keyed: Id is the entity's own DB primary key, guaranteed unique — plain ToDictionary
            // is safe (cannot throw on duplicates, cannot reorder).
            var typeOfUseById = typeOfUses.ToDictionary(x => x.Id);
            var typeOfUseGroupById = typeOfUseGroups.ToDictionary(x => x.Id);

            // Composite/business-key lookups: uniqueness on these keys is NOT guaranteed by any
            // constraint visible here, so build via GroupBy + first-of-group — never
            // list.ToDictionary(key) (throws on duplicates) and never a last-wins reduction (would
            // silently pick a different row than the original FirstOrDefault did). GroupBy streams the
            // source list in its original order, so g.First() reproduces exactly what
            // list.FirstOrDefault(predicateForThatKey) would have returned.
            var rateByYearRangeUseGroupFloorGroup = allRateMasters
                .GroupBy(x => (x.AssessmentYearRangeId, x.TypeOfUseGroupCVId, x.FloorGroupId))
                .ToDictionary(g => g.Key, g => g.First());

            var rateByYearRangeUseGroup = allRateMasters
                .GroupBy(x => (x.AssessmentYearRangeId, x.TypeOfUseGroupCVId))
                .ToDictionary(g => g.Key, g => g.First());
            // Two SEPARATE dictionaries mirroring the two SEPARATE FirstOrDefault stages in
            // CalculateFloorDetailCV (exact floor-group match, then floor-group-agnostic relaxed
            // match) — collapsing to one lookup would erase the distinction that drives
            // usedFallbackRate / the LogWarning it triggers.

            var natureFactorByConstructionTypeAndYearRange = natureFactors
                .GroupBy(x => (x.ConstructionTypeId, x.YearRangeCVId))
                .ToDictionary(g => g.Key, g => g.First().Factor);

            var useFactorByTypeOfUseYearRangeAndSubType = useFactors
                .GroupBy(x => (x.TypeOfUseId, x.YearRangeCVId, x.SubTypeOfUseId))
                .ToDictionary(g => g.Key, g => g.First().Factor);

            var ageFactorsByConstructionTypeAndYearRange = ageFactors
                .GroupBy(x => (x.ConstructionTypeId, x.YearRangeCVId))
                .ToDictionary(g => g.Key, g => g.ToList());
            // Bucketed, not collapsed to one row: the age lookup also has a range condition
            // (AgeFrom/AgeTo) outside the composite key, so the per-floor-detail call still scans —
            // just within its bucket (few rows) instead of the whole table. Rows outside the bucket
            // always fail the composite-key check regardless of order, so narrowing to the bucket
            // cannot change which row wins.

            var floorFactorByFloorAndYearRange = floorFactors
                .GroupBy(x => (x.FloorId, x.YearRangeCVId))
                .ToDictionary(g => g.Key, g => g.First());

            return new CVMasterData
            {
                YearRanges = yearRanges,
                FloorFactors = floorFactors,
                NatureFactors = natureFactors,
                UseFactors = useFactors,
                AgeFactors = ageFactors,
                TypeOfUses = typeOfUses,
                TypeOfUseGroups = typeOfUseGroups,
                AllRateMasters = allRateMasters,
                TypeOfUseById = typeOfUseById,
                TypeOfUseGroupById = typeOfUseGroupById,
                RateByYearRangeUseGroupFloorGroup = rateByYearRangeUseGroupFloorGroup,
                RateByYearRangeUseGroup = rateByYearRangeUseGroup,
                NatureFactorByConstructionTypeAndYearRange = natureFactorByConstructionTypeAndYearRange,
                UseFactorByTypeOfUseYearRangeAndSubType = useFactorByTypeOfUseYearRangeAndSubType,
                AgeFactorsByConstructionTypeAndYearRange = ageFactorsByConstructionTypeAndYearRange,
                FloorFactorByFloorAndYearRange = floorFactorByFloorAndYearRange
            };
        }


        /// <summary>
        /// Calculate CV for a single asset's floor details
        /// </summary>
        private async Task<AssetCVSummaryDto> CalculateAssetCVInternalAsync(
            AssetMasterEntity asset,
            CVMasterData masterData,
            long specificFloorDetailId,
            CancellationToken cancellationToken)
        {
            var result = new AssetCVSummaryDto
            {
                AssetId = asset.Id,
                AssetNo = asset.AssetNo,
                AssetName = asset.AssetName,
                ParentAssetId = asset.ParentAssetId,
                HierarchyLevel = asset.HierarchyLevel
            };

            // Get floor details with navigation properties
            var baseQuery = _assetFloorRepository.GetQueryable()
                .Where(x => x.AssetId == asset.Id && !x.MarkedForDeletion)
                .Include(x => x.Floor)
                .Include(x => x.SubFloor)
                .Include(x => x.ConstructionType)
                .Include(x => x.SubTypeOfUse)
                .AsQueryable();

            if (specificFloorDetailId > 0)
            {
                baseQuery = baseQuery.Where(x => x.Id == specificFloorDetailId);
            }

            var floorDetails = await baseQuery.ToListAsync(cancellationToken);
            result.FloorDetailsCount = floorDetails.Count;

            if (!floorDetails.Any())
            {
                // Open-plot assets are valued via CalculatePlotCVAsync (AssetMaster-driven), not floor
                // details, so an asset with none simply has nothing to compute here.
                _logger.LogInformation("No floor details found for AssetId: {AssetId}", asset.Id);
                return result;
            }

            // Filter rate masters by SubZoneId; fall back to all active rates if SubZoneId not set
            var assetRateMasters = masterData.AllRateMasters;

            if (!assetRateMasters.Any())
            {
                _logger.LogWarning("No rate masters found for AssetId: {AssetId}, SubZoneId: {SubZoneId}", asset.Id, null);
            }

            // Calculate CV for each floor detail
            foreach (var floorDetail in floorDetails)
            {
                // 1. Resolve Carpet Area using Room-Wise details — ONLY as a fallback when this row
                // doesn't already carry a directly-entered carpet area. The "Save Unit" flow
                // (ManageSubUnitsService) always computes and persists CarpetAreaSqMeter itself
                // (from the total-area field or from the room list it was actually given), so that
                // value is authoritative. Recomputing from *any* active room-wise rows unconditionally
                // is wrong: a unit can have unrelated leftover/stale room-wise rows from an earlier
                // configuration (e.g. a prior direct-room registration) that would silently override
                // the area the user just saved. Only fall back to summing rooms when the row has no
                // usable area of its own — the direct-room-registration path (CreateDirectRoomsAsync),
                // where CarpetAreaSqMeter is never set directly, is what this fallback exists for.
                var hasDirectArea = floorDetail.CarpetAreaSqMeter.HasValue && floorDetail.CarpetAreaSqMeter.Value > 0;
                var rooms = hasDirectArea
                    ? new List<AssetRoomWiseSubmissionDetailsEntity>()
                    : await _roomDetailsRepository.GetQueryable()
                        .Where(x => x.SubUnitsDetailsId == floorDetail.Id && !x.MarkedForDeletion)
                        .Include(x => x.RoomMinusData)
                        .ToListAsync(cancellationToken);

                if (rooms.Any())
                {
                    _logger.LogInformation("Resolving carpet area from Room-Wise submissions for FloorDetailsId: {FloorDetailsId}", floorDetail.Id);
                    double totalRoomArea = 0;
                    foreach (var room in rooms)
                    {
                        double roomNetArea = room.TotalAreaSqMtr ?? room.AreaSqMtr ?? (room.LengthMtr * room.WidthMtr) ?? 0d;
                        if (room.RoomMinusData != null && room.RoomMinusData.Any())
                        {
                            double minusArea = room.RoomMinusData
                                .Where(m => !m.MarkedForDeletion)
                                .Sum(m => m.AreaSqMtr ?? (m.LengthMtr * m.WidthMtr) ?? 0d);
                            roomNetArea -= minusArea;
                        }

                        if (room.MinusYesNo)
                        {
                            totalRoomArea -= roomNetArea;
                        }
                        else if (room.OuterYesNo)
                        {
                            totalRoomArea += roomNetArea * 0.8;
                        }
                        else
                        {
                            totalRoomArea += roomNetArea;
                        }
                    }

                    totalRoomArea = Math.Max(0.0, totalRoomArea);

                    if (totalRoomArea > 0)
                    {
                        floorDetail.CarpetAreaSqMeter = (decimal)totalRoomArea;
                        floorDetail.CarpetAreaSqFeet = (decimal)(totalRoomArea * 10.7639);
                    }
                }

                var cvResult = CalculateFloorDetailCV(
                    floorDetail,
                    false,
                    null,
                    masterData,
                    assetRateMasters);

                result.FloorDetails.Add(cvResult);

                if (cvResult.IsCalculated)
                {
                    result.CalculatedFloorDetailsCount++;

                    // 2. Save calculation history record
                    await SaveCalculationHistoryAsync(
                        asset.Id,
                        floorDetail.AssessmentYear ?? "2025-26",
                        floorDetail.FloorId,
                        cvResult.CVBaseRate ?? 0m,
                        cvResult.CarpetAreaSqMeter ?? 0m,
                        cvResult.CVNatureFactor ?? 1.0m,
                        cvResult.CVUseFactor ?? 1.0m,
                        cvResult.CVAgeFactor ?? 1.0m,
                        cvResult.CVFloorFactor ?? 1.0m,
                        cvResult.CapitalValue ?? 0m,
                        cvResult.CVCalculationFormula ?? string.Empty,
                        cancellationToken
                    );
                }
            }

            // Calculate totals
            result.TotalCapitalValue = result.FloorDetails.Where(x => x.IsCalculated).Sum(x => x.CapitalValue ?? 0);
            result.TotalCarpetAreaSqMeter = result.FloorDetails.Sum(x => x.CarpetAreaSqMeter ?? 0);
            result.LastCVCalculationDate = DateTime.Now;

            // Update asset's total CV
            return result;
        }

        /// <summary>
        /// Calculate CV for a single floor detail
        /// </summary>
        private AssetCapitalValueResultDto CalculateFloorDetailCV(
            SubUnitsDetailsEntity floorDetail,
            bool hasLift,
            int? subZoneId,
            CVMasterData masterData,
            List<RateMasterLookup> rateMasters)
        {
            // AMS.TypeOfUseMaster row for this floor detail's TypeOfUseId — replaces the old
            // floorDetail.TypeOfUse navigation (which pointed at the empty PTIS.TypeOfUseMaster).
            var typeOfUse = masterData.TypeOfUseById.GetValueOrDefault(floorDetail.TypeOfUseId);

            var result = new AssetCapitalValueResultDto
            {
                Id = floorDetail.Id,
                AssetId = floorDetail.AssetId,
                SubUnitsDetailsId = floorDetail.Id,
                FloorId = floorDetail.FloorId,
                FloorDescription = floorDetail.Floor?.Description,
                SubFloorId = floorDetail.SubFloorId,
                SubFloorDescription = floorDetail.SubFloor?.Description,
                ConstructionYear = floorDetail.ConstructionYear,
                AssessmentYear = floorDetail.AssessmentYear,
                ConstructionTypeId = floorDetail.ConstructionTypeId,
                ConstructionTypeDescription = floorDetail.ConstructionType?.Description,
                TypeOfUseId = floorDetail.TypeOfUseId,
                TypeOfUseDescription = typeOfUse?.Description,
                SubTypeOfUseId = floorDetail.SubTypeOfUseId,
                SubTypeOfUseDescription = floorDetail.SubTypeOfUse?.Description,
                CarpetAreaSqMeter = floorDetail.CarpetAreaSqMeter,
                CarpetAreaSqFeet = floorDetail.CarpetAreaSqFeet,
                BuiltUpAreaSqMeter = floorDetail.BuiltUpAreaSqMeter,
                BuiltUpAreaSqFeet = floorDetail.BuiltUpAreaSqFeet
            };

            // Validate and parse years
            if (!int.TryParse(floorDetail.AssessmentYear, out int assessmentYear) || assessmentYear <= 0)
            {
                result.IsCalculated = false;
                result.CalculationMessage = $"Invalid assessment year: '{floorDetail.AssessmentYear}'";
                return result;
            }

            if (!int.TryParse(floorDetail.ConstructionYear, out int constructionYear) || constructionYear <= 0)
            {
                result.IsCalculated = false;
                result.CalculationMessage = $"Invalid construction year: '{floorDetail.ConstructionYear}'";
                return result;
            }

            int ageOfProperty = assessmentYear - constructionYear;
            if (ageOfProperty < 0)
            {
                result.IsCalculated = false;
                result.CalculationMessage = $"Invalid property age: {ageOfProperty}";
                return result;
            }

            // Validate carpet area
            if (!floorDetail.CarpetAreaSqMeter.HasValue || floorDetail.CarpetAreaSqMeter.Value <= 0)
            {
                result.IsCalculated = false;
                result.CalculationMessage = $"Invalid carpet area: {floorDetail.CarpetAreaSqMeter}";
                return result;
            }

            // Find year range — AssessmentYearRangeMaster is tiny (a handful of year bands), so a
            // linear scan here is effectively O(1); not worth a bucketed/interval index.
            var yearRange = masterData.YearRanges.FirstOrDefault(x =>
                assessmentYear >= x.FromYear && assessmentYear <= x.ToYear);

            if (yearRange == null)
            {
                result.IsCalculated = false;
                result.CalculationMessage = $"Year range not found for: {assessmentYear}";
                return result;
            }

            // Find rate master
            int? typeOfUseGroupId = typeOfUse?.TypeOfUseGroupId;
            if (!typeOfUseGroupId.HasValue)
            {
                result.IsCalculated = false;
                result.CalculationMessage = $"TypeOfUseGroupId not found for TypeOfUseId: {floorDetail.TypeOfUseId}";
                return result;
            }

            var typeOfUseGroup = masterData.TypeOfUseGroupById.GetValueOrDefault(typeOfUseGroupId.Value);
            bool isFloorWiseRateApplicable = typeOfUseGroup?.IsFloorWiseRateApplicable ?? false;
            int? floorGroupId = isFloorWiseRateApplicable ? floorDetail.Floor?.FloorGroupId : null;

            // Rate resolution — stay within the floor's applicable assessment-year range (using another
            // year's rates would misvalue the asset). Prefer the exact floor-group rate; if that specific
            // FloorGroupId isn't seeded, relax ONLY the floor group and reuse another rate for the same
            // year range + use-group. Report "rate not found" only when the year range genuinely has no
            // rate for this use group (a data-seeding gap to fix in CVRateMaster).
            RateMasterLookup? rateMaster = null;
            bool usedFallbackRate = false;

            // 1. Most specific: same year range + use-group + floor-group (floor-wise use-groups only).
            if (isFloorWiseRateApplicable && floorGroupId.HasValue)
            {
                masterData.RateByYearRangeUseGroupFloorGroup.TryGetValue(
                    ((int?)yearRange.Id, typeOfUseGroupId, floorGroupId), out rateMaster);
            }

            // 2. Same year range + use-group (relax floor group — covers missing/mismatched FloorGroupId).
            if (rateMaster == null)
            {
                masterData.RateByYearRangeUseGroup.TryGetValue(
                    ((int?)yearRange.Id, typeOfUseGroupId), out rateMaster);
                if (rateMaster != null && isFloorWiseRateApplicable) usedFallbackRate = true;
            }

            if (rateMaster == null)
            {
                result.IsCalculated = false;
                result.CalculationMessage = $"Rate not found for TypeOfUseGroupId: {typeOfUseGroupId} in assessment year range {yearRange.Id} (seed CVRateMaster for this use group / year range)";
                return result;
            }

            if (usedFallbackRate)
            {
                _logger.LogWarning(
                    "CV rate fallback used for FloorDetailsId {FloorDetailsId} (AssetId {AssetId}): no exact rate for YearRange {YearRangeId} / UseGroup {UseGroupId} / FloorGroup {FloorGroupId}; reused same-year-range rate {RateId} = {RateAmount}. Seed the exact CVRateMaster row for accurate valuation.",
                    floorDetail.Id, floorDetail.AssetId, yearRange.Id, typeOfUseGroupId, floorGroupId, rateMaster.Id, rateMaster.RateAmount);
            }

            // RateAmount is nullable on the source table — a seeded rate row with no amount set must
            // fail the calculation, not throw, so this can't be an unchecked cast.
            if (!rateMaster.RateAmount.HasValue || rateMaster.RateAmount.Value <= 0m)
            {
                result.IsCalculated = false;
                result.CalculationMessage = $"Rate amount not set for rate master {rateMaster.Id} (TypeOfUseGroupId: {typeOfUseGroupId}, YearRange: {yearRange.Id})";
                return result;
            }

            decimal rate = rateMaster.RateAmount.Value;

            // Calculate factors
            decimal natureFactor = masterData.NatureFactorByConstructionTypeAndYearRange
                .GetValueOrDefault((floorDetail.ConstructionTypeId, yearRange.Id));
            if (natureFactor == 0) natureFactor = 1;

            decimal useFactor = 1;
            if (floorDetail.SubTypeOfUseId.HasValue)
            {
                useFactor = masterData.UseFactorByTypeOfUseYearRangeAndSubType
                    .GetValueOrDefault((floorDetail.TypeOfUseId, yearRange.Id, floorDetail.SubTypeOfUseId.Value));
                if (useFactor == 0) useFactor = 1;
            }

            var ageFactorBucket = masterData.AgeFactorsByConstructionTypeAndYearRange
                .GetValueOrDefault((floorDetail.ConstructionTypeId, yearRange.Id));
            var ageFactorEntity = ageFactorBucket?.FirstOrDefault(x =>
                ageOfProperty >= x.AgeFrom && ageOfProperty <= x.AgeTo);
            decimal ageFactor = ageFactorEntity?.Factor ?? 1;

            decimal floorFactor = 1;
            var floorFactorEntity = masterData.FloorFactorByFloorAndYearRange
                .GetValueOrDefault((floorDetail.FloorId, yearRange.Id));
            if (floorFactorEntity != null)
            {
                var rawFactor = hasLift ? floorFactorEntity.FactorWithLift : floorFactorEntity.FactorWithoutLift;
                floorFactor = rawFactor == 0 ? 1m : rawFactor;
            }

            // --- NEW LOGIC (Separated Calculation Engine) ---
            decimal carpetArea = floorDetail.CarpetAreaSqMeter ?? 0;
            var calculation = CapitalValueCalculationEngine.Calculate(
                rate, carpetArea, natureFactor, useFactor, ageFactor, floorFactor);

            // Update entity — persist the full breakdown, not just the final number, so
            // AMS.SubUnitsDetails reflects exactly how this unit's CV was derived.
            floorDetail.CapitalValue = calculation.CapitalValue;
            floorDetail.BaseValue = calculation.BaseValue;
            floorDetail.CVBaseRate = rate;
            floorDetail.CVNatureFactor = natureFactor;
            floorDetail.CVUseFactor = useFactor;
            floorDetail.CVAgeFactor = ageFactor;
            floorDetail.CVFloorFactor = floorFactor;

            // Update result
            result.CVBaseRate = rate;
            result.CVNatureFactor = natureFactor;
            result.CVUseFactor = useFactor;
            result.CVAgeFactor = ageFactor;
            result.CVFloorFactor = floorFactor;
            result.CapitalValue = calculation.CapitalValue;
            result.BaseValue = calculation.BaseValue;
            result.CVCalculationFormula = calculation.Formula;
            result.LastCVCalculationDate = DateTime.Now;
            result.IsCalculated = true;
            result.CalculationMessage = "CV calculated successfully using the pure math engine.";

            return result;
        }

        private AssetCapitalValueResultDto MapToDto(SubUnitsDetailsEntity entity)
        {
            return new AssetCapitalValueResultDto
            {
                Id = entity.Id,
                AssetId = entity.AssetId,
                SubUnitsDetailsId = entity.Id,
                FloorId = entity.FloorId,
                FloorDescription = entity.Floor?.Description,
                SubFloorId = entity.SubFloorId,
                SubFloorDescription = entity.SubFloor?.Description,
                ConstructionYear = entity.ConstructionYear,
                AssessmentYear = entity.AssessmentYear,
                ConstructionTypeId = entity.ConstructionTypeId,
                ConstructionTypeDescription = entity.ConstructionType?.Description,
                TypeOfUseId = entity.TypeOfUseId,
                TypeOfUseDescription = entity.TypeOfUse?.Description,
                SubTypeOfUseId = entity.SubTypeOfUseId,
                SubTypeOfUseDescription = entity.SubTypeOfUse?.Description,
                CarpetAreaSqMeter = entity.CarpetAreaSqMeter,
                CarpetAreaSqFeet = entity.CarpetAreaSqFeet,
                BuiltUpAreaSqMeter = entity.BuiltUpAreaSqMeter,
                BuiltUpAreaSqFeet = entity.BuiltUpAreaSqFeet,
                CVBaseRate = entity.CVBaseRate,
                CapitalValue = entity.CapitalValue,
                IsCalculated = entity.CapitalValue.HasValue && entity.CapitalValue.Value > 0,
                CalculationMessage = entity.CapitalValue.HasValue ? "CV previously calculated" : "CV not calculated"
            };
        }

        #endregion
    }
}
