using AutoMapper;
using FluentAssertions;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class SubZoneDetailsForCVServiceTests
{
    private readonly Mock<IRepository<SubZoneDetailsForCVEntity, int>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly SubZoneDetailsForCVService _service;

    public SubZoneDetailsForCVServiceTests()
    {
        _repositoryMock = new Mock<IRepository<SubZoneDetailsForCVEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _service = new SubZoneDetailsForCVService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsSubZoneDetailsForCVDto()
    {
        // Arrange
        var id = 1;
        var entity = new SubZoneDetailsForCVEntity
        {
            Id = id,
            MoujaId = 10,
            SubZoneNo = "SZ001",
            SubZoneName = "Zone A",
            IsActive = true,
            CreatedDate = DateTime.Now
        };
        var expectedDto = new SubZoneDetailsForCVDto
        {
            Id = id,
            MoujaId = 10,
            SubZoneNo = "SZ001",
            SubZoneName = "Zone A"
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mapperMock.Setup(m => m.Map<SubZoneDetailsForCVDto>(entity))
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
            .ReturnsAsync((SubZoneDetailsForCVEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedSubZoneDetailsForCVDto()
    {
        // Arrange
        var createDto = new CreateSubZoneDetailsForCVDto
        {
            MoujaId = 10,
            SubZoneNo = "SZ002",
            SubZoneName = "Zone B",
            CreatedBy = 1
        };
        var entity = new SubZoneDetailsForCVEntity
        {
            Id = 0,
            MoujaId = 10,
            SubZoneNo = "SZ002",
            SubZoneName = "Zone B",
            CreatedBy = 1
        };
        var savedEntity = new SubZoneDetailsForCVEntity
        {
            Id = 1,
            MoujaId = 10,
            SubZoneNo = "SZ002",
            SubZoneName = "Zone B",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };
        var expectedDto = new SubZoneDetailsForCVDto
        {
            Id = 1,
            MoujaId = 10,
            SubZoneNo = "SZ002",
            SubZoneName = "Zone B"
        };

        _mapperMock.Setup(m => m.Map<SubZoneDetailsForCVEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<SubZoneDetailsForCVEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedEntity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<SubZoneDetailsForCVDto>(It.IsAny<SubZoneDetailsForCVEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.SubZoneNo.Should().Be("SZ002");
        result.SubZoneName.Should().Be("Zone B");
        result.MoujaId.Should().Be(10);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateMoujaIdAndSubZoneNo_ThrowsException()
    {
        // Arrange
        var createDto = new CreateSubZoneDetailsForCVDto
        {
            MoujaId = 10,
            SubZoneNo = "SZ001", // Already exists for MoujaId 10
            SubZoneName = "Duplicate Zone",
            CreatedBy = 1
        };
        var entity = new SubZoneDetailsForCVEntity { MoujaId = 10, SubZoneNo = "SZ001" };

        _mapperMock.Setup(m => m.Map<SubZoneDetailsForCVEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<SubZoneDetailsForCVEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate MoujaId and SubZoneNo combination"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(createDto, CancellationToken.None));
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidIdAndDto_ReturnsUpdatedSubZoneDetailsForCVDto()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdateSubZoneDetailsForCVDto
        {
            MoujaId = 10,
            SubZoneNo = "SZ001-UPD",
            SubZoneName = "Zone A Updated",
            UpdatedBy = 1
        };
        var existingEntity = new SubZoneDetailsForCVEntity
        {
            Id = id,
            MoujaId = 10,
            SubZoneNo = "SZ001",
            SubZoneName = "Zone A",
            IsActive = true
        };
        var updatedEntity = new SubZoneDetailsForCVEntity
        {
            Id = id,
            MoujaId = 10,
            SubZoneNo = "SZ001-UPD",
            SubZoneName = "Zone A Updated",
            IsActive = true,
            UpdatedBy = 1,
            UpdatedDate = DateTime.Now
        };
        var expectedDto = new SubZoneDetailsForCVDto
        {
            Id = id,
            MoujaId = 10,
            SubZoneNo = "SZ001-UPD",
            SubZoneName = "Zone A Updated"
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _mapperMock.Setup(m => m.Map(updateDto, existingEntity))
            .Returns(updatedEntity);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SubZoneDetailsForCVEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<SubZoneDetailsForCVDto>(It.IsAny<SubZoneDetailsForCVEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.UpdateAsync(id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SubZoneNo.Should().Be("SZ001-UPD");
        result.SubZoneName.Should().Be("Zone A Updated");
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var id = 999;
        var updateDto = new UpdateSubZoneDetailsForCVDto
        {
            MoujaId = 10,
            SubZoneNo = "XXX",
            SubZoneName = "Non-Existent",
            UpdatedBy = 1
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubZoneDetailsForCVEntity?)null);

        // Act
        var result = await _service.UpdateAsync(id, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<SubZoneDetailsForCVEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ReturnsTrueAndSoftDeletes()
    {
        // Arrange
        var id = 1;
        var entity = new SubZoneDetailsForCVEntity
        {
            Id = id,
            MoujaId = 10,
            SubZoneNo = "SZ001",
            SubZoneName = "Zone A",
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<SubZoneDetailsForCVEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<SubZoneDetailsForCVEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var id = 999;

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubZoneDetailsForCVEntity?)null);

        // Act
        var result = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<SubZoneDetailsForCVEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
