using AutoMapper;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Asset_Management;

/// <summary>
/// Service for managing asset master operations and dashboard statistics.
/// Implementation is split by topic across partial class files in this folder:
/// AssetMasterService.Crud.cs (core CRUD + validation), .Activation.cs, .AssetNumbering.cs,
/// .FieldValues.cs, .Excel.cs, .Inventory.cs, .Lease.cs, .Location.cs, .SubAssets.cs.
/// </summary>
public partial class AssetMasterService : BaseCommonCrudService<AssetMasterEntity, AssetMasterDto, CreateAssetMasterDto, UpdateAssetMasterDto, AssetMasterQueryParameters, int>,
    IAssetMasterService
{
    #region Private Fields & Constructor

    private readonly IReferenceValidationService _referenceValidator;
    private readonly IRepository<AssetFieldValueEntity, int> _fieldValueRepository;
    private readonly IRepository<SubUnitsDetailsEntity, int> _floorDetailsRepository;
    private readonly IRepository<AssetRoomWiseSubmissionDetailsEntity, int> _roomWiseSubmissionRepository;
    private readonly IRepository<AssetCategoryEntity, int> _assetCategoryRepository;
    private readonly IRepository<AssetTypeEntity, int> _assetTypeRepository;
    private readonly IRepository<ULBMasterEntity, int> _ulbRepository;
    private readonly IRepository<AssetDetailsEntity, int> _detailsRepository;
    private readonly IRepository<AssetDocumentEntity, int> _assetDocumentRepository;
    private readonly IAssetPhotoApplicationService _assetPhotoApplicationService;
    private readonly IDocumentApplicationService _documentApplicationService;
    private readonly IRepository<ZoneEntity, int> _zoneRepository;
    private readonly IRepository<WardEntity, int> _wardRepository;
    private readonly IRepository<MoujaEntity, int> _moujaRepository;
    private readonly IRepository<SubZoneDetailsForCVEntity, int> _subZoneRepository;
    private readonly IRepository<OwningDepartmentEntity, int> _departmentRepository;
    private readonly IRepository<AssetOrganizationMasterEntity, int> _organizationRepository;
    private readonly IRepository<AssetConditionMasterEntity, int> _conditionRepository;
    private readonly IRepository<DepartmentMasterEntity, int> _deptMasterRepository;
    private readonly IRepository<AssetDesignationEntity, int> _designationRepository;
    private readonly IRepository<ModuleMasterEntity, int> _moduleMasterRepository;
    private readonly IRepository<AssetPhotoEntity, int> _assetPhotoRepository;
    private readonly IRepository<AssetTypeOfUseMasterEntity, int> _amsTypeOfUseRepository;
    private readonly IRepository<AssetSubTypeOfUseEntity, int> _amsSubTypeOfUseRepository;
    private readonly ILogger<AssetMasterService> _logger;
    private readonly IRepository<InventoryBatchEntity, int> _inventoryBatchRepository;
    private readonly IRepository<InventoryAssetDetailEntity, int> _inventoryAssetDetailRepository;
    private readonly IRepository<InventoryItemCategoryEntity, int> _inventoryCategoryRepository;
    private readonly IRepository<InventoryItemNameEntity, int> _inventoryNameRepository;
    private readonly IRepository<InventoryItemModelEntity, int> _inventoryModelRepository;
    private readonly IRepository<OwningDepartmentEntity, int> _inventoryDepartmentRepository;
    private readonly IInventoryDocumentApplicationService _inventoryDocumentApplicationService;
    private readonly IRepository<AssetLeaseRentDetailsEntity, int> _leaseRentDetailsRepository;

    public AssetMasterService(
        IRepository<AssetMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator,
        IRepository<AssetFieldValueEntity, int> fieldValueRepository,
        IRepository<SubUnitsDetailsEntity, int> floorDetailsRepository,
        IRepository<AssetRoomWiseSubmissionDetailsEntity, int> roomWiseSubmissionRepository,
        IRepository<AssetCategoryEntity, int> assetCategoryRepository,
        IRepository<AssetTypeEntity, int> assetTypeRepository,
        IRepository<ULBMasterEntity, int> ulbRepository,
        IRepository<AssetDetailsEntity, int> detailsRepository,
        IRepository<AssetDocumentEntity, int> assetDocumentRepository,
        IRepository<AssetPhotoEntity, int> assetPhotoRepository,
        IAssetPhotoApplicationService assetPhotoApplicationService,
        IDocumentApplicationService documentApplicationService,
        IRepository<ZoneEntity, int> zoneRepository,
        IRepository<WardEntity, int> wardRepository,
        IRepository<MoujaEntity, int> moujaRepository,
        IRepository<SubZoneDetailsForCVEntity, int> subZoneRepository,
        IRepository<OwningDepartmentEntity, int> departmentRepository,
        IRepository<AssetOrganizationMasterEntity, int> organizationRepository,
        IRepository<AssetConditionMasterEntity, int> conditionRepository,
        IRepository<DepartmentMasterEntity, int> deptMasterRepository,
        IRepository<ModuleMasterEntity, int> moduleMasterRepository,
        IRepository<AssetDesignationEntity, int> designationRepository,
        IRepository<AssetTypeOfUseMasterEntity, int> amsTypeOfUseRepository,
        IRepository<AssetSubTypeOfUseEntity, int> amsSubTypeOfUseRepository,
        ILogger<AssetMasterService> logger,
        IRepository<InventoryBatchEntity, int> inventoryBatchRepository,
        IRepository<InventoryAssetDetailEntity, int> inventoryAssetDetailRepository,
        IRepository<InventoryItemCategoryEntity, int> inventoryCategoryRepository,
        IRepository<InventoryItemNameEntity, int> inventoryNameRepository,
        IRepository<InventoryItemModelEntity, int> inventoryModelRepository,
        IRepository<OwningDepartmentEntity, int> inventoryDepartmentRepository,
        IInventoryDocumentApplicationService inventoryDocumentApplicationService,
        IRepository<AssetLeaseRentDetailsEntity, int> leaseRentDetailsRepository)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
        _fieldValueRepository = fieldValueRepository;
        _floorDetailsRepository = floorDetailsRepository;
        _roomWiseSubmissionRepository = roomWiseSubmissionRepository;
        _assetCategoryRepository = assetCategoryRepository;
        _assetTypeRepository = assetTypeRepository;
        _ulbRepository = ulbRepository;
        _detailsRepository = detailsRepository;
        _assetDocumentRepository = assetDocumentRepository;
        _assetPhotoRepository = assetPhotoRepository;
        _assetPhotoApplicationService = assetPhotoApplicationService;
        _documentApplicationService = documentApplicationService;
        _zoneRepository = zoneRepository;
        _wardRepository = wardRepository;
        _moujaRepository = moujaRepository;
        _subZoneRepository = subZoneRepository;
        _departmentRepository = departmentRepository;
        _organizationRepository = organizationRepository;
        _conditionRepository = conditionRepository;
        _deptMasterRepository = deptMasterRepository;
        _moduleMasterRepository = moduleMasterRepository;
        _designationRepository = designationRepository;
        _amsTypeOfUseRepository = amsTypeOfUseRepository;
        _amsSubTypeOfUseRepository = amsSubTypeOfUseRepository;
        _logger = logger;
        _inventoryBatchRepository = inventoryBatchRepository;
        _inventoryAssetDetailRepository = inventoryAssetDetailRepository;
        _inventoryCategoryRepository = inventoryCategoryRepository;
        _inventoryNameRepository = inventoryNameRepository;
        _inventoryModelRepository = inventoryModelRepository;
        _inventoryDepartmentRepository = inventoryDepartmentRepository;
        _inventoryDocumentApplicationService = inventoryDocumentApplicationService;
        _leaseRentDetailsRepository = leaseRentDetailsRepository;
    }

    #endregion

    #region Unused Helper (not called anywhere in this class or its partials — AssetDashboardService has its own separate copy)

    private static (decimal? Value, string? Unit) FormatMarketValue(decimal? value)
    {
        if (!value.HasValue)
            return (null, null);

        decimal absValue = Math.Abs(value.Value);

        if (absValue >= 10000000m)
        {
            return (Math.Round(value.Value / 10000000m, 2), "Cr");
        }
        else if (absValue >= 100000m)
        {
            return (Math.Round(value.Value / 100000m, 2), "L");
        }
        else if (absValue >= 1000m)
        {
            return (Math.Round(value.Value / 1000m, 2), "K");
        }
        else
        {
            return (Math.Round(value.Value, 2), "");
        }
    }

    #endregion
}
