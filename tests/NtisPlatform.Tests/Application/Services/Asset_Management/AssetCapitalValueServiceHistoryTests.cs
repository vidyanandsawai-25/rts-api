using AutoMapper;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.Mappings.Asset_Management;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.Services.Asset_Management;

/// <summary>
/// Covers <c>AssetCapitalValueService.History.cs</c>: <c>GetCalculationHistoryAsync</c>. Uses a REAL
/// AutoMapper instance built from the actual <see cref="AssetCVCalculationHistoryMappingProfile"/> —
/// not a mocked <c>IMapper</c> — because the bug this guards against (no <c>CreateMap</c> existed for
/// <c>AssetCVCalculationHistoryEntity</c> -&gt; <c>AssetCVCalculationHistoryDto</c>, so the
/// <c>_mapper.Map(...)</c> call threw <c>AutoMapperMappingException</c> at runtime) is invisible to a
/// mocked mapper, which would happily return whatever you tell it to regardless of configuration.
/// </summary>
public class AssetCapitalValueServiceHistoryTests
{
    #region Mock wiring (mirrors AssetCapitalValueServiceBuildingCVTests, but with a real IMapper)

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
        public readonly Mock<ILogger<AssetCapitalValueService>> Logger = new();
        public IMapper Mapper = null!;

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
            Mapper = CreateMapper();
        }

        private static void SetEmpty<T, TKey>(Mock<IRepository<T, TKey>> mock) where T : class
            => mock.Setup(r => r.GetQueryable()).Returns(new List<T>().BuildMockDbSet().Object);
    }

    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<AssetCVCalculationHistoryMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        return config.CreateMapper();
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
        m.Mapper,
        m.Logger.Object);

    #endregion

    [Fact]
    public async Task GetCalculationHistoryAsync_WithHistoryRows_MapsSuccessfully()
    {
        // Regression test for the missing AssetCVCalculationHistoryMappingProfile: before it existed,
        // this call threw AutoMapperMappingException instead of returning mapped DTOs.
        var mocks = new ServiceMocks();
        var asset = new AssetMasterEntity { Id = 1, AssetNo = "A-1", AssetName = "Test Asset" };
        var history = new AssetCVCalculationHistoryEntity
        {
            Id = 1,
            AssetId = 1,
            CalculationDate = new DateTime(2026, 1, 1),
            FinancialYear = "2025-26",
            FloorId = 3,
            BaseRate = 1000m,
            CapitalValue = 50000m,
            AssetMaster = asset
        };
        SetRows(mocks.HistoryRepository, history);
        SetRows(mocks.AssetFloorRepository, new SubUnitsDetailsEntity
        {
            Id = 3,
            AssetId = 1,
            FloorId = 3,
            Floor = new FloorEntity { Id = 3, Description = "Second Floor" }
        });
        var service = CreateService(mocks);

        var result = await service.GetCalculationHistoryAsync(1, CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(1, dto.AssetId);
        Assert.Equal("A-1", dto.AssetNo);
        Assert.Equal("Test Asset", dto.AssetName);
        Assert.Equal(50000m, dto.CapitalValue);
        Assert.Equal(1000m, dto.BaseRate);
        Assert.Equal("Second Floor", dto.FloorDescription);
    }

    [Fact]
    public async Task GetCalculationHistoryAsync_NoHistory_ReturnsEmptyList()
    {
        var mocks = new ServiceMocks();
        var service = CreateService(mocks);

        var result = await service.GetCalculationHistoryAsync(1, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCalculationHistoryAsync_WithoutAssetMasterLoaded_DoesNotThrow()
    {
        // AssetMaster navigation absent (e.g. Include failed to find a match) — the mapping profile's
        // null guard must hold rather than throwing a NullReferenceException.
        var mocks = new ServiceMocks();
        SetRows(mocks.HistoryRepository, new AssetCVCalculationHistoryEntity
        {
            Id = 1,
            AssetId = 1,
            CalculationDate = new DateTime(2026, 1, 1),
            FinancialYear = "2025-26",
            CapitalValue = 1000m,
            AssetMaster = null
        });
        var service = CreateService(mocks);

        var result = await service.GetCalculationHistoryAsync(1, CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(string.Empty, dto.AssetNo);
        Assert.Equal(string.Empty, dto.AssetName);
    }
}
