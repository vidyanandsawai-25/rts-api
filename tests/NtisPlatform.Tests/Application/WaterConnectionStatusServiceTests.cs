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

public class WaterConnectionStatusServiceTests
{
    private readonly Mock<IRepository<WaterConnectionStatusEntity, int>> _mockRepository;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly WaterConnectionStatusService _service;

    public WaterConnectionStatusServiceTests()
    {
        _mockRepository = new Mock<IRepository<WaterConnectionStatusEntity, int>>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new WaterConnectionStatusService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesSuccessfully()
    {
        var createDto = new CreateWaterConnectionStatusDto { StatusName = "Active" };
        var entity = new WaterConnectionStatusEntity { Id = 1, StatusName = "Active", IsActive = true };

        _mockMapper.Setup(m => m.Map<WaterConnectionStatusEntity>(It.IsAny<CreateWaterConnectionStatusDto>())).Returns(entity);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<WaterConnectionStatusEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<WaterConnectionStatusDto>(It.IsAny<WaterConnectionStatusEntity>()))
            .Returns(new WaterConnectionStatusDto { Id = 1, StatusName = "Active" });

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Active", result.StatusName);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<WaterConnectionStatusEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new WaterConnectionStatusEntity { Id = 1, StatusName = "Active", IsActive = true };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<WaterConnectionStatusDto>(entity))
            .Returns(new WaterConnectionStatusDto { Id = 1, StatusName = "Active" });

        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Active", result.StatusName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((WaterConnectionStatusEntity?)null);

        var result = await _service.GetByIdAsync(99, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ValidDto_UpdatesSuccessfully()
    {
        var updateDto = new UpdateWaterConnectionStatusDto { StatusName = "Disconnected", IsActive = true };
        var entity = new WaterConnectionStatusEntity { Id = 1, StatusName = "Active", IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<WaterConnectionStatusEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateWaterConnectionStatusDto>(), It.IsAny<WaterConnectionStatusEntity>()))
            .Callback((UpdateWaterConnectionStatusDto src, WaterConnectionStatusEntity dest) =>
            {
                dest.StatusName = src.StatusName;
                dest.IsActive = src.IsActive;
            });
        _mockMapper.Setup(m => m.Map<WaterConnectionStatusDto>(It.IsAny<WaterConnectionStatusEntity>()))
            .Returns(new WaterConnectionStatusDto { Id = 1, StatusName = "Disconnected" });

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Disconnected", result.StatusName);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<WaterConnectionStatusEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingWithReferences_ThrowsValidationException()
    {
        var updateDto = new UpdateWaterConnectionStatusDto { IsActive = false };
        var entity = new WaterConnectionStatusEntity { Id = 1, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateWaterConnectionStatusDto>(), It.IsAny<WaterConnectionStatusEntity>()))
            .Callback((UpdateWaterConnectionStatusDto src, WaterConnectionStatusEntity dest) => dest.IsActive = src.IsActive);
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<WaterConnectionStatusEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot deactivate: referenced by Water Connections"));

        var exception = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.UpdateAsync(1, updateDto, CancellationToken.None));

        Assert.Contains(exception.Errors, e => e.Value != null && e.Value.Contains("Cannot deactivate"));
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingWithoutReferences_UpdatesSuccessfully()
    {
        var updateDto = new UpdateWaterConnectionStatusDto { IsActive = false };
        var entity = new WaterConnectionStatusEntity { Id = 1, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<WaterConnectionStatusEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateWaterConnectionStatusDto>(), It.IsAny<WaterConnectionStatusEntity>()))
            .Callback((UpdateWaterConnectionStatusDto src, WaterConnectionStatusEntity dest) => dest.IsActive = src.IsActive);
        _mockMapper.Setup(m => m.Map<WaterConnectionStatusDto>(It.IsAny<WaterConnectionStatusEntity>()))
            .Returns(new WaterConnectionStatusDto { Id = 1, IsActive = false });
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<WaterConnectionStatusEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<WaterConnectionStatusEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NotDeactivating_DoesNotCheckReferences()
    {
        var updateDto = new UpdateWaterConnectionStatusDto { IsActive = true };
        var entity = new WaterConnectionStatusEntity { Id = 1, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<WaterConnectionStatusEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateWaterConnectionStatusDto>(), It.IsAny<WaterConnectionStatusEntity>()))
            .Callback((UpdateWaterConnectionStatusDto src, WaterConnectionStatusEntity dest) => dest.IsActive = src.IsActive);
        _mockMapper.Setup(m => m.Map<WaterConnectionStatusDto>(It.IsAny<WaterConnectionStatusEntity>()))
            .Returns(new WaterConnectionStatusDto { Id = 1, IsActive = true });

        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        _mockReferenceValidator.Verify(r => r.ValidateReferencesAsync<WaterConnectionStatusEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<WaterConnectionStatusEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_DeletesSuccessfully()
    {
        var entity = new WaterConnectionStatusEntity { Id = 1, StatusName = "Active", IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<WaterConnectionStatusEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<WaterConnectionStatusEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        await _service.DeleteAsync(1, CancellationToken.None);

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<WaterConnectionStatusEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ThrowsValidationException()
    {
        var entity = new WaterConnectionStatusEntity { Id = 1, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<WaterConnectionStatusEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot delete: referenced by Water Connections"));

        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.DeleteAsync(1, CancellationToken.None));

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<WaterConnectionStatusEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_DoesNothing()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((WaterConnectionStatusEntity?)null);

        await _service.DeleteAsync(99, CancellationToken.None);

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
