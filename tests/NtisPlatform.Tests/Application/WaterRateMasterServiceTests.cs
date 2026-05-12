using AutoMapper;
using Moq;
using MockQueryable;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Application.Services;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class WaterRateMasterServiceTests
{
    private readonly Mock<IRepository<WaterRateMasterEntity, int>> _mockRepository;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly WaterRateMasterService _service;

    public WaterRateMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<WaterRateMasterEntity, int>>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new WaterRateMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesSuccessfully()
    {
        var createDto = new CreateWaterRateMasterDto
        {
            WaterConnectionTypeId = 1,
            WaterConnectionSizeId = 1,
            FinanceYearId = 1,
            YearlyRate = 1200m
        };
        var entity = new WaterRateMasterEntity
        {
            Id = 1,
            WaterConnectionTypeId = 1,
            WaterConnectionSizeId = 1,
            FinanceYearId = 1,
            YearlyRate = 1200m,
            IsActive = true
        };

        _mockMapper.Setup(m => m.Map<WaterRateMasterEntity>(It.IsAny<CreateWaterRateMasterDto>())).Returns(entity);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<WaterRateMasterEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<WaterRateMasterDto>(It.IsAny<WaterRateMasterEntity>()))
            .Returns(new WaterRateMasterDto { Id = 1, WaterConnectionTypeId = 1, WaterConnectionSizeId = 1, FinanceYearId = 1, YearlyRate = 1200m });

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1200m, result.YearlyRate);
        Assert.Equal(1, result.WaterConnectionTypeId);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<WaterRateMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new WaterRateMasterEntity { Id = 1, WaterConnectionTypeId = 1, WaterConnectionSizeId = 1, FinanceYearId = 1, YearlyRate = 1200m, IsActive = true };
        var mockQueryable = new List<WaterRateMasterEntity> { entity }.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);
        _mockMapper.Setup(m => m.Map<WaterRateMasterDto>(entity))
            .Returns(new WaterRateMasterDto { Id = 1, YearlyRate = 1200m });

        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(1200m, result.YearlyRate);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var mockQueryable = new List<WaterRateMasterEntity>().BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var result = await _service.GetByIdAsync(99, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ValidDto_UpdatesSuccessfully()
    {
        var updateDto = new UpdateWaterRateMasterDto
        {
            WaterConnectionTypeId = 1,
            WaterConnectionSizeId = 1,
            FinanceYearId = 1,
            YearlyRate = 1500m,
            IsActive = true
        };
        var entity = new WaterRateMasterEntity { Id = 1, WaterConnectionTypeId = 1, WaterConnectionSizeId = 1, FinanceYearId = 1, YearlyRate = 1200m, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<WaterRateMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateWaterRateMasterDto>(), It.IsAny<WaterRateMasterEntity>()))
            .Callback((UpdateWaterRateMasterDto src, WaterRateMasterEntity dest) =>
            {
                dest.WaterConnectionTypeId = src.WaterConnectionTypeId;
                dest.WaterConnectionSizeId = src.WaterConnectionSizeId;
                dest.FinanceYearId = src.FinanceYearId;
                dest.YearlyRate = src.YearlyRate;
                dest.IsActive = src.IsActive;
            });
        _mockMapper.Setup(m => m.Map<WaterRateMasterDto>(It.IsAny<WaterRateMasterEntity>()))
            .Returns(new WaterRateMasterDto { Id = 1, YearlyRate = 1500m });

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1500m, result.YearlyRate);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<WaterRateMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingWithReferences_ThrowsValidationException()
    {
        var updateDto = new UpdateWaterRateMasterDto { IsActive = false };
        var entity = new WaterRateMasterEntity { Id = 1, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateWaterRateMasterDto>(), It.IsAny<WaterRateMasterEntity>()))
            .Callback((UpdateWaterRateMasterDto src, WaterRateMasterEntity dest) => dest.IsActive = src.IsActive);
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<WaterRateMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot deactivate: referenced by Water Connection Details"));

        var exception = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.UpdateAsync(1, updateDto, CancellationToken.None));

        Assert.Contains(exception.Errors, e => e.Value != null && e.Value.Contains("Cannot deactivate"));
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingWithoutReferences_UpdatesSuccessfully()
    {
        var updateDto = new UpdateWaterRateMasterDto { IsActive = false };
        var entity = new WaterRateMasterEntity { Id = 1, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<WaterRateMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateWaterRateMasterDto>(), It.IsAny<WaterRateMasterEntity>()))
            .Callback((UpdateWaterRateMasterDto src, WaterRateMasterEntity dest) => dest.IsActive = src.IsActive);
        _mockMapper.Setup(m => m.Map<WaterRateMasterDto>(It.IsAny<WaterRateMasterEntity>()))
            .Returns(new WaterRateMasterDto { Id = 1, IsActive = false });
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<WaterRateMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<WaterRateMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NotDeactivating_DoesNotCheckReferences()
    {
        var updateDto = new UpdateWaterRateMasterDto { IsActive = true };
        var entity = new WaterRateMasterEntity { Id = 1, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<WaterRateMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateWaterRateMasterDto>(), It.IsAny<WaterRateMasterEntity>()))
            .Callback((UpdateWaterRateMasterDto src, WaterRateMasterEntity dest) => dest.IsActive = src.IsActive);
        _mockMapper.Setup(m => m.Map<WaterRateMasterDto>(It.IsAny<WaterRateMasterEntity>()))
            .Returns(new WaterRateMasterDto { Id = 1, IsActive = true });

        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        _mockReferenceValidator.Verify(r => r.ValidateReferencesAsync<WaterRateMasterEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<WaterRateMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_DeletesSuccessfully()
    {
        var entity = new WaterRateMasterEntity { Id = 1, YearlyRate = 1200m, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<WaterRateMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<WaterRateMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        await _service.DeleteAsync(1, CancellationToken.None);

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<WaterRateMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ThrowsValidationException()
    {
        var entity = new WaterRateMasterEntity { Id = 1, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<WaterRateMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot delete: referenced by Water Connection Details"));

        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.DeleteAsync(1, CancellationToken.None));

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<WaterRateMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_DoesNothing()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((WaterRateMasterEntity?)null);

        await _service.DeleteAsync(99, CancellationToken.None);

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
