using AutoMapper;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using Xunit;
using MockQueryable;
using MockQueryable.Moq;
using ValidationResult = NtisPlatform.Application.Models.ValidationResult;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive tests for SocietyWingDetailsService
/// Coverage: Service layer CRUD operations, validation, transactional behavior, and DTO validation
/// </summary>
public class SocietyWingDetailsServiceTests
{
    private readonly Mock<IRepository<SocietyWingDetailsEntity, int>> _mockRepository;
    private readonly Mock<IRepository<SocietyDetailsEntity, int>> _mockSocietyDetailsRepo;
    private readonly Mock<IRepository<PropertyEntity, int>> _mockPropertyRepo;
    private readonly Mock<IRepository<WingEntity, int>> _mockWingRepo;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly SocietyWingDetailsService _service;

    public SocietyWingDetailsServiceTests()
    {
        _mockRepository = new Mock<IRepository<SocietyWingDetailsEntity, int>>();
        _mockSocietyDetailsRepo = new Mock<IRepository<SocietyDetailsEntity, int>>();
        _mockPropertyRepo = new Mock<IRepository<PropertyEntity, int>>();
        _mockWingRepo = new Mock<IRepository<WingEntity, int>>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new SocietyWingDetailsService(
            _mockRepository.Object,
            _mockPropertyRepo.Object,
            _mockWingRepo.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object,
            _mockSocietyDetailsRepo.Object);
    }

    #region Create Tests

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateSocietyWingDetailsDto
        {
            WingId = 1,
            PropertyId = 100,
            NewWingName = "A Wing",
            FromFloor = "1",
            ToFloor = "5",
            NoOfFlat = 10,
            NoOfShop = 2,
            CreatedBy = 1
        };

        var propertyData = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 100, IsActive = true, SocietyDetailId = 5 }
        };
        _mockPropertyRepo.Setup(r => r.GetQueryable())
            .Returns(propertyData.BuildMock());

        var wingData = new List<WingEntity>
        {
            new WingEntity { Id = 1, IsActive = true, WingNo = "A" }
        };
        _mockWingRepo.Setup(r => r.GetQueryable())
            .Returns(wingData.BuildMock());

        var societyData = new List<SocietyDetailsEntity>();
        _mockSocietyDetailsRepo.Setup(r => r.GetQueryable())
            .Returns(societyData.BuildMock());

        var entity = new SocietyWingDetailsEntity
        {
            Id = 1,
            WingId = 1,
            PropertyId = 100,
            NewWingName = "A Wing",
            FromFloor = "1",
            ToFloor = "5",
            NoOfFlat = 10,
            NoOfShop = 2,
            IsActive = true
        };

        _mockMapper.Setup(m => m.Map<SocietyWingDetailsEntity>(It.IsAny<CreateSocietyWingDetailsDto>())).Returns(entity);
        _mockMapper.Setup(m => m.Map<SocietyDetailsEntity>(It.IsAny<CreateSocietyWingDetailsDto>())).Returns(new SocietyDetailsEntity());
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<SocietyWingDetailsEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockSocietyDetailsRepo.Setup(r => r.AddAsync(It.IsAny<SocietyDetailsEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync(new SocietyDetailsEntity());
        _mockMapper.Setup(m => m.Map<SocietyWingDetailsDto>(It.IsAny<SocietyWingDetailsEntity>())).Returns(new SocietyWingDetailsDto
        {
            Id = 1,
            WingId = 1,
            PropertyId = 100,
            NewWingName = "A Wing"
        });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.PropertyId);
        Assert.Equal("A Wing", result.NewWingName);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<SocietyWingDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidPropertyId_ThrowsValidationException()
    {
        // Arrange
        var createDto = new CreateSocietyWingDetailsDto
        {
            WingId = 1,
            PropertyId = 999,
            NewWingName = "A Wing"
        };

        var propertyData = new List<PropertyEntity>();
        _mockPropertyRepo.Setup(r => r.GetQueryable())
            .Returns(propertyData.BuildMock());

        var wingData = new List<WingEntity>
        {
            new WingEntity { Id = 1, IsActive = true, WingNo = "A" }
        };
        _mockWingRepo.Setup(r => r.GetQueryable())
            .Returns(wingData.BuildMock());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));

        Assert.Contains("PropertyId", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_InvalidWingId_ThrowsValidationException()
    {
        // Arrange
        var createDto = new CreateSocietyWingDetailsDto
        {
            WingId = 999,
            PropertyId = 100,
            NewWingName = "A Wing"
        };

        var propertyData = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 100, IsActive = true }
        };
        _mockPropertyRepo.Setup(r => r.GetQueryable())
            .Returns(propertyData.BuildMock());

        var wingData = new List<WingEntity>();
        _mockWingRepo.Setup(r => r.GetQueryable())
            .Returns(wingData.BuildMock());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));

        Assert.Contains("WingId", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_FromFloorGreaterThanToFloor_ThrowsValidationException()
    {
        // Arrange
        var createDto = new CreateSocietyWingDetailsDto
        {
            WingId = 1,
            PropertyId = 100,
            FromFloor = "10",
            ToFloor = "5",
            NewWingName = "A Wing"
        };

        var propertyData = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 100, IsActive = true }
        };
        _mockPropertyRepo.Setup(r => r.GetQueryable())
            .Returns(propertyData.BuildMock());

        var wingData = new List<WingEntity>
        {
            new WingEntity { Id = 1, IsActive = true, WingNo = "A" }
        };
        _mockWingRepo.Setup(r => r.GetQueryable())
            .Returns(wingData.BuildMock());

        var societyData = new List<SocietyDetailsEntity>();
        _mockSocietyDetailsRepo.Setup(r => r.GetQueryable())
            .Returns(societyData.BuildMock());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));

        Assert.Contains("FromFloor cannot be greater than ToFloor", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_NonNumericFloors_ThrowsValidationException()
    {
        // Arrange
        var createDto = new CreateSocietyWingDetailsDto
        {
            WingId = 1,
            PropertyId = 100,
            FromFloor = "abc",
            ToFloor = "xyz",
            NewWingName = "A Wing"
        };

        var propertyData = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 100, IsActive = true }
        };
        _mockPropertyRepo.Setup(r => r.GetQueryable())
            .Returns(propertyData.BuildMock());

        var wingData = new List<WingEntity>
        {
            new WingEntity { Id = 1, IsActive = true, WingNo = "A" }
        };
        _mockWingRepo.Setup(r => r.GetQueryable())
            .Returns(wingData.BuildMock());

        var societyData = new List<SocietyDetailsEntity>();
        _mockSocietyDetailsRepo.Setup(r => r.GetQueryable())
            .Returns(societyData.BuildMock());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));

        Assert.Contains("valid numeric values", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_DuplicateWingDetails_ThrowsValidationException()
    {
        // Arrange
        var createDto = new CreateSocietyWingDetailsDto
        {
            WingId = 1,
            PropertyId = 100,
            NewWingName = "A Wing"
        };

        var propertyData = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 100, IsActive = true }
        };
        _mockPropertyRepo.Setup(r => r.GetQueryable())
            .Returns(propertyData.BuildMock());

        var wingData = new List<WingEntity>
        {
            new WingEntity { Id = 1, IsActive = true, WingNo = "A" }
        };
        _mockWingRepo.Setup(r => r.GetQueryable())
            .Returns(wingData.BuildMock());

        // Duplicate exists
        var societyData = new List<SocietyDetailsEntity>
        {
            new SocietyDetailsEntity { PropertyId = 100, WingId = 1, WingName = "A Wing", IsActive = true }
        };
        _mockSocietyDetailsRepo.Setup(r => r.GetQueryable())
            .Returns(societyData.BuildMock());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));

        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_TransactionRollsBackOnFailure()
    {
        // Arrange
        var createDto = new CreateSocietyWingDetailsDto
        {
            WingId = 1,
            PropertyId = 100,
            NewWingName = "A Wing"
        };

        var propertyData = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 100, IsActive = true }
        };
        _mockPropertyRepo.Setup(r => r.GetQueryable())
            .Returns(propertyData.BuildMock());

        var wingData = new List<WingEntity>
        {
            new WingEntity { Id = 1, IsActive = true, WingNo = "A" }
        };
        _mockWingRepo.Setup(r => r.GetQueryable())
            .Returns(wingData.BuildMock());

        var societyData = new List<SocietyDetailsEntity>();
        _mockSocietyDetailsRepo.Setup(r => r.GetQueryable())
            .Returns(societyData.BuildMock());

        _mockMapper.Setup(m => m.Map<SocietyWingDetailsEntity>(It.IsAny<CreateSocietyWingDetailsDto>()))
            .Returns(new SocietyWingDetailsEntity());
        _mockMapper.Setup(m => m.Map<SocietyDetailsEntity>(It.IsAny<CreateSocietyWingDetailsDto>()))
            .Returns(new SocietyDetailsEntity());
        _mockSocietyDetailsRepo.Setup(r => r.AddAsync(It.IsAny<SocietyDetailsEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SocietyDetailsEntity());

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<SocietyWingDetailsEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _service.CreateAsync(createDto, CancellationToken.None));

        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task UpdateAsync_ValidDto_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateSocietyWingDetailsDto
        {
            WingId = 1,
            PropertyId = 100,
            SocietyDetailId = 5,
            NewWingName = "Updated Wing",
            FromFloor = "1",
            ToFloor = "10",
            UpdatedBy = 2
        };

        var propertyData = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 100, IsActive = true }
        };
        _mockPropertyRepo.Setup(r => r.GetQueryable())
            .Returns(propertyData.BuildMock());

        var wingData = new List<WingEntity>
        {
            new WingEntity { Id = 1, IsActive = true, WingNo = "A" }
        };
        _mockWingRepo.Setup(r => r.GetQueryable())
            .Returns(wingData.BuildMock());

        var societyData = new List<SocietyDetailsEntity>
        {
            new SocietyDetailsEntity { Id = 5, PropertyId = 100, IsActive = true }
        };
        _mockSocietyDetailsRepo.Setup(r => r.GetQueryable())
            .Returns(societyData.BuildMock());

        var entity = new SocietyWingDetailsEntity
        {
            Id = 1,
            SocietyDetailId = 5,
            WingId = 1,
            PropertyId = 100,
            NewWingName = "Original Wing",
            IsActive = true
        };
        var wingQueryData = new List<SocietyWingDetailsEntity> { entity };
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(wingQueryData.BuildMock());

        var existingSocietyDetails = new SocietyDetailsEntity { Id = 5, PropertyId = 100, IsActive = true };
        _mockSocietyDetailsRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSocietyDetails);

        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateSocietyWingDetailsDto>(), It.IsAny<SocietyWingDetailsEntity>()));
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateSocietyWingDetailsDto>(), It.IsAny<SocietyDetailsEntity>()));
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<SocietyWingDetailsEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockSocietyDetailsRepo.Setup(r => r.UpdateAsync(It.IsAny<SocietyDetailsEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockMapper.Setup(m => m.Map<SocietyWingDetailsDto>(It.IsAny<SocietyWingDetailsEntity>())).Returns(new SocietyWingDetailsDto
        {
            Id = 1,
            WingId = 1,
            PropertyId = 100,
            NewWingName = "Updated Wing"
        });

        // Act
        var result = await _service.UpdateAsync(5, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Wing", result.NewWingName);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<SocietyWingDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_InvalidPropertyId_ThrowsValidationException()
    {
        // Arrange
        var updateDto = new UpdateSocietyWingDetailsDto
        {
            WingId = 1,
            PropertyId = 999,
            SocietyDetailId = 5,
            NewWingName = "Updated Wing"
        };

        var propertyData = new List<PropertyEntity>();
        _mockPropertyRepo.Setup(r => r.GetQueryable())
            .Returns(propertyData.BuildMock());

        var wingData = new List<WingEntity>
        {
            new WingEntity { Id = 1, IsActive = true, WingNo = "A" }
        };
        _mockWingRepo.Setup(r => r.GetQueryable())
            .Returns(wingData.BuildMock());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.UpdateAsync(5, updateDto, CancellationToken.None));

        Assert.Contains("PropertyId", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_InvalidWingId_ThrowsValidationException()
    {
        // Arrange
        var updateDto = new UpdateSocietyWingDetailsDto
        {
            WingId = 999,
            PropertyId = 100,
            SocietyDetailId = 5,
            NewWingName = "Updated Wing"
        };

        var propertyData = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 100, IsActive = true }
        };
        _mockPropertyRepo.Setup(r => r.GetQueryable())
            .Returns(propertyData.BuildMock());

        var wingData = new List<WingEntity>();
        _mockWingRepo.Setup(r => r.GetQueryable())
            .Returns(wingData.BuildMock());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.UpdateAsync(5, updateDto, CancellationToken.None));

        Assert.Contains("WingId", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_FromFloorGreaterThanToFloor_ThrowsValidationException()
    {
        // Arrange
        var updateDto = new UpdateSocietyWingDetailsDto
        {
            WingId = 1,
            PropertyId = 100,
            SocietyDetailId = 5,
            FromFloor = "10",
            ToFloor = "3"
        };

        var propertyData = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 100, IsActive = true }
        };
        _mockPropertyRepo.Setup(r => r.GetQueryable())
            .Returns(propertyData.BuildMock());

        var wingData = new List<WingEntity>
        {
            new WingEntity { Id = 1, IsActive = true, WingNo = "A" }
        };
        _mockWingRepo.Setup(r => r.GetQueryable())
            .Returns(wingData.BuildMock());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.UpdateAsync(5, updateDto, CancellationToken.None));

        Assert.Contains("FromFloor cannot be greater than ToFloor", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_EntityNotFound_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateSocietyWingDetailsDto
        {
            WingId = 1,
            PropertyId = 100,
            SocietyDetailId = 5,
            NewWingName = "Updated Wing"
        };

        var propertyData = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 100, IsActive = true }
        };
        _mockPropertyRepo.Setup(r => r.GetQueryable())
            .Returns(propertyData.BuildMock());

        var wingData = new List<WingEntity>
        {
            new WingEntity { Id = 1, IsActive = true, WingNo = "A" }
        };
        _mockWingRepo.Setup(r => r.GetQueryable())
            .Returns(wingData.BuildMock());

        var societyData = new List<SocietyDetailsEntity>
        {
            new SocietyDetailsEntity { Id = 5, PropertyId = 100, IsActive = true }
        };
        _mockSocietyDetailsRepo.Setup(r => r.GetQueryable())
            .Returns(societyData.BuildMock());

        // No entity found
        var emptyWingData = new List<SocietyWingDetailsEntity>();
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(emptyWingData.BuildMock());

        // Act
        var result = await _service.UpdateAsync(5, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteAsync_ExistingId_SoftDeletesBothRecords()
    {
        // Arrange
        var entity = new SocietyWingDetailsEntity
        {
            Id = 1,
            SocietyDetailId = 5,
            PropertyId = 100,
            IsActive = true
        };

        var wingData = new List<SocietyWingDetailsEntity> { entity };
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(wingData.BuildMock());

        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<SocietyWingDetailsEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<SocietyWingDetailsEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var societyDetails = new SocietyDetailsEntity { Id = 5, IsActive = true };
        _mockSocietyDetailsRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(societyDetails);
        _mockSocietyDetailsRepo.Setup(r => r.UpdateAsync(It.IsAny<SocietyDetailsEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(5, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.False(entity.IsActive);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<SocietyWingDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockSocietyDetailsRepo.Verify(r => r.UpdateAsync(It.IsAny<SocietyDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsFalse()
    {
        // Arrange
        var emptyData = new List<SocietyWingDetailsEntity>();
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(emptyData.BuildMock());

        // Act
        var result = await _service.DeleteAsync(99, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ThrowsValidationException()
    {
        // Arrange
        var entity = new SocietyWingDetailsEntity
        {
            Id = 1,
            SocietyDetailId = 5,
            PropertyId = 100,
            IsActive = true
        };

        var wingData = new List<SocietyWingDetailsEntity> { entity };
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(wingData.BuildMock());

        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<SocietyWingDetailsEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot delete this record because it is referenced"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.DeleteAsync(5, CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.Value != null && error.Value.Contains("Cannot delete"));
    }

    [Fact]
    public async Task DeleteAsync_InactiveEntity_ReturnsFalse()
    {
        // Arrange
        var entity = new SocietyWingDetailsEntity
        {
            Id = 1,
            SocietyDetailId = 5,
            PropertyId = 100,
            IsActive = false
        };

        var wingData = new List<SocietyWingDetailsEntity> { entity };
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(wingData.BuildMock());

        // Act
        var result = await _service.DeleteAsync(5, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    #endregion
}

/// <summary>
/// Tests for SocietyWingDetailsDto validation attributes
/// </summary>
public class SocietyWingDetailsDtoTests
{
    [Fact]
    public void SocietyWingDetailsDto_AllProperties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var dto = new SocietyWingDetailsDto
        {
            Id = 1,
            WingId = 5,
            PropertyId = 100,
            SocietyDetailId = 10,
            FromFloor = "1",
            ToFloor = "5",
            OldWingName = "Old A",
            NewWingName = "New A",
            NoOfFlat = 20,
            NoOfShop = 5,
            NoOfRowHouse = 3,
            WingPhoto = 101,
            BoardPhoto = 102,
            CreatedBy = 1,
            UpdatedBy = 2,
            IsActive = true
        };

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal(5, dto.WingId);
        Assert.Equal(100, dto.PropertyId);
        Assert.Equal(10, dto.SocietyDetailId);
        Assert.Equal("1", dto.FromFloor);
        Assert.Equal("5", dto.ToFloor);
        Assert.Equal("Old A", dto.OldWingName);
        Assert.Equal("New A", dto.NewWingName);
        Assert.Equal(20, dto.NoOfFlat);
        Assert.Equal(5, dto.NoOfShop);
        Assert.Equal(3, dto.NoOfRowHouse);
        Assert.Equal(101, dto.WingPhoto);
        Assert.Equal(102, dto.BoardPhoto);
        Assert.Equal(1, dto.CreatedBy);
        Assert.Equal(2, dto.UpdatedBy);
        Assert.True(dto.IsActive);
    }

    #region CreateDto Validation Tests

    [Fact]
    public void CreateDto_ValidData_PassesValidation()
    {
        // Arrange
        var dto = new CreateSocietyWingDetailsDto
        {
            WingId = 1,
            PropertyId = 100,
            FromFloor = "1",
            ToFloor = "5",
            NewWingName = "A Wing",
            NoOfFlat = 10,
            NoOfShop = 2,
            NoOfRowHouse = 0
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
    public void CreateDto_FromFloorExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var dto = new CreateSocietyWingDetailsDto
        {
            FromFloor = new string('1', 51) // Exceeds max length of 50
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, validationContext, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage == "SocietyWingDetails_FromFloor_MaxLen_50");
    }

    [Fact]
    public void CreateDto_ToFloorExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var dto = new CreateSocietyWingDetailsDto
        {
            ToFloor = new string('1', 51) // Exceeds max length of 50
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, validationContext, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage == "SocietyWingDetails_ToFloor_MaxLen_50");
    }

    [Fact]
    public void CreateDto_NewWingNameExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var dto = new CreateSocietyWingDetailsDto
        {
            NewWingName = new string('a', 501) // Exceeds max length of 500
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, validationContext, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage == "SocietyWingDetails_NewWingName_MaxLen_500");
    }

    [Fact]
    public void CreateDto_InvalidCharactersInFromFloor_FailsValidation()
    {
        // Arrange
        var dto = new CreateSocietyWingDetailsDto
        {
            FromFloor = "<script>alert('xss')</script>"
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, validationContext, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage == "SocietyWingDetails_FromFloor_InvalidCharacters");
    }

    [Fact]
    public void CreateDto_NegativeNoOfFlat_FailsValidation()
    {
        // Arrange
        var dto = new CreateSocietyWingDetailsDto
        {
            NoOfFlat = -1
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, validationContext, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage == "SocietyWingDetails_NoOfFlat_NonNegative");
    }

    [Fact]
    public void CreateDto_NegativeNoOfShop_FailsValidation()
    {
        // Arrange
        var dto = new CreateSocietyWingDetailsDto
        {
            NoOfShop = -5
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, validationContext, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage == "SocietyWingDetails_NoOfShop_NonNegative");
    }

    [Fact]
    public void CreateDto_NegativeNoOfRowHouse_FailsValidation()
    {
        // Arrange
        var dto = new CreateSocietyWingDetailsDto
        {
            NoOfRowHouse = -10
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, validationContext, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage == "SocietyWingDetails_NoOfRowHouse_NonNegative");
    }

    #endregion

    #region UpdateDto Validation Tests

    [Fact]
    public void UpdateDto_ValidData_PassesValidation()
    {
        // Arrange
        var dto = new UpdateSocietyWingDetailsDto
        {
            WingId = 1,
            PropertyId = 100,
            SocietyDetailId = 5,
            FromFloor = "1",
            ToFloor = "10",
            NewWingName = "Updated Wing",
            NoOfFlat = 20
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
    public void UpdateDto_InvalidCharactersInNewWingName_FailsValidation()
    {
        // Arrange
        var dto = new UpdateSocietyWingDetailsDto
        {
            NewWingName = "Wing{Test}&$%"
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, validationContext, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage == "SocietyWingDetails_NewWingName_InvalidCharacters");
    }

    [Fact]
    public void UpdateDto_NewWingNameExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var dto = new UpdateSocietyWingDetailsDto
        {
            NewWingName = new string('a', 501)
        };

        // Act
        var validationContext = new ValidationContext(dto);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, validationContext, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage == "SocietyWingDetails_NewWingName_MaxLen_500");
    }

    #endregion
}
