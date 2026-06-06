using AutoMapper;
using FluentAssertions;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class FloorGroupServiceTests
{
    private readonly Mock<IRepository<FloorGroupMasterEntity, int>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly FloorGroupService _service;

    public FloorGroupServiceTests()
    {
        _repositoryMock = new Mock<IRepository<FloorGroupMasterEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _service = new FloorGroupService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsFloorGroupDto()
    {
        // Arrange
        var id = 1;
        var entity = new FloorGroupMasterEntity
        {
            Id = id,
            FloorGroup = "Ground Floor",
            IsActive = true,
            CreatedDate = DateTime.Now
        };
        var expectedDto = new FloorGroupDto
        {
            Id = id,
            FloorGroup = "Ground Floor"
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mapperMock.Setup(m => m.Map<FloorGroupDto>(entity))
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
            .ReturnsAsync((FloorGroupMasterEntity?)null);

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
            .ReturnsAsync((FloorGroupMasterEntity?)null);

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
            .ReturnsAsync((FloorGroupMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedFloorGroupDto()
    {
        // Arrange
        var createDto = new CreateFloorGroupDto
        {
            FloorGroup = "First Floor",
            CreatedBy = 1
        };
        var entity = new FloorGroupMasterEntity
        {
            Id = 0,
            FloorGroup = "First Floor",
            CreatedBy = 1
        };
        var savedEntity = new FloorGroupMasterEntity
        {
            Id = 1,
            FloorGroup = "First Floor",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };
        var expectedDto = new FloorGroupDto
        {
            Id = 1,
            FloorGroup = "First Floor"
        };

        _mapperMock.Setup(m => m.Map<FloorGroupMasterEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<FloorGroupMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedEntity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<FloorGroupDto>(It.IsAny<FloorGroupMasterEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.FloorGroup.Should().Be("First Floor");
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<FloorGroupMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateFloorGroup_ThrowsException()
    {
        // Arrange
        var createDto = new CreateFloorGroupDto
        {
            FloorGroup = "Ground Floor", // Already exists
            CreatedBy = 1
        };
        var entity = new FloorGroupMasterEntity { FloorGroup = "Ground Floor" };

        _mapperMock.Setup(m => m.Map<FloorGroupMasterEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<FloorGroupMasterEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate FloorGroup"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(createDto, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WithEmptyFloorGroup_ThrowsException()
    {
        // Arrange
        var createDto = new CreateFloorGroupDto
        {
            FloorGroup = "",
            CreatedBy = 1
        };

        // Act & Assert - In real scenario, validation would prevent this
        // but testing service behavior if validation is bypassed
        var entity = new FloorGroupMasterEntity { FloorGroup = "" };
        _mapperMock.Setup(m => m.Map<FloorGroupMasterEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<FloorGroupMasterEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("FloorGroup cannot be empty"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateAsync(createDto, CancellationToken.None));
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidIdAndDto_ReturnsUpdatedFloorGroupDto()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdateFloorGroupDto
        {
            FloorGroup = "Ground Floor - Updated",
            UpdatedBy = 1
        };
        var existingEntity = new FloorGroupMasterEntity
        {
            Id = id,
            FloorGroup = "Ground Floor",
            IsActive = true
        };
        var updatedEntity = new FloorGroupMasterEntity
        {
            Id = id,
            FloorGroup = "Ground Floor - Updated",
            IsActive = true,
            UpdatedBy = 1,
            UpdatedDate = DateTime.Now
        };
        var expectedDto = new FloorGroupDto
        {
            Id = id,
            FloorGroup = "Ground Floor - Updated"
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _mapperMock.Setup(m => m.Map(updateDto, existingEntity))
            .Returns(updatedEntity);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<FloorGroupMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<FloorGroupDto>(It.IsAny<FloorGroupMasterEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.UpdateAsync(id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.FloorGroup.Should().Be("Ground Floor - Updated");
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<FloorGroupMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var id = 999;
        var updateDto = new UpdateFloorGroupDto { FloorGroup = "Non-Existent", UpdatedBy = 1 };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FloorGroupMasterEntity?)null);

        // Act
        var result = await _service.UpdateAsync(id, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<FloorGroupMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenEntityExists_CallsRepositoryAndUnitOfWork()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdateFloorGroupDto
        {
            FloorGroup = "Second Floor",
            UpdatedBy = 2
        };
        var existingEntity = new FloorGroupMasterEntity { Id = id, FloorGroup = "First Floor" };
        var updatedEntity = new FloorGroupMasterEntity { Id = id, FloorGroup = "Second Floor" };
        var expectedDto = new FloorGroupDto { Id = id, FloorGroup = "Second Floor" };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _mapperMock.Setup(m => m.Map(updateDto, existingEntity))
            .Returns(updatedEntity);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<FloorGroupMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<FloorGroupDto>(It.IsAny<FloorGroupMasterEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.UpdateAsync(id, updateDto, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<FloorGroupMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ReturnsTrueAndSoftDeletes()
    {
        // Arrange
        var id = 1;
        var entity = new FloorGroupMasterEntity
        {
            Id = id,
            FloorGroup = "Ground Floor",
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<FloorGroupMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<FloorGroupMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var id = 999;

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FloorGroupMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<FloorGroupMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithZeroId_ReturnsFalse()
    {
        // Arrange
        var id = 0;

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FloorGroupMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<FloorGroupMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenEntityExists_CallsRepositoryAndUnitOfWork()
    {
        // Arrange
        var id = 5;
        var entity = new FloorGroupMasterEntity
        {
            Id = id,
            FloorGroup = "Basement",
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<FloorGroupMasterEntity>(), It.IsAny<CancellationToken>()))
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
