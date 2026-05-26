using AutoMapper;
using Moq;
using NtisPlatform.Application.DTOs.Master.PropertyAssessmentStatus;
using NtisPlatform.Application.Services;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive tests for PropertyAssessmentStatusService
/// Covers all service methods and validation scenarios for 100% line coverage
/// </summary>
public class PropertyAssessmentStatusServiceTests
{
    private readonly Mock<IRepository<PropertyAssessmentStatusEntity, int>> _mockRepository;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PropertyAssessmentStatusService _service;

    public PropertyAssessmentStatusServiceTests()
    {
        _mockRepository = new Mock<IRepository<PropertyAssessmentStatusEntity, int>>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new PropertyAssessmentStatusService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreatePropertyAssessmentStatusDto { StatusName = "Pending Assessment" };
        var entity = new PropertyAssessmentStatusEntity { Id = 1, StatusName = "Pending Assessment", IsActive = true };

        _mockMapper.Setup(m => m.Map<PropertyAssessmentStatusEntity>(It.IsAny<CreatePropertyAssessmentStatusDto>())).Returns(entity);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<PropertyAssessmentStatusEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<PropertyAssessmentStatusDto>(It.IsAny<PropertyAssessmentStatusEntity>()))
            .Returns(new PropertyAssessmentStatusDto { Id = 1, StatusName = "Pending Assessment" });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Pending Assessment", result.StatusName);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<PropertyAssessmentStatusEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithIsActiveTrue_CreatesActiveEntity()
    {
        // Arrange
        var createDto = new CreatePropertyAssessmentStatusDto 
        { 
            StatusName = "Assessed",
            IsActive = true 
        };
        var entity = new PropertyAssessmentStatusEntity { Id = 1, StatusName = "Assessed", IsActive = true };

        _mockMapper.Setup(m => m.Map<PropertyAssessmentStatusEntity>(It.IsAny<CreatePropertyAssessmentStatusDto>())).Returns(entity);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<PropertyAssessmentStatusEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<PropertyAssessmentStatusDto>(It.IsAny<PropertyAssessmentStatusEntity>()))
            .Returns(new PropertyAssessmentStatusDto { Id = 1, StatusName = "Assessed", IsActive = true });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_WithIsActiveFalse_CreatesInactiveEntity()
    {
        // Arrange
        var createDto = new CreatePropertyAssessmentStatusDto 
        { 
            StatusName = "Rejected",
            IsActive = false 
        };
        var entity = new PropertyAssessmentStatusEntity { Id = 1, StatusName = "Rejected", IsActive = false };

        _mockMapper.Setup(m => m.Map<PropertyAssessmentStatusEntity>(It.IsAny<CreatePropertyAssessmentStatusDto>())).Returns(entity);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<PropertyAssessmentStatusEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<PropertyAssessmentStatusDto>(It.IsAny<PropertyAssessmentStatusEntity>()))
            .Returns(new PropertyAssessmentStatusDto { Id = 1, StatusName = "Rejected", IsActive = false });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.False(result.IsActive);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new PropertyAssessmentStatusEntity { Id = 1, StatusName = "Approved", IsActive = true };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<PropertyAssessmentStatusDto>(entity))
            .Returns(new PropertyAssessmentStatusDto { Id = 1, StatusName = "Approved" });

        // Act
        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Approved", result.StatusName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((PropertyAssessmentStatusEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(99, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        _mockRepository.Setup(r => r.GetByIdAsync(1, cancellationToken)).ReturnsAsync((PropertyAssessmentStatusEntity?)null);

        // Act
        await _service.GetByIdAsync(1, cancellationToken);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(1, cancellationToken), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidDto_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdatePropertyAssessmentStatusDto { StatusName = "Under Review", IsActive = true };
        var entity = new PropertyAssessmentStatusEntity { Id = 1, StatusName = "Pending", IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<PropertyAssessmentStatusEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdatePropertyAssessmentStatusDto>(), It.IsAny<PropertyAssessmentStatusEntity>()))
            .Callback((UpdatePropertyAssessmentStatusDto src, PropertyAssessmentStatusEntity dest) =>
            {
                dest.StatusName = src.StatusName;
                dest.IsActive = src.IsActive;
            });
        _mockMapper.Setup(m => m.Map<PropertyAssessmentStatusDto>(It.IsAny<PropertyAssessmentStatusEntity>()))
            .Returns(new PropertyAssessmentStatusDto { Id = 1, StatusName = "Under Review" });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Under Review", result.StatusName);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertyAssessmentStatusEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingWithReferences_ThrowsValidationException()
    {
        // Arrange
        var updateDto = new UpdatePropertyAssessmentStatusDto { StatusName = "Cancelled", IsActive = false };
        var entity = new PropertyAssessmentStatusEntity { Id = 1, StatusName = "Active", IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdatePropertyAssessmentStatusDto>(), It.IsAny<PropertyAssessmentStatusEntity>()))
            .Callback((UpdatePropertyAssessmentStatusDto src, PropertyAssessmentStatusEntity dest) => dest.IsActive = src.IsActive);
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<PropertyAssessmentStatusEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot deactivate: referenced by Property Assessments"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.UpdateAsync(1, updateDto, CancellationToken.None));

        Assert.Contains(exception.Errors, e => e.Value != null && e.Value.Contains("Cannot deactivate"));
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingWithoutReferences_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdatePropertyAssessmentStatusDto { StatusName = "Obsolete", IsActive = false };
        var entity = new PropertyAssessmentStatusEntity { Id = 1, StatusName = "Old Status", IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<PropertyAssessmentStatusEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdatePropertyAssessmentStatusDto>(), It.IsAny<PropertyAssessmentStatusEntity>()))
            .Callback((UpdatePropertyAssessmentStatusDto src, PropertyAssessmentStatusEntity dest) => 
            {
                dest.StatusName = src.StatusName;
                dest.IsActive = src.IsActive;
            });
        _mockMapper.Setup(m => m.Map<PropertyAssessmentStatusDto>(It.IsAny<PropertyAssessmentStatusEntity>()))
            .Returns(new PropertyAssessmentStatusDto { Id = 1, StatusName = "Obsolete", IsActive = false });
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<PropertyAssessmentStatusEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertyAssessmentStatusEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NotDeactivating_DoesNotCheckReferences()
    {
        // Arrange
        var updateDto = new UpdatePropertyAssessmentStatusDto { StatusName = "Updated", IsActive = true };
        var entity = new PropertyAssessmentStatusEntity { Id = 1, StatusName = "Original", IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<PropertyAssessmentStatusEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdatePropertyAssessmentStatusDto>(), It.IsAny<PropertyAssessmentStatusEntity>()))
            .Callback((UpdatePropertyAssessmentStatusDto src, PropertyAssessmentStatusEntity dest) => 
            {
                dest.StatusName = src.StatusName;
                dest.IsActive = src.IsActive;
            });
        _mockMapper.Setup(m => m.Map<PropertyAssessmentStatusDto>(It.IsAny<PropertyAssessmentStatusEntity>()))
            .Returns(new PropertyAssessmentStatusDto { Id = 1, StatusName = "Updated", IsActive = true });

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockReferenceValidator.Verify(r => r.ValidateReferencesAsync<PropertyAssessmentStatusEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertyAssessmentStatusEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ActivatingInactiveRecord_DoesNotCheckReferences()
    {
        // Arrange
        var updateDto = new UpdatePropertyAssessmentStatusDto { StatusName = "Reactivated", IsActive = true };
        var entity = new PropertyAssessmentStatusEntity { Id = 1, StatusName = "Inactive", IsActive = false };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<PropertyAssessmentStatusEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdatePropertyAssessmentStatusDto>(), It.IsAny<PropertyAssessmentStatusEntity>()))
            .Callback((UpdatePropertyAssessmentStatusDto src, PropertyAssessmentStatusEntity dest) => 
            {
                dest.StatusName = src.StatusName;
                dest.IsActive = src.IsActive;
            });
        _mockMapper.Setup(m => m.Map<PropertyAssessmentStatusDto>(It.IsAny<PropertyAssessmentStatusEntity>()))
            .Returns(new PropertyAssessmentStatusDto { Id = 1, StatusName = "Reactivated", IsActive = true });

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockReferenceValidator.Verify(r => r.ValidateReferencesAsync<PropertyAssessmentStatusEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdatePropertyAssessmentStatusDto { StatusName = "Does Not Exist" };
        _mockRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((PropertyAssessmentStatusEntity?)null);

        // Act
        var result = await _service.UpdateAsync(99, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertyAssessmentStatusEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingId_DeletesSuccessfully()
    {
        // Arrange
        var entity = new PropertyAssessmentStatusEntity { Id = 1, StatusName = "To Be Deleted", IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertyAssessmentStatusEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<PropertyAssessmentStatusEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        await _service.DeleteAsync(1, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertyAssessmentStatusEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ThrowsValidationException()
    {
        // Arrange
        var entity = new PropertyAssessmentStatusEntity { Id = 1, StatusName = "In Use", IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<PropertyAssessmentStatusEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot delete: referenced by Property Assessments"));

        // Act & Assert
        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.DeleteAsync(1, CancellationToken.None));

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertyAssessmentStatusEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_DoesNothing()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((PropertyAssessmentStatusEntity?)null);

        // Act
        await _service.DeleteAsync(99, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertyAssessmentStatusEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithCancellationToken_PassesTokenCorrectly()
    {
        // Arrange
        var entity = new PropertyAssessmentStatusEntity { Id = 1, StatusName = "Test", IsActive = true };
        var cancellationToken = new CancellationToken();

        _mockRepository.Setup(r => r.GetByIdAsync(1, cancellationToken)).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertyAssessmentStatusEntity>(), cancellationToken)).Returns(Task.CompletedTask);
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<PropertyAssessmentStatusEntity>(1, cancellationToken))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        await _service.DeleteAsync(1, cancellationToken);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertyAssessmentStatusEntity>(), cancellationToken), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(cancellationToken), Times.Once);
    }

    #endregion

    #region Validation Coverage Tests

    [Fact]
    public async Task ValidateForDeactivationAsync_WhenActiveToInactive_CallsReferenceValidator()
    {
        // Arrange
        var updateDto = new UpdatePropertyAssessmentStatusDto { IsActive = false };
        var entity = new PropertyAssessmentStatusEntity { Id = 1, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdatePropertyAssessmentStatusDto>(), It.IsAny<PropertyAssessmentStatusEntity>()))
            .Callback((UpdatePropertyAssessmentStatusDto src, PropertyAssessmentStatusEntity dest) => dest.IsActive = src.IsActive);
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<PropertyAssessmentStatusEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<PropertyAssessmentStatusEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<PropertyAssessmentStatusDto>(It.IsAny<PropertyAssessmentStatusEntity>()))
            .Returns(new PropertyAssessmentStatusDto { Id = 1, IsActive = false });

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockReferenceValidator.Verify(r => r.ValidateReferencesAsync<PropertyAssessmentStatusEntity>(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateForDeleteAsync_AlwaysCallsReferenceValidator()
    {
        // Arrange
        var entity = new PropertyAssessmentStatusEntity { Id = 1, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<PropertyAssessmentStatusEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertyAssessmentStatusEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(1, CancellationToken.None);

        // Assert
        _mockReferenceValidator.Verify(r => r.ValidateReferencesAsync<PropertyAssessmentStatusEntity>(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
