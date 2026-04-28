using AutoMapper;
using Moq;
using MockQueryable;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Entities.Master;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.ComponentModel.DataAnnotations;
using ValidationResult = NtisPlatform.Application.Models.ValidationResult;

namespace NtisPlatform.Tests.Application;
public class ConstructionTypeServiceTests
{
    private readonly Mock<IRepository<ConstructionTypeEntity, int>> _mockRepository;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly ConstructionTypeService _service;

    public ConstructionTypeServiceTests()
    {
        _mockRepository = new Mock<IRepository<ConstructionTypeEntity, int>>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new ConstructionTypeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object);
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingWithMultipleReferences_ThrowsValidationException()
    {
        // Arrange
        var updateDto = new UpdateConstructionTypeDto
        {
            IsActive = false
        };

        var existingEntity = new ConstructionTypeEntity
        {
            Id = 1,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateConstructionTypeDto>(), It.IsAny<ConstructionTypeEntity>()))
            .Callback((UpdateConstructionTypeDto src, ConstructionTypeEntity dest) =>
            {
                dest.IsActive = src.IsActive;
            });

        // Mock reference validation service to return failure with error message
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<ConstructionTypeEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot deactivate/delete this Construction Type because it is referenced in: Rates, Age Factors, Nature Factors, Property Details"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.UpdateAsync(1, updateDto, CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.Value != null && error.Value.Contains("Cannot deactivate"));
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingWithoutReferences_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateConstructionTypeDto
        {
            IsActive = false
        };

        var existingEntity = new ConstructionTypeEntity
        {
            Id = 1,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ConstructionTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateConstructionTypeDto>(), It.IsAny<ConstructionTypeEntity>()))
            .Callback((UpdateConstructionTypeDto src, ConstructionTypeEntity dest) =>
            {
                dest.IsActive = src.IsActive;
            });

        _mockMapper
            .Setup(m => m.Map<ConstructionTypeDto>(It.IsAny<ConstructionTypeEntity>()))
            .Returns(new ConstructionTypeDto { Id = 1, IsActive = false });

        // Mock reference validation service to return success (no references)
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<ConstructionTypeEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ConstructionTypeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NotDeactivating_DoesNotCheckReferences()
    {
        // Arrange
        var updateDto = new UpdateConstructionTypeDto
        {
            IsActive = true
        };

        var existingEntity = new ConstructionTypeEntity
        {
            Id = 1,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ConstructionTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateConstructionTypeDto>(), It.IsAny<ConstructionTypeEntity>()))
            .Callback((UpdateConstructionTypeDto src, ConstructionTypeEntity dest) =>
            {
                dest.IsActive = src.IsActive;
            });

        _mockMapper
            .Setup(m => m.Map<ConstructionTypeDto>(It.IsAny<ConstructionTypeEntity>()))
            .Returns(new ConstructionTypeDto { Id = 1, IsActive = true });

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockReferenceValidator.Verify(r => r.ValidateReferencesAsync<ConstructionTypeEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ConstructionTypeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateConstructionTypeDto
        {
            ConstructionCode = "C123",
            Description = "Test Construction",
            SearchSequence = 1
        };
        var entity = new ConstructionTypeEntity { Id = 1, ConstructionCode = "C123", Description = "Test Construction", SearchSequence = 1, IsActive = true };
        _mockMapper.Setup(m => m.Map<ConstructionTypeEntity>(It.IsAny<CreateConstructionTypeDto>())).Returns(entity);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ConstructionTypeEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<ConstructionTypeDto>(It.IsAny<ConstructionTypeEntity>())).Returns(new ConstructionTypeDto { Id = 1, ConstructionCode = "C123", Description = "Test Construction", SearchSequence = 1 });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("C123", result.ConstructionCode);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ConstructionTypeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void CreateAsync_InvalidDto_DataAnnotationsValidation()
    {
        // Arrange
        var createDto = new CreateConstructionTypeDto
        {
            ConstructionCode = "", // Required
            Description = new string('a', 101) // Exceeds max length
        };

        // Act & Assert - Verify DataAnnotations validation rules
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(createDto);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(createDto, validationContext, results, true);
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage == "ConstructionId_Required");
        Assert.Contains(results, r => r.ErrorMessage == "Construction_Description_MaxLen_100");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new ConstructionTypeEntity { Id = 1, ConstructionCode = "C123", Description = "Test Construction", SearchSequence = 1, IsActive = true };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<ConstructionTypeDto>(entity)).Returns(new ConstructionTypeDto { Id = 1, ConstructionCode = "C123", Description = "Test Construction", SearchSequence = 1 });

        // Act
        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("C123", result.ConstructionCode);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((ConstructionTypeEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(99, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ValidDto_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateConstructionTypeDto
        {
            ConstructionCode = "C124",
            Description = "Updated Desc",
            SearchSequence = 2,
            IsActive = true
        };
        var entity = new ConstructionTypeEntity { Id = 1, ConstructionCode = "C123", Description = "Test Construction", SearchSequence = 1, IsActive = true };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ConstructionTypeEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<ConstructionTypeDto>(It.IsAny<ConstructionTypeEntity>())).Returns(new ConstructionTypeDto { Id = 1, ConstructionCode = "C124", Description = "Updated Desc", SearchSequence = 2 });
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateConstructionTypeDto>(), entity)).Callback((UpdateConstructionTypeDto src, ConstructionTypeEntity dest) =>
        {
            dest.ConstructionCode = src.ConstructionCode;
            dest.Description = src.Description;
            dest.SearchSequence = src.SearchSequence;
            dest.IsActive = src.IsActive;
        });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("C124", result.ConstructionCode);
        Assert.Equal("Updated Desc", result.Description);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ConstructionTypeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void UpdateAsync_InvalidDto_ThrowsValidationException()
    {
        // Arrange
        var updateDto = new UpdateConstructionTypeDto
        {
            ConstructionCode = "", // Required
            Description = new string('a', 101) // Exceeds max length
        };

        // Act & Assert
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(updateDto);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(updateDto, validationContext, results, true);
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage == "ConstructionId_Required");
        Assert.Contains(results, r => r.ErrorMessage == "Construction_Description_MaxLen_100");
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_DeletesSuccessfully()
    {
        // Arrange
        var entity = new ConstructionTypeEntity { Id = 1, ConstructionCode = "C123", Description = "Test Construction", SearchSequence = 1, IsActive = true };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<ConstructionTypeEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Mock reference validation service to return success (no references)
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<ConstructionTypeEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        await _service.DeleteAsync(1, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<ConstructionTypeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_DoesNothing()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((ConstructionTypeEntity?)null);

        // Act
        await _service.DeleteAsync(99, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #region Bulk Operations Tests

    [Fact]
    public async Task BulkCreateAsync_ValidItems_CreatesAllAndReturnsSuccessResult()
    {
        // Arrange
        var createDtos = new[]
        {
            new CreateConstructionTypeDto { ConstructionCode = "C1", Description = "Desc1", SearchSequence = 1 },
            new CreateConstructionTypeDto { ConstructionCode = "C2", Description = "Desc2", SearchSequence = 2 }
        };

        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map<ConstructionTypeEntity>(It.IsAny<CreateConstructionTypeDto>()))
            .Returns((CreateConstructionTypeDto dto) => new ConstructionTypeEntity
            {
                ConstructionCode = dto.ConstructionCode,
                Description = dto.Description,
                SearchSequence = dto.SearchSequence,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<ConstructionTypeEntity[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map<List<ConstructionTypeDto>>(It.IsAny<List<ConstructionTypeEntity>>()))
            .Returns((List<ConstructionTypeEntity> entities) => entities.Select(e => new ConstructionTypeDto
            {
                Id = e.Id,
                ConstructionCode = e.ConstructionCode,
                Description = e.Description,
                SearchSequence = e.SearchSequence,
                IsActive = e.IsActive
            }).ToList());

        // Act
        var result = await _service.BulkCreateAsync(createDtos, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.True(result.AllSucceeded);
    }

    #endregion
}
