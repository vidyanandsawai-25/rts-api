using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.DTOs.Document;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Services.Asset_Management;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Asset_Management;
using Xunit;

namespace NtisPlatform.Tests.Application.Services.Asset_Management;

/// <summary>
/// Comprehensive unit tests for <see cref="ManageSubUnitsService"/>, covering every public
/// method backing <c>ManageSubUnitsController</c>: sub-unit listing/detail retrieval, bulk
/// child-asset generation (single-floor and across-floors), single child-asset create/update
/// (with room-wise, lease/rent, and photo-upload side effects), and the various read-only
/// lookup endpoints used by the eye-button / grid views.
/// </summary>
public class ManageSubUnitsServiceTests
{
    private readonly Mock<IRepository<AssetMasterEntity, int>> _assetRepo;
    private readonly Mock<IRepository<AssetLeaseRentDetailsEntity, int>> _leaseRentRepo;
    private readonly Mock<IRepository<AssetRoomWiseSubmissionDetailsEntity, int>> _roomWiseRepo;
    private readonly Mock<IRepository<SubUnitsDetailsEntity, int>> _floorDetailsRepo;
    private readonly Mock<IRepository<AssetApplicationTypeEntity, int>> _applicationTypeRepo;
    private readonly Mock<IRepository<AssetRoomWiseMinusDataEntity, int>> _minusRepo;
    private readonly Mock<IRepository<AssetDetailsEntity, int>> _locationDetailsRepo;
    private readonly Mock<IRepository<SubUnitsDetailsEntity, int>> _subUnitsDetailsRepo;
    private readonly Mock<IRepository<InventoryAssetDetailEntity, int>> _inventoryAssetDetailRepo;
    private readonly Mock<IRepository<AssetTypeOfUseMasterEntity, int>> _amsTypeOfUseRepo;
    private readonly Mock<IRepository<AssetSubTypeOfUseEntity, int>> _amsSubTypeOfUseRepo;
    private readonly Mock<IAssetMasterService> _assetMasterService;
    private readonly Mock<IAssetPhotoService> _assetPhotoService;
    private readonly Mock<IDocumentApplicationService> _documentApplicationService;
    private readonly Mock<IRepository<DepartmentMasterEntity, int>> _deptMasterRepo;
    private readonly Mock<IRepository<ModuleMasterEntity, int>> _moduleMasterRepo;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<ILogger<ManageSubUnitsService>> _logger;
    private readonly ManageSubUnitsService _service;

    private int _nextId = 1000;

    public ManageSubUnitsServiceTests()
    {
        _assetRepo = new Mock<IRepository<AssetMasterEntity, int>>();
        _leaseRentRepo = new Mock<IRepository<AssetLeaseRentDetailsEntity, int>>();
        _roomWiseRepo = new Mock<IRepository<AssetRoomWiseSubmissionDetailsEntity, int>>();
        _floorDetailsRepo = new Mock<IRepository<SubUnitsDetailsEntity, int>>();
        _applicationTypeRepo = new Mock<IRepository<AssetApplicationTypeEntity, int>>();
        _minusRepo = new Mock<IRepository<AssetRoomWiseMinusDataEntity, int>>();
        _locationDetailsRepo = new Mock<IRepository<AssetDetailsEntity, int>>();
        _subUnitsDetailsRepo = new Mock<IRepository<SubUnitsDetailsEntity, int>>();
        _inventoryAssetDetailRepo = new Mock<IRepository<InventoryAssetDetailEntity, int>>();
        _amsTypeOfUseRepo = new Mock<IRepository<AssetTypeOfUseMasterEntity, int>>();
        _amsSubTypeOfUseRepo = new Mock<IRepository<AssetSubTypeOfUseEntity, int>>();
        _assetMasterService = new Mock<IAssetMasterService>();
        _assetPhotoService = new Mock<IAssetPhotoService>();
        _documentApplicationService = new Mock<IDocumentApplicationService>();
        _deptMasterRepo = new Mock<IRepository<DepartmentMasterEntity, int>>();
        _moduleMasterRepo = new Mock<IRepository<ModuleMasterEntity, int>>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _logger = new Mock<ILogger<ManageSubUnitsService>>();

        // ---- Sensible empty-by-default query surfaces ----
        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity>().BuildMock());
        _leaseRentRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetLeaseRentDetailsEntity>().BuildMock());
        _roomWiseRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetRoomWiseSubmissionDetailsEntity>().BuildMock());
        _floorDetailsRepo.Setup(r => r.GetQueryable()).Returns(new List<SubUnitsDetailsEntity>().BuildMock());
        _applicationTypeRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetApplicationTypeEntity>().BuildMock());
        _minusRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetRoomWiseMinusDataEntity>().BuildMock());
        _subUnitsDetailsRepo.Setup(r => r.GetQueryable()).Returns(new List<SubUnitsDetailsEntity>().BuildMock());
        _inventoryAssetDetailRepo.Setup(r => r.GetQueryable()).Returns(new List<InventoryAssetDetailEntity>().BuildMock());
        _amsTypeOfUseRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetTypeOfUseMasterEntity>().BuildMock());
        _amsSubTypeOfUseRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetSubTypeOfUseEntity>().BuildMock());

        _deptMasterRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<DepartmentMasterEntity, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DepartmentMasterEntity>());
        _moduleMasterRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<ModuleMasterEntity, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ModuleMasterEntity>());

        // ---- AddAsync mocks mimic identity-generation: assign an Id and echo the entity back ----
        _assetRepo.Setup(r => r.AddAsync(It.IsAny<AssetMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns((AssetMasterEntity e, CancellationToken _) => { if (e.Id == 0) e.Id = _nextId++; return Task.FromResult(e); });
        _leaseRentRepo.Setup(r => r.AddAsync(It.IsAny<AssetLeaseRentDetailsEntity>(), It.IsAny<CancellationToken>()))
            .Returns((AssetLeaseRentDetailsEntity e, CancellationToken _) => { if (e.Id == 0) e.Id = _nextId++; return Task.FromResult(e); });
        _roomWiseRepo.Setup(r => r.AddAsync(It.IsAny<AssetRoomWiseSubmissionDetailsEntity>(), It.IsAny<CancellationToken>()))
            .Returns((AssetRoomWiseSubmissionDetailsEntity e, CancellationToken _) => { if (e.Id == 0) e.Id = _nextId++; return Task.FromResult(e); });
        _floorDetailsRepo.Setup(r => r.AddAsync(It.IsAny<SubUnitsDetailsEntity>(), It.IsAny<CancellationToken>()))
            .Returns((SubUnitsDetailsEntity e, CancellationToken _) => { if (e.Id == 0) e.Id = _nextId++; return Task.FromResult(e); });
        _subUnitsDetailsRepo.Setup(r => r.AddAsync(It.IsAny<SubUnitsDetailsEntity>(), It.IsAny<CancellationToken>()))
            .Returns((SubUnitsDetailsEntity e, CancellationToken _) => { if (e.Id == 0) e.Id = _nextId++; return Task.FromResult(e); });
        _minusRepo.Setup(r => r.AddAsync(It.IsAny<AssetRoomWiseMinusDataEntity>(), It.IsAny<CancellationToken>()))
            .Returns((AssetRoomWiseMinusDataEntity e, CancellationToken _) => { if (e.Id == 0) e.Id = _nextId++; return Task.FromResult(e); });

        // ---- IUnitOfWork / IAssetMasterService / IAssetPhotoService / IDocumentApplicationService defaults ----
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _assetMasterService
            .Setup(m => m.GenerateAssetNosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns((int categoryId, int typeId, int count, string? prefix, CancellationToken _) =>
                Task.FromResult(Enumerable.Range(1, count).Select(i => $"{prefix}-{i:D4}").ToList()));

        _assetPhotoService
            .Setup(p => p.CreateAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _nextId++);
        _assetPhotoService
            .Setup(p => p.GetLatestByAssetIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetPhotoEntity>());

        _documentApplicationService
            .Setup(d => d.UploadDocumentAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                It.IsAny<DocumentUploadDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocumentUploadResponseDto { DocumentId = 1, DocumentGuid = Guid.NewGuid() });

        _service = new ManageSubUnitsService(
            _assetRepo.Object,
            _leaseRentRepo.Object,
            _roomWiseRepo.Object,
            _floorDetailsRepo.Object,
            _applicationTypeRepo.Object,
            _minusRepo.Object,
            _locationDetailsRepo.Object,
            _subUnitsDetailsRepo.Object,
            _inventoryAssetDetailRepo.Object,
            _amsTypeOfUseRepo.Object,
            _amsSubTypeOfUseRepo.Object,
            _assetMasterService.Object,
            _assetPhotoService.Object,
            _documentApplicationService.Object,
            _deptMasterRepo.Object,
            _moduleMasterRepo.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static AssetMasterEntity NewAsset(
        int id, int? parentAssetId = null, string assetNo = "A-1", string assetName = "Flat 101",
        bool isActive = true, bool markedForDeletion = false, int categoryId = 1, int typeId = 1,
        string? occupancyStatus = "Vacant")
        => new()
        {
            Id = id,
            ParentAssetId = parentAssetId,
            AssetNo = assetNo,
            AssetName = assetName,
            AssetCategoryId = categoryId,
            AssetTypeId = typeId,
            IsActive = isActive,
            MarkedForDeletion = markedForDeletion,
            OccupancyStatus = occupancyStatus,
            HierarchyLevel = parentAssetId.HasValue ? 1 : 0,
            CreatedDate = DateTime.UtcNow
        };

    private static SubUnitsDetailsEntity NewFloorDetails(
        int id, int assetId, int floorId = 1, int? subFloorId = null, int constructionTypeId = 1,
        int typeOfUseId = 1, int? subTypeOfUseId = null, bool markedForDeletion = false)
        => new()
        {
            Id = id,
            AssetId = assetId,
            FloorId = floorId,
            SubFloorId = subFloorId,
            ConstructionTypeId = constructionTypeId,
            TypeOfUseId = typeOfUseId,
            SubTypeOfUseId = subTypeOfUseId,
            MarkedForDeletion = markedForDeletion,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

    private static Mock<IFormFile> NewFormFile(string fileName, string contentType, byte[]? content = null)
    {
        content ??= new byte[] { 1, 2, 3 };
        var stream = new MemoryStream(content);
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.ContentType).Returns(contentType);
        file.Setup(f => f.Length).Returns(content.Length);
        file.Setup(f => f.OpenReadStream()).Returns(stream);
        return file;
    }

    #region GetAllSubUnitsByParentIdAsync

    [Fact]
    public async Task GetAllSubUnitsByParentIdAsync_ReturnsActiveNonDeletedNonInventorySubUnits()
    {
        var parent = NewAsset(1, assetNo: "BLDG-1", assetName: "Building");
        var child1 = NewAsset(2, parentAssetId: 1, assetNo: "A-1", assetName: "Flat 101");
        var child2 = NewAsset(3, parentAssetId: 1, assetNo: "A-2", assetName: "Flat 102");
        var deletedChild = NewAsset(4, parentAssetId: 1, assetNo: "A-3", markedForDeletion: true);
        var inactiveIsFine = NewAsset(5, parentAssetId: 1, assetNo: "A-4", isActive: false);

        _assetRepo.Setup(r => r.GetQueryable())
            .Returns(new List<AssetMasterEntity> { parent, child1, child2, deletedChild, inactiveIsFine }.BuildMock());

        var result = await _service.GetAllSubUnitsByParentIdAsync(1, CancellationToken.None);

        // inactiveIsFine is excluded too: the query requires IsActive == true
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Id == 2);
        Assert.Contains(result, r => r.Id == 3);
        Assert.DoesNotContain(result, r => r.Id == 4);
        Assert.DoesNotContain(result, r => r.Id == 5);
    }

    [Fact]
    public async Task GetAllSubUnitsByParentIdAsync_ExcludesInventoryAssets()
    {
        var child = NewAsset(2, parentAssetId: 1, assetNo: "A-1");
        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { child }.BuildMock());
        _inventoryAssetDetailRepo.Setup(r => r.GetQueryable())
            .Returns(new List<InventoryAssetDetailEntity> { new() { Id = 1, AssetId = 2 } }.BuildMock());

        var result = await _service.GetAllSubUnitsByParentIdAsync(1, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllSubUnitsByParentIdAsync_ComputesAssetLifeAndAggregatesFromFloorDetails()
    {
        var currentYear = DateTime.Today.Year;
        var child = NewAsset(2, parentAssetId: 1, assetNo: "A-1");
        var floor = new SubUnitsDetailsEntity
        {
            Id = 10,
            AssetId = 2,
            FloorId = 1,
            ConstructionTypeId = 1,
            TypeOfUseId = 1,
            ConstructionYear = (currentYear - 5).ToString(),
            BuiltUpAreaSqMeter = 100m,
            CarpetAreaSqMeter = 80m,
            CapitalValue = 500000m,
            IsActive = true,
            MarkedForDeletion = false
        };

        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { child }.BuildMock());
        _floorDetailsRepo.Setup(r => r.GetQueryable()).Returns(new List<SubUnitsDetailsEntity> { floor }.BuildMock());

        var result = await _service.GetAllSubUnitsByParentIdAsync(1, CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(5, dto.AssetLife);
        Assert.Equal(100m, dto.BuiltUpAreaSqMeter);
        Assert.Equal(80m, dto.CarpetAreaSqMeter);
        Assert.Equal(500000m, dto.CapitalValue);
    }

    [Fact]
    public async Task GetAllSubUnitsByParentIdAsync_NoMatchingChildren_ReturnsEmptyList()
    {
        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity>().BuildMock());

        var result = await _service.GetAllSubUnitsByParentIdAsync(999, CancellationToken.None);

        Assert.Empty(result);
    }

    #endregion

    #region GetSubUnitDetailsByIdAsync

    [Fact]
    public async Task GetSubUnitDetailsByIdAsync_NonPositiveAssetId_ThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.GetSubUnitDetailsByIdAsync(0, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubUnitDetailsByIdAsync_AssetNotFound_ThrowsKeyNotFoundException()
    {
        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity>().BuildMock());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GetSubUnitDetailsByIdAsync(123, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubUnitDetailsByIdAsync_MarkedForDeletion_ThrowsKeyNotFoundException()
    {
        var asset = NewAsset(5, markedForDeletion: true);
        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { asset }.BuildMock());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GetSubUnitDetailsByIdAsync(5, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubUnitDetailsByIdAsync_IsInventoryData_ThrowsKeyNotFoundException()
    {
        var asset = NewAsset(5);
        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { asset }.BuildMock());
        _inventoryAssetDetailRepo.Setup(r => r.GetQueryable())
            .Returns(new List<InventoryAssetDetailEntity> { new() { Id = 1, AssetId = 5 } }.BuildMock());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GetSubUnitDetailsByIdAsync(5, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubUnitDetailsByIdAsync_Success_ReturnsRoomWiseRenterAndFloorDetailsWithResolvedNames()
    {
        var parent = NewAsset(1, assetNo: "BLDG-1", assetName: "Building");
        var asset = NewAsset(5, parentAssetId: 1, assetNo: "A-5", assetName: "Flat 105");
        asset.ParentAsset = parent;

        // GetFloorDetailsForSubAssetAsync resolves TypeOfUseName/SubTypeOfUseName from the
        // SubUnitsDetailsEntity.TypeOfUse/.SubTypeOfUse navigation properties (Core's own
        // TypeOfUseEntity/SubTypeOfUseEntity) directly -- not via the AMS lookup repositories.
        var floor = NewFloorDetails(20, assetId: 1, typeOfUseId: 7, subTypeOfUseId: 8);
        floor.TypeOfUse = new TypeOfUseEntity { Id = 7, Description = "Residential" };
        floor.SubTypeOfUse = new SubTypeOfUseEntity { Id = 8, Description = "Owner Occupied" };
        var roomWise = new AssetRoomWiseSubmissionDetailsEntity { Id = 30, AssetId = 5, SubUnitsDetailsId = 20, RoomNo = "1" };
        var renter = new AssetLeaseRentDetailsEntity { Id = 40, AssetId = 5, TenantName = "John Doe" };

        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { asset }.BuildMock());
        _roomWiseRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetRoomWiseSubmissionDetailsEntity> { roomWise }.BuildMock());
        _leaseRentRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetLeaseRentDetailsEntity> { renter }.BuildMock());
        _floorDetailsRepo.Setup(r => r.GetQueryable()).Returns(new List<SubUnitsDetailsEntity> { floor }.BuildMock());

        var result = await _service.GetSubUnitDetailsByIdAsync(5, CancellationToken.None);

        Assert.Equal(5, result.Id);
        Assert.Equal("A-5", result.AssetNo);
        Assert.Single(result.RoomWiseSubmissions);
        Assert.Single(result.RenterDetails);
        Assert.Single(result.FloorDetails);
        Assert.Equal("Residential", result.TypeOfUseName);
        Assert.Equal("Owner Occupied", result.SubTypeOfUseName);
    }

    #endregion

    #region BulkGenerateChildAssetsAsync

    [Fact]
    public async Task BulkGenerateChildAssetsAsync_ParentNotFound_ReturnsErrorWithoutThrowing()
    {
        var dto = new BulkGenerateChildAssetsDto { ParentAssetId = 99, Type = "Flat", Count = 2 };

        var result = await _service.BulkGenerateChildAssetsAsync(dto, CancellationToken.None);

        Assert.Equal(0, result.TotalGenerated);
        Assert.Contains(result.Errors, e => e.Contains("Parent asset with Id 99 not found"));
    }

    [Fact]
    public async Task BulkGenerateChildAssetsAsync_FloorDetailsIdProvidedButNotFound_ReturnsError()
    {
        var parent = NewAsset(1);
        _assetRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parent);
        _floorDetailsRepo.Setup(r => r.GetByIdAsync(500, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubUnitsDetailsEntity?)null);

        var dto = new BulkGenerateChildAssetsDto { ParentAssetId = 1, Type = "Flat", Count = 2, FloorDetailsId = 500 };

        var result = await _service.BulkGenerateChildAssetsAsync(dto, CancellationToken.None);

        Assert.Equal(0, result.TotalGenerated);
        Assert.Contains(result.Errors, e => e.Contains("Floor details with Id 500 not found"));
    }

    [Fact]
    public async Task BulkGenerateChildAssetsAsync_WithoutFloor_GeneratesAssetsWithoutRoomWiseRecords()
    {
        var parent = NewAsset(1);
        _assetRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parent);

        var dto = new BulkGenerateChildAssetsDto { ParentAssetId = 1, Type = "Flat", Count = 3 };

        var result = await _service.BulkGenerateChildAssetsAsync(dto, CancellationToken.None);

        Assert.Empty(result.Errors);
        Assert.Equal(3, result.TotalGenerated);
        Assert.Equal(3, result.GeneratedAssets.Count);
        Assert.All(result.GeneratedAssets, g => Assert.Null(g.RoomWiseSubmissionDetailsId));
        _roomWiseRepo.Verify(r => r.AddAsync(It.IsAny<AssetRoomWiseSubmissionDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkGenerateChildAssetsAsync_WithFloor_GeneratesAssetsAndRoomWiseRecords()
    {
        var parent = NewAsset(1);
        var floor = NewFloorDetails(500, assetId: 1);
        _assetRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parent);
        _floorDetailsRepo.Setup(r => r.GetByIdAsync(500, It.IsAny<CancellationToken>())).ReturnsAsync(floor);

        var dto = new BulkGenerateChildAssetsDto { ParentAssetId = 1, Type = "Shop", Count = 2, FloorDetailsId = 500 };

        var result = await _service.BulkGenerateChildAssetsAsync(dto, CancellationToken.None);

        Assert.Empty(result.Errors);
        Assert.Equal(2, result.TotalGenerated);
        Assert.All(result.GeneratedAssets, g => Assert.NotNull(g.RoomWiseSubmissionDetailsId));
        _roomWiseRepo.Verify(r => r.AddAsync(It.IsAny<AssetRoomWiseSubmissionDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion

    #region BulkGenerateAcrossFloorsAsync

    [Fact]
    public async Task BulkGenerateAcrossFloorsAsync_EmptyFloorIds_ReturnsError()
    {
        var dto = new BulkGenerateAcrossFloorsDto { ParentAssetId = 1, Type = "Shop", FloorIds = new List<int>(), UnitsPerFloor = 2 };

        var result = await _service.BulkGenerateAcrossFloorsAsync(dto, CancellationToken.None);

        Assert.Contains(result.Errors, e => e.Contains("At least one FloorId is required"));
    }

    [Fact]
    public async Task BulkGenerateAcrossFloorsAsync_UnitsPerFloorLessThanOne_ReturnsError()
    {
        var dto = new BulkGenerateAcrossFloorsDto { ParentAssetId = 1, Type = "Shop", FloorIds = new List<int> { 1 }, UnitsPerFloor = 0 };

        var result = await _service.BulkGenerateAcrossFloorsAsync(dto, CancellationToken.None);

        Assert.Contains(result.Errors, e => e.Contains("UnitsPerFloor must be at least 1"));
    }

    [Fact]
    public async Task BulkGenerateAcrossFloorsAsync_ParentNotFound_ReturnsError()
    {
        var dto = new BulkGenerateAcrossFloorsDto { ParentAssetId = 99, Type = "Shop", FloorIds = new List<int> { 1 }, UnitsPerFloor = 2 };

        var result = await _service.BulkGenerateAcrossFloorsAsync(dto, CancellationToken.None);

        Assert.Contains(result.Errors, e => e.Contains("Parent asset with Id 99 not found"));
    }

    [Fact]
    public async Task BulkGenerateAcrossFloorsAsync_Success_GeneratesOneAssetAndOneSubUnitsDetailsPerFloorUnit()
    {
        var parent = NewAsset(1);
        _assetRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parent);

        var dto = new BulkGenerateAcrossFloorsDto
        {
            ParentAssetId = 1,
            Type = "Shop",
            FloorIds = new List<int> { 10, 20 },
            UnitsPerFloor = 3,
            ConstructionTypeId = 1,
            TypeOfUseId = 1
        };

        var result = await _service.BulkGenerateAcrossFloorsAsync(dto, CancellationToken.None);

        Assert.Empty(result.Errors);
        Assert.Equal(6, result.TotalGenerated);
        Assert.Equal(6, result.GeneratedAssets.Count);
        _assetRepo.Verify(r => r.AddAsync(It.IsAny<AssetMasterEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(6));
        _subUnitsDetailsRepo.Verify(r => r.AddAsync(It.IsAny<SubUnitsDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(6));
    }

    [Fact]
    public async Task BulkGenerateAcrossFloorsAsync_ExceptionMidLoop_RollsBackAndRethrows()
    {
        var parent = NewAsset(1);
        _assetRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parent);
        _subUnitsDetailsRepo.Setup(r => r.AddAsync(It.IsAny<SubUnitsDetailsEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db failure"));

        var dto = new BulkGenerateAcrossFloorsDto { ParentAssetId = 1, Type = "Shop", FloorIds = new List<int> { 10 }, UnitsPerFloor = 1 };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.BulkGenerateAcrossFloorsAsync(dto, CancellationToken.None));
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CreateChildAssetAsync

    [Fact]
    public async Task CreateChildAssetAsync_ParentNotFound_ReturnsFailureWithoutThrowing()
    {
        var dto = new CreateChildAssetDto { ParentAssetId = 99, AssetId = 1 };

        var result = await _service.CreateChildAssetAsync(dto, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Parent asset with Id 99 not found", result.Message);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateChildAssetAsync_ChildAssetNotFound_ReturnsFailureWithoutThrowing()
    {
        var parent = NewAsset(1);
        _assetRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parent);
        _assetRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync((AssetMasterEntity?)null);

        var dto = new CreateChildAssetDto { ParentAssetId = 1, AssetId = 2 };

        var result = await _service.CreateChildAssetAsync(dto, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Asset with Id 2 not found", result.Message);
    }

    [Fact]
    public async Task CreateChildAssetAsync_NoExistingSubUnitsDetails_CreatesNewRow()
    {
        var parent = NewAsset(1);
        var child = NewAsset(2, parentAssetId: 1, assetNo: "A-2");
        _assetRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parent);
        _assetRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(child);
        _floorDetailsRepo.Setup(r => r.GetQueryable()).Returns(new List<SubUnitsDetailsEntity>().BuildMock());

        var dto = new CreateChildAssetDto { ParentAssetId = 1, AssetId = 2, UnitNo = "101", CarpetAreaSqFeet = 500 };

        var result = await _service.CreateChildAssetAsync(dto, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.SubUnitsDetailsId);
        _floorDetailsRepo.Verify(r => r.AddAsync(It.IsAny<SubUnitsDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _floorDetailsRepo.Verify(r => r.UpdateAsync(It.IsAny<SubUnitsDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateChildAssetAsync_ExistingSubUnitsDetailsOnDefaultFloor_UpdatesRow()
    {
        var parent = NewAsset(1);
        var child = NewAsset(2, parentAssetId: 1, assetNo: "A-2");
        var existingFloorDetail = NewFloorDetails(50, assetId: 2, floorId: 1);
        _assetRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parent);
        _assetRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(child);
        _floorDetailsRepo.Setup(r => r.GetQueryable()).Returns(new List<SubUnitsDetailsEntity> { existingFloorDetail }.BuildMock());

        var dto = new CreateChildAssetDto { ParentAssetId = 1, AssetId = 2, CarpetAreaSqFeet = 300 };

        var result = await _service.CreateChildAssetAsync(dto, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(50, result.SubUnitsDetailsId);
        _floorDetailsRepo.Verify(r => r.AddAsync(It.IsAny<SubUnitsDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _floorDetailsRepo.Verify(r => r.UpdateAsync(It.IsAny<SubUnitsDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateChildAssetAsync_RoomDetailsWithoutValidDimensions_AreSkipped()
    {
        var parent = NewAsset(1);
        var child = NewAsset(2, parentAssetId: 1);
        _assetRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parent);
        _assetRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(child);

        var dto = new CreateChildAssetDto
        {
            ParentAssetId = 1,
            AssetId = 2,
            RoomDetails = new List<RoomDetailDto>
            {
                new() { LengthMtr = 3, WidthMtr = 4 },              // valid
                new() { LengthMtr = null, WidthMtr = null, AreaSqMtr = null, HeightMtr = null } // invalid -> skipped
            }
        };

        var result = await _service.CreateChildAssetAsync(dto, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.RoomWiseSubmissionDetailsId);
        _roomWiseRepo.Verify(r => r.AddAsync(It.IsAny<AssetRoomWiseSubmissionDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateChildAssetAsync_DeletesExistingRoomWiseAndMinusDataBeforeRegenerating()
    {
        var parent = NewAsset(1);
        var child = NewAsset(2, parentAssetId: 1);
        var existingRoom = new AssetRoomWiseSubmissionDetailsEntity { Id = 77, AssetId = 2 };
        _assetRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parent);
        _assetRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(child);
        _roomWiseRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetRoomWiseSubmissionDetailsEntity> { existingRoom }.BuildMock());
        _minusRepo.Setup(r => r.GetQueryable())
            .Returns(new List<AssetRoomWiseMinusDataEntity> { new() { Id = 5, RoomWiseSubmissionId = 77 } }.BuildMock());

        var dto = new CreateChildAssetDto
        {
            ParentAssetId = 1,
            AssetId = 2,
            RoomDetails = new List<RoomDetailDto> { new() { LengthMtr = 2, WidthMtr = 2 } }
        };

        await _service.CreateChildAssetAsync(dto, CancellationToken.None);

        _minusRepo.Verify(r => r.DeleteAsync(5, It.IsAny<CancellationToken>()), Times.Once);
        _roomWiseRepo.Verify(r => r.DeleteAsync(77, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateChildAssetAsync_RentInformationProvided_CreatesLeaseRentDetailsWithLeaseType()
    {
        var parent = NewAsset(1);
        var child = NewAsset(2, parentAssetId: 1);
        _assetRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parent);
        _assetRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(child);

        AssetLeaseRentDetailsEntity? captured = null;
        _leaseRentRepo.Setup(r => r.AddAsync(It.IsAny<AssetLeaseRentDetailsEntity>(), It.IsAny<CancellationToken>()))
            .Callback<AssetLeaseRentDetailsEntity, CancellationToken>((e, _) => captured = e)
            .Returns((AssetLeaseRentDetailsEntity e, CancellationToken _) => { e.Id = 900; return Task.FromResult(e); });

        var dto = new CreateChildAssetDto
        {
            ParentAssetId = 1,
            AssetId = 2,
            RenterName = "Jane Doe",
            MobileNo = "9999999999",
            RentInformation = new RentInformationDto { LeaseRentType = "Lease Deed", RentAmount = 5000 }
        };

        var result = await _service.CreateChildAssetAsync(dto, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(900, result.RenterDetailsId);
        Assert.NotNull(captured);
        Assert.Equal("Lease", captured!.LeaseType);
        Assert.Equal(5000, captured.RentAmount);
    }

    [Fact]
    public async Task CreateChildAssetAsync_RentInformationWithRentType_SetsLeaseTypeToRent()
    {
        var parent = NewAsset(1);
        var child = NewAsset(2, parentAssetId: 1);
        _assetRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parent);
        _assetRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(child);

        AssetLeaseRentDetailsEntity? captured = null;
        _leaseRentRepo.Setup(r => r.AddAsync(It.IsAny<AssetLeaseRentDetailsEntity>(), It.IsAny<CancellationToken>()))
            .Callback<AssetLeaseRentDetailsEntity, CancellationToken>((e, _) => captured = e)
            .Returns((AssetLeaseRentDetailsEntity e, CancellationToken _) => { e.Id = 901; return Task.FromResult(e); });

        var dto = new CreateChildAssetDto
        {
            ParentAssetId = 1,
            AssetId = 2,
            RentInformation = new RentInformationDto { LeaseRentType = "Monthly Rent" }
        };

        await _service.CreateChildAssetAsync(dto, CancellationToken.None);

        Assert.Equal("Rent", captured!.LeaseType);
    }

    [Fact]
    public async Task CreateChildAssetAsync_PhotoFilesProvided_CreatesPhotoWithSubUnitDetailsIdAndUploadsDocument()
    {
        var parent = NewAsset(1);
        var child = NewAsset(2, parentAssetId: 1);
        _assetRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parent);
        _assetRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(child);

        int? capturedSubUnitId = null;
        _assetPhotoService
            .Setup(p => p.CreateAsync(2, 1, It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, int?, int?, string?, int, CancellationToken>((_, _, subUnitId, _, _, _, _) => capturedSubUnitId = subUnitId)
            .ReturnsAsync(777);

        var file = NewFormFile("front.jpg", "image/jpeg");
        var dto = new CreateChildAssetDto
        {
            ParentAssetId = 1,
            AssetId = 2,
            PhotoFiles = new List<IFormFile> { file.Object },
            PhotoMetadataJson = "[{\"photoTypeId\":1,\"displayOrder\":1,\"remarks\":\"Front\"}]"
        };

        var result = await _service.CreateChildAssetAsync(dto, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(result.SubUnitsDetailsId, capturedSubUnitId);
        _documentApplicationService.Verify(d => d.UploadDocumentAsync(
            It.IsAny<Stream>(), "front.jpg", "image/jpeg", It.IsAny<long>(),
            It.Is<DocumentUploadDto>(u => u.ReferenceTableName == "AssetPhoto" && u.ReferenceTableId == 777),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateChildAssetAsync_PhotoUploadThrows_IsNonFatalAndResponseStillSucceeds()
    {
        var parent = NewAsset(1);
        var child = NewAsset(2, parentAssetId: 1);
        _assetRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parent);
        _assetRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(child);
        _documentApplicationService
            .Setup(d => d.UploadDocumentAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                It.IsAny<DocumentUploadDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("storage unavailable"));

        var file = NewFormFile("front.jpg", "image/jpeg");
        var dto = new CreateChildAssetDto
        {
            ParentAssetId = 1,
            AssetId = 2,
            PhotoFiles = new List<IFormFile> { file.Object },
            PhotoMetadataJson = "[{\"photoTypeId\":1}]"
        };

        var result = await _service.CreateChildAssetAsync(dto, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Document upload encountered an error"));
    }

    [Fact]
    public async Task CreateChildAssetAsync_UnexpectedException_RollsBackAndReturnsFailure()
    {
        var parent = NewAsset(1);
        var child = NewAsset(2, parentAssetId: 1);
        _assetRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parent);
        _assetRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(child);
        _assetRepo.Setup(r => r.UpdateAsync(It.IsAny<AssetMasterEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db write failure"));

        var dto = new CreateChildAssetDto { ParentAssetId = 1, AssetId = 2 };

        var result = await _service.CreateChildAssetAsync(dto, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Error while updating child asset", result.Message);
        _unitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateChildAssetAsync_DoesNotDeleteAnySubUnitsDetailsRows()
    {
        // Regression guard: a previous "STEP 4" deleted the parent asset's SubUnitsDetails row
        // for this floor, which corrupted sibling units sharing the same floor (floor details are
        // parent-owned and read by GetFloorDetailsForSubAssetAsync for every unit on that floor).
        var parent = NewAsset(1);
        var child = NewAsset(2, parentAssetId: 1);
        var siblingsFloorDetail = NewFloorDetails(999, assetId: 1, floorId: 1);
        _assetRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(parent);
        _assetRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(child);
        _floorDetailsRepo.Setup(r => r.GetQueryable()).Returns(new List<SubUnitsDetailsEntity> { siblingsFloorDetail }.BuildMock());

        var dto = new CreateChildAssetDto { ParentAssetId = 1, AssetId = 2 };

        await _service.CreateChildAssetAsync(dto, CancellationToken.None);

        _floorDetailsRepo.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetChildAssetByIdAsync

    [Fact]
    public async Task GetChildAssetByIdAsync_AssetNotFound_ReturnsFailure()
    {
        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity>().BuildMock());

        var result = await _service.GetChildAssetByIdAsync(999, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Asset with Id 999 not found", result.Message);
    }

    [Fact]
    public async Task GetChildAssetByIdAsync_Success_ReturnsRoomWiseAndRenterDetails()
    {
        var asset = NewAsset(2);
        var roomWise = new AssetRoomWiseSubmissionDetailsEntity { Id = 1, AssetId = 2, RoomNo = "1" };
        var renter = new AssetLeaseRentDetailsEntity { Id = 2, AssetId = 2, TenantName = "Alice" };

        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { asset }.BuildMock());
        _roomWiseRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetRoomWiseSubmissionDetailsEntity> { roomWise }.BuildMock());
        _leaseRentRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetLeaseRentDetailsEntity> { renter }.BuildMock());

        var result = await _service.GetChildAssetByIdAsync(2, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(result.RoomWiseDetails!);
        Assert.NotNull(result.RenterDetails);
        Assert.Equal("Alice", result.RenterDetails!.RenterName);
    }

    #endregion

    #region GetSubUnitsByAssetIdAsync

    [Fact]
    public async Task GetSubUnitsByAssetIdAsync_ReturnsFloorDetailsIdAndAreaFromSubUnitsDetails()
    {
        var child = NewAsset(2, parentAssetId: 1, assetName: "Flat Unit");
        var floor = NewFloorDetails(30, assetId: 2);
        floor.CarpetAreaSqFeet = 450;

        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { child }.BuildMock());
        _floorDetailsRepo.Setup(r => r.GetQueryable()).Returns(new List<SubUnitsDetailsEntity> { floor }.BuildMock());

        var result = await _service.GetSubUnitsByAssetIdAsync(1, CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(30, dto.FloorDetailsId);
        Assert.Equal(450, dto.TotalAreaSqFt);
    }

    [Fact]
    public async Task GetSubUnitsByAssetIdAsync_DraftRoomWiseRecord_ResolvesUnitTypeFromRoomType()
    {
        // The draft record created by BulkGenerateChildAssetsAsync has RoomType holding the unit
        // type ("Flat"/"Shop") and no RoomNo — real, user-submitted rooms always get a RoomNo.
        var child = NewAsset(2, parentAssetId: 1, assetName: "Flat Unit");
        var draftRoomWise = new AssetRoomWiseSubmissionDetailsEntity { Id = 1, AssetId = 2, RoomNo = null, RoomType = "Shop" };

        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { child }.BuildMock());
        _roomWiseRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetRoomWiseSubmissionDetailsEntity> { draftRoomWise }.BuildMock());

        var result = await _service.GetSubUnitsByAssetIdAsync(1, CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal("Shop", dto.UnitType);
    }

    [Fact]
    public async Task GetSubUnitsByAssetIdAsync_OnlyRealRoomsExist_FallsBackToAssetNamePrefix()
    {
        // Once real rooms are configured (RoomNo populated), the draft-only lookup finds nothing
        // and the unit type falls back to the AssetName prefix.
        var child = NewAsset(2, parentAssetId: 1, assetName: "Flat Unit");
        var realRoom = new AssetRoomWiseSubmissionDetailsEntity { Id = 1, AssetId = 2, RoomNo = "1", RoomType = "Bed Room" };

        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { child }.BuildMock());
        _roomWiseRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetRoomWiseSubmissionDetailsEntity> { realRoom }.BuildMock());

        var result = await _service.GetSubUnitsByAssetIdAsync(1, CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal("Flat", dto.UnitType);
    }

    [Fact]
    public async Task GetSubUnitsByAssetIdAsync_NoRoomWiseData_DerivesUnitTypeFromAssetNamePrefix()
    {
        var child = NewAsset(2, parentAssetId: 1, assetName: "Flat 202");
        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { child }.BuildMock());

        var result = await _service.GetSubUnitsByAssetIdAsync(1, CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal("Flat", dto.UnitType);
    }

    [Fact]
    public async Task GetSubUnitsByAssetIdAsync_ExcludesInventoryAssets()
    {
        var child = NewAsset(2, parentAssetId: 1);
        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { child }.BuildMock());
        _inventoryAssetDetailRepo.Setup(r => r.GetQueryable())
            .Returns(new List<InventoryAssetDetailEntity> { new() { Id = 1, AssetId = 2 } }.BuildMock());

        var result = await _service.GetSubUnitsByAssetIdAsync(1, CancellationToken.None);

        Assert.Empty(result);
    }

    #endregion

    #region GetSubUnitLeaseRentBySubUnitDetailsIdAsync

    [Fact]
    public async Task GetSubUnitLeaseRentBySubUnitDetailsIdAsync_ResolvesDirectlyByAssetId()
    {
        var asset = NewAsset(5, assetNo: "A-5", assetName: "Flat 105");
        var floor = NewFloorDetails(10, assetId: 5);
        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { asset }.BuildMock());
        _floorDetailsRepo.Setup(r => r.GetQueryable()).Returns(new List<SubUnitsDetailsEntity> { floor }.BuildMock());

        var result = await _service.GetSubUnitLeaseRentBySubUnitDetailsIdAsync(5, CancellationToken.None);

        Assert.Equal(5, result.AssetId);
        Assert.Equal(10, result.SubUnitDetailsId);
    }

    [Fact]
    public async Task GetSubUnitLeaseRentBySubUnitDetailsIdAsync_FallsBackToSubUnitsDetailsAssetId_WhenAssetIdIsActuallyAFloorDetailId()
    {
        // No AssetMaster row with Id == 10, but a SubUnitsDetails row with Id == 10 points to AssetId 5.
        var asset = NewAsset(5, assetNo: "A-5");
        var floor = NewFloorDetails(10, assetId: 5);
        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { asset }.BuildMock());
        _floorDetailsRepo.Setup(r => r.GetQueryable()).Returns(new List<SubUnitsDetailsEntity> { floor }.BuildMock());

        var result = await _service.GetSubUnitLeaseRentBySubUnitDetailsIdAsync(10, CancellationToken.None);

        Assert.Equal(5, result.AssetId);
        Assert.Equal(10, result.SubUnitDetailsId);
    }

    [Fact]
    public async Task GetSubUnitLeaseRentBySubUnitDetailsIdAsync_NeitherAssetNorFloorFound_ThrowsKeyNotFound()
    {
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GetSubUnitLeaseRentBySubUnitDetailsIdAsync(999, CancellationToken.None));
        Assert.Contains("999", ex.Message);
    }

    [Fact]
    public async Task GetSubUnitLeaseRentBySubUnitDetailsIdAsync_AssetFoundButNoFloorDetails_ThrowsKeyNotFound()
    {
        var asset = NewAsset(5);
        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { asset }.BuildMock());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GetSubUnitLeaseRentBySubUnitDetailsIdAsync(5, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubUnitLeaseRentBySubUnitDetailsIdAsync_PhotoLookupThrows_IsSwallowedAndPhotosEmpty()
    {
        var asset = NewAsset(5);
        var floor = NewFloorDetails(10, assetId: 5);
        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { asset }.BuildMock());
        _floorDetailsRepo.Setup(r => r.GetQueryable()).Returns(new List<SubUnitsDetailsEntity> { floor }.BuildMock());
        _assetPhotoService.Setup(p => p.GetLatestByAssetIdAsync(5, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("photo service down"));

        var result = await _service.GetSubUnitLeaseRentBySubUnitDetailsIdAsync(5, CancellationToken.None);

        Assert.Empty(result.Photos);
    }

    [Fact]
    public async Task GetSubUnitLeaseRentBySubUnitDetailsIdAsync_FiltersPhotosBySubUnitAndRemarks()
    {
        var asset = NewAsset(5);
        var floor = NewFloorDetails(10, assetId: 5);
        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { asset }.BuildMock());
        _floorDetailsRepo.Setup(r => r.GetQueryable()).Returns(new List<SubUnitsDetailsEntity> { floor }.BuildMock());

        var matchingImage = AssetPhotoEntity.Create(5, 1, subUnitDetailsId: 10, displayOrder: 1, remarks: "Asset Image");
        var matchingPlan = AssetPhotoEntity.Create(5, 1, subUnitDetailsId: 10, displayOrder: 2, remarks: "Asset Photo Plan");
        var wrongSubUnit = AssetPhotoEntity.Create(5, 1, subUnitDetailsId: 999, displayOrder: 3, remarks: "Asset Image");
        var wrongRemark = AssetPhotoEntity.Create(5, 1, subUnitDetailsId: 10, displayOrder: 4, remarks: "Something Else");
        var assetLevelPhoto = AssetPhotoEntity.Create(5, 1, subUnitDetailsId: null, displayOrder: 5, remarks: "Asset Image");

        _assetPhotoService.Setup(p => p.GetLatestByAssetIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AssetPhotoEntity> { matchingImage, matchingPlan, wrongSubUnit, wrongRemark, assetLevelPhoto });

        var result = await _service.GetSubUnitLeaseRentBySubUnitDetailsIdAsync(5, CancellationToken.None);

        Assert.Equal(2, result.Photos.Count);
        Assert.All(result.Photos, p => Assert.Contains(p.Remarks, new[] { "Asset Image", "Asset Photo Plan" }));
    }

    #endregion

    #region GetSubUnitsCompleteDetailsByParentIdAsync

    [Fact]
    public async Task GetSubUnitsCompleteDetailsByParentIdAsync_NoChildAssets_ReturnsEmptyList()
    {
        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity>().BuildMock());

        var result = await _service.GetSubUnitsCompleteDetailsByParentIdAsync(1, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSubUnitsCompleteDetailsByParentIdAsync_ReturnsGroupedFloorRoomWiseAndMinusDetails()
    {
        var child = NewAsset(2, parentAssetId: 1, assetNo: "A-2");
        var floor = NewFloorDetails(10, assetId: 2, typeOfUseId: 3, subTypeOfUseId: 4);
        var room = new AssetRoomWiseSubmissionDetailsEntity { Id = 20, AssetId = 2, RoomNo = "1" };
        var minus = new AssetRoomWiseMinusDataEntity { Id = 30, RoomWiseSubmissionId = 20 };

        _assetRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetMasterEntity> { child }.BuildMock());
        _floorDetailsRepo.Setup(r => r.GetQueryable()).Returns(new List<SubUnitsDetailsEntity> { floor }.BuildMock());
        _roomWiseRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetRoomWiseSubmissionDetailsEntity> { room }.BuildMock());
        _minusRepo.Setup(r => r.GetQueryable()).Returns(new List<AssetRoomWiseMinusDataEntity> { minus }.BuildMock());
        _amsTypeOfUseRepo.Setup(r => r.GetQueryable())
            .Returns(new List<AssetTypeOfUseMasterEntity> { new() { Id = 3, Description = "Residential" } }.BuildMock());
        _amsSubTypeOfUseRepo.Setup(r => r.GetQueryable())
            .Returns(new List<AssetSubTypeOfUseEntity> { new() { Id = 4, Description = "Owner Occupied" } }.BuildMock());

        var result = await _service.GetSubUnitsCompleteDetailsByParentIdAsync(1, CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(2, dto.Id);
        var floorDetail = Assert.Single(dto.FloorDetails);
        Assert.Equal("Residential", floorDetail.TypeOfUseName);
        Assert.Equal("Owner Occupied", floorDetail.SubTypeOfUseName);
        var roomDetail = Assert.Single(dto.RoomWiseDetails);
        var minusDetail = Assert.Single(roomDetail.MinusDetails);
        Assert.Equal(30, minusDetail.Id);
    }

    #endregion
}
