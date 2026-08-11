using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for calculating Capital Value for Assets
/// Supports individual assets (shops/units) and building-level aggregation
/// CV = (Rate × CarpetArea) × NatureFactor × UseFactor × AgeFactor × FloorFactor
/// Implementation is split by topic across partial class files in this folder:
/// AssetCapitalValueService.BuildingCV.cs (floor/building CV calculation + master-data-driven
/// lookups), .MovableAssets.cs, .OpenPlot.cs, .History.cs, .MasterData.cs (CVMasterData /
/// RateMasterLookup / CapitalValueCalculationEngine).
/// </summary>
public partial class AssetCapitalValueService : IAssetCapitalValueService
{
    private readonly IRepository<AssetMasterEntity, long> _assetRepository;
    private readonly IRepository<SubUnitsDetailsEntity, long> _assetFloorRepository;
    private readonly IRepository<CVRateMasterEntity, int> _rateRepository;
    private readonly IRepository<AssetNatureFactorCVMasterEntity, int> _natureFactorRepository;
    private readonly IRepository<AssetUseFactorCVMasterEntity, int> _useFactorRepository;
    private readonly IRepository<AssetAgeFactorCVMasterEntity, int> _ageFactorRepository;
    private readonly IRepository<AssetFloorFactorCVEntity, int> _floorFactorRepository;
    private readonly IRepository<AssetAssessmentYearRangeMasterCVEntity, int> _assessmentYearRangeRepository;
    private readonly IRepository<AssetTypeOfUseMasterEntity, int> _typeOfUseRepository;
    private readonly IRepository<AssetTypeOfUseGroupEntity, int> _typeOfUseGroupRepository;
    private readonly IRepository<CSNDetailsEntity, int> _csnDetailsRepository;
    private readonly IRepository<AssetRoomWiseSubmissionDetailsEntity, int> _roomDetailsRepository;
    private readonly IRepository<AssetCVCalculationHistoryEntity, int> _historyRepository;
    // Legacy: only used to update AssetDetails.CapitalValue on parent assets that already have a row.
    // That field is Ignore()'d in the EF model — the Asset Register actually sums CV from
    // SubUnitsDetailsEntity.CapitalValue. See PersistAssetCapitalValueAsync.
    private readonly IRepository<AssetDetailsEntity, int> _detailsRepository;
    // Used only by the read-only parent-asset CV rollup — never written to here.
    private readonly IRepository<InventoryBatchEntity, int> _inventoryBatchRepository;
    private readonly IRepository<InventoryAssetDetailEntity, int> _inventoryAssetDetailRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<AssetCapitalValueService> _logger;

    public AssetCapitalValueService(
        IRepository<AssetMasterEntity, long> assetRepository,
        IRepository<SubUnitsDetailsEntity, long> assetFloorRepository,
        IRepository<CVRateMasterEntity, int> rateRepository,
        IRepository<AssetNatureFactorCVMasterEntity, int> natureFactorRepository,
        IRepository<AssetUseFactorCVMasterEntity, int> useFactorRepository,
        IRepository<AssetAgeFactorCVMasterEntity, int> ageFactorRepository,
        IRepository<AssetFloorFactorCVEntity, int> floorFactorRepository,
        IRepository<AssetAssessmentYearRangeMasterCVEntity, int> assessmentYearRangeRepository,
        IRepository<AssetTypeOfUseMasterEntity, int> typeOfUseRepository,
        IRepository<AssetTypeOfUseGroupEntity, int> typeOfUseGroupRepository,
        IRepository<CSNDetailsEntity, int> csnDetailsRepository,
        IRepository<AssetRoomWiseSubmissionDetailsEntity, int> roomDetailsRepository,
        IRepository<AssetCVCalculationHistoryEntity, int> historyRepository,
        IRepository<AssetDetailsEntity, int> detailsRepository,
        IRepository<InventoryBatchEntity, int> inventoryBatchRepository,
        IRepository<InventoryAssetDetailEntity, int> inventoryAssetDetailRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<AssetCapitalValueService> logger)
    {
        _assetRepository = assetRepository;
        _assetFloorRepository = assetFloorRepository;
        _rateRepository = rateRepository;
        _natureFactorRepository = natureFactorRepository;
        _useFactorRepository = useFactorRepository;
        _ageFactorRepository = ageFactorRepository;
        _floorFactorRepository = floorFactorRepository;
        _assessmentYearRangeRepository = assessmentYearRangeRepository;
        _typeOfUseRepository = typeOfUseRepository;
        _typeOfUseGroupRepository = typeOfUseGroupRepository;
        _csnDetailsRepository = csnDetailsRepository;
        _roomDetailsRepository = roomDetailsRepository;
        _historyRepository = historyRepository;
        _detailsRepository = detailsRepository;
        _inventoryBatchRepository = inventoryBatchRepository;
        _inventoryAssetDetailRepository = inventoryAssetDetailRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Upserts AssetDetails.CapitalValue for an asset so the Asset Register / list (which reads
    /// AssetDetails.CapitalValue) reflects the calculated CV. Does not call SaveChanges — the caller
    /// commits as part of its own unit of work.
    /// </summary>
    private async Task PersistAssetCapitalValueAsync(int assetId, decimal capitalValue, CancellationToken cancellationToken)
    {
        // AMS.AssetDetails is 1:1 with a PARENT asset only (location + KYC details) — child assets
        // (shops/units/floors) never get their own row. AssetDetailsEntity.CapitalValue is also a
        // compatibility shim Ignore()'d in the EF model (see ApplicationDbContext), so writing it here
        // is a no-op regardless; the Asset Register/dashboard actually sums CV from
        // SubUnitsDetailsEntity.CapitalValue (AssetMasterService.GetAllAsync). Only update the row when
        // the asset already has one (i.e. it's a parent) — never create one just to hold a dead field.
        //
        // Read TRACKED: if the context already tracks this AssetDetails row (same AssetId key), EF returns
        // that same instance, so mutating it is safe. We deliberately do NOT call repository.UpdateAsync
        // (_dbSet.Update) here — calling Update() on an already-tracked entity (or re-attaching the required
        // Asset navigation graph) can throw "another instance with the same key is already being tracked".
        // A tracked entity's property change is persisted by SaveChangesAsync automatically.
        var details = await _detailsRepository.GetQueryable()
            .FirstOrDefaultAsync(d => d.AssetId == assetId, cancellationToken);
        if (details != null)
        {
            details.CapitalValue = capitalValue;
            details.UpdatedDate = DateTime.UtcNow;
        }
    }
}
