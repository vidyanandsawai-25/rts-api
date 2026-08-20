using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxCalculationDetail;
using NtisPlatform.Application.Services.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.RetrospectiveTax;

public class RetrospectiveTaxCalculationDetailServiceTests
{
    private readonly Mock<IRepository<RetrospectiveTaxCalculationDetailEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly RetrospectiveTaxCalculationDetailService _service;

    public RetrospectiveTaxCalculationDetailServiceTests()
    {
        _mockRepository = new Mock<IRepository<RetrospectiveTaxCalculationDetailEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new RetrospectiveTaxCalculationDetailService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new RetrospectiveTaxCalculationDetailEntity
        {
            Id = 1,
            CalculationId = 10,
            PropertyId = 100,
            FloorId = 1,
            FinancialYear = "2024-25",
            FromDate = new DateTime(2024, 4, 1),
            ToDate = new DateTime(2025, 3, 31),
            BaseTaxAmount = 1000m,
            TaxMultiplier = 1.5m,
            RetrospectiveTaxAmount = 1500m,
            PenaltyAmount = 0m,
            TotalAmount = 1500m,
            CreatedDate = DateTime.Now
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<RetrospectiveTaxCalculationDetailDto>(It.IsAny<RetrospectiveTaxCalculationDetailEntity>()))
            .Returns((RetrospectiveTaxCalculationDetailEntity e) => new RetrospectiveTaxCalculationDetailDto
            {
                Id = e.Id,
                CalculationId = e.CalculationId,
                PropertyId = e.PropertyId,
                FloorId = e.FloorId,
                FinancialYear = e.FinancialYear,
                FromDate = e.FromDate,
                ToDate = e.ToDate,
                BaseTaxAmount = e.BaseTaxAmount,
                TaxMultiplier = e.TaxMultiplier,
                RetrospectiveTaxAmount = e.RetrospectiveTaxAmount,
                TotalAmount = e.TotalAmount
            });

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("2024-25", result.FinancialYear);
        Assert.Equal(1.5m, result.TaxMultiplier);
        Assert.Equal(1500m, result.TotalAmount);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveTaxCalculationDetailEntity?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        var entities = new List<RetrospectiveTaxCalculationDetailEntity>
        {
            new() { Id = 1, CalculationId = 10, PropertyId = 100, FloorId = 1, FinancialYear = "2023-24", FromDate = DateTime.Now, ToDate = DateTime.Now, CreatedDate = DateTime.Now },
            new() { Id = 2, CalculationId = 10, PropertyId = 100, FloorId = 1, FinancialYear = "2024-25", FromDate = DateTime.Now, ToDate = DateTime.Now, CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<RetrospectiveTaxCalculationDetailEntity, RetrospectiveTaxCalculationDetailDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new RetrospectiveTaxCalculationDetailService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new RetrospectiveTaxCalculationDetailQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
        };

        var result = await service.GetAllAsync(qp, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.FinancialYear == "2023-24");
        Assert.Contains(items, x => x.FinancialYear == "2024-25");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var createDto = new CreateRetrospectiveTaxCalculationDetailDto
        {
            CalculationId = 10,
            PropertyId = 100,
            FloorId = 1,
            FinancialYear = "2024-25",
            FromDate = new DateTime(2024, 4, 1),
            ToDate = new DateTime(2025, 3, 31),
            RateMode = "HISTORIC_YEAR_WISE",
            PercentageMode = "HISTORIC_YEAR_WISE",
            BaseTaxAmount = 1000m,
            TaxMultiplier = 1.5m,
            RetrospectiveTaxAmount = 1500m,
            PenaltyPercent = 5m,
            PenaltyAmount = 75m,
            TotalAmount = 1575m,
            IsActive = true,
            CreatedBy = 3
        };

        _mockMapper
            .Setup(m => m.Map<RetrospectiveTaxCalculationDetailEntity>(It.IsAny<CreateRetrospectiveTaxCalculationDetailDto>()))
            .Returns((CreateRetrospectiveTaxCalculationDetailDto dto) => new RetrospectiveTaxCalculationDetailEntity
            {
                Id = 1,
                CalculationId = dto.CalculationId,
                PropertyId = dto.PropertyId,
                FloorId = dto.FloorId,
                FinancialYear = dto.FinancialYear,
                FromDate = dto.FromDate,
                ToDate = dto.ToDate,
                RateMode = dto.RateMode,
                PercentageMode = dto.PercentageMode,
                BaseTaxAmount = dto.BaseTaxAmount,
                TaxMultiplier = dto.TaxMultiplier,
                RetrospectiveTaxAmount = dto.RetrospectiveTaxAmount,
                PenaltyPercent = dto.PenaltyPercent,
                PenaltyAmount = dto.PenaltyAmount,
                TotalAmount = dto.TotalAmount,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy,
                CreatedDate = DateTime.Now,
                Calculation = new RetrospectiveTaxCalculationEntity { Id = 10, PropertyId = 100, CalculationMode = "PROPERTY" }
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveTaxCalculationDetailEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveTaxCalculationDetailEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<RetrospectiveTaxCalculationDetailDto>(It.IsAny<RetrospectiveTaxCalculationDetailEntity>()))
            .Returns((RetrospectiveTaxCalculationDetailEntity e) => new RetrospectiveTaxCalculationDetailDto
            {
                Id = e.Id,
                CalculationId = e.CalculationId,
                PropertyId = e.PropertyId,
                FloorId = e.FloorId,
                FinancialYear = e.FinancialYear,
                FromDate = e.FromDate,
                ToDate = e.ToDate,
                RateMode = e.RateMode,
                PercentageMode = e.PercentageMode,
                BaseTaxAmount = e.BaseTaxAmount,
                TaxMultiplier = e.TaxMultiplier,
                RetrospectiveTaxAmount = e.RetrospectiveTaxAmount,
                PenaltyPercent = e.PenaltyPercent,
                PenaltyAmount = e.PenaltyAmount,
                TotalAmount = e.TotalAmount,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate
            });

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("2024-25", result.FinancialYear);
        Assert.Equal("HISTORIC_YEAR_WISE", result.RateMode);
        Assert.Equal(1575m, result.TotalAmount);

        _mockRepository.Verify(r => r.AddAsync(
            It.Is<RetrospectiveTaxCalculationDetailEntity>(e => e.Calculation != null && e.Calculation.CalculationMode == "PROPERTY"),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var updateDto = new UpdateRetrospectiveTaxCalculationDetailDto
        {
            CalculationId = 10,
            PropertyId = 100,
            FloorId = 1,
            FinancialYear = "2025-26",
            FromDate = DateTime.Now,
            ToDate = DateTime.Now,
            RateMode = "CURRENT_YEAR_FOR_ALL_YEARS",
            PercentageMode = "CURRENT_YEAR_FOR_ALL_YEARS",
            BaseTaxAmount = 1200m,
            TaxMultiplier = 2.0m,
            RetrospectiveTaxAmount = 2400m,
            PenaltyPercent = 10m,
            PenaltyAmount = 240m,
            TotalAmount = 2640m,
            IsActive = true,
            UpdatedBy = 9
        };

        var existingEntity = new RetrospectiveTaxCalculationDetailEntity
        {
            Id = 1,
            CalculationId = 10,
            PropertyId = 100,
            FloorId = 1,
            FinancialYear = "2024-25",
            FromDate = DateTime.Now,
            ToDate = DateTime.Now,
            TaxMultiplier = 1.5m,
            CreatedDate = DateTime.Now
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RetrospectiveTaxCalculationDetailEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRetrospectiveTaxCalculationDetailDto>(), It.IsAny<RetrospectiveTaxCalculationDetailEntity>()))
            .Callback((UpdateRetrospectiveTaxCalculationDetailDto src, RetrospectiveTaxCalculationDetailEntity dest) =>
            {
                dest.CalculationId = src.CalculationId;
                dest.PropertyId = src.PropertyId;
                dest.FloorId = src.FloorId;
                dest.FinancialYear = src.FinancialYear;
                dest.FromDate = src.FromDate;
                dest.ToDate = src.ToDate;
                dest.RateMode = src.RateMode;
                dest.PercentageMode = src.PercentageMode;
                dest.BaseTaxAmount = src.BaseTaxAmount;
                dest.TaxMultiplier = src.TaxMultiplier;
                dest.RetrospectiveTaxAmount = src.RetrospectiveTaxAmount;
                dest.PenaltyPercent = src.PenaltyPercent;
                dest.PenaltyAmount = src.PenaltyAmount;
                dest.TotalAmount = src.TotalAmount;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
                dest.UpdatedDate = DateTime.Now;
            });

        _mockMapper
            .Setup(m => m.Map<RetrospectiveTaxCalculationDetailDto>(It.IsAny<RetrospectiveTaxCalculationDetailEntity>()))
            .Returns((RetrospectiveTaxCalculationDetailEntity e) => new RetrospectiveTaxCalculationDetailDto
            {
                Id = e.Id,
                FinancialYear = e.FinancialYear,
                TaxMultiplier = e.TaxMultiplier,
                IsActive = e.IsActive,
                UpdatedDate = e.UpdatedDate
            });

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveTaxCalculationDetailEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal("2025-26", existingEntity.FinancialYear);
        Assert.Equal(2.0m, existingEntity.TaxMultiplier);
        Assert.Equal("CURRENT_YEAR_FOR_ALL_YEARS", existingEntity.RateMode);
        Assert.Equal("CURRENT_YEAR_FOR_ALL_YEARS", existingEntity.PercentageMode);
        Assert.Equal(1200m, existingEntity.BaseTaxAmount);
        Assert.Equal(2400m, existingEntity.RetrospectiveTaxAmount);
        Assert.Equal(10m, existingEntity.PenaltyPercent);
        Assert.Equal(240m, existingEntity.PenaltyAmount);
        Assert.Equal(2640m, existingEntity.TotalAmount);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        var updateDto = new UpdateRetrospectiveTaxCalculationDetailDto
        {
            CalculationId = 10,
            PropertyId = 100,
            FloorId = 1,
            FinancialYear = "2024-25",
            FromDate = DateTime.Now,
            ToDate = DateTime.Now
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveTaxCalculationDetailEntity?)null);

        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveTaxCalculationDetailEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        var idToDelete = 1;
        var existingEntity = new RetrospectiveTaxCalculationDetailEntity { Id = 1, CalculationId = 10, PropertyId = 100, FloorId = 1, FinancialYear = "2024-25" };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<RetrospectiveTaxCalculationDetailEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectiveTaxCalculationDetailEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveTaxCalculationDetailEntity?)null);

        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectiveTaxCalculationDetailEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
