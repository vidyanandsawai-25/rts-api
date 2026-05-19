using AutoMapper;
using Moq;
using MockQueryable;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Application.Services;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class WaterConnectionServiceTests
{
    private readonly Mock<IRepository<WaterConnectionMasterEntity, int>> _mockRepository;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IRepository<WaterRateMasterEntity, int>> _mockRateRepository;
    private readonly Mock<IRepository<YearMasterEntity, int>> _mockYearRepository;
    private readonly Mock<IWaterConnectionDetailsService> _mockDetailsService;
    private readonly Mock<ILogger<WaterConnectionService>> _mockLogger;
    private readonly WaterConnectionService _service;

    public WaterConnectionServiceTests()
    {
        _mockRepository = new Mock<IRepository<WaterConnectionMasterEntity, int>>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockRateRepository = new Mock<IRepository<WaterRateMasterEntity, int>>();
        _mockYearRepository = new Mock<IRepository<YearMasterEntity, int>>();
        _mockDetailsService = new Mock<IWaterConnectionDetailsService>();
        _mockLogger = new Mock<ILogger<WaterConnectionService>>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new WaterConnectionService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object,
            _mockRateRepository.Object,
            _mockYearRepository.Object,
            _mockDetailsService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesSuccessfully()
    {
        var createDto = new CreateWaterConnectionDto
        {
            PropertyId = 10,
            WaterConnectionTypeId = 1,
            WaterConnectionSizeId = 1,
            ConnectionNo = "WC-001",
            ConnectionStartDate = new DateTime(2024, 4, 1)
        };
        var entity = new WaterConnectionMasterEntity
        {
            Id = 1,
            PropertyId = 10,
            WaterConnectionTypeId = 1,
            WaterConnectionSizeId = 1,
            ConnectionNo = "WC-001",
            ConnectionStartDate = new DateTime(2024, 4, 1),
            IsActive = true
        };

        _mockMapper.Setup(m => m.Map<WaterConnectionMasterEntity>(It.IsAny<CreateWaterConnectionDto>())).Returns(entity);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<WaterConnectionMasterEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<WaterConnectionDto>(It.IsAny<WaterConnectionMasterEntity>()))
            .Returns(new WaterConnectionDto { Id = 1, PropertyId = 10, ConnectionNo = "WC-001" });

        // Mock the year repository to return an active financial year
        var today = DateTime.Today;
        var currentYear = new YearMasterEntity
        {
            Id = 1,
            YearCode = "2024-25",
            StartDate = today.AddMonths(-6),
            EndDate = today.AddMonths(6),
            IsActive = true
        };
        var mockYearQuery = new List<YearMasterEntity> { currentYear }.BuildMock();
        _mockYearRepository.Setup(r => r.GetQueryable()).Returns(mockYearQuery);

        // Mock GenerateBillAsync to return successfully
        _mockDetailsService
            .Setup(s => s.GenerateBillAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WaterConnectionDetailsDto { Id = 1, WaterConnectionId = 1, FinanceYearId = 1 });

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("WC-001", result.ConnectionNo);
        Assert.Equal(10, result.PropertyId);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<WaterConnectionMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockDetailsService.Verify(s => s.GenerateBillAsync(1, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // WaterConnectionService overrides GetByIdAsync to use GetQueryable with includes
        var entity = new WaterConnectionMasterEntity
        {
            Id = 1,
            PropertyId = 10,
            ConnectionNo = "WC-001",
            WaterConnectionTypeId = 1,
            WaterConnectionSizeId = 1,
            ConnectionStartDate = new DateTime(2024, 4, 1),
            IsActive = true
        };

        var mockQuery = new List<WaterConnectionMasterEntity> { entity }.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        // Mock the year and rate repositories for PopulateRateFieldsAsync
        var mockYearQuery = new List<YearMasterEntity>().BuildMock();
        _mockYearRepository.Setup(r => r.GetQueryable()).Returns(mockYearQuery);

        var mockRateQuery = new List<WaterRateMasterEntity>().BuildMock();
        _mockRateRepository.Setup(r => r.GetQueryable()).Returns(mockRateQuery);

        _mockMapper.Setup(m => m.Map<WaterConnectionDto>(It.IsAny<WaterConnectionMasterEntity>()))
            .Returns(new WaterConnectionDto { Id = 1, PropertyId = 10, ConnectionNo = "WC-001" });

        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("WC-001", result.ConnectionNo);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var mockQuery = new List<WaterConnectionMasterEntity>().BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var result = await _service.GetByIdAsync(99, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ValidDto_UpdatesSuccessfully()
    {
        var updateDto = new UpdateWaterConnectionDto
        {
            PropertyId = 10,
            WaterConnectionTypeId = 2,
            WaterConnectionSizeId = 1,
            ConnectionNo = "WC-001-U",
            ConnectionStartDate = new DateTime(2024, 4, 1),
            IsActive = true
        };
        var entity = new WaterConnectionMasterEntity
        {
            Id = 1,
            PropertyId = 10,
            WaterConnectionTypeId = 1,
            WaterConnectionSizeId = 1,
            ConnectionNo = "WC-001",
            ConnectionStartDate = new DateTime(2024, 4, 1),
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<WaterConnectionMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateWaterConnectionDto>(), It.IsAny<WaterConnectionMasterEntity>()))
            .Callback((UpdateWaterConnectionDto src, WaterConnectionMasterEntity dest) =>
            {
                dest.ConnectionNo = src.ConnectionNo;
                dest.WaterConnectionTypeId = src.WaterConnectionTypeId;
                dest.IsActive = src.IsActive;
            });
        _mockMapper.Setup(m => m.Map<WaterConnectionDto>(It.IsAny<WaterConnectionMasterEntity>()))
            .Returns(new WaterConnectionDto { Id = 1, ConnectionNo = "WC-001-U", WaterConnectionTypeId = 2 });

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("WC-001-U", result.ConnectionNo);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<WaterConnectionMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingWithReferences_ThrowsValidationException()
    {
        var updateDto = new UpdateWaterConnectionDto { IsActive = false };
        var entity = new WaterConnectionMasterEntity { Id = 1, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateWaterConnectionDto>(), It.IsAny<WaterConnectionMasterEntity>()))
            .Callback((UpdateWaterConnectionDto src, WaterConnectionMasterEntity dest) => dest.IsActive = src.IsActive);
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<WaterConnectionMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot deactivate: referenced by Water Connection Details"));

        var exception = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.UpdateAsync(1, updateDto, CancellationToken.None));

        Assert.Contains(exception.Errors, e => e.Value != null && e.Value.Contains("Cannot deactivate"));
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingWithoutReferences_UpdatesSuccessfully()
    {
        var updateDto = new UpdateWaterConnectionDto { IsActive = false };
        var entity = new WaterConnectionMasterEntity { Id = 1, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<WaterConnectionMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateWaterConnectionDto>(), It.IsAny<WaterConnectionMasterEntity>()))
            .Callback((UpdateWaterConnectionDto src, WaterConnectionMasterEntity dest) => dest.IsActive = src.IsActive);
        _mockMapper.Setup(m => m.Map<WaterConnectionDto>(It.IsAny<WaterConnectionMasterEntity>()))
            .Returns(new WaterConnectionDto { Id = 1, IsActive = false });
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<WaterConnectionMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<WaterConnectionMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NotDeactivating_DoesNotCheckReferences()
    {
        var updateDto = new UpdateWaterConnectionDto { IsActive = true };
        var entity = new WaterConnectionMasterEntity { Id = 1, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<WaterConnectionMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateWaterConnectionDto>(), It.IsAny<WaterConnectionMasterEntity>()))
            .Callback((UpdateWaterConnectionDto src, WaterConnectionMasterEntity dest) => dest.IsActive = src.IsActive);
        _mockMapper.Setup(m => m.Map<WaterConnectionDto>(It.IsAny<WaterConnectionMasterEntity>()))
            .Returns(new WaterConnectionDto { Id = 1, IsActive = true });

        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        _mockReferenceValidator.Verify(r => r.ValidateReferencesAsync<WaterConnectionMasterEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<WaterConnectionMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_DeletesSuccessfully()
    {
        var entity = new WaterConnectionMasterEntity { Id = 1, ConnectionNo = "WC-001", IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<WaterConnectionMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<WaterConnectionMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        await _service.DeleteAsync(1, CancellationToken.None);

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<WaterConnectionMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ThrowsValidationException()
    {
        var entity = new WaterConnectionMasterEntity { Id = 1, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<WaterConnectionMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot delete: referenced by Water Connection Details"));

        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.DeleteAsync(1, CancellationToken.None));

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<WaterConnectionMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_DoesNothing()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((WaterConnectionMasterEntity?)null);

        await _service.DeleteAsync(99, CancellationToken.None);

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NoActiveFinancialYear_CreatesWithoutBillDetails()
    {
        var createDto = new CreateWaterConnectionDto
        {
            PropertyId = 10,
            WaterConnectionTypeId = 1,
            WaterConnectionSizeId = 1,
            ConnectionNo = "WC-002",
            ConnectionStartDate = new DateTime(2024, 4, 1)
        };
        var entity = new WaterConnectionMasterEntity
        {
            Id = 2,
            PropertyId = 10,
            WaterConnectionTypeId = 1,
            WaterConnectionSizeId = 1,
            ConnectionNo = "WC-002",
            ConnectionStartDate = new DateTime(2024, 4, 1),
            IsActive = true
        };

        _mockMapper.Setup(m => m.Map<WaterConnectionMasterEntity>(It.IsAny<CreateWaterConnectionDto>())).Returns(entity);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<WaterConnectionMasterEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<WaterConnectionDto>(It.IsAny<WaterConnectionMasterEntity>()))
            .Returns(new WaterConnectionDto { Id = 2, PropertyId = 10, ConnectionNo = "WC-002" });

        // Mock no active financial year
        var mockYearQuery = new List<YearMasterEntity>().BuildMock();
        _mockYearRepository.Setup(r => r.GetQueryable()).Returns(mockYearQuery);

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("WC-002", result.ConnectionNo);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<WaterConnectionMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        // GenerateBillAsync should not be called when no active financial year exists
        _mockDetailsService.Verify(s => s.GenerateBillAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_BillGenerationFails_StillCreatesConnection()
    {
        var createDto = new CreateWaterConnectionDto
        {
            PropertyId = 10,
            WaterConnectionTypeId = 1,
            WaterConnectionSizeId = 1,
            ConnectionNo = "WC-003",
            ConnectionStartDate = new DateTime(2024, 4, 1)
        };
        var entity = new WaterConnectionMasterEntity
        {
            Id = 3,
            PropertyId = 10,
            WaterConnectionTypeId = 1,
            WaterConnectionSizeId = 1,
            ConnectionNo = "WC-003",
            ConnectionStartDate = new DateTime(2024, 4, 1),
            IsActive = true
        };

        _mockMapper.Setup(m => m.Map<WaterConnectionMasterEntity>(It.IsAny<CreateWaterConnectionDto>())).Returns(entity);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<WaterConnectionMasterEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<WaterConnectionDto>(It.IsAny<WaterConnectionMasterEntity>()))
            .Returns(new WaterConnectionDto { Id = 3, PropertyId = 10, ConnectionNo = "WC-003" });

        // Mock active financial year with dynamic dates relative to today
        var today = DateTime.Today;
        var currentYear = new YearMasterEntity
        {
            Id = 1,
            YearCode = "Current-FY",
            StartDate = today.AddMonths(-6),
            EndDate = today.AddMonths(6),
            IsActive = true
        };
        var mockYearQuery = new List<YearMasterEntity> { currentYear }.BuildMock();
        _mockYearRepository.Setup(r => r.GetQueryable()).Returns(mockYearQuery);

        // Mock GenerateBillAsync to throw an exception
        _mockDetailsService
            .Setup(s => s.GenerateBillAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No active rate found"));

        // Should not throw - connection creation should succeed even if bill generation fails
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("WC-003", result.ConnectionNo);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<WaterConnectionMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify that GenerateBillAsync was actually invoked (and failed)
        _mockDetailsService.Verify(s => s.GenerateBillAsync(3, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_BillGenerationCancelled_PropagatesCancellation()
    {
        var createDto = new CreateWaterConnectionDto
        {
            PropertyId = 10,
            WaterConnectionTypeId = 1,
            WaterConnectionSizeId = 1,
            ConnectionNo = "WC-004",
            ConnectionStartDate = new DateTime(2024, 4, 1)
        };
        var entity = new WaterConnectionMasterEntity
        {
            Id = 4,
            PropertyId = 10,
            WaterConnectionTypeId = 1,
            WaterConnectionSizeId = 1,
            ConnectionNo = "WC-004",
            ConnectionStartDate = new DateTime(2024, 4, 1),
            IsActive = true
        };

        _mockMapper.Setup(m => m.Map<WaterConnectionMasterEntity>(It.IsAny<CreateWaterConnectionDto>())).Returns(entity);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<WaterConnectionMasterEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<WaterConnectionDto>(It.IsAny<WaterConnectionMasterEntity>()))
            .Returns(new WaterConnectionDto { Id = 4, PropertyId = 10, ConnectionNo = "WC-004" });

        // Mock active financial year with dynamic dates relative to today
        var today = DateTime.Today;
        var currentYear = new YearMasterEntity
        {
            Id = 1,
            YearCode = "Current-FY",
            StartDate = today.AddMonths(-6),
            EndDate = today.AddMonths(6),
            IsActive = true
        };
        var mockYearQuery = new List<YearMasterEntity> { currentYear }.BuildMock();
        _mockYearRepository.Setup(r => r.GetQueryable()).Returns(mockYearQuery);

        // Mock GenerateBillAsync to throw OperationCanceledException
        _mockDetailsService
            .Setup(s => s.GenerateBillAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("Operation was cancelled"));

        // Should propagate cancellation - not swallow it
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));

        // Connection should still be created before bill generation
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<WaterConnectionMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify that GenerateBillAsync was invoked before cancellation
        _mockDetailsService.Verify(s => s.GenerateBillAsync(4, 1, It.IsAny<CancellationToken>()), Times.Once);
    }
}
