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
/// Covers <c>AssetCapitalValueService.MovableAssets.cs</c>: <c>CalculateMovableAssetCVAsync</c>,
/// the entry point behind
/// <see cref="Api.Controllers.Asset_Management.AssetCapitalValueController.CalculateMovableAssetCV"/>.
/// Only <c>_assetRepository</c>/<c>_unitOfWork</c> are exercised — the movable-asset calculation is
/// pure computation over the loaded <c>AssetMasterEntity</c>, no other master data involved.
/// </summary>
public class AssetCapitalValueServiceMovableAssetsTests
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

    private static AssetMasterEntity MovableAsset(int id, decimal? purchaseValue, DateTime? purchaseDate) => new()
    {
        Id = id,
        AssetNo = $"M-{id}",
        AssetName = $"Movable {id}",
        PurchaseValue = purchaseValue,
        PurchaseDate = purchaseDate
    };

    /// <summary>
    /// A PurchaseDate that reliably yields AgeInYears == <paramref name="years"/> from the service's
    /// <c>(int)(age.TotalDays / 365.25)</c> calculation. A plain <c>DateTime.Now.AddYears(-years)</c>
    /// is NOT safe here: calendar years vary between 365-366 days, so for many "years" values
    /// AddYears(-years) lands short of years*365.25 days and truncates down to years-1. The 20-day
    /// buffer pushes comfortably past the years*365.25 threshold without reaching (years+1)*365.25.
    /// </summary>
    private static DateTime PurchaseDateYearsAgo(int years) => DateTime.Now.AddDays(-(365.25 * years + 20));

    #endregion

    [Fact]
    public async Task CalculateMovableAssetCVAsync_AssetNotFound_ThrowsInvalidOperationException()
    {
        var mocks = new ServiceMocks();
        var service = CreateService(mocks);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CalculateMovableAssetCVAsync(new CalculateMovableAssetCVRequestDto { AssetId = 999 }, CancellationToken.None));
    }

    [Fact]
    public async Task CalculateMovableAssetCVAsync_DepreciatedValue_ComputesExpectedCapitalValue()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, MovableAsset(1, purchaseValue: 100000m, purchaseDate: PurchaseDateYearsAgo(3)));
        var service = CreateService(mocks);

        var result = await service.CalculateMovableAssetCVAsync(new CalculateMovableAssetCVRequestDto
        {
            AssetId = 1,
            ValuationMethod = MovableAssetValuationMethod.DepreciatedValue,
            ConditionFactor = 1.0m
        }, CancellationToken.None);

        // Default depreciation rate 10%/yr * 3 years = 30% total depreciation.
        Assert.True(result.IsCalculated);
        Assert.Equal(3, result.AgeInYears);
        Assert.Equal(0.30m, result.AccumulatedDepreciation!.Value / 100000m);
        Assert.Equal(70000m, result.CapitalValue); // 100000 * (1 - 0.3) * 1.0
        // CalculateMovableAssetCV only builds a DTO — it never mutates asset or writes a history row,
        // so there is nothing to save. SaveChangesAsync must not be called.
        mocks.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CalculateMovableAssetCVAsync_DepreciatedValue_CustomRateOverridesDefault()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, MovableAsset(1, purchaseValue: 100000m, purchaseDate: PurchaseDateYearsAgo(2)));
        var service = CreateService(mocks);

        var result = await service.CalculateMovableAssetCVAsync(new CalculateMovableAssetCVRequestDto
        {
            AssetId = 1,
            ValuationMethod = MovableAssetValuationMethod.DepreciatedValue,
            CustomDepreciationRate = 0.2m,
            ConditionFactor = 1.0m
        }, CancellationToken.None);

        Assert.Equal(60000m, result.CapitalValue); // 100000 * (1 - 0.2*2)
    }

    [Fact]
    public async Task CalculateMovableAssetCVAsync_DepreciatedValue_CapsDepreciationAtNinetyPercent()
    {
        var mocks = new ServiceMocks();
        // 20 years * 10%/yr = 200% -> capped at 90%.
        SetRows(mocks.AssetRepository, MovableAsset(1, purchaseValue: 100000m, purchaseDate: PurchaseDateYearsAgo(20)));
        var service = CreateService(mocks);

        var result = await service.CalculateMovableAssetCVAsync(new CalculateMovableAssetCVRequestDto
        {
            AssetId = 1,
            ValuationMethod = MovableAssetValuationMethod.DepreciatedValue,
            ConditionFactor = 1.0m
        }, CancellationToken.None);

        Assert.Equal(0.1m, result.DepreciationFactor); // 1 - 0.9 cap
        Assert.Equal(10000m, result.CapitalValue); // 100000 * 0.1
    }

    [Fact]
    public async Task CalculateMovableAssetCVAsync_DepreciatedValue_MissingPurchaseValue_ReturnsUncalculated()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, MovableAsset(1, purchaseValue: null, purchaseDate: PurchaseDateYearsAgo(1)));
        var service = CreateService(mocks);

        var result = await service.CalculateMovableAssetCVAsync(new CalculateMovableAssetCVRequestDto
        {
            AssetId = 1,
            ValuationMethod = MovableAssetValuationMethod.DepreciatedValue
        }, CancellationToken.None);

        Assert.False(result.IsCalculated);
        Assert.Contains("Purchase value is required", result.CalculationMessage);
    }

    [Fact]
    public async Task CalculateMovableAssetCVAsync_MarketValue_AlwaysReturnsNotSet()
    {
        // AssetMasterEntity has no market-value/appraisal column to read from, so this method
        // unconditionally reports "not set" regardless of input.
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, MovableAsset(1, purchaseValue: 50000m, purchaseDate: PurchaseDateYearsAgo(1)));
        var service = CreateService(mocks);

        var result = await service.CalculateMovableAssetCVAsync(new CalculateMovableAssetCVRequestDto
        {
            AssetId = 1,
            ValuationMethod = MovableAssetValuationMethod.MarketValue
        }, CancellationToken.None);

        Assert.False(result.IsCalculated);
        Assert.Contains("Market value is not set", result.CalculationMessage);
    }

    [Fact]
    public async Task CalculateMovableAssetCVAsync_BookValue_WithPurchaseValue_ComputesDepreciatedBookValue()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, MovableAsset(1, purchaseValue: 100000m, purchaseDate: PurchaseDateYearsAgo(2)));
        var service = CreateService(mocks);

        var result = await service.CalculateMovableAssetCVAsync(new CalculateMovableAssetCVRequestDto
        {
            AssetId = 1,
            ValuationMethod = MovableAssetValuationMethod.BookValue
        }, CancellationToken.None);

        Assert.True(result.IsCalculated);
        Assert.Equal(80000m, result.CapitalValue); // 100000 * (1 - 0.1*2)
    }

    [Fact]
    public async Task CalculateMovableAssetCVAsync_BookValue_WithoutPurchaseValue_ReturnsUncalculated()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, MovableAsset(1, purchaseValue: null, purchaseDate: null));
        var service = CreateService(mocks);

        var result = await service.CalculateMovableAssetCVAsync(new CalculateMovableAssetCVRequestDto
        {
            AssetId = 1,
            ValuationMethod = MovableAssetValuationMethod.BookValue
        }, CancellationToken.None);

        Assert.False(result.IsCalculated);
        Assert.Contains("Neither book value nor purchase value is set", result.CalculationMessage);
    }

    [Fact]
    public async Task CalculateMovableAssetCVAsync_ReplacementCost_AppliesInflationAndCondition()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, MovableAsset(1, purchaseValue: 100000m, purchaseDate: PurchaseDateYearsAgo(5)));
        var service = CreateService(mocks);

        var result = await service.CalculateMovableAssetCVAsync(new CalculateMovableAssetCVRequestDto
        {
            AssetId = 1,
            ValuationMethod = MovableAssetValuationMethod.ReplacementCost,
            ConditionFactor = 0.8m
        }, CancellationToken.None);

        // inflationFactor = 1 + 0.03*5 = 1.15; CV = 100000 * 1.15 * 0.8
        Assert.True(result.IsCalculated);
        Assert.Equal(92000m, result.CapitalValue);
    }

    [Fact]
    public async Task CalculateMovableAssetCVAsync_ReplacementCost_MissingPurchaseValue_ReturnsUncalculated()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, MovableAsset(1, purchaseValue: null, purchaseDate: PurchaseDateYearsAgo(1)));
        var service = CreateService(mocks);

        var result = await service.CalculateMovableAssetCVAsync(new CalculateMovableAssetCVRequestDto
        {
            AssetId = 1,
            ValuationMethod = MovableAssetValuationMethod.ReplacementCost
        }, CancellationToken.None);

        Assert.False(result.IsCalculated);
        Assert.Contains("Purchase value is required", result.CalculationMessage);
    }

    [Theory]
    [InlineData(1.0, "Excellent")]
    [InlineData(0.9, "Excellent")]
    [InlineData(0.8, "Good")]
    [InlineData(0.7, "Good")]
    [InlineData(0.6, "Fair")]
    [InlineData(0.5, "Fair")]
    [InlineData(0.4, "Poor")]
    [InlineData(0.3, "Poor")]
    [InlineData(0.2, "Very Poor")]
    public async Task CalculateMovableAssetCVAsync_ConditionFactor_MapsToExpectedDescription(double conditionFactor, string expectedDescription)
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, MovableAsset(1, purchaseValue: 10000m, purchaseDate: PurchaseDateYearsAgo(1)));
        var service = CreateService(mocks);

        var result = await service.CalculateMovableAssetCVAsync(new CalculateMovableAssetCVRequestDto
        {
            AssetId = 1,
            ValuationMethod = MovableAssetValuationMethod.DepreciatedValue,
            ConditionFactor = (decimal)conditionFactor
        }, CancellationToken.None);

        Assert.Equal(expectedDescription, result.ConditionDescription);
    }

    [Fact]
    public async Task CalculateMovableAssetCVAsync_NoPurchaseDate_LeavesAgeAtZero()
    {
        var mocks = new ServiceMocks();
        SetRows(mocks.AssetRepository, MovableAsset(1, purchaseValue: 10000m, purchaseDate: null));
        var service = CreateService(mocks);

        var result = await service.CalculateMovableAssetCVAsync(new CalculateMovableAssetCVRequestDto
        {
            AssetId = 1,
            ValuationMethod = MovableAssetValuationMethod.DepreciatedValue
        }, CancellationToken.None);

        Assert.Equal(0, result.AgeInYears);
        Assert.Equal(0, result.AgeInMonths);
    }
}
