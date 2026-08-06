using AutoMapper;
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
/// Covers <c>AssetCapitalValueService.OpenPlot.cs</c>: <c>CalculatePlotCVAsync</c> and
/// <c>GetPlotCVAsync</c>, the entry point behind
/// <see cref="Api.Controllers.Asset_Management.AssetCapitalValueController.CalculatePlotCV"/>.
/// </summary>
public class AssetCapitalValueServiceOpenPlotTests
{
    #region Mock wiring (mirrors AssetCapitalValueServiceBuildingCVTests)

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

    private static AssetMasterEntity Asset(int id) => new() { Id = id, AssetNo = $"P-{id}", AssetName = $"Plot {id}" };

    private static AssetAssessmentYearRangeMasterCVEntity YearRange(int id = 1) => new()
    {
        Id = id,
        FromYear = 2020,
        ToYear = 2030,
        IsActive = true
    };

    #endregion

    [Fact]
    public async Task CalculatePlotCVAsync_AssetNotFound_ThrowsInvalidOperationException()
    {
        var mocks = new ServiceMocks();
        var service = CreateService(mocks);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CalculatePlotCVAsync(new CalculatePlotCVRequestDto { AssetId = 999 }, CancellationToken.None));
    }

    [Fact]
    public async Task CalculatePlotCVAsync_NoLandArea_ReturnsUncalculatedSummary()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, Asset(1));
        SetRows(mocks.DetailsRepository, new AssetDetailsEntity { AssetId = 1, LandAreaSqMeter = null });
        var service = CreateService(mocks);

        var result = await service.CalculatePlotCVAsync(new CalculatePlotCVRequestDto { AssetId = 1 }, CancellationToken.None);

        Assert.Equal(0, result.CalculatedPlots);
        Assert.False(result.IsFullyCalculated);
        var detail = Assert.Single(result.PlotDetails);
        Assert.False(detail.IsCalculated);
        Assert.Contains("No land area found", detail.CalculationMessage);
    }

    [Fact]
    public async Task CalculatePlotCVAsync_NoAssetDetailsRow_TreatsAreaAsZero()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, Asset(1));
        // No AssetDetails row at all for AssetId 1.
        var service = CreateService(mocks);

        var result = await service.CalculatePlotCVAsync(new CalculatePlotCVRequestDto { AssetId = 1 }, CancellationToken.None);

        Assert.Equal(0, result.CalculatedPlots);
    }

    [Fact]
    public async Task CalculatePlotCVAsync_ValidAreaAndExactRate_ComputesCapitalValue()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, Asset(1));
        SetRows(mocks.DetailsRepository, new AssetDetailsEntity { AssetId = 1, LandAreaSqMeter = 200m });
        SetRows(mocks.AssessmentYearRangeRepository, YearRange());
        SetRows(mocks.RateRepository, new CVRateMasterEntity
        {
            Id = 1, AssessmentYearRangeId = 1, TypeOfUseGroupCVId = 0, FloorGroupId = 0, RateAmount = 500m, IsActive = true
        });
        var service = CreateService(mocks);

        var result = await service.CalculatePlotCVAsync(new CalculatePlotCVRequestDto { AssetId = 1 }, CancellationToken.None);

        Assert.Equal(1, result.CalculatedPlots);
        Assert.True(result.IsFullyCalculated);
        Assert.Equal(100000m, result.TotalCapitalValue); // 500 * 200
        mocks.HistoryRepository.Verify(r => r.AddAsync(It.IsAny<AssetCVCalculationHistoryEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        mocks.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CalculatePlotCVAsync_NoExactRate_FallsBackToAnyRateInSameYearRange()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, Asset(1));
        SetRows(mocks.DetailsRepository, new AssetDetailsEntity { AssetId = 1, LandAreaSqMeter = 50m });
        SetRows(mocks.AssessmentYearRangeRepository, YearRange());
        // Doesn't match TypeOfUseGroupCVId=0/FloorGroupId=0, but same year range -> fallback applies.
        SetRows(mocks.RateRepository, new CVRateMasterEntity
        {
            Id = 1, AssessmentYearRangeId = 1, TypeOfUseGroupCVId = 5, FloorGroupId = 3, RateAmount = 800m, IsActive = true
        });
        var service = CreateService(mocks);

        var result = await service.CalculatePlotCVAsync(new CalculatePlotCVRequestDto { AssetId = 1 }, CancellationToken.None);

        Assert.Equal(1, result.CalculatedPlots);
        Assert.Equal(40000m, result.TotalCapitalValue); // 800 * 50
    }

    [Fact]
    public async Task CalculatePlotCVAsync_NoRateAtAll_ReturnsUncalculatedSummary()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, Asset(1));
        SetRows(mocks.DetailsRepository, new AssetDetailsEntity { AssetId = 1, LandAreaSqMeter = 50m });
        SetRows(mocks.AssessmentYearRangeRepository, YearRange());
        // No rate masters seeded.
        var service = CreateService(mocks);

        var result = await service.CalculatePlotCVAsync(new CalculatePlotCVRequestDto { AssetId = 1 }, CancellationToken.None);

        Assert.Equal(0, result.CalculatedPlots);
        var detail = Assert.Single(result.PlotDetails);
        Assert.False(detail.IsCalculated);
        Assert.Contains("No rate found", detail.CalculationMessage);
    }

    [Fact]
    public async Task CalculatePlotCVAsync_NoActiveYearRange_ThrowsInvalidOperationException()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, Asset(1));
        SetRows(mocks.DetailsRepository, new AssetDetailsEntity { AssetId = 1, LandAreaSqMeter = 50m });
        // No year ranges seeded at all.
        var service = CreateService(mocks);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CalculatePlotCVAsync(new CalculatePlotCVRequestDto { AssetId = 1 }, CancellationToken.None));
    }

    #region GetPlotCVAsync Tests

    [Fact]
    public async Task GetPlotCVAsync_AssetNotFound_ReturnsNull()
    {
        var mocks = new ServiceMocks();
        var service = CreateService(mocks);

        var result = await service.GetPlotCVAsync(999, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPlotCVAsync_AssetFound_ReturnsUncalculatedPlaceholder()
    {
        // Pre-existing anomaly (documented in AssetCapitalValueService.OPTIMIZATION.md): this method
        // hardcodes area=0 and hasCV=false regardless of any stored data — it never actually reads
        // AMS.AssetDetails/SubUnitsDetails. Pinned here so a future change to this behavior is a
        // deliberate, visible decision rather than an accidental regression.
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, Asset(1));
        var service = CreateService(mocks);

        var result = await service.GetPlotCVAsync(1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0, result!.CalculatedPlots);
        Assert.False(result.PlotDetails.Single().IsCalculated);
    }

    #endregion
}
