using AutoMapper;
using FluentAssertions;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class WingServiceTests
{
    private readonly Mock<IRepository<WingEntity, int>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly WingService _service;

    public WingServiceTests()
    {
        _repositoryMock = new Mock<IRepository<WingEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _service = new WingService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsWingDto()
    {
        // Arrange
        var Id = 1;
        var entity = new WingEntity
        {
            Id = Id,
            WingNo = "A",
            SequenceNo = 1,
            IsActive = true,
            CreatedDate = DateTime.Now
        };
        var expectedDto = new WingDto
        {
            Id = Id,
            WingNo = "A",
            SequenceNo = 1
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mapperMock.Setup(m => m.Map<WingDto>(entity))
            .Returns(expectedDto);

        // Act
        var result = await _service.GetByIdAsync(Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDto);
        _repositoryMock.Verify(r => r.GetByIdAsync(Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var Id = 999;
        _repositoryMock.Setup(r => r.GetByIdAsync(Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WingEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(Id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _repositoryMock.Verify(r => r.GetByIdAsync(Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedWingDto()
    {
        // Arrange
        var createDto = new CreateWingDto
        {
            WingNo = "F",
            SequenceNo = 6,
            CreatedBy = 1
        };
        var entity = new WingEntity
        {
            Id = 0,
            WingNo = "F",
            SequenceNo = 6,
            CreatedBy = 1
        };
        var savedEntity = new WingEntity
        {
            Id = 6,
            WingNo = "F",
            SequenceNo = 6,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };
        var expectedDto = new WingDto
        {
            Id = 6,
            WingNo = "F",
            SequenceNo = 6
        };

        _mapperMock.Setup(m => m.Map<WingEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<WingEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedEntity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<WingDto>(It.IsAny<WingEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(6);
        result.WingNo.Should().Be("F");
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<WingEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateWingNo_ThrowsException()
    {
        // Arrange
        var createDto = new CreateWingDto
        {
            WingNo = "A", // Already exists
            SequenceNo = 10,
            CreatedBy = 1
        };
        var entity = new WingEntity { WingNo = "A" };

        _mapperMock.Setup(m => m.Map<WingEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<WingEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate WingNo"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(createDto, CancellationToken.None));
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidIdAndDto_ReturnsUpdatedWingDto()
    {
        // Arrange
        var Id = 1;
        var updateDto = new UpdateWingDto
        {
            WingNo = "A-Updated",
            SequenceNo = 10,
            UpdatedBy = 1
        };
        var existingEntity = new WingEntity
        {
            Id = Id,
            WingNo = "A",
            SequenceNo = 1,
            IsActive = true
        };
        var updatedEntity = new WingEntity
        {
            Id = Id,
            WingNo = "A-Updated",
            SequenceNo = 10,
            IsActive = true,
            UpdatedBy = 1,
            UpdatedDate = DateTime.Now
        };
        var expectedDto = new WingDto
        {
            Id = Id,
            WingNo = "A-Updated",
            SequenceNo = 10
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _mapperMock.Setup(m => m.Map(updateDto, existingEntity))
            .Returns(updatedEntity);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<WingEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<WingDto>(It.IsAny<WingEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.UpdateAsync(Id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.WingNo.Should().Be("A-Updated");
        result.SequenceNo.Should().Be(10);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<WingEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var Id = 999;
        var updateDto = new UpdateWingDto { WingNo = "X", UpdatedBy = 1 };

        _repositoryMock.Setup(r => r.GetByIdAsync(Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WingEntity?)null);

        // Act
        var result = await _service.UpdateAsync(Id, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<WingEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ReturnsTrueAndSoftDeletes()
    {
        // Arrange
        var Id = 1;
        var entity = new WingEntity
        {
            Id = Id,
            WingNo = "A",
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(r => r.DeleteAsync(Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(Id, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteAsync(Id, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var Id = 999;

        _repositoryMock.Setup(r => r.GetByIdAsync(Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WingEntity?)null);

        // Act
        var result = await _service.DeleteAsync(Id, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}