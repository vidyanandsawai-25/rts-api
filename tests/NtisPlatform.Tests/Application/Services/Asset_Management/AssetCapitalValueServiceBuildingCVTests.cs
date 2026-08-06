using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.AssetCapitalValue;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.Services.Asset_Management;

/// <summary>
/// Covers <c>AssetCapitalValueService.BuildingCV.cs</c>: <c>CalculateAsync</c>,
/// <c>CalculateBuildingCVAsync</c>, and <c>GetParentAssetValuationAsync</c> — the three
/// entry points <see cref="Api.Controllers.Asset_Management.AssetCapitalValueController"/>
/// exposes for floor/building CV. Exercises the master-data lookup dictionaries added by the
/// DSA optimization pass indirectly (every computed CapitalValue below depends on them
/// resolving the same value the old FirstOrDefault scans would have).
/// </summary>
public class AssetCapitalValueServiceBuildingCVTests
{
    #region Mock wiring

    private sealed class ServiceMocks
    {
        public readonly Mock<IRepository<AssetMasterEntity, long>> AssetRepository = new();
        public readonly Mock<IRepository<SubUnitsDetailsEntity, long>> AssetFloorRepository = new();
        public readonly Mock<IRepository<CVRateMasterEntity, int>> RateRepository = new();
        public readonly Mock<IRepository<AssetNatureFactorCVMasterEntity, int>> NatureFactorRepository = new();
        public readonly Mock<IRepository<AssetUseFactorCVMasterEntity, int>> UseFactorRepository = new();
        public readonly Mock<IRepository<AssetAgeFactorCVMasterEntity, int>> AgeFactorRepository = new();
        public readonly Mock<IRepository<AssetFloorFactorCVEntity, int>> FloorFactorRepository = new();
        public readonly Mock<IRepository<AssetAssessmentYearRangeMasterCVEntity, int>> AssessmentYearRangeRepository = new();
        public readonly Mock<IRepository<AssetTypeOfUseMasterEntity, int>> TypeOfUseRepository = new();
        public readonly Mock<IRepository<AssetTypeOfUseGroupEntity, int>> TypeOfUseGroupRepository = new();
        public readonly Mock<IRepository<CSNDetailsEntity, int>> CsnDetailsRepository = new();
        public readonly Mock<IRepository<AssetRoomWiseSubmissionDetailsEntity, int>> RoomDetailsRepository = new();
        public readonly Mock<IRepository<AssetCVCalculationHistoryEntity, int>> HistoryRepository = new();
        public readonly Mock<IRepository<AssetDetailsEntity, int>> DetailsRepository = new();
        public readonly Mock<IRepository<InventoryBatchEntity, int>> InventoryBatchRepository = new();
        public readonly Mock<IRepository<InventoryAssetDetailEntity, int>> InventoryAssetDetailRepository = new();
        public readonly Mock<IUnitOfWork> UnitOfWork = new();
        public readonly Mock<IMapper> Mapper = new();
        public readonly Mock<ILogger<AssetCapitalValueService>> Logger = new();

        public ServiceMocks()
        {
            // Sensible empty defaults for every repository LoadMasterDataAsync touches (and every
            // other repo the constructor requires) so no path NREs on an unconfigured GetQueryable().
            SetEmpty(AssetRepository);
            SetEmpty(AssetFloorRepository);
            SetEmpty(RateRepository);
            SetEmpty(NatureFactorRepository);
            SetEmpty(UseFactorRepository);
            SetEmpty(AgeFactorRepository);
            SetEmpty(FloorFactorRepository);
            SetEmpty(AssessmentYearRangeRepository);
            SetEmpty(TypeOfUseRepository);
            SetEmpty(TypeOfUseGroupRepository);
            SetEmpty(CsnDetailsRepository);
            SetEmpty(RoomDetailsRepository);
            SetEmpty(HistoryRepository);
            SetEmpty(DetailsRepository);
            SetEmpty(InventoryBatchRepository);
            SetEmpty(InventoryAssetDetailRepository);
        }

        private static void SetEmpty<T, TKey>(Mock<IRepository<T, TKey>> mock) where T : class
            => mock.Setup(r => r.GetQueryable()).Returns(new List<T>().BuildMockDbSet().Object);
    }

    private static void SetRows<T, TKey>(Mock<IRepository<T, TKey>> mock, params T[] rows) where T : class
        => mock.Setup(r => r.GetQueryable()).Returns(rows.ToList().BuildMockDbSet().Object);

    private static AssetCapitalValueService CreateService(ServiceMocks m) => new(
        m.AssetRepository.Object,
        m.AssetFloorRepository.Object,
        m.RateRepository.Object,
        m.NatureFactorRepository.Object,
        m.UseFactorRepository.Object,
        m.AgeFactorRepository.Object,
        m.FloorFactorRepository.Object,
        m.AssessmentYearRangeRepository.Object,
        m.TypeOfUseRepository.Object,
        m.TypeOfUseGroupRepository.Object,
        m.CsnDetailsRepository.Object,
        m.RoomDetailsRepository.Object,
        m.HistoryRepository.Object,
        m.DetailsRepository.Object,
        m.InventoryBatchRepository.Object,
        m.InventoryAssetDetailRepository.Object,
        m.UnitOfWork.Object,
        m.Mapper.Object,
        m.Logger.Object);

    #endregion

    #region Entity builders

    private static AssetMasterEntity Asset(int id, int? parentAssetId = null, string assetNo = "A-1") => new()
    {
        Id = id,
        AssetNo = assetNo,
        AssetName = $"Asset {id}",
        ParentAssetId = parentAssetId,
        HierarchyLevel = parentAssetId == null ? 0 : 1
    };

    /// <summary>A floor detail wired to fully calculate: FloorId/ConstructionTypeId/TypeOfUseId = 1,
    /// 25-year-old RCC construction, 100 sq.m carpet area, no sub-type-of-use.</summary>
    private static SubUnitsDetailsEntity CalculableFloorDetail(int id, int assetId) => new()
    {
        Id = id,
        AssetId = assetId,
        FloorId = 1,
        ConstructionTypeId = 1,
        TypeOfUseId = 1,
        SubTypeOfUseId = null,
        ConstructionYear = "2000",
        AssessmentYear = "2025",
        CarpetAreaSqMeter = 100m,
        Floor = new FloorEntity { Id = 1, FloorGroupId = null, Description = "Ground" },
        ConstructionType = new ConstructionTypeEntity { Description = "RCC" }
    };

    private static AssetAssessmentYearRangeMasterCVEntity YearRange(int id = 1) => new()
    {
        Id = id,
        FromYear = 2020,
        ToYear = 2030,
        IsActive = true
    };

    private static AssetTypeOfUseMasterEntity TypeOfUse(int id = 1, int typeOfUseGroupId = 1) => new()
    {
        Id = id,
        TypeOfUseGroupId = typeOfUseGroupId,
        Description = "Residential",
        IsActive = true
    };

    private static AssetTypeOfUseGroupEntity TypeOfUseGroup(int id = 1, bool isFloorWiseRateApplicable = false) => new()
    {
        Id = id,
        IsFloorWiseRateApplicable = isFloorWiseRateApplicable,
        IsActive = true
    };

    private static CVRateMasterEntity RateMaster(int id, int yearRangeId, int typeOfUseGroupId, int? floorGroupId, decimal rateAmount) => new()
    {
        Id = id,
        AssessmentYearRangeId = yearRangeId,
        TypeOfUseGroupCVId = typeOfUseGroupId,
        FloorGroupId = floorGroupId,
        RateAmount = rateAmount,
        IsActive = true
    };

    private static AssetNatureFactorCVMasterEntity NatureFactor(int constructionTypeId, int yearRangeId, decimal factor) => new()
    {
        ConstructionTypeId = constructionTypeId,
        YearRangeCVId = yearRangeId,
        Factor = factor,
        IsActive = true
    };

    private static AssetAgeFactorCVMasterEntity AgeFactor(int constructionTypeId, int yearRangeId, int ageFrom, int ageTo, decimal factor) => new()
    {
        ConstructionTypeId = constructionTypeId,
        YearRangeCVId = yearRangeId,
        AgeFrom = ageFrom,
        AgeTo = ageTo,
        Factor = factor,
        IsActive = true
    };

    private static AssetFloorFactorCVEntity FloorFactor(int floorId, int yearRangeId, decimal factorWithLift, decimal factorWithoutLift) => new()
    {
        FloorId = floorId,
        YearRangeCVId = yearRangeId,
        FactorWithLift = factorWithLift,
        FactorWithoutLift = factorWithoutLift
    };

    #endregion

    #region CalculateAsync Tests

    [Fact]
    public async Task CalculateAsync_AssetNotFound_ThrowsInvalidOperationException()
    {
        var mocks = new ServiceMocks();
        var service = CreateService(mocks);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CalculateAsync(new CalculateAssetCVRequestDto { AssetId = 999 }, CancellationToken.None));
    }

    [Fact]
    public async Task CalculateAsync_NoFloorDetails_ReturnsZeroedSummary()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, Asset(1));
        var service = CreateService(mocks);

        var result = await service.CalculateAsync(new CalculateAssetCVRequestDto { AssetId = 1 }, CancellationToken.None);

        Assert.Equal(0, result.FloorDetailsCount);
        Assert.Equal(0m, result.TotalCapitalValue);
    }

    [Fact]
    public async Task CalculateAsync_FullyConfiguredMasterData_ComputesExpectedCapitalValue()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, Asset(1));
        SetRows(mocks.AssetFloorRepository, CalculableFloorDetail(10, 1));
        SetRows(mocks.AssessmentYearRangeRepository, YearRange());
        SetRows(mocks.TypeOfUseRepository, TypeOfUse());
        SetRows(mocks.TypeOfUseGroupRepository, TypeOfUseGroup(isFloorWiseRateApplicable: false));
        SetRows(mocks.RateRepository, RateMaster(1, yearRangeId: 1, typeOfUseGroupId: 1, floorGroupId: null, rateAmount: 1000m));
        SetRows(mocks.NatureFactorRepository, NatureFactor(1, 1, 1.1m));
        SetRows(mocks.AgeFactorRepository, AgeFactor(1, 1, 0, 50, 0.9m));
        SetRows(mocks.FloorFactorRepository, FloorFactor(1, 1, factorWithLift: 1.5m, factorWithoutLift: 1.05m));
        var service = CreateService(mocks);

        var result = await service.CalculateAsync(new CalculateAssetCVRequestDto { AssetId = 1, SubUnitsDetailsId = 0 }, CancellationToken.None);

        // BaseValue = 1000 * 100 = 100000; CV = 100000 * 1.1(nature) * 1(use) * 0.9(age) * 1.05(floor, no-lift)
        Assert.Equal(103950m, result.TotalCapitalValue);
        var detail = Assert.Single(result.FloorDetails);
        Assert.True(detail.IsCalculated);
        Assert.Equal(1.1m, detail.CVNatureFactor);
        Assert.Equal(0.9m, detail.CVAgeFactor);
        Assert.Equal(1.05m, detail.CVFloorFactor); // hasLift is always false on this path -> FactorWithoutLift
        mocks.HistoryRepository.Verify(r => r.AddAsync(It.IsAny<AssetCVCalculationHistoryEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CalculateAsync_RateFallback_UsesRelaxedFloorGroupMatch()
    {
        // Use-group is floor-wise, but the seeded rate has a different FloorGroupId than the floor's
        // own FloorGroupId -> exact match misses, relaxed (year range + use group only) match hits.
        var mocks = new ServiceMocks();
        var floorDetail = CalculableFloorDetail(10, 1);
        floorDetail.Floor!.FloorGroupId = 5;

        SetRows(mocks.AssetRepository, Asset(1));
        SetRows(mocks.AssetFloorRepository, floorDetail);
        SetRows(mocks.AssessmentYearRangeRepository, YearRange());
        SetRows(mocks.TypeOfUseRepository, TypeOfUse());
        SetRows(mocks.TypeOfUseGroupRepository, TypeOfUseGroup(isFloorWiseRateApplicable: true));
        SetRows(mocks.RateRepository, RateMaster(1, yearRangeId: 1, typeOfUseGroupId: 1, floorGroupId: 999, rateAmount: 500m));
        SetRows(mocks.FloorFactorRepository, FloorFactor(1, 1, 1m, 1m));
        var service = CreateService(mocks);

        var result = await service.CalculateAsync(new CalculateAssetCVRequestDto { AssetId = 1 }, CancellationToken.None);

        var detail = Assert.Single(result.FloorDetails);
        Assert.True(detail.IsCalculated);
        Assert.Equal(500m, detail.CVBaseRate);
    }

    [Fact]
    public async Task CalculateAsync_NoMatchingRate_MarksFloorDetailUncalculated()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, Asset(1));
        SetRows(mocks.AssetFloorRepository, CalculableFloorDetail(10, 1));
        SetRows(mocks.AssessmentYearRangeRepository, YearRange());
        SetRows(mocks.TypeOfUseRepository, TypeOfUse());
        SetRows(mocks.TypeOfUseGroupRepository, TypeOfUseGroup());
        // No rate masters seeded at all.
        var service = CreateService(mocks);

        var result = await service.CalculateAsync(new CalculateAssetCVRequestDto { AssetId = 1 }, CancellationToken.None);

        var detail = Assert.Single(result.FloorDetails);
        Assert.False(detail.IsCalculated);
        Assert.Contains("Rate not found", detail.CalculationMessage);
        Assert.Equal(0m, result.TotalCapitalValue);
    }

    [Fact]
    public async Task CalculateAsync_RateMasterWithNullRateAmount_MarksFloorDetailUncalculated_DoesNotThrow()
    {
        // Regression test: RateAmount is nullable on CVRateMasterEntity. A seeded rate row with no
        // amount set must fail the calculation gracefully, not throw an InvalidCastException.
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, Asset(1));
        SetRows(mocks.AssetFloorRepository, CalculableFloorDetail(10, 1));
        SetRows(mocks.AssessmentYearRangeRepository, YearRange());
        SetRows(mocks.TypeOfUseRepository, TypeOfUse());
        SetRows(mocks.TypeOfUseGroupRepository, TypeOfUseGroup());
        SetRows(mocks.RateRepository, new CVRateMasterEntity
        {
            Id = 1, AssessmentYearRangeId = 1, TypeOfUseGroupCVId = 1, FloorGroupId = null, RateAmount = null, IsActive = true
        });
        var service = CreateService(mocks);

        var result = await service.CalculateAsync(new CalculateAssetCVRequestDto { AssetId = 1 }, CancellationToken.None);

        var detail = Assert.Single(result.FloorDetails);
        Assert.False(detail.IsCalculated);
        Assert.Contains("Rate amount not set", detail.CalculationMessage);
        Assert.Equal(0m, result.TotalCapitalValue);
    }

    [Fact]
    public async Task CalculateAsync_RateMasterWithZeroRateAmount_MarksFloorDetailUncalculated()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, Asset(1));
        SetRows(mocks.AssetFloorRepository, CalculableFloorDetail(10, 1));
        SetRows(mocks.AssessmentYearRangeRepository, YearRange());
        SetRows(mocks.TypeOfUseRepository, TypeOfUse());
        SetRows(mocks.TypeOfUseGroupRepository, TypeOfUseGroup());
        SetRows(mocks.RateRepository, RateMaster(1, yearRangeId: 1, typeOfUseGroupId: 1, floorGroupId: null, rateAmount: 0m));
        var service = CreateService(mocks);

        var result = await service.CalculateAsync(new CalculateAssetCVRequestDto { AssetId = 1 }, CancellationToken.None);

        var detail = Assert.Single(result.FloorDetails);
        Assert.False(detail.IsCalculated);
        Assert.Contains("Rate amount not set", detail.CalculationMessage);
    }

    [Fact]
    public async Task CalculateAsync_InvalidAssessmentYear_MarksFloorDetailUncalculated()
    {
        var mocks = new ServiceMocks();
        var floorDetail = CalculableFloorDetail(10, 1);
        floorDetail.AssessmentYear = "not-a-year";

        SetRows(mocks.AssetRepository, Asset(1));
        SetRows(mocks.AssetFloorRepository, floorDetail);
        var service = CreateService(mocks);

        var result = await service.CalculateAsync(new CalculateAssetCVRequestDto { AssetId = 1 }, CancellationToken.None);

        var detail = Assert.Single(result.FloorDetails);
        Assert.False(detail.IsCalculated);
        Assert.Contains("Invalid assessment year", detail.CalculationMessage);
    }

    [Fact]
    public async Task CalculateAsync_MissingCarpetArea_MarksFloorDetailUncalculated()
    {
        var mocks = new ServiceMocks();
        var floorDetail = CalculableFloorDetail(10, 1);
        floorDetail.CarpetAreaSqMeter = null;

        SetRows(mocks.AssetRepository, Asset(1));
        SetRows(mocks.AssetFloorRepository, floorDetail);
        var service = CreateService(mocks);

        var result = await service.CalculateAsync(new CalculateAssetCVRequestDto { AssetId = 1 }, CancellationToken.None);

        var detail = Assert.Single(result.FloorDetails);
        Assert.False(detail.IsCalculated);
        Assert.Contains("Invalid carpet area", detail.CalculationMessage);
    }

    [Fact]
    public async Task CalculateAsync_YearRangeNotSeeded_MarksFloorDetailUncalculated()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, Asset(1));
        SetRows(mocks.AssetFloorRepository, CalculableFloorDetail(10, 1));
        // No year ranges seeded -> assessmentYear 2025 matches nothing.
        var service = CreateService(mocks);

        var result = await service.CalculateAsync(new CalculateAssetCVRequestDto { AssetId = 1 }, CancellationToken.None);

        var detail = Assert.Single(result.FloorDetails);
        Assert.False(detail.IsCalculated);
        Assert.Contains("Year range not found", detail.CalculationMessage);
    }

    [Fact]
    public async Task CalculateAsync_MissingNatureAndAgeFactors_DefaultToOne()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, Asset(1));
        SetRows(mocks.AssetFloorRepository, CalculableFloorDetail(10, 1));
        SetRows(mocks.AssessmentYearRangeRepository, YearRange());
        SetRows(mocks.TypeOfUseRepository, TypeOfUse());
        SetRows(mocks.TypeOfUseGroupRepository, TypeOfUseGroup());
        SetRows(mocks.RateRepository, RateMaster(1, 1, 1, null, 1000m));
        // No nature/age/floor factor rows seeded at all.
        var service = CreateService(mocks);

        var result = await service.CalculateAsync(new CalculateAssetCVRequestDto { AssetId = 1 }, CancellationToken.None);

        var detail = Assert.Single(result.FloorDetails);
        Assert.True(detail.IsCalculated);
        Assert.Equal(1m, detail.CVNatureFactor);
        Assert.Equal(1m, detail.CVAgeFactor);
        Assert.Equal(1m, detail.CVFloorFactor);
        Assert.Equal(100000m, detail.CapitalValue); // 1000 * 100 * 1 * 1 * 1 * 1
    }

    [Fact]
    public async Task CalculateAsync_RoomWiseFallback_ComputesCarpetAreaFromRooms()
    {
        var mocks = new ServiceMocks();
        var floorDetail = CalculableFloorDetail(10, 1);
        floorDetail.CarpetAreaSqMeter = null; // forces the room-wise fallback

        SetRows(mocks.AssetRepository, Asset(1));
        SetRows(mocks.AssetFloorRepository, floorDetail);
        SetRows(mocks.RoomDetailsRepository, new AssetRoomWiseSubmissionDetailsEntity
        {
            SubUnitsDetailsId = 10,
            AreaSqMtr = 40d,
            MinusYesNo = false,
            OuterYesNo = false
        });
        SetRows(mocks.AssessmentYearRangeRepository, YearRange());
        SetRows(mocks.TypeOfUseRepository, TypeOfUse());
        SetRows(mocks.TypeOfUseGroupRepository, TypeOfUseGroup());
        SetRows(mocks.RateRepository, RateMaster(1, 1, 1, null, 1000m));
        var service = CreateService(mocks);

        var result = await service.CalculateAsync(new CalculateAssetCVRequestDto { AssetId = 1 }, CancellationToken.None);

        var detail = Assert.Single(result.FloorDetails);
        Assert.True(detail.IsCalculated);
        Assert.Equal(40m, detail.CarpetAreaSqMeter);
        Assert.Equal(40000m, detail.CapitalValue); // 1000 * 40
    }

    [Fact]
    public async Task CalculateAsync_IncludeChildAssets_ReturnedSummaryIsParentOnly()
    {
        // CalculateAsync's returned result is computed from the PARENT asset alone, before the
        // children loop runs — IncludeChildAssets calculates and persists CV for each child too,
        // but the returned AssetCVSummaryDto never aggregates them. This test pins that behavior so
        // it isn't accidentally "fixed" as a side effect of an unrelated change.
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, Asset(1), Asset(2, parentAssetId: 1));
        SetRows(mocks.AssetFloorRepository, CalculableFloorDetail(10, 1), CalculableFloorDetail(11, 2));
        SetRows(mocks.AssessmentYearRangeRepository, YearRange());
        SetRows(mocks.TypeOfUseRepository, TypeOfUse());
        SetRows(mocks.TypeOfUseGroupRepository, TypeOfUseGroup());
        SetRows(mocks.RateRepository, RateMaster(1, 1, 1, null, 1000m));
        var service = CreateService(mocks);

        var result = await service.CalculateAsync(
            new CalculateAssetCVRequestDto { AssetId = 1, IncludeChildAssets = true }, CancellationToken.None);

        Assert.Equal(100000m, result.TotalCapitalValue); // parent's own floor detail only
        Assert.Single(result.FloorDetails);
    }

    #endregion

    #region CalculateBuildingCVAsync Tests

    [Fact]
    public async Task CalculateBuildingCVAsync_BuildingNotFound_ThrowsInvalidOperationException()
    {
        var mocks = new ServiceMocks();
        var service = CreateService(mocks);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CalculateBuildingCVAsync(new CalculateBuildingCVRequestDto { BuildingAssetId = 999 }, CancellationToken.None));
    }

    [Fact]
    public async Task CalculateBuildingCVAsync_WithChildAssets_AggregatesTotals()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, Asset(1), Asset(2, parentAssetId: 1), Asset(3, parentAssetId: 1));
        SetRows(mocks.AssetFloorRepository,
            CalculableFloorDetail(10, 1), CalculableFloorDetail(11, 2), CalculableFloorDetail(12, 3));
        SetRows(mocks.AssessmentYearRangeRepository, YearRange());
        SetRows(mocks.TypeOfUseRepository, TypeOfUse());
        SetRows(mocks.TypeOfUseGroupRepository, TypeOfUseGroup());
        SetRows(mocks.RateRepository, RateMaster(1, 1, 1, null, 1000m));
        var service = CreateService(mocks);

        var result = await service.CalculateBuildingCVAsync(
            new CalculateBuildingCVRequestDto { BuildingAssetId = 1 }, CancellationToken.None);

        Assert.Equal(2, result.TotalChildAssets);
        Assert.Equal(2, result.CalculatedChildAssets);
        Assert.Equal(100000m, result.BuildingOwnCapitalValue);
        Assert.Equal(200000m, result.ChildAssetsCapitalValue); // 2 children * 100000
        Assert.Equal(300000m, result.TotalBuildingCapitalValue);
        Assert.True(result.IsFullyCalculated);
        mocks.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CalculateBuildingCVAsync_OneChildUncalculated_ReportsPartialCompletion()
    {
        var mocks = new ServiceMocks();
        var badChild = CalculableFloorDetail(11, 2);
        badChild.CarpetAreaSqMeter = null; // forces this child's only floor detail to fail

        SetRows(mocks.AssetRepository, Asset(1), Asset(2, parentAssetId: 1));
        SetRows(mocks.AssetFloorRepository, CalculableFloorDetail(10, 1), badChild);
        SetRows(mocks.AssessmentYearRangeRepository, YearRange());
        SetRows(mocks.TypeOfUseRepository, TypeOfUse());
        SetRows(mocks.TypeOfUseGroupRepository, TypeOfUseGroup());
        SetRows(mocks.RateRepository, RateMaster(1, 1, 1, null, 1000m));
        var service = CreateService(mocks);

        var result = await service.CalculateBuildingCVAsync(
            new CalculateBuildingCVRequestDto { BuildingAssetId = 1 }, CancellationToken.None);

        Assert.Equal(1, result.TotalChildAssets);
        Assert.Equal(0, result.CalculatedChildAssets);
        Assert.False(result.IsFullyCalculated);
        Assert.Contains("0/1", result.CalculationMessage);
    }

    [Fact]
    public async Task CalculateBuildingCVAsync_NoChildAssets_ReflectsBuildingOwnCVOnly()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, Asset(1));
        SetRows(mocks.AssetFloorRepository, CalculableFloorDetail(10, 1));
        SetRows(mocks.AssessmentYearRangeRepository, YearRange());
        SetRows(mocks.TypeOfUseRepository, TypeOfUse());
        SetRows(mocks.TypeOfUseGroupRepository, TypeOfUseGroup());
        SetRows(mocks.RateRepository, RateMaster(1, 1, 1, null, 1000m));
        var service = CreateService(mocks);

        var result = await service.CalculateBuildingCVAsync(
            new CalculateBuildingCVRequestDto { BuildingAssetId = 1 }, CancellationToken.None);

        Assert.Equal(0, result.TotalChildAssets);
        Assert.Equal(100000m, result.TotalBuildingCapitalValue);
        Assert.True(result.IsFullyCalculated);
    }

    #endregion

    #region GetParentAssetValuationAsync Tests

    [Fact]
    public async Task GetParentAssetValuationAsync_ParentNotFound_ReturnsNull()
    {
        var mocks = new ServiceMocks();
        var service = CreateService(mocks);

        var result = await service.GetParentAssetValuationAsync(999, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetParentAssetValuationAsync_WithSubUnitsAndInventory_SumsAllThreeComponents()
    {
        var mocks = new ServiceMocks();
        var parent = Asset(1);
        var child = Asset(2, parentAssetId: 1);
        var parentFloorDetail = CalculableFloorDetail(10, 1);
        parentFloorDetail.CapitalValue = 50000m;
        var childFloorDetail = CalculableFloorDetail(11, 2);
        childFloorDetail.CapitalValue = 30000m;

        SetRows(mocks.AssetRepository, parent, child);
        SetRows(mocks.AssetFloorRepository, parentFloorDetail, childFloorDetail);
        SetRows(mocks.InventoryBatchRepository, new InventoryBatchEntity
        {
            Id = 1,
            ParentAssetId = 1,
            IsActive = true,
            TotalBatchCV = 5000m,
            Quantity = 3
        });
        var service = CreateService(mocks);

        var result = await service.GetParentAssetValuationAsync(1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(50000m, result!.BaseValue);
        Assert.Equal(30000m, result.SubUnitsCapitalValue);
        Assert.Equal(1, result.SubUnitsCount);
        Assert.Equal(5000m, result.InventoryCapitalValue);
        Assert.Equal(1, result.InventoryBatchesCount);
        Assert.Equal(3, result.TotalInventoryCount);
        Assert.Equal(85000m, result.TotalCapitalValue); // 50000 + 30000 + 5000
    }

    [Fact]
    public async Task GetParentAssetValuationAsync_ChildWithInventoryAssigned_ExcludedFromSubUnits()
    {
        // A child asset that has its own InventoryAssetDetail rows is an inventory-tracked item,
        // not a sub-unit -- it must not be double counted as a "sub-unit".
        var mocks = new ServiceMocks();
        var parent = Asset(1);
        var childWithInventory = Asset(2, parentAssetId: 1);

        SetRows(mocks.AssetRepository, parent, childWithInventory);
        SetRows(mocks.AssetFloorRepository, CalculableFloorDetail(10, 1));
        SetRows(mocks.InventoryAssetDetailRepository, new InventoryAssetDetailEntity { AssetId = 2, BatchId = 1 });
        var service = CreateService(mocks);

        var result = await service.GetParentAssetValuationAsync(1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0, result!.SubUnitsCount);
        Assert.Equal(0m, result.SubUnitsCapitalValue);
    }

    #endregion
}
