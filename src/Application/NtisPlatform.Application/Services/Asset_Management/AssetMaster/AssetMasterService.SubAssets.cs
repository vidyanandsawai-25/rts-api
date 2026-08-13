using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.DTOs.Asset_Management.SubUnitsDetails;
using NtisPlatform.Application.DTOs.Asset_Management.AssetRoomWiseSubmissionDetails;
using NtisPlatform.Application.DTOs.Asset_Management.AssetDetails;
using NtisPlatform.Application.DTOs.Asset_Management.InventoryAsset;
using NtisPlatform.Application.DTOs.Asset_Management.AssetFieldValue;

namespace NtisPlatform.Application.Services.Asset_Management
{
    public partial class AssetMasterService
    {
        #region Endpoint: Get Sub-Assets By Parent ID

        /// <summary>
        /// Gets all child assets by parent asset ID and floor details ID.
        /// </summary>
        public async Task<List<AssetMasterDto>> GetByParentAssetIdAsync(int parentAssetId, int floorDetailsId, CancellationToken cancellationToken = default)
        {
            var childAssets = await _repository.GetQueryable()
                .AsNoTracking()
                .Where(a => a.IsActive &&
                a.ParentAssetId == parentAssetId
                            && !a.MarkedForDeletion)
                .Select(a => new AssetMasterDto
                {
                    Id = a.Id,
                    IsActive = a.IsActive,
                    CreatedDate = a.CreatedDate,
                    UpdatedDate = a.UpdatedDate,
                    AssetNo = a.AssetNo,
                    AssetName = a.AssetName,
                    AssetCategoryId = a.AssetCategoryId,
                    AssetTypeId = a.AssetTypeId,
                    ParentAssetId = a.ParentAssetId,
                    HierarchyLevel = a.HierarchyLevel,
                    HierarchyPath = a.HierarchyPath,
                    OwnershipType = a.OwnershipType,
                    OccupancyStatus = a.OccupancyStatus,
                    Details = new AssetDetailsDto
                    {
                        Id = a.Details != null ? a.Details.Id : 0,
                        PropertyNo = a.Details != null ? a.Details.PropertyNo : null,
                        PartitionNo = a.Details != null ? a.Details.PartitionNo : null,
                        PlotNo = a.Details != null ? a.Details.PlotNo : null,
                        UpicId = a.Details != null ? a.Details.UpicId : null,
                        InChargeName = a.Details != null ? a.Details.InChargeName : null,
                        InChargeDesignationId = a.Details != null ? a.Details.InChargeDesignationId : null,
                        InChargeMobile = a.Details != null ? a.Details.InChargeMobile : null,
                        InChargeEmail = a.Details != null ? a.Details.InChargeEmail : null,
                        LandRate = a.Details != null ? a.Details.LandRate : null
                    },
                    Names = new AssetMasterNamesDto
                    {
                        AssetCategoryName = a.AssetCategory != null ? a.AssetCategory.CategoryName : null,
                        AssetTypeName = a.AssetType != null ? a.AssetType.TypeName : null,
                        ParentAssetName = a.ParentAsset != null ? a.ParentAsset.AssetName : null
                    },
                    FieldValues = a.FieldValues!
                        .Where(fv => fv.IsActive == a.IsActive && !fv.MarkedForDeletion)
                        .Select(fv => new AssetFieldValueDto
                        {
                            Id = fv.Id,
                            FieldDefinitionId = fv.FieldDefinitionId,
                            FieldName = fv.FieldName,
                            FieldValue = fv.FieldValue
                        })
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            await EnrichLocationAsync(childAssets, cancellationToken);
            return childAssets;
        }

        #endregion

        #region Sub-Asset and Floor Details Query Methods

        public async Task<AssetFloorAndOtherDetailsResponseDto?> GetAssetFloorAndOtherDetailsAsync(int parentAssetId, CancellationToken cancellationToken = default)
        {
            var parentAsset = await _repository.GetQueryable()
                .AsNoTracking()
                .Include(a => a.AssetCategory)
                .Include(a => a.AssetType)
                .FirstOrDefaultAsync(a => a.Id == parentAssetId, cancellationToken);

            if (parentAsset == null)
            {
                return null;
            }

            var childAssets = await _repository.GetQueryable()
            .AsNoTracking()
            .Where(a =>
                a.ParentAssetId == parentAssetId &&
                a.IsActive &&
                !a.MarkedForDeletion &&
                _floorDetailsRepository.GetQueryable()
                    .Any(fd =>
                        fd.AssetId == a.Id &&
                        fd.IsActive &&
                        !fd.MarkedForDeletion))
            .Include(a => a.AssetCategory)
            .Include(a => a.AssetType)
            .OrderBy(a => a.Id)
            .ToListAsync(cancellationToken);

            var childAssetIds = childAssets.Select(a => a.Id).ToList();

            // Load AMS TypeOfUse / SubTypeOfUse lookup dictionaries in one query each.
            var typeOfUseLookup = await _amsTypeOfUseRepository.GetQueryable()
                .AsNoTracking()
                .Where(t => t.IsActive)
                .ToDictionaryAsync(t => t.Id, t => t.Description, cancellationToken);

            var subTypeOfUseLookup = await _amsSubTypeOfUseRepository.GetQueryable()
                .AsNoTracking()
                .Where(t => t.IsActive)
                .ToDictionaryAsync(t => t.Id, t => t.Description, cancellationToken);

            var childFloorDetails = await _floorDetailsRepository.GetQueryable()
                .AsNoTracking()
                .Where(f => childAssetIds.Contains(f.AssetId) && f.IsActive && !f.MarkedForDeletion)
                .Include(f => f.Asset)
                    .ThenInclude(a => a!.AssetCategory)
                .Include(f => f.Asset)
                    .ThenInclude(a => a!.AssetType)
                .Include(f => f.Floor)
                .Include(f => f.SubFloor)
                .Include(f => f.ConstructionType)
                .OrderBy(f => f.AssetId)
                .ThenBy(f => f.FloorId)
                .ThenBy(f => f.SubFloorId)
                .ToListAsync(cancellationToken);

            static decimal? SumOrNull(IEnumerable<decimal?> values)
            {
                decimal sum = 0m;
                var any = false;
                foreach (var v in values)
                {
                    if (v.HasValue)
                    {
                        sum += v.Value;
                        any = true;
                    }
                }
                return any ? sum : null;
            }

            var floorDetailDtos = childFloorDetails
                .GroupBy(f => f.FloorId)
                .OrderBy(g => g.First().Floor != null ? g.First().Floor!.SequenceNo ?? int.MaxValue : int.MaxValue)
                .ThenBy(g => g.Key)
                .Select(g =>
                {
                    var first = g.First();
                    return new AssetFloorDetailResponseDto
                    {
                        FloorName = first.Floor != null ? first.Floor.Description : null,
                        ConstructionTypeName = first.ConstructionType != null ? first.ConstructionType.Description : null,
                        TypeOfUseName = first.TypeOfUseId > 0 && typeOfUseLookup.TryGetValue(first.TypeOfUseId, out var tou) ? tou : null,
                        ConstructionYear = first.ConstructionYear,
                        CarpetAreaSqMeter = SumOrNull(g.Select(x => x.CarpetAreaSqMeter)),
                        CarpetAreaSqFeet = SumOrNull(g.Select(x => x.CarpetAreaSqFeet)),
                        BuiltUpAreaSqMeter = SumOrNull(g.Select(x => x.BuiltUpAreaSqMeter)),
                        BuiltUpAreaSqFeet = SumOrNull(g.Select(x => x.BuiltUpAreaSqFeet)),
                        CapitalValue = SumOrNull(g.Select(x => x.CapitalValue)),
                    };
                })
                .ToList();

            var floorSummary = new AssetFloorSummaryResponseDto
            {
                FloorDetails = floorDetailDtos,
                TotalBaseValue = 0m,
                TotalCapitalValue = floorDetailDtos.Sum(x => x.CapitalValue ?? 0m),
                TotalMarketValue = 0m,
                TotalFloors = floorDetailDtos.Count
            };

            var childAssetDtos = childAssets
                .GroupJoin(
                    childFloorDetails,
                    asset => asset.Id,
                    floor => floor.AssetId,
                    (asset, floors) => new { asset, floors })
                .Select(group =>
                {
                    var firstFloor = group.floors.FirstOrDefault();
                    return new AssetChildAssetResponseDto
                    {
                        AssetId = group.asset.Id,
                        SubUnitsDetailsId = firstFloor != null ? firstFloor.Id : 0,
                        AssetNo = group.asset.AssetNo ?? string.Empty,
                        AssetName = group.asset.AssetName ?? string.Empty,
                        Category = group.asset.AssetCategory?.CategoryName,
                        Type = group.asset.AssetType?.TypeName,
                        OccupancyStatus = group.asset.OccupancyStatus,
                        TypeOfUse = group.floors
                            .Select(f => f.TypeOfUseId > 0 && typeOfUseLookup.TryGetValue(f.TypeOfUseId, out var n) ? n : null)
                            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                        SubTypeOfUse = group.floors
                            .Select(f => f.SubTypeOfUseId.HasValue && subTypeOfUseLookup.TryGetValue(f.SubTypeOfUseId.Value, out var sn) ? sn : null)
                            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                        CarpetAreaSqMeter = group.floors.Sum(f => f.CarpetAreaSqMeter ?? 0m),
                        CarpetAreaSqFeet = group.floors.Sum(f => f.CarpetAreaSqFeet ?? 0m),
                        BuiltUpAreaSqMeter = group.floors.Sum(f => f.BuiltUpAreaSqMeter ?? 0m),
                        BuiltUpAreaSqFeet = group.floors.Sum(f => f.BuiltUpAreaSqFeet ?? 0m),
                        FloorName = group.floors.Select(f => f.Floor != null ? f.Floor.Description : null).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)),
                        FloorDetails = group.floors.Select(f => new AssetChildFloorDetailResponseDto
                        {
                            FloorName = f.Floor != null ? f.Floor.Description : null,
                            ConstructionTypeName = f.ConstructionType != null ? f.ConstructionType.Description : null,
                            TypeOfUseName = f.TypeOfUseId > 0 && typeOfUseLookup.TryGetValue(f.TypeOfUseId, out var tu) ? tu : null,
                            SubTypeOfUseName = f.SubTypeOfUseId.HasValue && subTypeOfUseLookup.TryGetValue(f.SubTypeOfUseId.Value, out var stu) ? stu : null,
                            ConstructionYear = f.ConstructionYear,
                            CarpetAreaSqMeter = f.CarpetAreaSqMeter,
                            CarpetAreaSqFeet = f.CarpetAreaSqFeet,
                            BuiltUpAreaSqMeter = f.BuiltUpAreaSqMeter,
                            BuiltUpAreaSqFeet = f.BuiltUpAreaSqFeet,
                            CapitalValue = f.CapitalValue,
                        }).ToList()
                    };
                })
                .ToList();

            var inventoryData = await BuildInventoryDataAsync(parentAssetId, cancellationToken);

            return new AssetFloorAndOtherDetailsResponseDto
            {
                FloorSummary = floorSummary,
                ChildAssets = childAssetDtos,
                InventoryData = MapInventoryData(inventoryData)
            };
        }

        private static AssetInventoryDataResponseDto? MapInventoryData(InventoryBatchListResponseDto? inventoryData)
        {
            if (inventoryData == null) return null;

            return new AssetInventoryDataResponseDto
            {
                ParentAssetId = inventoryData.ParentAssetId,
                ParentAssetName = inventoryData.ParentAssetName,
                TotalBatches = inventoryData.TotalBatches,
                TotalUnits = inventoryData.TotalUnits,
                TotalPurchaseValue = inventoryData.TotalPurchaseValue,
                TotalCapitalValue = inventoryData.TotalCapitalValue,
                Batches = inventoryData.Batches.Select(batch => new AssetInventoryBatchResponseDto
                {
                    BatchId = batch.BatchId,
                    InventoryType = batch.Names.InventoryType,
                    ItemName = batch.Names.ItemName,
                    ModelBrand = batch.Names.ModelBrand,
                    Specifications = batch.Specifications,
                    PurchaseDate = batch.PurchaseDate,
                    OwningDepartment = batch.Names.OwningDepartment,
                    Condition = batch.Names.Condition,
                    Quantity = batch.Quantity,
                    UnitValue = batch.UnitValue,
                    TotalBatchValue = batch.TotalBatchValue,
                    TotalBatchCV = batch.TotalBatchCV,
                    PhotoFileName = batch.PhotoFileName,
                    InvoiceFileName = batch.InvoiceFileName,
                    Units = batch.Units.Select(unit => new AssetInventoryUnitResponseDto
                    {
                        AssetId = unit.AssetId,
                        UnitNumber = unit.UnitNumber,
                        Condition = unit.Condition,
                        UnitPurchaseValue = unit.UnitPurchaseValue,
                        UnitCapitalValue = unit.UnitCapitalValue
                    }).ToList(),
                    Documents = batch.Documents
                        .Where(doc => string.Equals(doc.Remarks, "Inventory Image", StringComparison.OrdinalIgnoreCase) ||
                                      string.Equals(doc.Remarks, "Inventory Invoice", StringComparison.OrdinalIgnoreCase))
                        .ToList()
                }).ToList()
            };
        }

        #endregion

        #region Sub-Asset Grouping Query Methods

        public async Task<SubAssetGroupedResponseDto> GetSubAssetsGroupedByParentAsync(int parentAssetId, CancellationToken cancellationToken = default)
        {
            if (parentAssetId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(parentAssetId), "Parent asset ID must be greater than zero.");
            }

            // Get parent asset with all fields matching GetByIdAsync
            var parentAsset = await _repository.GetQueryable()
                .AsNoTracking()
                .Where(a => a.Id == parentAssetId && a.IsActive && !a.MarkedForDeletion)
                .Select(a => new ParentAssetDetailDto
                {
                    Id = a.Id,
                    IsActive = a.IsActive,
                    CreatedDate = a.CreatedDate,
                    UpdatedDate = a.UpdatedDate,
                    AssetNo = a.AssetNo ?? string.Empty,
                    AssetName = a.AssetName ?? string.Empty,
                    AssetCategoryId = (int?)a.AssetCategoryId,
                    AssetTypeId = (int?)a.AssetTypeId,
                    ParentAssetId = a.ParentAssetId,
                    HierarchyLevel = (int?)a.HierarchyLevel,
                    HierarchyPath = a.HierarchyPath,
                    OwnershipType = a.OwnershipType,
                    OccupancyStatus = a.OccupancyStatus,
                    Details = new AssetDetailsDto
                    {
                        Id = a.Details != null ? a.Details.Id : 0,
                        PropertyNo = a.Details != null ? a.Details.PropertyNo : null,
                        PartitionNo = a.Details != null ? a.Details.PartitionNo : null,
                        PlotNo = a.Details != null ? a.Details.PlotNo : null,
                        UpicId = a.Details != null ? a.Details.UpicId : null
                    },
                    Names = new AssetMasterNamesDto
                    {
                        AssetCategoryName = a.AssetCategory != null ? a.AssetCategory.CategoryName : null,
                        AssetTypeName = a.AssetType != null ? a.AssetType.TypeName : null,
                        ParentAssetName = a.ParentAsset != null ? a.ParentAsset.AssetName : null
                    },
                    FieldValues = a.FieldValues != null
                        ? a.FieldValues
                            .Where(fv => fv.IsActive == a.IsActive && !fv.MarkedForDeletion)
                            .Select(fv => new AssetFieldValueDto
                            {
                                Id = fv.Id,
                                FieldDefinitionId = fv.FieldDefinitionId,
                                FieldName = fv.FieldName ?? string.Empty,
                                FieldValue = fv.FieldValue
                            })
                            .ToList()
                        : new List<AssetFieldValueDto>()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (parentAsset == null)
            {
                return new SubAssetGroupedResponseDto
                {
                    ParentAsset = null,
                    TotalSubAssets = 0,
                    SubAssets = []
                };
            }

            // Resolve Zone/Ward/Mouja names for the parent asset.
            var parentLocations = await GetLocationInfoByAssetIdsAsync(new[] { parentAsset.Id }, cancellationToken);
            if (parentLocations.TryGetValue(parentAsset.Id, out var parentLocation))
            {
                parentAsset.DepartmentId = parentLocation.DepartmentId;
                ApplyLocation(parentAsset.Details, parentAsset.Names, parentLocation);
            }

            var allocatedAssetIds = await GetAllocatedAssetIdsAsync(parentAssetId, cancellationToken);

            // Get all sub-assets (assets where ParentAssetId = parentAssetId) with all fields from GetByIdAsync
            var subAssets = await _repository.GetQueryable()
                .AsNoTracking()
                .Where(a => a.ParentAssetId == parentAssetId
                && a.IsActive
                && !a.MarkedForDeletion
                && !allocatedAssetIds.Contains(a.Id))
                .Select(a => new SubAssetDetailDto
                {
                    Id = a.Id,
                    IsActive = a.IsActive,
                    CreatedDate = a.CreatedDate,
                    UpdatedDate = a.UpdatedDate,
                    AssetNo = a.AssetNo ?? string.Empty,
                    AssetName = a.AssetName ?? string.Empty,
                    AssetCategoryId = (int?)a.AssetCategoryId,
                    AssetTypeId = (int?)a.AssetTypeId,
                    ParentAssetId = a.ParentAssetId,
                    HierarchyLevel = (int?)a.HierarchyLevel,
                    HierarchyPath = a.HierarchyPath,
                    OwnershipType = a.OwnershipType,
                    OccupancyStatus = a.OccupancyStatus,
                    Details = new AssetDetailsDto
                    {
                        Id = a.Details != null ? a.Details.Id : 0,
                        PropertyNo = a.Details != null ? a.Details.PropertyNo : null,
                        PartitionNo = a.Details != null ? a.Details.PartitionNo : null,
                        PlotNo = a.Details != null ? a.Details.PlotNo : null,
                        UpicId = a.Details != null ? a.Details.UpicId : null
                    },
                    Names = new AssetMasterNamesDto
                    {
                        AssetCategoryName = a.AssetCategory != null ? a.AssetCategory.CategoryName : null,
                        AssetTypeName = a.AssetType != null ? a.AssetType.TypeName : null,
                        ParentAssetName = a.ParentAsset != null ? a.ParentAsset.AssetName : null
                    },
                    FieldValues = a.FieldValues != null
                        ? a.FieldValues
                            .Where(fv => fv.IsActive == a.IsActive && !fv.MarkedForDeletion)
                            .Select(fv => new AssetFieldValueDto
                            {
                                Id = fv.Id,
                                FieldDefinitionId = fv.FieldDefinitionId,
                                FieldName = fv.FieldName ?? string.Empty,
                                FieldValue = fv.FieldValue
                            })
                            .ToList()
                        : new List<AssetFieldValueDto>()
                })
                .ToListAsync(cancellationToken);

            var subAssetIds = subAssets.Select(a => a.Id).ToList();

            var subLocations = await GetLocationInfoByAssetIdsAsync(subAssetIds, cancellationToken);
            foreach (var subAsset in subAssets)
            {
                if (subLocations.TryGetValue(subAsset.Id, out var subLoc))
                {
                    subAsset.DepartmentId = subLoc.DepartmentId;
                    ApplyLocation(subAsset.Details, subAsset.Names, subLoc);
                }
            }

            var floorDetails = await _floorDetailsRepository.GetQueryable()
                .AsNoTracking()
                .Where(f => subAssetIds.Contains(f.AssetId) && f.IsActive && !f.MarkedForDeletion)
                .Select(f => new SubUnitsDetailsDto
                {
                    Id = f.Id,
                    FloorId = f.FloorId,
                    AssetId = f.AssetId,
                    IsActive = f.IsActive,
                    Names = new SubUnitsDetailsNamesDto
                    {
                        FloorName = f.Floor != null ? f.Floor.Description : null,
                        TypeOfUseName = f.TypeOfUse != null ? f.TypeOfUse.Description : null,
                        SubTypeOfUseName = f.SubTypeOfUse != null ? f.SubTypeOfUse.Description : null
                    }
                })
                .ToListAsync(cancellationToken);

            var roomWiseSubmissions = await _roomWiseSubmissionRepository.GetQueryable()
                .AsNoTracking()
                .Where(r => r.AssetId.HasValue && subAssetIds.Contains(r.AssetId.Value) && r.IsActive && !r.MarkedForDeletion)
                .Select(r => new AssetRoomWiseSubmissionDetailsDto
                {
                    Id = r.Id,
                    IsActive = r.IsActive,
                    CreatedDate = r.CreatedDate,
                    UpdatedDate = r.UpdatedDate,
                    ParentAssetId = r.Asset != null ? r.Asset.ParentAssetId : null,
                    AssetId = r.AssetId,
                    FloorDetailsId = r.SubUnitsDetailsId,
                    LengthMtr = r.LengthMtr,
                    WidthMtr = r.WidthMtr,
                    HeightMtr = r.HeightMtr,
                    AreaSqMtr = r.LengthMtr * r.WidthMtr,
                    TotalAreaSqMtr = r.LengthMtr * r.WidthMtr,
                    Shape = r.Shape,
                    RoomNo = r.RoomNo,
                    RoomType = r.RoomType,
                    OuterYesNo = r.OuterYesNo,
                    MinusYesNo = r.MinusYesNo,
                    MarkedForDeletion = r.MarkedForDeletion,
                    MarkedForDeletionDate = r.MarkedForDeletionDate,
                    ParentAssetName = r.Asset != null && r.Asset.ParentAsset != null ? r.Asset.ParentAsset.AssetName : null,
                    AssetName = r.Asset != null ? r.Asset.AssetName : null
                })
                .ToListAsync(cancellationToken);

            var renterDetails = await GetRenterDetailsAsync(subAssetIds, cancellationToken);

            var floorDetailsGrouped = floorDetails
                .GroupBy(f => f.AssetId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var roomWiseGrouped = roomWiseSubmissions
                .Where(r => r.AssetId.HasValue)
                .GroupBy(r => r.AssetId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var renterGrouped = renterDetails
                .GroupBy(r => r.AssetId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var subAsset in subAssets)
            {
                subAsset.RoomWiseSubmissions = roomWiseGrouped.TryGetValue(subAsset.Id, out var rooms) ? rooms : [];
                subAsset.RenterDetails = renterGrouped.TryGetValue(subAsset.Id, out var renters) ? renters : [];

                if (floorDetailsGrouped.TryGetValue(subAsset.Id, out var floors))
                {
                    // Hash the (small) per-asset room-submission floor ids once instead of running a
                    // linear Any() scan over them for every floor in the asset's floor list.
                    var roomFloorDetailsIds = subAsset.RoomWiseSubmissions
                        .Where(r => r.FloorDetailsId.HasValue)
                        .Select(r => r.FloorDetailsId!.Value)
                        .ToHashSet();
                    subAsset.FloorDetails = floors.Where(f => roomFloorDetailsIds.Contains(f.Id)).ToList();
                }
                else
                {
                    subAsset.FloorDetails = [];
                }

                var resolvedFloorDetail = subAsset.FloorDetails.FirstOrDefault();
                if (resolvedFloorDetail != null)
                {
                    subAsset.TypeOfUseName = resolvedFloorDetail.Names?.TypeOfUseName;
                    subAsset.SubTypeOfUseName = resolvedFloorDetail.Names?.SubTypeOfUseName;
                }
                else
                {
                    subAsset.TypeOfUseName = null;
                    subAsset.SubTypeOfUseName = null;
                }

                subAsset.FieldValues ??= [];
            }

            _logger.LogInformation(
                "Retrieved {SubAssetCount} sub-assets for parent asset {ParentAssetId} with {FloorCount} floor details, {RoomCount} room-wise submissions, and {RenterCount} renter details.",
                subAssets.Count,
                parentAssetId,
                floorDetails.Count,
                roomWiseSubmissions.Count,
                renterDetails.Count);

            return new SubAssetGroupedResponseDto
            {
                ParentAsset = parentAsset,
                TotalSubAssets = subAssets.Count,
                SubAssets = subAssets
            };
        }

        #endregion
    }
}
