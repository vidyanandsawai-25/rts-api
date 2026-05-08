using AutoMapper;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using Xunit;
using ValidationResult = NtisPlatform.Application.Models.ValidationResult;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive tests for SocietyDetails Service
/// Coverage: Service layer CRUD operations, validation, and bulk operations
/// </summary>
public class SocietyDetailsServiceTests
{
    private readonly Mock<IRepository<SocietyDetailsEntity, int>> _mockRepository;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly SocietyDetailsService _service;

    public SocietyDetailsServiceTests()
    {
        _mockRepository = new Mock<IRepository<SocietyDetailsEntity, int>>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new SocietyDetailsService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object);
    }

    #region Create Tests

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateSocietyDetailsDto
        {
            PropertyId = 549357,
            WingId = 1,
            WingName = "A Wing",
            SocietyName = "Test Society",
            SocietyAddress = "123 Main Street",
            SecretaryName = "John Doe",
            ManagerName = "Jane Smith",
            SocietyEmailId = "society@example.com",
            IsActive = true
        };

        var entity = new SocietyDetailsEntity
        {
            Id = 1,
            PropertyId = 549357,
            WingId = 1,
            WingName = "A Wing",
            SocietyName = "Test Society",
            SocietyAddress = "123 Main Street",
            SecretaryName = "John Doe",
            ManagerName = "Jane Smith",
            SocietyEmailId = "society@example.com",
            IsActive = true
        };

        _mockMapper.Setup(m => m.Map<SocietyDetailsEntity>(It.IsAny<CreateSocietyDetailsDto>())).Returns(entity);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<SocietyDetailsEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<SocietyDetailsDto>(It.IsAny<SocietyDetailsEntity>())).Returns(new SocietyDetailsDto
        {
            Id = 1,
            PropertyId = 549357,
            WingId = 1,
            WingName = "A Wing",
            SocietyName = "Test Society"
        });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(549357, result.PropertyId);
        Assert.Equal("Test Society", result.SocietyName);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<SocietyDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void CreateAsync_InvalidEmail_DataAnnotationsValidation()
    {
        // Arrange
        var createDto = new CreateSocietyDetailsDto
        {
            PropertyId = 1,
            WingId = 1,
            SocietyEmailId = "invalid-email",
            SecretaryEmailId = "another-invalid",
            ManagerEmailId = "not-an-email"
        };

        // Act & Assert - Verify DataAnnotations validation rules
        var validationContext = new ValidationContext(createDto);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(createDto, validationContext, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage == "SocietyDetails_SocietyEmailId_Invalid");
        Assert.Contains(results, r => r.ErrorMessage == "SocietyDetails_SecretaryEmailId_Invalid");
        Assert.Contains(results, r => r.ErrorMessage == "SocietyDetails_ManagerEmailId_Invalid");
    }

    [Fact]
    public void CreateAsync_ExceedsMaxLength_DataAnnotationsValidation()
    {
        // Arrange
        var createDto = new CreateSocietyDetailsDto
        {
            PropertyId = 1,
            WingId = 1,
            WingName = new string('a', 31), // Exceeds max length of 30
            SocietyName = new string('b', 501), // Exceeds max length of 500
            SocietyAddress = new string('c', 201), // Exceeds max length of 200
            ManagerMobileNo = new string('1', 14) // Exceeds max length of 13
        };

        // Act & Assert
        var validationContext = new ValidationContext(createDto);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(createDto, validationContext, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage == "SocietyDetails_WingName_MaxLen_30");
        Assert.Contains(results, r => r.ErrorMessage == "SocietyDetails_SocietyName_MaxLen_500");
        Assert.Contains(results, r => r.ErrorMessage == "SocietyDetails_SocietyAddress_MaxLen_200");
        Assert.Contains(results, r => r.ErrorMessage == "SocietyDetails_ManagerMobileNo_MaxLen_13");
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new SocietyDetailsEntity
        {
            Id = 1,
            PropertyId = 549357,
            SocietyName = "Test Society",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<SocietyDetailsDto>(entity)).Returns(new SocietyDetailsDto
        {
            Id = 1,
            PropertyId = 549357,
            SocietyName = "Test Society"
        });

        // Act
        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(549357, result.PropertyId);
        Assert.Equal("Test Society", result.SocietyName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((SocietyDetailsEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(99, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task UpdateAsync_ValidDto_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateSocietyDetailsDto
        {
            PropertyId = 549357,
            SocietyName = "Updated Society",
            SocietyAddress = "456 New Street",
            IsActive = true
        };

        var entity = new SocietyDetailsEntity
        {
            Id = 1,
            PropertyId = 549357,
            SocietyName = "Original Society",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<SocietyDetailsEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<SocietyDetailsDto>(It.IsAny<SocietyDetailsEntity>())).Returns(new SocietyDetailsDto
        {
            Id = 1,
            PropertyId = 549357,
            SocietyName = "Updated Society",
            SocietyAddress = "456 New Street"
        });
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateSocietyDetailsDto>(), entity)).Callback((UpdateSocietyDetailsDto src, SocietyDetailsEntity dest) =>
        {
            dest.SocietyName = src.SocietyName;
            dest.SocietyAddress = src.SocietyAddress;
            dest.IsActive = src.IsActive;
        });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Society", result.SocietyName);
        Assert.Equal("456 New Street", result.SocietyAddress);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<SocietyDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingWithReferences_ThrowsValidationException()
    {
        // Arrange
        var updateDto = new UpdateSocietyDetailsDto
        {
            IsActive = false
        };

        var existingEntity = new SocietyDetailsEntity
        {
            Id = 1,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateSocietyDetailsDto>(), It.IsAny<SocietyDetailsEntity>()))
            .Callback((UpdateSocietyDetailsDto src, SocietyDetailsEntity dest) =>
            {
                dest.IsActive = src.IsActive;
            });

        // Mock reference validation service to return failure
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<SocietyDetailsEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot deactivate/delete this Society Details because it is referenced in: Properties"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.UpdateAsync(1, updateDto, CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.Value != null && error.Value.Contains("Cannot deactivate"));
    }

    [Fact]
    public async Task UpdateAsync_DeactivatingWithoutReferences_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateSocietyDetailsDto
        {
            IsActive = false
        };

        var existingEntity = new SocietyDetailsEntity
        {
            Id = 1,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<SocietyDetailsEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateSocietyDetailsDto>(), It.IsAny<SocietyDetailsEntity>()))
            .Callback((UpdateSocietyDetailsDto src, SocietyDetailsEntity dest) =>
            {
                dest.IsActive = src.IsActive;
            });

        _mockMapper
            .Setup(m => m.Map<SocietyDetailsDto>(It.IsAny<SocietyDetailsEntity>()))
            .Returns(new SocietyDetailsDto { Id = 1, IsActive = false });

        // Mock reference validation service to return success (no references)
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<SocietyDetailsEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<SocietyDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NotDeactivating_DoesNotCheckReferences()
    {
        // Arrange
        var updateDto = new UpdateSocietyDetailsDto
        {
            SocietyName = "Updated Name",
            IsActive = true
        };

        var existingEntity = new SocietyDetailsEntity
        {
            Id = 1,
            SocietyName = "Original Name",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<SocietyDetailsEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateSocietyDetailsDto>(), It.IsAny<SocietyDetailsEntity>()))
            .Callback((UpdateSocietyDetailsDto src, SocietyDetailsEntity dest) =>
            {
                dest.SocietyName = src.SocietyName;
                dest.IsActive = src.IsActive;
            });

        _mockMapper
            .Setup(m => m.Map<SocietyDetailsDto>(It.IsAny<SocietyDetailsEntity>()))
            .Returns(new SocietyDetailsDto { Id = 1, SocietyName = "Updated Name", IsActive = true });

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockReferenceValidator.Verify(r => r.ValidateReferencesAsync<SocietyDetailsEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<SocietyDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void UpdateAsync_InvalidDto_DataAnnotationsValidation()
    {
        // Arrange
        var updateDto = new UpdateSocietyDetailsDto
        {
            PropertyId = 1,
            WingId = 1,
            SocietyName = new string('a', 501), // Exceeds max length
            SecretaryEmailId = "invalid-email"
        };

        // Act & Assert
        var validationContext = new ValidationContext(updateDto);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(updateDto, validationContext, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage == "SocietyDetails_SocietyName_MaxLen_500");
        Assert.Contains(results, r => r.ErrorMessage == "SocietyDetails_SecretaryEmailId_Invalid");
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteAsync_ExistingId_DeletesSuccessfully()
    {
        // Arrange
        var entity = new SocietyDetailsEntity
        {
            Id = 1,
            PropertyId = 549357,
            SocietyName = "Test Society",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<SocietyDetailsEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Mock reference validation service to return success (no references)
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<SocietyDetailsEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        await _service.DeleteAsync(1, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<SocietyDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_DoesNothing()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((SocietyDetailsEntity?)null);

        // Act
        await _service.DeleteAsync(99, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<SocietyDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ThrowsValidationException()
    {
        // Arrange
        var entity = new SocietyDetailsEntity
        {
            Id = 1,
            PropertyId = 549357,
            SocietyName = "Test Society",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        // Mock reference validation service to return failure
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<SocietyDetailsEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot delete this Society Details because it is referenced in: Properties"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.DeleteAsync(1, CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.Value != null && error.Value.Contains("Cannot delete"));
    }

    #endregion

    #region Bulk Operations Tests

    [Fact]
    public async Task BulkCreateAsync_ValidItems_CreatesAllAndReturnsSuccessResult()
    {
        // Arrange
        var createDtos = new[]
        {
            new CreateSocietyDetailsDto { PropertyId = 1, WingId = 1, SocietyName = "Society 1", IsActive = true },
            new CreateSocietyDetailsDto { PropertyId = 2, WingId = 2, SocietyName = "Society 2", IsActive = true }
        };

        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map<SocietyDetailsEntity>(It.IsAny<CreateSocietyDetailsDto>()))
            .Returns((CreateSocietyDetailsDto dto) => new SocietyDetailsEntity
            {
                PropertyId = dto.PropertyId,
                WingId = dto.WingId,
                SocietyName = dto.SocietyName,
                IsActive = dto.IsActive
            });

        _mockRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<SocietyDetailsEntity[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map<List<SocietyDetailsDto>>(It.IsAny<List<SocietyDetailsEntity>>()))
            .Returns((List<SocietyDetailsEntity> entities) => entities.Select(e => new SocietyDetailsDto
            {
                Id = e.Id,
                PropertyId = e.PropertyId,
                WingId = e.WingId,
                SocietyName = e.SocietyName,
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

/// <summary>
/// Tests for SocietyDetailsDto classes
/// </summary>
public class SocietyDetailsDtoTests
{
    [Fact]
    public void SocietyDetailsDto_AllProperties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var dto = new SocietyDetailsDto
        {
            Id = 1,
            PropertyId = 549357,
            WingId = 5,
            WingName = "West Wing",
            SocietyName = "ABC Society",
            SocietyAddress = "123 Main Street",
            SecretaryName = "John Secretary",
            ManagerName = "Jane Manager",
            LandOwnerName = "Land Owner",
            BuilderName = "Builder Corp",
            SocietyNameEnglish = "ABC Society Eng",
            SocietyAddressEnglish = "123 Main St Eng",
            SecretaryNameEnglish = "John Sec Eng",
            ManagerNameEnglish = "Jane Mgr Eng",
            LandOwnerNameEnglish = "Land Owner Eng",
            BuilderNameEnglish = "Builder Eng",
            ManagerMobileNo = "9876543210",
            SecretaryMobileNo = "8765432109",
            SocietyEmailId = "society@example.com",
            SecretaryEmailId = "secretary@example.com",
            ManagerEmailId = "manager@example.com",
            MarkedForDeletion = false,
            IsActive = true
        };

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal(549357, dto.PropertyId);
        Assert.Equal(5, dto.WingId);
        Assert.Equal("West Wing", dto.WingName);
        Assert.Equal("ABC Society", dto.SocietyName);
        Assert.Equal("123 Main Street", dto.SocietyAddress);
        Assert.Equal("John Secretary", dto.SecretaryName);
        Assert.Equal("Jane Manager", dto.ManagerName);
        Assert.Equal("Land Owner", dto.LandOwnerName);
        Assert.Equal("Builder Corp", dto.BuilderName);
        Assert.Equal("ABC Society Eng", dto.SocietyNameEnglish);
        Assert.Equal("123 Main St Eng", dto.SocietyAddressEnglish);
        Assert.Equal("John Sec Eng", dto.SecretaryNameEnglish);
        Assert.Equal("Jane Mgr Eng", dto.ManagerNameEnglish);
        Assert.Equal("Land Owner Eng", dto.LandOwnerNameEnglish);
        Assert.Equal("Builder Eng", dto.BuilderNameEnglish);
        Assert.Equal("9876543210", dto.ManagerMobileNo);
        Assert.Equal("8765432109", dto.SecretaryMobileNo);
        Assert.Equal("society@example.com", dto.SocietyEmailId);
        Assert.Equal("secretary@example.com", dto.SecretaryEmailId);
        Assert.Equal("manager@example.com", dto.ManagerEmailId);
        Assert.False(dto.MarkedForDeletion);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public void SocietyDetailsDto_AllOptionalProperties_CanBeNull()
    {
        // Arrange & Act
        var dto = new SocietyDetailsDto
        {
            Id = 1
        };

        // Assert
        Assert.Null(dto.PropertyId);
        Assert.Null(dto.WingId);
        Assert.Null(dto.WingName);
        Assert.Null(dto.SocietyName);
        Assert.Null(dto.SocietyAddress);
        Assert.Null(dto.SecretaryName);
        Assert.Null(dto.ManagerName);
        Assert.Null(dto.LandOwnerName);
        Assert.Null(dto.BuilderName);
        Assert.Null(dto.SocietyNameEnglish);
        Assert.Null(dto.SocietyAddressEnglish);
        Assert.Null(dto.SecretaryNameEnglish);
        Assert.Null(dto.ManagerNameEnglish);
        Assert.Null(dto.LandOwnerNameEnglish);
        Assert.Null(dto.BuilderNameEnglish);
        Assert.Null(dto.ManagerMobileNo);
        Assert.Null(dto.SecretaryMobileNo);
        Assert.Null(dto.SocietyEmailId);
        Assert.Null(dto.SecretaryEmailId);
        Assert.Null(dto.ManagerEmailId);
    }

    [Fact]
    public void CreateSocietyDetailsDto_ValidEmail_PassesValidation()
    {
        // Arrange
        var dto = new CreateSocietyDetailsDto
        {
            PropertyId = 1,
            WingId = 1,
            SocietyEmailId = "valid@example.com",
            SecretaryEmailId = "secretary@example.com",
            ManagerEmailId = "manager@example.com"
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, validationContext, results, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void CreateSocietyDetailsDto_ValidMobileNo_PassesValidation()
    {
        // Arrange
        var dto = new CreateSocietyDetailsDto
        {
            PropertyId = 1,
            WingId = 1,
            ManagerMobileNo = "9876543210",
            SecretaryMobileNo = "8765432109"
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, validationContext, results, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateSocietyDetailsDto_AllFieldsWithinMaxLength_PassesValidation()
    {
        // Arrange
        var dto = new UpdateSocietyDetailsDto
        {
            PropertyId = 1,
            WingId = 1,
            WingName = new string('a', 30),
            SocietyName = new string('b', 500),
            SocietyAddress = new string('c', 200),
            SecretaryName = new string('d', 200),
            ManagerName = new string('e', 200),
            LandOwnerName = new string('f', 200),
            BuilderName = new string('g', 200),
            ManagerMobileNo = new string('1', 13),
            SecretaryMobileNo = new string('2', 13),
            SocietyEmailId = "valid@example.com",
            SecretaryEmailId = "secretary@example.com",
            ManagerEmailId = "manager@example.com"
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, validationContext, results, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }
}

/// <summary>
/// Tests for SocietyDetailsQueryParameters
/// </summary>
public class SocietyDetailsQueryParametersTests
{
    [Fact]
    public void SocietyDetailsQueryParameters_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var parameters = new SocietyDetailsQueryParameters();

        // Assert
        Assert.Equal(1, parameters.PageNumber);
        Assert.Equal(10, parameters.PageSize);
        Assert.Null(parameters.SearchTerm);
        Assert.Null(parameters.SortBy);
        Assert.Equal("asc", parameters.SortOrder);
    }

    [Fact]
    public void SocietyDetailsQueryParameters_FilterProperties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var parameters = new SocietyDetailsQueryParameters
        {
            PropertyId = 549357,
            WingId = 5,
            WingName = "A Wing",
            SocietyName = "Test Society",
            SecretaryName = "John Doe",
            ManagerName = "Jane Smith",
            SocietyEmailId = "society@example.com",
            PageNumber = 2,
            PageSize = 20,
            SearchTerm = "test",
            SortBy = "SocietyName",
            SortOrder = "desc"
        };

        // Assert
        Assert.Equal(549357, parameters.PropertyId);
        Assert.Equal(5, parameters.WingId);
        Assert.Equal("A Wing", parameters.WingName);
        Assert.Equal("Test Society", parameters.SocietyName);
        Assert.Equal("John Doe", parameters.SecretaryName);
        Assert.Equal("Jane Smith", parameters.ManagerName);
        Assert.Equal("society@example.com", parameters.SocietyEmailId);
        Assert.Equal(2, parameters.PageNumber);
        Assert.Equal(20, parameters.PageSize);
        Assert.Equal("test", parameters.SearchTerm);
        Assert.Equal("SocietyName", parameters.SortBy);
        Assert.Equal("desc", parameters.SortOrder);
    }

    [Fact]
    public void SocietyDetailsQueryParameters_PageSizeExceedsMax_IsCappedAt100()
    {
        // Arrange & Act
        var parameters = new SocietyDetailsQueryParameters
        {
            PageSize = 500
        };

        // Assert
        Assert.Equal(100, parameters.PageSize);
    }

    [Fact]
    public void SocietyDetailsQueryParameters_PageNumberLessThan1_SetsTo1()
    {
        // Arrange & Act
        var parameters = new SocietyDetailsQueryParameters
        {
            PageNumber = -5
        };

        // Assert
        Assert.Equal(1, parameters.PageNumber);
    }

    [Fact]
    public void SocietyDetailsQueryParameters_PageSizeNegative1_AllowsAllResults()
    {
        // Arrange & Act
        var parameters = new SocietyDetailsQueryParameters
        {
            PageSize = -1
        };

        // Assert
        Assert.Equal(-1, parameters.PageSize);
    }
}

/// <summary>
/// Tests for SocietyDetailsEntity
/// </summary>
public class SocietyDetailsEntityTests
{
    [Fact]
    public void SocietyDetailsEntity_AllProperties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var now = DateTime.UtcNow;
        var entity = new SocietyDetailsEntity
        {
            Id = 1,
            PropertyId = 549357,
            WingId = 5,
            WingName = "West Wing",
            SocietyName = "ABC Society",
            SocietyAddress = "123 Main Street",
            SecretaryName = "John Secretary",
            ManagerName = "Jane Manager",
            LandOwnerName = "Land Owner",
            BuilderName = "Builder Corp",
            SecretaryNameEnglish = "John Sec Eng",
            SocietyNameEnglish = "ABC Society Eng",
            SocietyAddressEnglish = "123 Main St Eng",
            ManagerNameEnglish = "Jane Mgr Eng",
            LandOwnerNameEnglish = "Land Owner Eng",
            BuilderNameEnglish = "Builder Eng",
            ManagerMobileNo = "9876543210",
            SecretaryMobileNo = "8765432109",
            SocietyEmailId = "society@example.com",
            SecretaryEmailId = "secretary@example.com",
            ManagerEmailId = "manager@example.com",
            MarkedForDeletion = false,
            MarkedForDeletionDate = null,
            IsActive = true,
            CreatedDate = now,
            UpdatedDate = now,
            CreatedBy = 1,
            UpdatedBy = 2
        };

        // Assert
        Assert.Equal(1, entity.Id);
        Assert.Equal(549357, entity.PropertyId);
        Assert.Equal(5, entity.WingId);
        Assert.Equal("West Wing", entity.WingName);
        Assert.Equal("ABC Society", entity.SocietyName);
        Assert.Equal("123 Main Street", entity.SocietyAddress);
        Assert.Equal("John Secretary", entity.SecretaryName);
        Assert.Equal("Jane Manager", entity.ManagerName);
        Assert.Equal("Land Owner", entity.LandOwnerName);
        Assert.Equal("Builder Corp", entity.BuilderName);
        Assert.Equal("John Sec Eng", entity.SecretaryNameEnglish);
        Assert.Equal("ABC Society Eng", entity.SocietyNameEnglish);
        Assert.Equal("123 Main St Eng", entity.SocietyAddressEnglish);
        Assert.Equal("Jane Mgr Eng", entity.ManagerNameEnglish);
        Assert.Equal("Land Owner Eng", entity.LandOwnerNameEnglish);
        Assert.Equal("Builder Eng", entity.BuilderNameEnglish);
        Assert.Equal("9876543210", entity.ManagerMobileNo);
        Assert.Equal("8765432109", entity.SecretaryMobileNo);
        Assert.Equal("society@example.com", entity.SocietyEmailId);
        Assert.Equal("secretary@example.com", entity.SecretaryEmailId);
        Assert.Equal("manager@example.com", entity.ManagerEmailId);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
        Assert.True(entity.IsActive);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(now, entity.UpdatedDate);
        Assert.Equal(1, entity.CreatedBy);
        Assert.Equal(2, entity.UpdatedBy);
    }

    [Fact]
    public void SocietyDetailsEntity_MarkedForDeletion_DefaultsToFalse()
    {
        // Arrange & Act
        var entity = new SocietyDetailsEntity();

        // Assert
        Assert.False(entity.MarkedForDeletion);
    }

    [Fact]
    public void SocietyDetailsEntity_IsActive_DefaultsToTrue()
    {
        // Arrange & Act
        var entity = new SocietyDetailsEntity();

        // Assert
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void SocietyDetailsEntity_InheritsFromBaseEntity()
    {
        // Arrange & Act
        var entity = new SocietyDetailsEntity();

        // Assert
        Assert.IsAssignableFrom<BaseEntity>(entity);
    }
}
