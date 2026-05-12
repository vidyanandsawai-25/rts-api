using AutoMapper;
using Moq;
using MockQueryable;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Application.Services;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class WaterConnectionDetailsServiceTests
{
    private readonly Mock<IRepository<WaterConnectionDetailsEntity, int>> _mockRepository;
    private readonly Mock<IRepository<WaterConnectionMasterEntity, int>> _mockConnectionRepository;
    private readonly Mock<IRepository<YearMasterEntity, int>> _mockYearRepository;
    private readonly Mock<IRepository<WaterRateMasterEntity, int>> _mockRateRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly WaterConnectionDetailsService _service;

    public WaterConnectionDetailsServiceTests()
    {
        _mockRepository = new Mock<IRepository<WaterConnectionDetailsEntity, int>>();
        _mockConnectionRepository = new Mock<IRepository<WaterConnectionMasterEntity, int>>();
        _mockYearRepository = new Mock<IRepository<YearMasterEntity, int>>();
        _mockRateRepository = new Mock<IRepository<WaterRateMasterEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new WaterConnectionDetailsService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockConnectionRepository.Object,
            _mockYearRepository.Object,
            _mockRateRepository.Object);
    }

    #region Standard CRUD Tests

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesSuccessfully()
    {
        var createDto = new CreateWaterConnectionDetailsDto
        {
            WaterConnectionId = 1,
            FinanceYearId = 1,
            BillDate = DateTime.Today,
            FromDate = new DateTime(2024, 4, 1),
            ToDate = new DateTime(2025, 3, 31),
            ChargeMonths = 12,
            YearlyRate = 1200m,
            WaterBill = 1200m
        };
        var entity = new WaterConnectionDetailsEntity
        {
            Id = 1,
            WaterConnectionId = 1,
            FinanceYearId = 1,
            BillDate = DateTime.Today,
            FromDate = new DateTime(2024, 4, 1),
            ToDate = new DateTime(2025, 3, 31),
            ChargeMonths = 12,
            YearlyRate = 1200m,
            WaterBill = 1200m,
            IsActive = true
        };

        _mockMapper.Setup(m => m.Map<WaterConnectionDetailsEntity>(It.IsAny<CreateWaterConnectionDetailsDto>())).Returns(entity);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<WaterConnectionDetailsEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<WaterConnectionDetailsDto>(It.IsAny<WaterConnectionDetailsEntity>()))
            .Returns(new WaterConnectionDetailsDto { Id = 1, WaterConnectionId = 1, FinanceYearId = 1, WaterBill = 1200m, ChargeMonths = 12 });

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.WaterConnectionId);
        Assert.Equal(1200m, result.WaterBill);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<WaterConnectionDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // WaterConnectionDetailsService overrides GetByIdAsync to use GetQueryable with includes
        var entity = new WaterConnectionDetailsEntity
        {
            Id = 1,
            WaterConnectionId = 1,
            FinanceYearId = 1,
            ChargeMonths = 12,
            YearlyRate = 1200m,
            WaterBill = 1200m,
            IsActive = true
        };

        var mockQuery = new List<WaterConnectionDetailsEntity> { entity }.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);
        _mockMapper.Setup(m => m.Map<WaterConnectionDetailsDto>(It.IsAny<WaterConnectionDetailsEntity>()))
            .Returns(new WaterConnectionDetailsDto { Id = 1, WaterConnectionId = 1, WaterBill = 1200m });

        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(1200m, result.WaterBill);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var mockQuery = new List<WaterConnectionDetailsEntity>().BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var result = await _service.GetByIdAsync(99, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ValidDto_UpdatesSuccessfully()
    {
        var updateDto = new UpdateWaterConnectionDetailsDto
        {
            WaterConnectionId = 1,
            FinanceYearId = 1,
            BillDate = DateTime.Today,
            FromDate = new DateTime(2024, 10, 1),
            ToDate = new DateTime(2025, 3, 31),
            ChargeMonths = 6,
            YearlyRate = 1200m,
            WaterBill = 600m,
            IsActive = true
        };
        var entity = new WaterConnectionDetailsEntity
        {
            Id = 1,
            WaterConnectionId = 1,
            FinanceYearId = 1,
            ChargeMonths = 12,
            YearlyRate = 1200m,
            WaterBill = 1200m,
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<WaterConnectionDetailsEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateWaterConnectionDetailsDto>(), It.IsAny<WaterConnectionDetailsEntity>()))
            .Callback((UpdateWaterConnectionDetailsDto src, WaterConnectionDetailsEntity dest) =>
            {
                dest.ChargeMonths = src.ChargeMonths;
                dest.WaterBill = src.WaterBill;
                dest.IsActive = src.IsActive;
            });
        _mockMapper.Setup(m => m.Map<WaterConnectionDetailsDto>(It.IsAny<WaterConnectionDetailsEntity>()))
            .Returns(new WaterConnectionDetailsDto { Id = 1, ChargeMonths = 6, WaterBill = 600m });

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(6, result.ChargeMonths);
        Assert.Equal(600m, result.WaterBill);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<WaterConnectionDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_DeletesSuccessfully()
    {
        var entity = new WaterConnectionDetailsEntity { Id = 1, WaterConnectionId = 1, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<WaterConnectionDetailsEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.DeleteAsync(1, CancellationToken.None);

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<WaterConnectionDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_DoesNothing()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((WaterConnectionDetailsEntity?)null);

        await _service.DeleteAsync(99, CancellationToken.None);

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GenerateBillAsync Tests

    private WaterConnectionMasterEntity BuildConnection(
        DateTime start,
        DateTime? stop = null,
        int typeId = 1,
        int sizeId = 1)
        => new()
        {
            Id = 1,
            PropertyId = 10,
            WaterConnectionTypeId = typeId,
            WaterConnectionSizeId = sizeId,
            ConnectionNo = "WC-001",
            ConnectionStartDate = start,
            ConnectionStopDate = stop,
            IsActive = true
        };

    private YearMasterEntity BuildFinanceYear(DateTime start, DateTime end)
        => new()
        {
            Id = 1,
            YearCode = "2024-25",
            StartDate = start,
            EndDate = end,
            IsActive = true
        };

    private WaterRateMasterEntity BuildRate(decimal yearlyRate = 1200m)
        => new()
        {
            Id = 1,
            WaterConnectionTypeId = 1,
            WaterConnectionSizeId = 1,
            FinanceYearId = 1,
            YearlyRate = yearlyRate,
            IsActive = true
        };

    private void SetupConnectionRepository(WaterConnectionMasterEntity connection)
    {
        var mockQuery = new List<WaterConnectionMasterEntity> { connection }.BuildMock();
        _mockConnectionRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);
    }

    private void SetupConnectionNotFound()
    {
        var mockQuery = new List<WaterConnectionMasterEntity>().BuildMock();
        _mockConnectionRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);
    }

    private void SetupRateRepository(WaterRateMasterEntity rate)
    {
        var mockQuery = new List<WaterRateMasterEntity> { rate }.BuildMock();
        _mockRateRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);
    }

    private void SetupRateNotFound()
    {
        var mockQuery = new List<WaterRateMasterEntity>().BuildMock();
        _mockRateRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);
    }

    private void SetupNoExistingBill()
    {
        var mockQuery = new List<WaterConnectionDetailsEntity>().BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);
    }

    private void SetupExistingBill(WaterConnectionDetailsEntity existing)
    {
        var mockQuery = new List<WaterConnectionDetailsEntity> { existing }.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);
    }

    [Fact]
    public async Task GenerateBillAsync_ConnectionNotFound_ThrowsInvalidOperationException()
    {
        SetupConnectionNotFound();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GenerateBillAsync(999, 1, CancellationToken.None));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task GenerateBillAsync_FinanceYearNotFound_ThrowsInvalidOperationException()
    {
        var connection = BuildConnection(new DateTime(2024, 4, 1));
        SetupConnectionRepository(connection);
        _mockYearRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((YearMasterEntity?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GenerateBillAsync(1, 999, CancellationToken.None));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task GenerateBillAsync_FinanceYearHasNoStartDate_ThrowsInvalidOperationException()
    {
        var connection = BuildConnection(new DateTime(2024, 4, 1));
        SetupConnectionRepository(connection);

        var fyNoDate = new YearMasterEntity { Id = 1, YearCode = "2024-25", StartDate = null, EndDate = null };
        _mockYearRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(fyNoDate);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GenerateBillAsync(1, 1, CancellationToken.None));

        Assert.Contains("no valid StartDate", ex.Message);
    }

    [Fact]
    public async Task GenerateBillAsync_ConnectionStoppedBeforeFYStart_ReturnsNull()
    {
        // Connection stopped Jan 31 2024, FY starts Apr 1 2024
        var connection = BuildConnection(
            start: new DateTime(2023, 4, 1),
            stop: new DateTime(2024, 1, 31));
        SetupConnectionRepository(connection);

        var fy = BuildFinanceYear(new DateTime(2024, 4, 1), new DateTime(2025, 3, 31));
        _mockYearRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(fy);

        var result = await _service.GenerateBillAsync(1, 1, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateBillAsync_ConnectionStartsAfterFYEnd_ReturnsNull()
    {
        // Connection starts May 1 2025, FY ends Mar 31 2025
        var connection = BuildConnection(start: new DateTime(2025, 5, 1));
        SetupConnectionRepository(connection);

        var fy = BuildFinanceYear(new DateTime(2024, 4, 1), new DateTime(2025, 3, 31));
        _mockYearRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(fy);

        var result = await _service.GenerateBillAsync(1, 1, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateBillAsync_FullYearConnection_Calculates12MonthsBill()
    {
        // Connection active for the full FY → 12 months → bill = YearlyRate
        var connection = BuildConnection(start: new DateTime(2024, 4, 1));
        SetupConnectionRepository(connection);

        var fy = BuildFinanceYear(new DateTime(2024, 4, 1), new DateTime(2025, 3, 31));
        _mockYearRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(fy);

        SetupRateRepository(BuildRate(yearlyRate: 1200m));
        SetupNoExistingBill();

        WaterConnectionDetailsEntity? savedEntity = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<WaterConnectionDetailsEntity>(), It.IsAny<CancellationToken>()))
            .Callback<WaterConnectionDetailsEntity, CancellationToken>((e, _) => savedEntity = e)
            .ReturnsAsync((WaterConnectionDetailsEntity e, CancellationToken _) => e);

        _mockMapper.Setup(m => m.Map<WaterConnectionDetailsDto>(It.IsAny<WaterConnectionDetailsEntity>()))
            .Returns((WaterConnectionDetailsEntity e) => new WaterConnectionDetailsDto
            {
                Id = 1,
                WaterConnectionId = e.WaterConnectionId,
                FinanceYearId = e.FinanceYearId,
                ChargeMonths = e.ChargeMonths,
                YearlyRate = e.YearlyRate,
                WaterBill = e.WaterBill
            });

        var result = await _service.GenerateBillAsync(1, 1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(12, result.ChargeMonths);
        Assert.Equal(1200m, result.WaterBill);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<WaterConnectionDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateBillAsync_PartialYearConnection_CalculatesProRataBill()
    {
        // Connection starts Oct 1 2024 (6 months remaining in FY) → 6 months → bill = 600
        var connection = BuildConnection(start: new DateTime(2024, 10, 1));
        SetupConnectionRepository(connection);

        var fy = BuildFinanceYear(new DateTime(2024, 4, 1), new DateTime(2025, 3, 31));
        _mockYearRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(fy);

        SetupRateRepository(BuildRate(yearlyRate: 1200m));
        SetupNoExistingBill();

        WaterConnectionDetailsEntity? savedEntity = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<WaterConnectionDetailsEntity>(), It.IsAny<CancellationToken>()))
            .Callback<WaterConnectionDetailsEntity, CancellationToken>((e, _) => savedEntity = e)
            .ReturnsAsync((WaterConnectionDetailsEntity e, CancellationToken _) => e);

        _mockMapper.Setup(m => m.Map<WaterConnectionDetailsDto>(It.IsAny<WaterConnectionDetailsEntity>()))
            .Returns((WaterConnectionDetailsEntity e) => new WaterConnectionDetailsDto
            {
                ChargeMonths = e.ChargeMonths,
                YearlyRate = e.YearlyRate,
                WaterBill = e.WaterBill
            });

        var result = await _service.GenerateBillAsync(1, 1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(6, result.ChargeMonths);
        Assert.Equal(600m, result.WaterBill);
    }

    [Fact]
    public async Task GenerateBillAsync_ConnectionStopsInMiddleOfFY_CalculatesProRataBill()
    {
        // Connection starts with FY but stops Sep 30 2024 → first 6 months → bill = 600
        var connection = BuildConnection(
            start: new DateTime(2024, 4, 1),
            stop: new DateTime(2024, 9, 30));
        SetupConnectionRepository(connection);

        var fy = BuildFinanceYear(new DateTime(2024, 4, 1), new DateTime(2025, 3, 31));
        _mockYearRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(fy);

        SetupRateRepository(BuildRate(yearlyRate: 1200m));
        SetupNoExistingBill();

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<WaterConnectionDetailsEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WaterConnectionDetailsEntity e, CancellationToken _) => e);

        _mockMapper.Setup(m => m.Map<WaterConnectionDetailsDto>(It.IsAny<WaterConnectionDetailsEntity>()))
            .Returns((WaterConnectionDetailsEntity e) => new WaterConnectionDetailsDto
            {
                ChargeMonths = e.ChargeMonths,
                WaterBill = e.WaterBill
            });

        var result = await _service.GenerateBillAsync(1, 1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(6, result.ChargeMonths);
        Assert.Equal(600m, result.WaterBill);
    }

    [Fact]
    public async Task GenerateBillAsync_NoActiveRate_ThrowsInvalidOperationException()
    {
        var connection = BuildConnection(start: new DateTime(2024, 4, 1));
        SetupConnectionRepository(connection);

        var fy = BuildFinanceYear(new DateTime(2024, 4, 1), new DateTime(2025, 3, 31));
        _mockYearRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(fy);

        SetupRateNotFound();
        SetupNoExistingBill();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GenerateBillAsync(1, 1, CancellationToken.None));

        Assert.Contains("No active rate found", ex.Message);
    }

    [Fact]
    public async Task GenerateBillAsync_ExistingBill_UpdatesInsteadOfInserts()
    {
        var connection = BuildConnection(start: new DateTime(2024, 4, 1));
        SetupConnectionRepository(connection);

        var fy = BuildFinanceYear(new DateTime(2024, 4, 1), new DateTime(2025, 3, 31));
        _mockYearRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(fy);

        SetupRateRepository(BuildRate(yearlyRate: 1200m));

        var existingBill = new WaterConnectionDetailsEntity
        {
            Id = 5,
            WaterConnectionId = 1,
            FinanceYearId = 1,
            ChargeMonths = 10,
            YearlyRate = 1000m,
            WaterBill = 833.33m,
            IsActive = true
        };
        SetupExistingBill(existingBill);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<WaterConnectionDetailsEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _mockMapper.Setup(m => m.Map<WaterConnectionDetailsDto>(It.IsAny<WaterConnectionDetailsEntity>()))
            .Returns((WaterConnectionDetailsEntity e) => new WaterConnectionDetailsDto
            {
                Id = e.Id,
                ChargeMonths = e.ChargeMonths,
                WaterBill = e.WaterBill
            });

        var result = await _service.GenerateBillAsync(1, 1, CancellationToken.None);

        Assert.NotNull(result);
        // Bill recalculated: 12 months at 1200/yr
        Assert.Equal(12, result.ChargeMonths);
        Assert.Equal(1200m, result.WaterBill);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<WaterConnectionDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<WaterConnectionDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
