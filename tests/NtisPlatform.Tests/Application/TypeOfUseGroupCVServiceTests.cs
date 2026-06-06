using AutoMapper;
using FluentAssertions;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class TypeOfUseGroupCVServiceTests
{
    private readonly Mock<IRepository<TypeOfUseGroupCVEntity, int>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly TypeOfUseGroupCVService _service;

    public TypeOfUseGroupCVServiceTests()
    {
        _repositoryMock = new Mock<IRepository<TypeOfUseGroupCVEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _service = new TypeOfUseGroupCVService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsTypeOfUseGroupCVDto()
    {
        // Arrange
        var id = 1;
        var entity = new TypeOfUseGroupCVEntity
        {
            Id = id,
            TypeOfUseGroupCVCode = "RES",
            GroupName = "Residential",
            GroupIcon = "home-icon",
            IsFloorWiseRateApplicable = true,
            IsActive = true,
            CreatedDate = DateTime.Now
        };
        var expectedDto = new TypeOfUseGroupCVDto
        {
            Id = id,
            TypeOfUseGroupCVCode = "RES",
            GroupName = "Residential",
            GroupIcon = "home-icon",
            IsFloorWiseRateApplicable = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mapperMock.Setup(m => m.Map<TypeOfUseGroupCVDto>(entity))
            .Returns(expectedDto);

        // Act
        var result = await _service.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDto);
        _repositoryMock.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var id = 999;
        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseGroupCVEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _repositoryMock.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithZeroId_ReturnsNull()
    {
        // Arrange
        var id = 0;
        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseGroupCVEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithNegativeId_ReturnsNull()
    {
        // Arrange
        var id = -1;
        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseGroupCVEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedTypeOfUseGroupCVDto()
    {
        // Arrange
        var createDto = new CreateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "COM",
            GroupName = "Commercial",
            GroupIcon = "business-icon",
            IsFloorWiseRateApplicable = false,
            CreatedBy = 1
        };
        var entity = new TypeOfUseGroupCVEntity
        {
            Id = 0,
            TypeOfUseGroupCVCode = "COM",
            GroupName = "Commercial",
            GroupIcon = "business-icon",
            IsFloorWiseRateApplicable = false,
            CreatedBy = 1
        };
        var savedEntity = new TypeOfUseGroupCVEntity
        {
            Id = 1,
            TypeOfUseGroupCVCode = "COM",
            GroupName = "Commercial",
            GroupIcon = "business-icon",
            IsFloorWiseRateApplicable = false,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };
        var expectedDto = new TypeOfUseGroupCVDto
        {
            Id = 1,
            TypeOfUseGroupCVCode = "COM",
            GroupName = "Commercial",
            GroupIcon = "business-icon",
            IsFloorWiseRateApplicable = false
        };

        _mapperMock.Setup(m => m.Map<TypeOfUseGroupCVEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<TypeOfUseGroupCVEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedEntity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<TypeOfUseGroupCVDto>(It.IsAny<TypeOfUseGroupCVEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.TypeOfUseGroupCVCode.Should().Be("COM");
        result.GroupName.Should().Be("Commercial");
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<TypeOfUseGroupCVEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCode_ThrowsException()
    {
        // Arrange
        var createDto = new CreateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "RES", // Already exists
            GroupName = "Residential Duplicate",
            CreatedBy = 1
        };
        var entity = new TypeOfUseGroupCVEntity { TypeOfUseGroupCVCode = "RES" };

        _mapperMock.Setup(m => m.Map<TypeOfUseGroupCVEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<TypeOfUseGroupCVEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate TypeOfUseGroupCVCode"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(createDto, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WithEmptyCode_ThrowsException()
    {
        // Arrange
        var createDto = new CreateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "",
            GroupName = "Test Group",
            CreatedBy = 1
        };

        var entity = new TypeOfUseGroupCVEntity { TypeOfUseGroupCVCode = "" };
        _mapperMock.Setup(m => m.Map<TypeOfUseGroupCVEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<TypeOfUseGroupCVEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("TypeOfUseGroupCVCode cannot be empty"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateAsync(createDto, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WithFloorWiseRateApplicableTrue_CreatesCorrectly()
    {
        // Arrange
        var createDto = new CreateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "IND",
            GroupName = "Industrial",
            GroupIcon = "factory-icon",
            IsFloorWiseRateApplicable = true,
            CreatedBy = 1
        };
        var entity = new TypeOfUseGroupCVEntity
        {
            TypeOfUseGroupCVCode = "IND",
            GroupName = "Industrial",
            GroupIcon = "factory-icon",
            IsFloorWiseRateApplicable = true
        };
        var savedEntity = new TypeOfUseGroupCVEntity
        {
            Id = 5,
            TypeOfUseGroupCVCode = "IND",
            GroupName = "Industrial",
            IsFloorWiseRateApplicable = true,
            IsActive = true
        };
        var expectedDto = new TypeOfUseGroupCVDto
        {
            Id = 5,
            IsFloorWiseRateApplicable = true
        };

        _mapperMock.Setup(m => m.Map<TypeOfUseGroupCVEntity>(createDto)).Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<TypeOfUseGroupCVEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedEntity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<TypeOfUseGroupCVDto>(It.IsAny<TypeOfUseGroupCVEntity>())).Returns(expectedDto);

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFloorWiseRateApplicable.Should().BeTrue();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidIdAndDto_ReturnsUpdatedTypeOfUseGroupCVDto()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "RES-UPD",
            GroupName = "Residential Updated",
            GroupIcon = "new-home-icon",
            IsFloorWiseRateApplicable = false,
            UpdatedBy = 1
        };
        var existingEntity = new TypeOfUseGroupCVEntity
        {
            Id = id,
            TypeOfUseGroupCVCode = "RES",
            GroupName = "Residential",
            GroupIcon = "home-icon",
            IsFloorWiseRateApplicable = true,
            IsActive = true
        };
        var updatedEntity = new TypeOfUseGroupCVEntity
        {
            Id = id,
            TypeOfUseGroupCVCode = "RES-UPD",
            GroupName = "Residential Updated",
            GroupIcon = "new-home-icon",
            IsFloorWiseRateApplicable = false,
            IsActive = true,
            UpdatedBy = 1,
            UpdatedDate = DateTime.Now
        };
        var expectedDto = new TypeOfUseGroupCVDto
        {
            Id = id,
            TypeOfUseGroupCVCode = "RES-UPD",
            GroupName = "Residential Updated",
            GroupIcon = "new-home-icon",
            IsFloorWiseRateApplicable = false
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _mapperMock.Setup(m => m.Map(updateDto, existingEntity))
            .Returns(updatedEntity);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<TypeOfUseGroupCVEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<TypeOfUseGroupCVDto>(It.IsAny<TypeOfUseGroupCVEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.UpdateAsync(id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.TypeOfUseGroupCVCode.Should().Be("RES-UPD");
        result.GroupName.Should().Be("Residential Updated");
        result.IsFloorWiseRateApplicable.Should().BeFalse();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<TypeOfUseGroupCVEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var id = 999;
        var updateDto = new UpdateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "XXX",
            GroupName = "Non-Existent",
            UpdatedBy = 1
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseGroupCVEntity?)null);

        // Act
        var result = await _service.UpdateAsync(id, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<TypeOfUseGroupCVEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenEntityExists_CallsRepositoryAndUnitOfWork()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "AGR",
            GroupName = "Agriculture",
            GroupIcon = "farm-icon",
            IsFloorWiseRateApplicable = true,
            UpdatedBy = 2
        };
        var existingEntity = new TypeOfUseGroupCVEntity
        {
            Id = id,
            TypeOfUseGroupCVCode = "OLD",
            GroupName = "Old Name"
        };
        var updatedEntity = new TypeOfUseGroupCVEntity
        {
            Id = id,
            TypeOfUseGroupCVCode = "AGR",
            GroupName = "Agriculture"
        };
        var expectedDto = new TypeOfUseGroupCVDto
        {
            Id = id,
            TypeOfUseGroupCVCode = "AGR"
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _mapperMock.Setup(m => m.Map(updateDto, existingEntity))
            .Returns(updatedEntity);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<TypeOfUseGroupCVEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<TypeOfUseGroupCVDto>(It.IsAny<TypeOfUseGroupCVEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.UpdateAsync(id, updateDto, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<TypeOfUseGroupCVEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ToggleFloorWiseRateApplicable_UpdatesCorrectly()
    {
        // Arrange
        var id = 3;
        var updateDto = new UpdateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "MIX",
            GroupName = "Mixed Use",
            IsFloorWiseRateApplicable = true, // Changed from false
            UpdatedBy = 5
        };
        var existingEntity = new TypeOfUseGroupCVEntity
        {
            Id = id,
            IsFloorWiseRateApplicable = false
        };
        var updatedEntity = new TypeOfUseGroupCVEntity
        {
            Id = id,
            IsFloorWiseRateApplicable = true
        };
        var expectedDto = new TypeOfUseGroupCVDto
        {
            Id = id,
            IsFloorWiseRateApplicable = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mapperMock.Setup(m => m.Map(updateDto, existingEntity)).Returns(updatedEntity);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<TypeOfUseGroupCVEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<TypeOfUseGroupCVDto>(It.IsAny<TypeOfUseGroupCVEntity>())).Returns(expectedDto);

        // Act
        var result = await _service.UpdateAsync(id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.IsFloorWiseRateApplicable.Should().BeTrue();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ReturnsTrueAndSoftDeletes()
    {
        // Arrange
        var id = 1;
        var entity = new TypeOfUseGroupCVEntity
        {
            Id = id,
            TypeOfUseGroupCVCode = "RES",
            GroupName = "Residential",
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<TypeOfUseGroupCVEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<TypeOfUseGroupCVEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var id = 999;

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseGroupCVEntity?)null);

        // Act
        var result = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<TypeOfUseGroupCVEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithZeroId_ReturnsFalse()
    {
        // Arrange
        var id = 0;

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TypeOfUseGroupCVEntity?)null);

        // Act
        var result = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<TypeOfUseGroupCVEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenEntityExists_CallsRepositoryAndUnitOfWork()
    {
        // Arrange
        var id = 5;
        var entity = new TypeOfUseGroupCVEntity
        {
            Id = id,
            TypeOfUseGroupCVCode = "EDU",
            GroupName = "Educational",
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<TypeOfUseGroupCVEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.DeleteAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
