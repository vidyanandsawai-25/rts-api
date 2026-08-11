using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.DTOs.Asset_Management.AssetDetails;
using NtisPlatform.Application.DTOs.Asset_Management.AssetFieldValue;
using NtisPlatform.Application.DTOs.Asset_Management.AssetLeaseRentDetails;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.DTOs.Asset_Management.AssetRoomWiseSubmissionDetails;
using NtisPlatform.Application.DTOs.Asset_Management.ManageSubUnits;
using NtisPlatform.Application.DTOs.Asset_Management.SubUnitsDetails;
using NtisPlatform.Application.DTOs.Document;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Asset_Management;
using System.Text.Json;

namespace NtisPlatform.Application.Services.Asset_Management;

/// <summary>
/// Service for Manage Sub Units.
/// 
/// APIs supported:
/// 1. GET /api/ManageSubUnits/by-asset/{assetId}
///    - Get all non-inventory sub-units by parent asset ID.
///
/// 2. GET /api/ManageSubUnits/{assetId}
///    - Get complete details of one non-inventory sub-unit for eye button.
/// </summary>
public class ManageSubUnitsService : IManageSubUnitsService
{
    private readonly IRepository<AssetMasterEntity, int> _assetRepository;
    private readonly IRepository<AssetLeaseRentDetailsEntity, int> _leaseRentDetailsRepository;
    private readonly IRepository<AssetRoomWiseSubmissionDetailsEntity, int> _roomWiseRepository;
    private readonly IRepository<SubUnitsDetailsEntity, int> _floorDetailsRepository;
    private readonly IRepository<AssetApplicationTypeEntity, int> _applicationTypeRepository;
    private readonly IRepository<AssetRoomWiseMinusDataEntity, int> _minusRepository;
    private readonly IRepository<AssetDetailsEntity, int> _locationDetailsRepository;
    private readonly IRepository<SubUnitsDetailsEntity, int> _subUnitsDetailsRepository;
    private readonly IRepository<InventoryAssetDetailEntity, int> _inventoryAssetDetailRepository;
    // AMS TypeOfUse / SubTypeOfUse — FK values in AMS.SubUnitsDetails point here, not PTIS tables.
    private readonly IRepository<AssetTypeOfUseMasterEntity, int> _amsTypeOfUseRepository;
    private readonly IRepository<AssetSubTypeOfUseEntity, int> _amsSubTypeOfUseRepository;
    private readonly IAssetMasterService _assetMasterService;
    private readonly IAssetPhotoService _assetPhotoService;
    private readonly IDocumentApplicationService _documentApplicationService;
    private readonly IRepository<DepartmentMasterEntity, int> _deptMasterRepository;
    private readonly IRepository<ModuleMasterEntity, int> _moduleMasterRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ManageSubUnitsService> _logger;

    public ManageSubUnitsService(
        IRepository<AssetMasterEntity, int> assetRepository,
        IRepository<AssetLeaseRentDetailsEntity, int> leaseRentDetailsRepository,
        IRepository<AssetRoomWiseSubmissionDetailsEntity, int> roomWiseRepository,
        IRepository<SubUnitsDetailsEntity, int> floorDetailsRepository,
        IRepository<AssetApplicationTypeEntity, int> applicationTypeRepository,
        IRepository<AssetRoomWiseMinusDataEntity, int> minusRepository,
        IRepository<AssetDetailsEntity, int> locationDetailsRepository,
        IRepository<SubUnitsDetailsEntity, int> subUnitsDetailsRepository,
        IRepository<InventoryAssetDetailEntity, int> inventoryAssetDetailRepository,
        IRepository<AssetTypeOfUseMasterEntity, int> amsTypeOfUseRepository,
        IRepository<AssetSubTypeOfUseEntity, int> amsSubTypeOfUseRepository,
        IAssetMasterService assetMasterService,
        IAssetPhotoService assetPhotoService,
        IDocumentApplicationService documentApplicationService,
        IRepository<DepartmentMasterEntity, int> deptMasterRepository,
        IRepository<ModuleMasterEntity, int> moduleMasterRepository,
        IUnitOfWork unitOfWork,
        ILogger<ManageSubUnitsService> logger)
    {
        _assetRepository = assetRepository;
        _leaseRentDetailsRepository = leaseRentDetailsRepository;
        _roomWiseRepository = roomWiseRepository;
        _floorDetailsRepository = floorDetailsRepository;
        _applicationTypeRepository = applicationTypeRepository;
        _minusRepository = minusRepository;
        _locationDetailsRepository = locationDetailsRepository;
        _subUnitsDetailsRepository = subUnitsDetailsRepository;
        _inventoryAssetDetailRepository = inventoryAssetDetailRepository;
        _amsTypeOfUseRepository = amsTypeOfUseRepository;
        _amsSubTypeOfUseRepository = amsSubTypeOfUseRepository;
        _assetMasterService = assetMasterService;
        _assetPhotoService = assetPhotoService;
        _documentApplicationService = documentApplicationService;
        _deptMasterRepository = deptMasterRepository;
        _moduleMasterRepository = moduleMasterRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get all sub-units by parent asset ID.
    /// 
    /// Important:
    /// Inventory/furniture records are excluded using InventoryBatchId == null.
    /// </summary>
    public async Task<List<SubUnitListDto>> GetAllSubUnitsByParentIdAsync(
        int parentAssetId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Retrieving all non-inventory sub-units for ParentAssetId: {ParentAssetId}",
            parentAssetId);

        var floorDetailsQuery = _floorDetailsRepository.GetQueryable().AsNoTracking();

        // Project to anonymous type first so we can capture the raw ConstructionYear string.
        // EF Core cannot parse strings to integers in SQL, so Life is computed in-memory below.
        var rawResult = await _assetRepository.GetQueryable()
            .AsNoTracking()
            .Where(a =>
                a.ParentAssetId == parentAssetId &&
                a.IsActive &&
                !a.MarkedForDeletion &&
                !_inventoryAssetDetailRepository.GetQueryable().Any(iad => iad.AssetId == a.Id))
            .OrderBy(a => a.AssetNo)
            .Select(a => new
            {
                a.Id,
                a.AssetNo,
                a.AssetName,
                a.IsActive,
                Occupancy = a.OccupancyStatus ?? string.Empty,
                CategoryName = a.AssetCategory != null ? a.AssetCategory.CategoryName : string.Empty,
                TypeName = a.AssetType != null ? a.AssetType.TypeName : string.Empty,
                BuiltUpAreaSqMeter = floorDetailsQuery
                    .Where(fd => fd.AssetId == a.Id && fd.IsActive && !fd.MarkedForDeletion)
                    .Sum(fd => (decimal?)fd.BuiltUpAreaSqMeter),
                CarpetAreaSqMeter = floorDetailsQuery
                    .Where(fd => fd.AssetId == a.Id && fd.IsActive && !fd.MarkedForDeletion)
                    .Sum(fd => (decimal?)fd.CarpetAreaSqMeter),
                CapitalValue = floorDetailsQuery
                    .Where(fd => fd.AssetId == a.Id && fd.IsActive && !fd.MarkedForDeletion)
                    .Sum(fd => (decimal?)fd.CapitalValue),
                // Earliest construction year string across all active floor details for this sub-unit
                ConstructionYearStr = floorDetailsQuery
                    .Where(fd => fd.AssetId == a.Id && fd.IsActive && !fd.MarkedForDeletion && fd.ConstructionYear != null)
                    .OrderBy(fd => fd.ConstructionYear)
                    .Select(fd => fd.ConstructionYear)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        int currentYear = DateTime.Today.Year;

        var result = rawResult.Select(r => new SubUnitListDto
        {
            Id = r.Id,
            AssetNo = r.AssetNo,
            AssetName = r.AssetName,
            Status = r.IsActive ? "Active" : "Inactive",
            Occupancy = r.Occupancy,
            BuiltUpAreaSqMeter = r.BuiltUpAreaSqMeter,
            CarpetAreaSqMeter = r.CarpetAreaSqMeter,
            CapitalValue = r.CapitalValue,
            LastCVDate = null,
            AssetLife = int.TryParse(r.ConstructionYearStr, out var cy) ? currentYear - cy : null,
            Names = new SubUnitNamesDto
            {
                Category = r.CategoryName,
                Type = r.TypeName,
                UseType = null,
                SubUseType = null,
                Zone = null,
                Ward = null,
                Mouja = null
            }
        }).ToList();

        _logger.LogInformation(
            "Retrieved {Count} non-inventory sub-units for ParentAssetId: {ParentAssetId}",
            result.Count,
            parentAssetId);

        return result;
    }

    /// <summary>
    /// Get complete details of a single sub-unit (asset + renter + room-wise + floor) in one
    /// payload. Used for the eye button.
    ///
    /// This mirrors the per-sub-asset shape produced by
    /// <c>AssetMasterService.GetSubAssetsGroupedByParentAsync</c>:
    /// the asset itself is projected with full fields and display names; floor details are
    /// fetched against the PARENT asset and then filtered to the floors referenced by this
    /// sub-unit's room-wise submissions; <c>TypeOfUseName</c>/<c>SubTypeOfUseName</c> are
    /// resolved from the matched floor.
    ///
    /// Important:
    /// Inventory/furniture records are excluded using InventoryBatchId == null.
    /// </summary>
    public async Task<SubAssetDetailDto> GetSubUnitDetailsByIdAsync(
        int assetId,
        CancellationToken cancellationToken = default)
    {
        if (assetId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(assetId),
                "Asset ID must be greater than zero.");
        }

        _logger.LogInformation(
            "Retrieving sub-unit asset details for AssetId: {AssetId}",
            assetId);

        // 1. Get the sub-asset from AssetMaster with the full detail projection.
        //    Inventory/furniture data is excluded using InventoryBatchId == null.
        var asset = await _assetRepository.GetQueryable()
            .AsNoTracking()
            .Where(a =>
                a.Id == assetId &&
                !a.MarkedForDeletion &&
                !_inventoryAssetDetailRepository.GetQueryable().Any(iad => iad.AssetId == a.Id))
            .Select(a => new SubAssetDetailDto
            {
                // Base entity fields
                Id = a.Id,
                IsActive = a.IsActive,
                CreatedDate = a.CreatedDate,
                UpdatedDate = a.UpdatedDate,

                // Jurisdiction / Ownership Context

                // Identification / Category
                AssetNo = a.AssetNo ?? string.Empty,
                AssetName = a.AssetName ?? string.Empty,
                AssetCategoryId = (int?)a.AssetCategoryId,
                AssetTypeId = (int?)a.AssetTypeId,
                ParentAssetId = a.ParentAssetId,

                // Hierarchy
                HierarchyLevel = (int?)a.HierarchyLevel,
                HierarchyPath = a.HierarchyPath,

                // Location
                Details = new AssetDetailsDto
                {
                    PropertyNo = a.PropertyNo,
                    PartitionNo = a.PartitionNo,
                    UpicId = a.UpicId,
                },

                // Legal / Acquisition
                OwnershipType = a.OwnershipType,

                // Status
                OccupancyStatus = a.OccupancyStatus,

                // Revenue & Operations

                // Floor details reference

                // Navigation property names for display - all null-safe
                Names = new AssetMasterNamesDto
                {
                    OrganizationName = null,
                    DepartmentName = null,
                    AssetCategoryName = a.AssetCategory != null ? a.AssetCategory.CategoryName : null,
                    AssetTypeName = a.AssetType != null ? a.AssetType.TypeName : null,
                    ParentAssetName = a.ParentAsset != null ? a.ParentAsset.AssetName : null,
                    ZoneName = null,
                    WardName = null,
                    MoujaName = null,
                },
                TypeOfUseName = null,
                SubTypeOfUseName = null,

                // Field Values for dynamic fields - null-safe with empty list fallback
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

        if (asset == null)
        {
            throw new KeyNotFoundException(
                $"Sub-unit with AssetId {assetId} not found, deleted, or it is inventory data.");
        }

        // 2. Room-wise submissions for this sub-asset.
        asset.RoomWiseSubmissions = await GetRoomWiseSubmissionsByAssetIdAsync(
            assetId,
            cancellationToken);

        // 3. Renter details for this sub-asset.
        asset.RenterDetails = await GetRenterDetailsByAssetIdAsync(
            assetId,
            cancellationToken);

        // 4. Floor details: fetched against the PARENT asset, then filtered to the floors
        //    referenced by this sub-unit's room-wise submissions (same logic as the grouped API).
        asset.FloorDetails = await GetFloorDetailsForSubAssetAsync(
            assetId,
            asset.ParentAssetId,
            asset.RoomWiseSubmissions,
            cancellationToken);

        // 5. Resolve the use-type display names from the matched floor (grouped-API behavior).
        var resolvedFloorDetail = asset.FloorDetails.FirstOrDefault();
        if (resolvedFloorDetail != null)
        {
            asset.TypeOfUseName = resolvedFloorDetail.Names?.TypeOfUseName;
            asset.SubTypeOfUseName = resolvedFloorDetail.Names?.SubTypeOfUseName;
        }

        _logger.LogInformation(
            "Retrieved sub-unit details for AssetId: {AssetId}. RenterCount: {RenterCount}, RoomWiseCount: {RoomWiseCount}, FloorCount: {FloorCount}",
            assetId,
            asset.RenterDetails.Count,
            asset.RoomWiseSubmissions.Count,
            asset.FloorDetails.Count);

        return asset;
    }

    /// <inheritdoc />
    public async Task<SubUnitLeaseRentDetailDto> GetSubUnitLeaseRentBySubUnitDetailsIdAsync(
        int assetId,
        CancellationToken cancellationToken = default)
    {
        if (assetId <= 0)
            throw new ArgumentOutOfRangeException(nameof(assetId), "AssetId must be greater than zero.");

        var asset = await _assetRepository.GetQueryable()
            .AsNoTracking()
            .Where(a => a.Id == assetId && !a.MarkedForDeletion)
            .Select(a => new
            {
                a.Id,
                a.AssetNo,
                a.AssetName,
                a.ParentAssetId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (asset == null)
        {
            var floorAssetId = await _floorDetailsRepository.GetQueryable()
                .AsNoTracking()
                .Where(f => f.Id == assetId && !f.MarkedForDeletion)
                .Select(f => (int?)f.AssetId)
                .FirstOrDefaultAsync(cancellationToken);

            if (floorAssetId.HasValue)
            {
                asset = await _assetRepository.GetQueryable()
                    .AsNoTracking()
                    .Where(a => a.Id == floorAssetId.Value && !a.MarkedForDeletion)
                    .Select(a => new
                    {
                        a.Id,
                        a.AssetNo,
                        a.AssetName,
                        a.ParentAssetId
                    })
                    .FirstOrDefaultAsync(cancellationToken);
            }
        }

        if (asset == null)
            throw new KeyNotFoundException($"Asset with Id {assetId} not found.");

        var floorDetailsEntity = await _floorDetailsRepository.GetQueryable()
            .AsNoTracking()
            .Where(f => f.AssetId == asset.Id && !f.MarkedForDeletion)
            .OrderByDescending(f => f.Id)
            .Include(f => f.Floor)
            .Include(f => f.SubFloor)
            .Include(f => f.ConstructionType)
            .FirstOrDefaultAsync(cancellationToken);

        if (floorDetailsEntity == null)
            throw new KeyNotFoundException($"SubUnitDetails for AssetId {asset.Id} not found.");

        // Resolve TypeOfUse / SubTypeOfUse names from AMS tables (FK in SubUnitsDetails points here).
        string? typeOfUseName = null;
        string? subTypeOfUseName = null;
        if (floorDetailsEntity.TypeOfUseId > 0)
        {
            typeOfUseName = await _amsTypeOfUseRepository.GetQueryable()
                .AsNoTracking()
                .Where(t => t.Id == floorDetailsEntity.TypeOfUseId && t.IsActive)
                .Select(t => t.Description)
                .FirstOrDefaultAsync(cancellationToken);
        }
        if (floorDetailsEntity.SubTypeOfUseId.HasValue)
        {
            subTypeOfUseName = await _amsSubTypeOfUseRepository.GetQueryable()
                .AsNoTracking()
                .Where(t => t.Id == floorDetailsEntity.SubTypeOfUseId.Value && t.IsActive)
                .Select(t => t.Description)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var floorDetails = new SubUnitFloorDetailDto
        {
            Id = floorDetailsEntity.Id,
            AssetId = floorDetailsEntity.AssetId,
            FloorId = floorDetailsEntity.FloorId,
            SubFloorId = floorDetailsEntity.SubFloorId,
            ConstructionYear = floorDetailsEntity.ConstructionYear,
            AssessmentYear = floorDetailsEntity.AssessmentYear,
            ConstructionTypeId = floorDetailsEntity.ConstructionTypeId,
            TypeOfUseId = floorDetailsEntity.TypeOfUseId,
            SubTypeOfUseId = floorDetailsEntity.SubTypeOfUseId,
            CarpetAreaSqMeter = floorDetailsEntity.CarpetAreaSqMeter,
            CarpetAreaSqFeet = floorDetailsEntity.CarpetAreaSqFeet,
            BuiltupAreaSqMeter = floorDetailsEntity.BuiltUpAreaSqMeter,
            BuiltupAreaSqFeet = floorDetailsEntity.BuiltUpAreaSqFeet,
            NoOfRooms = floorDetailsEntity.NoOfRooms,
            BaseValue = floorDetailsEntity.BaseValue,
            CapitalValue = floorDetailsEntity.CapitalValue,
            CVAgeFactor = floorDetailsEntity.CVAgeFactor,
            CVFloorFactor = floorDetailsEntity.CVFloorFactor,
            CVNatureFactor = floorDetailsEntity.CVNatureFactor,
            CVUseFactor = floorDetailsEntity.CVUseFactor,
            CVBaseRate = floorDetailsEntity.CVBaseRate,
            IsRented = floorDetailsEntity.IsRented,
            IsActive = floorDetailsEntity.IsActive,
            FloorName = floorDetailsEntity.Floor?.Description,
            SubFloorName = floorDetailsEntity.SubFloor?.Description,
            ConstructionTypeName = floorDetailsEntity.ConstructionType?.Description,
            TypeOfUseName = typeOfUseName,
            SubTypeOfUseName = subTypeOfUseName
        };

        var leaseRentDetails = await _leaseRentDetailsRepository.GetQueryable()
            .AsNoTracking()
            .Where(l => l.AssetId == asset.Id && !l.MarkedForDeletion)
            .OrderByDescending(l => l.Id)
            .Select(l => new AssetLeaseRentDetailsDto
            {
                Id = l.Id,
                IsActive = l.IsActive,
                CreatedDate = l.CreatedDate,
                UpdatedDate = l.UpdatedDate,
                ParentAssetId = l.Asset != null ? l.Asset.ParentAssetId : null,
                AssetId = l.AssetId,
                FloorDetailsId = l.FloorDetailsId,
                ShopNo = l.ShopNo,
                ShopName = l.ShopName,
                TenantName = l.TenantName,
                TenantMobile = l.TenantMobile,
                TenantEmail = l.TenantEmail,
                TenantType = l.TenantType,
                TenantAadhaarNo = l.TenantAadhaarNo,
                TenantPanCardNo = l.TenantPanCardNo,
                TenantAddress = l.TenantAddress,
                GSTNo = l.GSTNo,
                TotalAreaSqFt = l.TotalAreaSqFt,
                ApplicationTypeId = l.ApplicationTypeId,
                LeaseType = l.LeaseType,
                LeaseStartDate = l.LeaseStartDate,
                LeaseEndDate = l.LeaseEndDate,
                Duration = l.Duration,
                MonthlyRent = l.RentAmount ?? 0m,
                RentAmount = l.RentAmount,
                SecurityDeposit = l.SecurityDeposit,
                DepositType = l.DepositType,
                PaymentFrequency = l.PaymentFrequency,
                AgreementId = l.AgreementId,
                IncrementFrequency = l.IncrementFrequency,
                IncrementType = l.IncrementType,
                IncrementValue = l.IncrementValue,
                IncrementMethod = l.IncrementMethod,
                Reason = l.Reason,
                WorkflowStatus = l.WorkflowStatus,
                RejectionReason = l.RejectionReason,
                IsRejection = l.IsRejection,
                RejectionBy = l.RejectionBy,
                RejectionDate = l.RejectionDate,
                IsVerified = l.IsVerified,
                VerifiedBy = l.VerifiedBy,
                VerifiedDate = l.VerifiedDate,
                IsApproved = l.IsApproved,
                ApprovedBy = l.ApprovedBy,
                ApprovedDate = l.ApprovedDate,
                Names = new AssetLeaseRentDetailsNamesDto
                {
                    AssetNo = l.Asset != null ? l.Asset.AssetNo : null,
                    AssetName = l.Asset != null ? l.Asset.AssetName : null,
                    AssetCategoryName = l.Asset != null && l.Asset.AssetCategory != null ? l.Asset.AssetCategory.CategoryName : null,
                    ApplicationTypeName = l.ApplicationType != null ? l.ApplicationType.ApplicationTypeName : null,
                    FloorDescription = l.ShopName
                }
            })
            .FirstOrDefaultAsync(cancellationToken);

        List<SubUnitPhotoDto> photos = new();
        try
        {
            var rawPhotos = await _assetPhotoService.GetLatestByAssetIdAsync(
                asset.Id,
                cancellationToken);

            photos = rawPhotos
                .Where(p => p.SubUnitsDetailsId == floorDetails.Id
                            && (p.Remarks == "Asset Image" || p.Remarks == "Asset Photo Plan"))
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.Id)
                .Select(p => new SubUnitPhotoDto
                {
                    PhotoId = p.Id,
                    PhotoTypeCode = p.PhotoType?.PhotoTypeCode ?? string.Empty,
                    PhotoTypeName = p.PhotoType?.PhotoTypeName ?? string.Empty,
                    Remarks = p.Remarks,
                    DocumentGuid = p.DocumentBinding?.Document is { IsActive: true, MarkedForDeletion: false }
                                         ? p.DocumentBinding.Document.DocumentGuid
                                         : (Guid?)null,
                    FileName = p.DocumentBinding?.Document is { IsActive: true, MarkedForDeletion: false }
                                         ? p.DocumentBinding.Document.OriginalFileName
                                         : null,
                    MimeType = p.DocumentBinding?.Document is { IsActive: true, MarkedForDeletion: false }
                                         ? p.DocumentBinding.Document.MimeType
                                         : null,
                    DisplayOrder = p.DisplayOrder
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Optional photo lookup failed for AssetId: {AssetId}, SubUnitDetailsId: {SubUnitDetailsId}",
                floorDetails.AssetId,
                floorDetails.Id);
        }

        return new SubUnitLeaseRentDetailDto
        {
            SubUnitDetailsId = floorDetails.Id,
            AssetId          = asset.Id,
            AssetNo          = asset.AssetNo ?? leaseRentDetails?.Names?.AssetNo ?? string.Empty,
            AssetName        = asset.AssetName ?? leaseRentDetails?.Names?.AssetName ?? string.Empty,
            ParentAssetId    = asset.ParentAssetId ?? leaseRentDetails?.ParentAssetId,
            FloorDetails     = floorDetails,
            LeaseRentDetails = leaseRentDetails,
            Photos           = photos
        };
    }

    private async Task<List<AssetRoomWiseSubmissionDetailsDto>> GetRoomWiseSubmissionsByAssetIdAsync(
        int assetId,
        CancellationToken cancellationToken)
    {
        return await _roomWiseRepository.GetQueryable()
            .AsNoTracking()
            .Where(r =>
                r.AssetId == assetId &&
                !r.MarkedForDeletion)
            .OrderBy(r => r.Id)
            .Select(r => new AssetRoomWiseSubmissionDetailsDto
            {
                // Base entity fields
                Id = r.Id,
                IsActive = r.IsActive,
                CreatedDate = r.CreatedDate,
                UpdatedDate = r.UpdatedDate,

                // Foreign keys
                ParentAssetId = r.Asset != null ? r.Asset.ParentAssetId : null,
                AssetId = r.AssetId,
                FloorDetailsId = r.SubUnitsDetailsId,

                // Dimension fields
                LengthMtr = r.LengthMtr,
                WidthMtr = r.WidthMtr,
                HeightMtr = r.HeightMtr,

                // Room details
                Shape = r.Shape,
                RoomNo = r.RoomNo,
                RoomType = r.RoomType,

                // Boolean flags
                OuterYesNo = r.OuterYesNo,
                MinusYesNo = r.MinusYesNo,

                // Soft delete fields
                MarkedForDeletion = r.MarkedForDeletion,
                MarkedForDeletionDate = r.MarkedForDeletionDate,

                // Navigation property names for display - all null-safe
                ParentAssetName = r.Asset != null && r.Asset.ParentAsset != null ? r.Asset.ParentAsset.AssetName : null,
                AssetName = r.Asset != null ? r.Asset.AssetName : null
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<AssetLeaseRentDetailsDto>> GetRenterDetailsByAssetIdAsync(
        int assetId,
        CancellationToken cancellationToken)
    {
        return await _leaseRentDetailsRepository.GetQueryable()
            .AsNoTracking()
            .Where(r =>
                r.AssetId == assetId &&
                !r.MarkedForDeletion)
            .OrderByDescending(r => r.Id)
            .Select(r => new AssetLeaseRentDetailsDto
            {
                // Base entity fields
                Id = r.Id,
                IsActive = r.IsActive,
                CreatedDate = r.CreatedDate,
                UpdatedDate = r.UpdatedDate,

                // foreign key references
                FloorDetailsId = r.FloorDetailsId,
                ParentAssetId = r.Asset != null ? r.Asset.ParentAssetId : null,
                AssetId = r.AssetId,

                // Basic Information Fields
                TenantName = r.TenantName,
                GSTNo = r.GSTNo,
                TotalAreaSqFt = r.TotalAreaSqFt,
                TenantAadhaarNo = r.TenantAadhaarNo,
                TenantPanCardNo = r.TenantPanCardNo,
                TenantMobile = r.TenantMobile,
                TenantEmail = r.TenantEmail,

                // Rent Information Fields
                LeaseStartDate = r.LeaseStartDate,
                LeaseEndDate = r.LeaseEndDate,
                Duration = r.Duration,
                PaymentFrequency = r.PaymentFrequency,
                RentAmount = r.RentAmount,
                SecurityDeposit = r.SecurityDeposit,
                DepositType = r.DepositType,

                // Legacy/Existing Fields
                AgreementId = r.AgreementId,
                IncrementFrequency = r.IncrementFrequency,
                IncrementType = r.IncrementType,
                IncrementValue = r.IncrementValue,
                IncrementMethod = r.IncrementMethod,

                // Navigation property names for display - all null-safe
                Names = new AssetLeaseRentDetailsNamesDto
                {
                    AssetNo = r.Asset != null ? r.Asset.AssetNo : null,
                    AssetName = r.Asset != null ? r.Asset.AssetName : null
                }
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// SubUnitsDetails rows referenced by this sub-unit's room-wise submissions (via
    /// FloorDetailsId) can be owned either by the PARENT asset (shared floor, created via
    /// <c>BulkGenerateChildAssetsAsync</c>) or by the sub-unit itself (dedicated floor row,
    /// created via <c>CreateChildAssetAsync</c>). <c>referencedFloorIds</c> already comes from
    /// this sub-unit's own room-wise submissions, so it uniquely identifies the rows we want —
    /// we just need to accept either ownership shape.
    /// </summary>
    private async Task<List<SubUnitsDetailsDto>> GetFloorDetailsForSubAssetAsync(
        int assetId,
        int? parentAssetId,
        List<AssetRoomWiseSubmissionDetailsDto> roomWiseSubmissions,
        CancellationToken cancellationToken)
    {
        var referencedFloorIds = roomWiseSubmissions
            .Where(r => r.FloorDetailsId.HasValue)
            .Select(r => r.FloorDetailsId!.Value)
            .Distinct()
            .ToList();

        if (referencedFloorIds.Count == 0)
        {
            return new List<SubUnitsDetailsDto>();
        }

        return await _floorDetailsRepository.GetQueryable()
            .AsNoTracking()
            .Where(f =>
                (f.AssetId == assetId || (parentAssetId.HasValue && f.AssetId == parentAssetId.Value)) &&
                referencedFloorIds.Contains(f.Id) &&
                !f.MarkedForDeletion)
            .OrderBy(f => f.FloorId)
            .ThenBy(f => f.SubFloorId)
            .Select(f => new SubUnitsDetailsDto
            {
                // Base entity fields
                Id = f.Id,
                IsActive = f.IsActive,
                CreatedDate = f.CreatedDate,
                UpdatedDate = f.UpdatedDate,

                // Foreign keys
                AssetId = f.AssetId,
                FloorId = f.FloorId,
                SubFloorId = f.SubFloorId,
                ConstructionTypeId = f.ConstructionTypeId,
                TypeOfUseId = f.TypeOfUseId,
                SubTypeOfUseId = f.SubTypeOfUseId,

                // Data fields
                ConstructionYear = f.ConstructionYear,
                AssessmentYear = f.AssessmentYear,
                CarpetAreaSqMeter = f.CarpetAreaSqMeter,
                CarpetAreaSqFeet = f.CarpetAreaSqFeet,
                BuiltUpAreaSqMeter = f.BuiltUpAreaSqMeter,
                BuiltUpAreaSqFeet = f.BuiltUpAreaSqFeet,
                NoOfRooms = f.NoOfRooms,

                // Valuation fields
                CapitalValue = f.CapitalValue,                
                BaseValue = f.BaseValue,
                CVBaseRate = f.CVBaseRate,

                // Soft delete fields
                MarkedForDeletion = f.MarkedForDeletion,
                MarkedForDeletionDate = f.MarkedForDeletionDate,

                // Navigation property names for display - all null-safe
                Names = new SubUnitsDetailsNamesDto
                {
                    AssetName = f.Asset != null ? f.Asset.AssetName : null,
                    FloorName = f.Floor != null ? f.Floor.Description : null,
                    SubFloorName = f.SubFloor != null ? f.SubFloor.Description : null,
                    ConstructionTypeName = f.ConstructionType != null ? f.ConstructionType.Description : null,
                    TypeOfUseName = f.TypeOfUse != null ? f.TypeOfUse.Description : null,
                    SubTypeOfUseName = f.SubTypeOfUse != null ? f.SubTypeOfUse.Description : null
                }
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Bulk generates child assets (rooms/flats/shops) under a parent asset.
    /// Gets basic info from parent asset and links to specific floor details.
    /// Creates entries in both AssetMaster and RoomWiseSubmissionDetails tables.
    /// </summary>
    public async Task<BulkGenerateChildAssetsResponseDto> BulkGenerateChildAssetsAsync(
        BulkGenerateChildAssetsDto dto,
        CancellationToken cancellationToken = default)
    {
        var response = new BulkGenerateChildAssetsResponseDto();

        // Step 1: Validate parent asset exists
        var parentAsset = await _assetRepository.GetByIdAsync(dto.ParentAssetId, cancellationToken);
        if (parentAsset == null)
        {
            response.Errors.Add($"Parent asset with Id {dto.ParentAssetId} not found");
            return response;
        }

        // Step 2: Floor details are optional at generation time — assigned later when user configures the unit.
        SubUnitsDetailsEntity? floorDetails = null;
        if (dto.FloorDetailsId.HasValue && dto.FloorDetailsId.Value > 0)
        {
            floorDetails = await _floorDetailsRepository.GetByIdAsync(dto.FloorDetailsId.Value, cancellationToken);
            if (floorDetails == null)
            {
                response.Errors.Add($"Floor details with Id {dto.FloorDetailsId} not found");
                return response;
            }
        }

        _logger.LogInformation("Starting bulk generation: {Count} {Type} units for parent asset {ParentAssetId}",
            dto.Count, dto.Type, dto.ParentAssetId);

        // Use the unit Type (Flat/Shop/Office) as the sub-unit prefix —
        // produces the same format as the main asset: Akola01-BLDG-MUNI-FLAT-0001
        var generatedAssetNos = await _assetMasterService.GenerateAssetNosAsync(
            parentAsset.AssetCategoryId,
            parentAsset.AssetTypeId,
            dto.Count,
            dto.Type,          // "Flat" → segment "FLAT"
            cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var generatedAssets = new List<GeneratedAssetDto>();

            for (int i = 0; i < dto.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var assetNo   = generatedAssetNos[i];
                var assetName = $"{dto.Type} Unit";   // generic name — updated when user fills details

                // Create child asset — no floor assignment yet; floor set via createChildAssetAction later
                var childAsset = new AssetMasterEntity
                {
                    // Inherit from parent
                    AssetCategoryId = parentAsset.AssetCategoryId,
                    AssetTypeId     = parentAsset.AssetTypeId,
                    AssetLocationDetailsId = parentAsset.AssetLocationDetailsId,

                    // Basic info
                    AssetNo         = assetNo,
                    AssetName       = assetName,
                    ParentAssetId   = dto.ParentAssetId,

                    // Hierarchy
                    HierarchyLevel = parentAsset.HierarchyLevel + 1,
                    HierarchyPath  = parentAsset.HierarchyPath != null
                        ? $"{parentAsset.HierarchyPath}/{dto.ParentAssetId}"
                        : $"/{dto.ParentAssetId}",

                    // Location — inherit from parent
                    PropertyNo = parentAsset.PropertyNo,
                    PartitionNo = parentAsset.PartitionNo,
                    UpicId = parentAsset.UpicId,

                    // Type of Use — inherit from floor if available, else null

                    // Area starts at 0 — set when rooms are configured

                    // Capital Value from floor if available

                    // Status
                    OccupancyStatus    = "Vacant",

                    // Audit
                    IsActive    = false,               // activated when details are fully configured
                    CreatedBy   = dto.CreatedBy,
                    CreatedDate = DateTime.UtcNow
                };

                await _assetRepository.AddAsync(childAsset, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created draft unit {AssetNo} ID={AssetId}", childAsset.AssetNo, childAsset.Id);

                // Room-wise submission details are only created when a floor is known.
                // Without a floor, we skip this record. The unit type ("Flat"/"Shop") is
                // recoverable from AssetName ("Flat Unit") when the pool reloads.
                int? roomWiseId = null;
                if (dto.FloorDetailsId.HasValue && dto.FloorDetailsId.Value > 0)
                {
                    var roomWiseDetail = new AssetRoomWiseSubmissionDetailsEntity
                    {
                        AssetId        = childAsset.Id,
                        SubUnitsDetailsId = dto.FloorDetailsId.Value,
                        AreaSqMtr      = null,
                        TotalAreaSqMtr = null,
                        Shape          = "Rectangle",
                        RoomType       = dto.Type,
                        OuterYesNo     = false,
                        MinusYesNo     = false,
                        IsActive       = true,
                        CreatedBy      = dto.CreatedBy,
                        CreatedDate    = DateTime.UtcNow
                    };
                    await _roomWiseRepository.AddAsync(roomWiseDetail, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    roomWiseId = roomWiseDetail.Id;
                }

                generatedAssets.Add(new GeneratedAssetDto
                {
                    AssetId                     = childAsset.Id,
                    AssetNo                     = childAsset.AssetNo,
                    AssetName                   = childAsset.AssetName,
                    RoomWiseSubmissionDetailsId = roomWiseId
                });
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            response.TotalGenerated = generatedAssets.Count;
            response.GeneratedAssets = generatedAssets;

            _logger.LogInformation("✓ Successfully generated {Count} child assets for parent {ParentAssetId}", 
                generatedAssets.Count, dto.ParentAssetId);

            return response;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "✗ Error during bulk generation for parent asset {ParentAssetId}", dto.ParentAssetId);
            response.Errors.Add($"Error during bulk generation: {ex.Message}");
            return response;
        }
    }

    /// <summary>
    /// Updates an existing child asset (room/shop) under a parent asset with complete details from the form.
    /// 
    /// FLOW:
    ///   STEP 1: Update the existing child asset (e.g., FLAT-101) using PUT operation
    ///   STEP 2: Use the AssetId to create/update room-wise submission details in AssetRoomWiseSubmissionDetails table (POST)
    ///   STEP 3: Use the AssetId to create/update lease/rent details in AssetLeaseRentDetails table (POST)
    /// 
    /// All operations are wrapped in a transaction - if any step fails, everything is rolled back.
    /// </summary>
    public async Task<CreateChildAssetResponseDto> CreateChildAssetAsync(
        CreateChildAssetDto dto,
        CancellationToken cancellationToken = default)
    {
        var response = new CreateChildAssetResponseDto();

        // Validate parent asset exists
        var parentAsset = await _assetRepository.GetByIdAsync(dto.ParentAssetId, cancellationToken);
        if (parentAsset == null)
        {
            response.Success = false;
            response.Message = $"Parent asset with Id {dto.ParentAssetId} not found";
            response.Errors.Add(response.Message);
            return response;
        }

        // STEP 1: Get existing asset by AssetId (PUT operation - update existing asset)
        var existingAsset = await _assetRepository.GetByIdAsync(dto.AssetId, cancellationToken);
        if (existingAsset == null)
        {
            response.Success = false;
            response.Message = $"Asset with Id {dto.AssetId} not found";
            response.Errors.Add(response.Message);
            return response;
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            _logger.LogInformation("Starting update for Asset ID: {AssetId}", dto.AssetId);

            // STEP 1: Update existing child asset (PUT operation)
            // Update basic information from form
            existingAsset.AssetNo = dto.UnitNo ?? existingAsset.AssetNo;
            existingAsset.AssetName = dto.ShopUnitName ?? dto.ComplexName ?? existingAsset.AssetName;
            existingAsset.ParentAssetId = dto.ParentAssetId;
            existingAsset.DepartmentId = dto.DepartmentId ?? existingAsset.DepartmentId;

            // Update location
            if (!string.IsNullOrEmpty(dto.PropertyNo))
            {
                existingAsset.PropertyNo = dto.PropertyNo;
            }
            if (!string.IsNullOrEmpty(dto.PartitionNo))
                existingAsset.PartitionNo = dto.PartitionNo;
            if (!string.IsNullOrEmpty(dto.UpicId))
                existingAsset.UpicId = dto.UpicId;

            // Carpet Area = actual room area from room-wise submission
            // Built-up Area = Carpet × 1.2 (20% standard multiplier for walls/common areas)

            // Update capital value from floor configuration (₹ 46,45,113)

            // Update status based on rent information
            existingAsset.OccupancyStatus = dto.RentInformation != null ? "Rented" : "Vacant";

            // Update audit fields
            existingAsset.UpdatedBy = dto.CreatedBy;
            existingAsset.UpdatedDate = DateTime.UtcNow;

            await _assetRepository.UpdateAsync(existingAsset, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✓ STEP 1 (PUT): Updated existing asset - Id: {AssetId}, AssetNo: {AssetNo}", 
                existingAsset.Id, existingAsset.AssetNo);

            // Resolve floor details (subunit details) for the child asset directly (no connection to parent).
            SubUnitsDetailsEntity? resolvedFloorDetail = null;

            int floorMasterId = dto.FloorId ?? 0;
            if (floorMasterId == 0 && dto.FloorDetailsId.HasValue && dto.FloorDetailsId.Value > 0)
            {
                var floorDetailRow = await _floorDetailsRepository.GetByIdAsync(dto.FloorDetailsId.Value, cancellationToken);
                if (floorDetailRow != null)
                {
                    floorMasterId = floorDetailRow.FloorId;
                }
                else
                {
                    floorMasterId = dto.FloorDetailsId.Value;
                }
            }

            if (floorMasterId <= 0)
            {
                floorMasterId = 1; // Default fallback floor
            }

            // Try to find existing subunit details for the child asset on the specified floor
            resolvedFloorDetail = await _floorDetailsRepository.GetQueryable()
                .Where(f => f.AssetId == existingAsset.Id && f.FloorId == floorMasterId && !f.MarkedForDeletion)
                .OrderBy(f => f.Id)
                .FirstOrDefaultAsync(cancellationToken);

            // Compute areas
            decimal? carpetSqM  = dto.CarpetAreaSqMeter;
            decimal? carpetSqFt = dto.CarpetAreaSqFeet;
            decimal? builtupSqM = dto.BuiltupAreaSqMeter;
            decimal? builtupSqFt = dto.BuiltupAreaSqFeet;

            if ((carpetSqM ?? 0) == 0)
            {
                if (dto.RoomDetails != null && dto.RoomDetails.Any())
                {
                    decimal computedSum = 0;
                    foreach (var r in dto.RoomDetails)
                    {
                        var area = (decimal)(r.TotalAreaSqMtr ?? ((r.AreaSqMtr ?? 0) * (r.NoOfRooms ?? 1)));
                        if (r.MinusYesNo)
                        {
                            computedSum -= area;
                        }
                        else if (r.OuterYesNo)
                        {
                            computedSum += area * 0.8m;
                        }
                        else
                        {
                            computedSum += area;
                        }
                    }
                    carpetSqM  = Math.Max(0m, computedSum);
                    carpetSqFt = carpetSqM  / 0.092903m;
                    builtupSqM = carpetSqM  * 1.2m;
                    builtupSqFt = carpetSqFt * 1.2m;
                }
                else
                {
                    carpetSqFt = dto.FloorConfiguration?.UnitAreaSqFt ?? dto.TotalAreaSqFt ?? 0;
                    carpetSqM  = carpetSqFt * 0.092903m;
                    builtupSqM = carpetSqM  * 1.2m;
                    builtupSqFt = carpetSqFt * 1.2m;
                }
            }

            if (resolvedFloorDetail != null)
            {
                // UPDATE subunit configuration with child asset details
                resolvedFloorDetail.SubFloorId         = dto.SubFloorId          ?? resolvedFloorDetail.SubFloorId;
                resolvedFloorDetail.ConstructionYear   = dto.ConstructionYear    ?? resolvedFloorDetail.ConstructionYear;
                resolvedFloorDetail.AssessmentYear     = dto.AssessmentYear      ?? resolvedFloorDetail.AssessmentYear;
                resolvedFloorDetail.ConstructionTypeId = dto.ConstructionTypeId  ?? resolvedFloorDetail.ConstructionTypeId;
                resolvedFloorDetail.TypeOfUseId        = dto.TypeOfUseId         ?? resolvedFloorDetail.TypeOfUseId;
                resolvedFloorDetail.SubTypeOfUseId     = dto.SubTypeOfUseId      ?? resolvedFloorDetail.SubTypeOfUseId;
                resolvedFloorDetail.CarpetAreaSqMeter  = carpetSqM;
                resolvedFloorDetail.CarpetAreaSqFeet   = carpetSqFt;
                resolvedFloorDetail.BuiltUpAreaSqMeter = builtupSqM;
                resolvedFloorDetail.BuiltUpAreaSqFeet  = builtupSqFt;
                resolvedFloorDetail.IsRented           = dto.RentInformation != null;
                resolvedFloorDetail.UpdatedBy          = dto.CreatedBy;
                resolvedFloorDetail.UpdatedDate        = DateTime.UtcNow;

                await _floorDetailsRepository.UpdateAsync(resolvedFloorDetail, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Updated existing subunit details ID={Id} for ChildAsset={AssetId}",
                    resolvedFloorDetail.Id, existingAsset.Id);
            }
            else
            {
                // INSERT subunit configuration with child asset details
                resolvedFloorDetail = new SubUnitsDetailsEntity
                {
                    AssetId           = existingAsset.Id, // Child asset ID only
                    FloorId           = floorMasterId,
                    SubFloorId        = dto.SubFloorId,
                    // Fall back to AssessmentYear (not the current real-world year) when construction
                    // year is omitted (e.g. Open Space submissions, where "construction year" doesn't
                    // apply). Falling back to DateTime.UtcNow.Year made property age = AssessmentYear -
                    // CurrentYear, which goes negative — and permanently fails CV calculation — for any
                    // assessment year before the current calendar year.
                    ConstructionYear  = dto.ConstructionYear ?? dto.AssessmentYear ?? DateTime.UtcNow.Year.ToString(),
                    AssessmentYear    = dto.AssessmentYear ?? DateTime.UtcNow.Year.ToString(),
                    ConstructionTypeId = dto.ConstructionTypeId ?? 1,
                    TypeOfUseId       = dto.TypeOfUseId ?? 1,
                    SubTypeOfUseId    = dto.SubTypeOfUseId,
                    CarpetAreaSqMeter = carpetSqM,
                    CarpetAreaSqFeet  = carpetSqFt,
                    BuiltUpAreaSqMeter = builtupSqM,
                    BuiltUpAreaSqFeet  = builtupSqFt,
                    IsRented           = dto.RentInformation != null,
                    IsActive           = true,
                    CreatedBy          = dto.CreatedBy,
                    CreatedDate        = DateTime.UtcNow
                };

                await _floorDetailsRepository.AddAsync(resolvedFloorDetail, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created new subunit details ID={Id} for ChildAsset={AssetId}",
                    resolvedFloorDetail.Id, existingAsset.Id);
            }

            response.SubUnitsDetailsId = resolvedFloorDetail.Id;

            int subunitsDetailsId = resolvedFloorDetail.Id;

            // STEP 2: Create room-wise submission details using the AssetId (POST operation)
            // Delete existing room details for this asset to regenerate them
            if (dto.RoomDetails != null && dto.RoomDetails.Any())
            {
                // Delete existing room details for this asset
                var existingRoomDetails = await _roomWiseRepository.GetQueryable()
                    .Where(r => r.AssetId == existingAsset.Id)
                    .ToListAsync(cancellationToken);

                if (existingRoomDetails.Any())
                {
                    var roomDetailIds = existingRoomDetails.Select(r => r.Id).ToList();
                    var existingMinusData = await _minusRepository.GetQueryable()
                        .Where(m => m.RoomWiseSubmissionId.HasValue && roomDetailIds.Contains(m.RoomWiseSubmissionId.Value))
                        .ToListAsync(cancellationToken);

                    foreach (var minus in existingMinusData)
                    {
                        await _minusRepository.DeleteAsync(minus.Id, cancellationToken);
                    }

                    foreach (var roomDetail in existingRoomDetails)
                    {
                        await _roomWiseRepository.DeleteAsync(roomDetail.Id, cancellationToken);
                    }
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Deleted {Count} existing room details and {MinusCount} minus details for Asset: {AssetId}", 
                        existingRoomDetails.Count, existingMinusData.Count, existingAsset.Id);
                }

                _logger.LogInformation("Creating {Count} room details for Asset: {AssetId}, ParentAssetId: {ParentAssetId}, SubUnitsDetailsId: {SubUnitsDetailsId}", 
                    dto.RoomDetails.Count, existingAsset.Id, dto.ParentAssetId, subunitsDetailsId);

                foreach (var roomDetail in dto.RoomDetails)
                {
                    // Calculate area from TotalAreaSqFt if not provided
                    var areaSqMtr = roomDetail.AreaSqMtr ?? 
                        (dto.TotalAreaSqFt.HasValue ? (double)(dto.TotalAreaSqFt.Value * 0.092903m) : null);

                    // CHECK CONSTRAINT VALIDATION: At least one dimension must be > 0
                    // If no dimensions are provided, skip this room detail to avoid constraint violation
                    var hasValidDimensions = (roomDetail.LengthMtr.HasValue && roomDetail.LengthMtr.Value > 0) ||
                                            (roomDetail.WidthMtr.HasValue && roomDetail.WidthMtr.Value > 0) ||
                                            (areaSqMtr.HasValue && areaSqMtr.Value > 0) ||
                                            (roomDetail.HeightMtr.HasValue && roomDetail.HeightMtr.Value > 0) ||
                                            (roomDetail.Base1Mtr.HasValue && roomDetail.Base1Mtr.Value > 0) ||
                                            (roomDetail.Base2Mtr.HasValue && roomDetail.Base2Mtr.Value > 0);

                    if (!hasValidDimensions)
                    {
                        _logger.LogWarning("Skipping room detail for Asset: {AssetId} - No valid dimensions provided", 
                            existingAsset.Id);
                        continue;
                    }

                    var newRoomWiseDetail = new AssetRoomWiseSubmissionDetailsEntity
                    {
                        AssetId = existingAsset.Id,
                        SubUnitsDetailsId = subunitsDetailsId,


                        // Room dimensions
                        LengthMtr = roomDetail.LengthMtr,
                        WidthMtr = roomDetail.WidthMtr,
                        AreaSqMtr = areaSqMtr,
                        HeightMtr = roomDetail.HeightMtr,

                        // Room details
                        TotalAreaSqMtr = roomDetail.TotalAreaSqMtr ?? areaSqMtr,
                        Shape = roomDetail.Shape ?? "Rectangle",
                        RoomNo = roomDetail.RoomNo ?? "1",
                        RoomType = roomDetail.RoomType ?? "Commercial",

                        // Flags
                        OuterYesNo = roomDetail.OuterYesNo,
                        MinusYesNo = roomDetail.MinusYesNo,

                        // Audit
                        IsActive = true,
                        CreatedBy = dto.CreatedBy,
                        CreatedDate = DateTime.UtcNow
                    };

                    await _roomWiseRepository.AddAsync(newRoomWiseDetail, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    if (roomDetail.Offsets != null && roomDetail.Offsets.Any())
                    {
                        foreach (var offset in roomDetail.Offsets)
                        {
                            var minusEntity = new AssetRoomWiseMinusDataEntity
                            {
                                RoomWiseSubmissionId = newRoomWiseDetail.Id,
                                LengthMtr = offset.Length,
                                WidthMtr = offset.Width,
                                AreaSqMtr = offset.AreaSqM,
                                HeightMtr = offset.Height,
                                Shape = offset.Shape ?? "Rectangle",
                                IsActive = true,
                                CreatedBy = dto.CreatedBy ?? 1,
                                CreatedDate = DateTime.UtcNow
                            };

                            await _minusRepository.AddAsync(minusEntity, cancellationToken);
                        }
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }

                    _logger.LogInformation("STEP 2 (POST): Created room-wise details - Id: {RoomId}, RoomNo: {RoomNo} for Asset: {AssetId}",
                        newRoomWiseDetail.Id, newRoomWiseDetail.RoomNo, existingAsset.Id);

                    response.RoomWiseSubmissionDetailsId = newRoomWiseDetail.Id;
                }
            }

            // STEP 3: Create lease/rent details using the AssetId (POST operation)
            if (dto.RentInformation != null)
            {
                // Delete existing lease/rent details for this asset to regenerate them
                var existingLeaseRentDetails = await _leaseRentDetailsRepository.GetQueryable()
                    .Where(r => r.AssetId == existingAsset.Id)
                    .ToListAsync(cancellationToken);

                if (existingLeaseRentDetails.Any())
                {
                    foreach (var leaseRentDetail in existingLeaseRentDetails)
                    {
                        await _leaseRentDetailsRepository.DeleteAsync(leaseRentDetail.Id, cancellationToken);
                    }
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Deleted {Count} existing lease/rent details for Asset: {AssetId}",
                        existingLeaseRentDetails.Count, existingAsset.Id);
                }

                _logger.LogInformation("Creating lease/rent details for Asset: {AssetId}", existingAsset.Id);

                // Resolve SubUnitsDetails PK for the new schema FK column (SubUnitDetailsId).
                int? subUnitDetailsId = subunitsDetailsId;

                // Calculate duration in months if both dates are provided
                int? calculatedDuration = null;
                if (dto.RentInformation.LeaseStart.HasValue && dto.RentInformation.LeaseEnd.HasValue)
                {
                    var startDate = dto.RentInformation.LeaseStart.Value;
                    var endDate   = dto.RentInformation.LeaseEnd.Value;
                    calculatedDuration = ((endDate.Year - startDate.Year) * 12) + endDate.Month - startDate.Month;
                }

                // Determine LeaseType — DB has NOT NULL constraint, must be "Lease" or "Rent"
                var leaseType = (dto.RentInformation.LeaseRentType ?? string.Empty)
                    .ToLower().Contains("lease") ? "Lease" : "Rent";

                var newLeaseRentDetail = new AssetLeaseRentDetailsEntity
                {
                    // UpdatedCleanAms: FloorDetailsId links to AMS.SubUnitsDetails
                    FloorDetailsId   = subUnitDetailsId,
                    AssetId          = existingAsset.Id,

                    // Basic Information from form
                    ShopNo          = dto.UnitNo,
                    ShopName        = dto.ShopUnitName ?? dto.ComplexName,
                    TenantName      = dto.RenterName ?? dto.ShopUnitName ?? dto.ComplexName ?? string.Empty,
                    TenantMobile    = dto.MobileNo ?? string.Empty,
                    TenantEmail     = dto.EmailId,
                    TenantType      = "Individual",
                    TenantAadhaarNo = dto.AadhaarCardNo,
                    TenantPanCardNo = dto.PanCardNo,
                    TenantAddress   = dto.PropertyDescription,
                    GSTNo           = dto.GSTNo,
                    TotalAreaSqFt   = dto.TotalAreaSqFt,

                    // Rent Information
                    LeaseType         = leaseType,                  // NOT NULL in new schema
                    LeaseStartDate    = dto.RentInformation.LeaseStart ?? DateTime.UtcNow,
                    LeaseEndDate      = dto.RentInformation.LeaseEnd,
                    Duration          = dto.RentInformation.Duration ?? calculatedDuration,
                    PaymentFrequency  = dto.RentInformation.RentFrequency ?? "Monthly",
                    RentAmount        = dto.RentInformation.RentAmount,
                    SecurityDeposit   = dto.RentInformation.SecurityDeposit ?? 0m,
                    DepositType       = dto.RentInformation.DepositType,
                    ApplicationTypeId = await ResolveApplicationTypeIdAsync(cancellationToken),

                    IncrementFrequency = dto.RentInformation.RentFrequency,
                    IncrementType      = dto.RentInformation.LeaseRentType,
                    Reason             = "Registered asset and tenant/renter",
                    WorkflowStatus     = "Pending",

                    IsActive    = true,
                    CreatedBy   = dto.CreatedBy,
                    CreatedDate = DateTime.UtcNow
                };

                await _leaseRentDetailsRepository.AddAsync(newLeaseRentDetail, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("✓ STEP 3 (POST): Created lease/rent details - Id: {LeaseRentId} for Asset: {AssetId}",
                    newLeaseRentDetail.Id, existingAsset.Id);

                response.RenterDetailsId = newLeaseRentDetail.Id;
            }


            // STEP 4: Removed. This used to delete the parent asset's own SubUnitsDetails row for
            // this floor, but floor details for a given floor are shared/parent-owned and read by
            // GetFloorDetailsForSubAssetAsync for every sibling unit on that floor — deleting it
            // here corrupted sibling units whenever more than one unit was configured per floor.

            // STEP 5: Removed (Already handled at start of transaction)

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Document / Photo Upload Flow (similar to RegisterInventoryBatch and AssetMaster creation)
            if (dto.PhotoFiles is { Count: > 0 } && !string.IsNullOrWhiteSpace(dto.PhotoMetadataJson))
            {
                try
                {
                    // Query department and module IDs dynamically from databases to keep it architecture-compliant
                    var deptEntity = (await _deptMasterRepository.GetAsync(
                        d => d.DepartmentName != null && d.DepartmentName.ToLower().Contains("asset"),
                        cancellationToken)).FirstOrDefault();
                    var modEntity = (await _moduleMasterRepository.GetAsync(
                        m => m.ModuleName != null && m.ModuleName.ToLower().Contains("asset"),
                        cancellationToken)).FirstOrDefault();

                    var dynamicDeptId = deptEntity?.Id ?? 3;
                    var dynamicModId = modEntity?.Id ?? 2;

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var photoMetadata = JsonSerializer.Deserialize<List<AssetPhotoItemDto>>(dto.PhotoMetadataJson, options) ?? new List<AssetPhotoItemDto>();

                    for (var i = 0; i < dto.PhotoFiles.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var file = dto.PhotoFiles[i];
                        var meta = photoMetadata.ElementAtOrDefault(i);

                        if (file == null || file.Length <= 0 || meta == null)
                            continue;

                        // Create the photo slot in the database
                        var photoId = await _assetPhotoService.CreateAsync(
                            dto.AssetId, // Child asset ID
                            meta.PhotoTypeId,
                            subunitsDetailsId, // subUnitDetailsId
                            meta.DisplayOrder ?? 0,
                            meta.Remarks,
                            dto.CreatedBy ?? 1,
                            cancellationToken);

                        await using var fileStream = file.OpenReadStream();
                        await _documentApplicationService.UploadDocumentAsync(
                            fileStream,
                            file.FileName,
                            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                            file.Length,
                            new DocumentUploadDto
                            {
                                ReferenceTableName = "AssetPhoto",
                                ReferenceTableId = photoId,
                                ModuleId = dynamicModId,
                                DepartmentId = dynamicDeptId,
                                DocumentType = "AssetPhoto",
                                AuthDepartmentId = dynamicDeptId
                            },
                            uploadedBy: dto.CreatedBy ?? 1,
                            cancellationToken: cancellationToken);
                    }
                }
                catch (Exception uploadEx)
                {
                    _logger.LogError(uploadEx, "Non-fatal error uploading documents/photos for child asset update");
                    response.Errors.Add($"Document upload encountered an error: {uploadEx.Message}");
                }
            }


            response.Success = true;
            response.Message = $"✓ Child asset '{existingAsset.AssetNo}' updated successfully with all related details";
            response.AssetId = existingAsset.Id;
            response.AssetNo = existingAsset.AssetNo;

            _logger.LogInformation("✓ SUCCESS: Child asset update completed - AssetId: {AssetId}, AssetNo: {AssetNo}, RoomDetailsId: {RoomId}, LeaseRentDetailsId: {LeaseRentId}",
                existingAsset.Id, existingAsset.AssetNo, response.RoomWiseSubmissionDetailsId, response.RenterDetailsId);

            return response;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "✗ Error during child asset update for AssetId: {AssetId}, ParentAssetId: {ParentAssetId}", dto.AssetId, dto.ParentAssetId);

            response.Success = false;
            response.Message = $"Error while updating child asset: {ex.Message}";
            response.Errors.Add(response.Message);
            return response;
        }
    }

    /// <summary>
    /// Retrieves room-wise submission and lease/rent details for a child asset by AssetId.
    /// Note: Asset master and floor details should be retrieved using their respective GET APIs.
    /// </summary>
    public async Task<GetChildAssetResponseDto> GetChildAssetByIdAsync(
        int assetId,
        CancellationToken cancellationToken = default)
    {
        var response = new GetChildAssetResponseDto
        {
            AssetId = assetId,
            RoomWiseDetails = new List<RoomWiseDetailsDto>()
        };

        try
        {
            var asset = await _assetRepository.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == assetId && !a.MarkedForDeletion, cancellationToken);

            if (asset == null)
            {
                response.Success = false;
                response.Message = $"Asset with Id {assetId} not found";
                return response;
            }

            var roomWiseDetails = await _roomWiseRepository.GetQueryable()
                .AsNoTracking()
                .Where(r => r.AssetId == assetId && !r.MarkedForDeletion)
                .OrderBy(r => r.Id)
                .Select(r => new RoomWiseDetailsDto
                {
                    Id = r.Id,
                    AssetId = r.AssetId,
                    FloorDetailsId = r.SubUnitsDetailsId,
                    RoomNo = r.RoomNo,
                    RoomType = r.RoomType,
                    Shape = r.Shape,
                    LengthMtr = r.LengthMtr,
                    WidthMtr = r.WidthMtr,
                    HeightMtr = r.HeightMtr,
                    AreaSqMtr = r.AreaSqMtr,
                    TotalAreaSqMtr = r.TotalAreaSqMtr,
                    OuterYesNo = r.OuterYesNo,
                    MinusYesNo = r.MinusYesNo
                })
                .ToListAsync(cancellationToken);

            var renter = await _leaseRentDetailsRepository.GetQueryable()
                .AsNoTracking()
                .Where(r => r.AssetId == assetId && !r.MarkedForDeletion)
                .OrderByDescending(r => r.Id)
                .Select(r => new RenterDetailsDto
                {
                    Id              = r.Id,
                    // UpdatedCleanAms: FloorDetailsId + ParentAssetId removed from AssetLeaseRentDetails
                    // Map FloorDetailsId so downstream consumers still work
                    FloorDetailsId  = r.FloorDetailsId ?? 0,
                    AssetId         = r.AssetId,
                    RenterName      = r.TenantName,
                    GSTNo           = r.GSTNo,
                    TotalAreaSqFt   = r.TotalAreaSqFt,
                    AadhaarCardNo   = r.TenantAadhaarNo,
                    PANCardNo       = r.TenantPanCardNo,
                    MobileNo        = r.TenantMobile,
                    EmailId         = r.TenantEmail,
                    FromDate        = r.LeaseStartDate,
                    ToDate          = r.LeaseEndDate,
                    Duration        = r.Duration,
                    RentFrequency   = r.PaymentFrequency,
                    RentAmount      = r.RentAmount,
                    SecurityDeposit = r.SecurityDeposit,
                    DepositType     = r.DepositType,
                    AgreementId     = r.AgreementId,
                    IncrementFrequency = r.IncrementFrequency,
                    IncrementType      = r.IncrementType,
                    IncrementValue     = r.IncrementValue,
                    IncrementMethod    = r.IncrementMethod
                })
                .FirstOrDefaultAsync(cancellationToken);

            response.Success = true;
            response.Message = "Child asset details retrieved successfully";
            response.RenterDetails = renter;
            response.RoomWiseDetails = roomWiseDetails;

            _logger.LogInformation("Retrieved child asset details for AssetId: {AssetId}. RoomDetailsCount: {RoomCount}, HasRenter: {HasRenter}",
                assetId, roomWiseDetails.Count, renter != null);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Error while retrieving child asset details for AssetId: {AssetId}", assetId);

            response.Success = false;
            response.Message = $"Error while retrieving child asset details: {ex.Message}";
            return response;
        }
    }

    /// <summary>
    /// Retrieves all child assets (subunits) under a parent asset.
    /// </summary>
    public async Task<List<SubUnitResponseDto>> GetSubUnitsByAssetIdAsync(int parentAssetId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all child assets for ParentAssetId: {ParentAssetId}", parentAssetId);

        var childAssets = await _assetRepository.GetQueryable()
            .AsNoTracking()
            .Where(a =>
                a.ParentAssetId == parentAssetId &&
                !a.MarkedForDeletion &&
                !_inventoryAssetDetailRepository.GetQueryable().Any(iad => iad.AssetId == a.Id))
            .ToListAsync(cancellationToken);

        var subUnitIds = childAssets.Select(u => u.Id).ToList();

        // Fetch sub-unit configuration IDs and CarpetAreaSqFeet from SubUnitsDetails directly
        var subUnitConfigs = await _floorDetailsRepository.GetQueryable()
            .AsNoTracking()
            .Where(f => subUnitIds.Contains(f.AssetId) && !f.MarkedForDeletion)
            .ToDictionaryAsync(f => f.AssetId, f => new { f.Id, f.CarpetAreaSqFeet }, cancellationToken);

        var roomWiseMap = await _roomWiseRepository.GetQueryable()
            .AsNoTracking()
            .Where(r => r.AssetId.HasValue && subUnitIds.Contains(r.AssetId.Value) && !r.MarkedForDeletion)
            .GroupBy(r => r.AssetId!.Value)
            .Select(g => new
            {
                AssetId        = g.Key,
                // Draft record RoomType holds unit type ("Flat"/"Shop"). It has no RoomNo (real
                // rooms always get one — see CreateChildAssetAsync). After rooms are added,
                // RoomType becomes "Bed Room"/"Kitchen" etc. — ignore those.
                UnitType       = g.Where(x => x.RoomNo == null && !string.IsNullOrEmpty(x.RoomType))
                                  .Select(x => x.RoomType)
                                  .FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.AssetId, x => x.UnitType, cancellationToken);

        var response = new List<SubUnitResponseDto>();

        foreach (var asset in childAssets)
        {
            int? floorDetailsId = null;
            decimal? totalAreaSqFt = null;
            string? unitType = null;

            if (subUnitConfigs.TryGetValue(asset.Id, out var config))
            {
                floorDetailsId = config.Id;
                totalAreaSqFt  = config.CarpetAreaSqFeet;
            }

            if (roomWiseMap.TryGetValue(asset.Id, out var uType))
            {
                unitType       = uType;
            }

            // Derive unit type from asset name prefix when room-wise data is absent
            // e.g. "Flat 101" → "Flat", "Shop 01" → "Shop"
            if (string.IsNullOrWhiteSpace(unitType) && !string.IsNullOrWhiteSpace(asset.AssetName))
            {
                var parts = asset.AssetName.Split(' ');
                if (parts.Length >= 1) unitType = parts[0];
            }

            response.Add(new SubUnitResponseDto
            {
                Id = asset.Id,
                ParentAssetId = parentAssetId,
                AssetId = asset.Id,
                ComplexName = asset.ParentAsset?.AssetName,
                ShopUnitName = asset.AssetName,
                UnitNo = asset.AssetNo,
                CreatedDate = asset.CreatedDate,
                FloorDetailsId = floorDetailsId,
                TotalAreaSqFt = totalAreaSqFt,
                UnitType = unitType ?? "Flat"
            });
        }

        return response;
    }

    private async Task<int?> ResolveApplicationTypeIdAsync(CancellationToken cancellationToken)
    {
        var applicationType = await _applicationTypeRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ApplicationTypeName == "New Application" && x.IsActive, cancellationToken);

        return applicationType?.Id;
    }

    /// <summary>
    /// Bulk generates child assets (rooms/shops) across multiple floors.
    /// Per unit: creates one AssetMaster row + one SubUnitsDetails row (linked to the child asset + floor).
    /// All operations are in a single transaction.
    /// </summary>
    public async Task<BulkGenerateAcrossFloorsResponseDto> BulkGenerateAcrossFloorsAsync(
        BulkGenerateAcrossFloorsDto dto,
        CancellationToken cancellationToken = default)
    {
        var response = new BulkGenerateAcrossFloorsResponseDto();

        if (dto.FloorIds == null || dto.FloorIds.Count == 0)
        {
            response.Errors.Add("At least one FloorId is required.");
            return response;
        }

        if (dto.UnitsPerFloor < 1)
        {
            response.Errors.Add("UnitsPerFloor must be at least 1.");
            return response;
        }

        // Validate parent asset
        var parentAsset = await _assetRepository.GetByIdAsync(dto.ParentAssetId, cancellationToken);
        if (parentAsset == null)
        {
            response.Errors.Add($"Parent asset with Id {dto.ParentAssetId} not found.");
            return response;
        }

        int totalCount = dto.FloorIds.Count * dto.UnitsPerFloor;

        // Generate all asset numbers up front
        var generatedAssetNos = await _assetMasterService.GenerateAssetNosAsync(
            parentAsset.AssetCategoryId,
            parentAsset.AssetTypeId,
            totalCount,
            dto.Type,
            cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var generatedAssets = new List<GeneratedAssetDto>();
            int assetNoIndex = 0;

            foreach (var floorId in dto.FloorIds)
            {
                for (int u = 0; u < dto.UnitsPerFloor; u++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var assetNo = generatedAssetNos[assetNoIndex++];
                    var assetName = $"{dto.Type} Unit";

                    // 1. Create child AssetMaster entry
                    var childAsset = new AssetMasterEntity
                    {
                        AssetCategoryId = parentAsset.AssetCategoryId,
                        AssetTypeId     = parentAsset.AssetTypeId,
                        AssetNo         = assetNo,
                        AssetName       = assetName,
                        ParentAssetId   = dto.ParentAssetId,
                        HierarchyLevel  = parentAsset.HierarchyLevel + 1,
                        HierarchyPath   = parentAsset.HierarchyPath != null
                            ? $"{parentAsset.HierarchyPath}/{dto.ParentAssetId}"
                            : $"/{dto.ParentAssetId}",
                        OccupancyStatus = "Vacant",
                        IsActive        = false,   // activated when user fully configures the unit
                        CreatedBy       = dto.CreatedBy,
                        CreatedDate     = DateTime.UtcNow
                    };

                    await _assetRepository.AddAsync(childAsset, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Created draft unit {AssetNo} ID={AssetId} on FloorId={FloorId}",
                        childAsset.AssetNo, childAsset.Id, floorId);

                    // 2. Create SubUnitsDetails row for this child + floor
                    var subUnitsDetail = new SubUnitsDetailsEntity
                    {
                        AssetId           = childAsset.Id,
                        FloorId           = floorId,
                        SubFloorId        = null,
                        ConstructionYear  = dto.ConstructionYear,
                        AssessmentYear    = DateTime.UtcNow.Year.ToString(),
                        ConstructionTypeId = dto.ConstructionTypeId,
                        TypeOfUseId       = dto.TypeOfUseId,
                        SubTypeOfUseId    = dto.SubTypeOfUseId,
                        IsRented          = false,
                        IsActive          = true,
                        CreatedBy         = dto.CreatedBy,
                        CreatedDate       = DateTime.UtcNow
                    };

                    await _subUnitsDetailsRepository.AddAsync(subUnitsDetail, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    generatedAssets.Add(new GeneratedAssetDto
                    {
                        AssetId   = childAsset.Id,
                        AssetNo   = childAsset.AssetNo,
                        AssetName = childAsset.AssetName
                    });
                }
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            response.TotalGenerated = generatedAssets.Count;
            response.GeneratedAssets = generatedAssets;

            _logger.LogInformation(
                "BulkGenerateAcrossFloors: Generated {Count} units for parent {ParentAssetId} across {FloorCount} floors.",
                generatedAssets.Count, dto.ParentAssetId, dto.FloorIds.Count);

            return response;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "BulkGenerateAcrossFloors failed for parent {ParentAssetId}", dto.ParentAssetId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves complete details of all child assets (subunits) under a parent asset.
    /// Includes floor details, room-wise submissions, and room-wise minus details.
    /// </summary>
    public async Task<List<SubUnitCompleteDetailDto>> GetSubUnitsCompleteDetailsByParentIdAsync(
        int parentAssetId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving complete sub-unit details for parent asset {ParentAssetId}", parentAssetId);

        var childAssets = await _assetRepository.GetQueryable()
            .AsNoTracking()
            .Where(a => a.ParentAssetId == parentAssetId && !a.MarkedForDeletion && !_inventoryAssetDetailRepository.GetQueryable().Any(iad => iad.AssetId == a.Id))
            .OrderBy(a => a.AssetNo)
            .ToListAsync(cancellationToken);

        if (!childAssets.Any())
        {
            return new List<SubUnitCompleteDetailDto>();
        }

        var subUnitIds = childAssets.Select(a => a.Id).ToList();

        // Load TypeOfUse and SubTypeOfUse master data lookup dictionaries
        var typeOfUseLookup = await _amsTypeOfUseRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => !x.MarkedForDeletion)
            .ToDictionaryAsync(x => x.Id, x => x.Description, cancellationToken);

        var subTypeOfUseLookup = await _amsSubTypeOfUseRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => !x.MarkedForDeletion)
            .ToDictionaryAsync(x => x.Id, x => x.Description, cancellationToken);

        // Fetch floor details (SubUnitsDetails) for these child assets with their master relation names
        var floorDetails = await _floorDetailsRepository.GetQueryable()
            .AsNoTracking()
            .Include(f => f.Floor)
            .Include(f => f.SubFloor)
            .Include(f => f.ConstructionType)
            .Where(f => subUnitIds.Contains(f.AssetId) && !f.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        // Fetch room-wise submissions for these child assets
        var roomWiseSubmissions = await _roomWiseRepository.GetQueryable()
            .AsNoTracking()
            .Where(r => r.AssetId.HasValue && subUnitIds.Contains(r.AssetId.Value) && !r.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        var roomIds = roomWiseSubmissions.Select(r => r.Id).ToList();

        // Fetch minus details (room-wise offsets)
        var minusDetails = roomIds.Any()
            ? await _minusRepository.GetQueryable()
                .AsNoTracking()
                .Where(m => m.RoomWiseSubmissionId.HasValue && roomIds.Contains(m.RoomWiseSubmissionId.Value) && !m.MarkedForDeletion)
                .ToListAsync(cancellationToken)
            : new List<AssetRoomWiseMinusDataEntity>();

        // Map and construct the response
        var response = new List<SubUnitCompleteDetailDto>();

        var floorDetailsGrouped = floorDetails.GroupBy(f => f.AssetId).ToDictionary(g => g.Key, g => g.ToList());
        var roomWiseGrouped = roomWiseSubmissions.GroupBy(r => r.AssetId!.Value).ToDictionary(g => g.Key, g => g.ToList());
        var minusGrouped = minusDetails.GroupBy(m => m.RoomWiseSubmissionId!.Value).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var asset in childAssets)
        {
            var dto = new SubUnitCompleteDetailDto
            {
                Id = asset.Id,
                AssetNo = asset.AssetNo,
                AssetName = asset.AssetName,
                ParentAssetId = asset.ParentAssetId,
                OccupancyStatus = asset.OccupancyStatus,
                IsActive = asset.IsActive,
                DepartmentId = asset.DepartmentId
            };

            if (floorDetailsGrouped.TryGetValue(asset.Id, out var floors))
            {
                dto.FloorDetails = floors.Select(f => new SubUnitFloorDetailDto
                {
                    Id = f.Id,
                    AssetId = f.AssetId,
                    FloorId = f.FloorId,
                    SubFloorId = f.SubFloorId,
                    ConstructionYear = f.ConstructionYear,
                    AssessmentYear = f.AssessmentYear,
                    ConstructionTypeId = f.ConstructionTypeId,
                    TypeOfUseId = f.TypeOfUseId,
                    SubTypeOfUseId = f.SubTypeOfUseId,
                    CarpetAreaSqMeter = f.CarpetAreaSqMeter,
                    CarpetAreaSqFeet = f.CarpetAreaSqFeet,
                    BuiltupAreaSqMeter = f.BuiltUpAreaSqMeter,
                    BuiltupAreaSqFeet = f.BuiltUpAreaSqFeet,
                    NoOfRooms = f.NoOfRooms,
                    BaseValue = f.BaseValue,
                    CapitalValue = f.CapitalValue,
                    CVAgeFactor = f.CVAgeFactor,
                    CVFloorFactor = f.CVFloorFactor,
                    CVNatureFactor = f.CVNatureFactor,
                    CVUseFactor = f.CVUseFactor,
                    CVBaseRate = f.CVBaseRate,
                    IsRented = f.IsRented,
                    IsActive = f.IsActive,
                    FloorName = f.Floor != null ? f.Floor.Description : null,
                    SubFloorName = f.SubFloor != null ? f.SubFloor.Description : null,
                    ConstructionTypeName = f.ConstructionType != null ? f.ConstructionType.Description : null,
                    TypeOfUseName = typeOfUseLookup.TryGetValue(f.TypeOfUseId, out var touName) ? touName : null,
                    SubTypeOfUseName = f.SubTypeOfUseId.HasValue && subTypeOfUseLookup.TryGetValue(f.SubTypeOfUseId.Value, out var stouName) ? stouName : null
                }).ToList();
            }

            if (roomWiseGrouped.TryGetValue(asset.Id, out var rooms))
            {
                dto.RoomWiseDetails = rooms.Select(r => new SubUnitRoomWiseDetailDto
                {
                    Id = r.Id,
                    AssetId = r.AssetId,
                    FloorDetailsId = r.SubUnitsDetailsId,
                    RoomNo = r.RoomNo,
                    RoomType = r.RoomType,
                    Shape = r.Shape,
                    LengthMtr = r.LengthMtr,
                    WidthMtr = r.WidthMtr,
                    HeightMtr = r.HeightMtr,
                    AreaSqMtr = r.AreaSqMtr,
                    TotalAreaSqMtr = r.TotalAreaSqMtr,
                    OuterYesNo = r.OuterYesNo,
                    MinusYesNo = r.MinusYesNo,
                    IsActive = r.IsActive,
                    MinusDetails = minusGrouped.TryGetValue(r.Id, out var offsets)
                        ? offsets.Select(m => new SubUnitRoomWiseMinusDetailDto
                        {
                            Id = m.Id,
                            RoomWiseSubmissionId = m.RoomWiseSubmissionId,
                            Shape = m.Shape,
                            LengthMtr = m.LengthMtr,
                            WidthMtr = m.WidthMtr,
                            HeightMtr = m.HeightMtr,
                            AreaSqMtr = m.AreaSqMtr,
                            IsActive = m.IsActive
                        }).ToList()
                        : new List<SubUnitRoomWiseMinusDetailDto>()
                }).ToList();
            }

            response.Add(dto);
        }

        return response;
    }
}

