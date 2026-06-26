using AutoMapper;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Unit tests for UlbImageMasterService
/// </summary>
public class UlbImageMasterServiceTests
{
    private readonly Mock<IRepository<UlbImageMasterEntity, int>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IConfigurationProvider> _configurationProviderMock;
    private readonly UlbImageMasterService _service;

    public UlbImageMasterServiceTests()
    {
        _repositoryMock = new Mock<IRepository<UlbImageMasterEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _configurationProviderMock = new Mock<IConfigurationProvider>();

        _mapperMock.Setup(m => m.ConfigurationProvider).Returns(_configurationProviderMock.Object);

        _service = new UlbImageMasterService(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object
        );
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsDto()
    {
        // Arrange
        var entity = new UlbImageMasterEntity
        {
            Id = 1,
            ImageType = "Logo",
            ImageId = 10,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now
        };
        var dto = new UlbImageMasterDto
        {
            Id = 1,
            ImageType = "Logo",
            ImageId = 10,
            IsActive = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mapperMock.Setup(x => x.Map<UlbImageMasterDto>(entity)).Returns(dto);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Logo", result.ImageType);
        Assert.Equal(10, result.ImageId);
        Assert.True(result.IsActive);
        _repositoryMock.Verify(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UlbImageMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
        _repositoryMock.Verify(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithZeroId_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UlbImageMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(0);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithNullImageId_ReturnsDto()
    {
        // Arrange
        var entity = new UlbImageMasterEntity { Id = 1, ImageType = "Banner", ImageId = null, IsActive = true };
        var dto = new UlbImageMasterDto { Id = 1, ImageType = "Banner", ImageId = null, IsActive = true };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mapperMock.Setup(x => x.Map<UlbImageMasterDto>(entity)).Returns(dto);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ImageId);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesAndReturnsDto()
    {
        // Arrange
        var createDto = new CreateUlbImageMasterDto
        {
            ImageType = "Logo",
            ImageId = 10,
            CreatedBy = 1
        };
        var entity = new UlbImageMasterEntity
        {
            Id = 1,
            ImageType = "Logo",
            ImageId = 10,
            IsActive = true,
            CreatedBy = 1
        };
        var returnDto = new UlbImageMasterDto
        {
            Id = 1,
            ImageType = "Logo",
            ImageId = 10,
            IsActive = true
        };

        _mapperMock.Setup(x => x.Map<UlbImageMasterEntity>(createDto)).Returns(entity);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<UlbImageMasterDto>(entity)).Returns(returnDto);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Logo", result.ImageType);
        Assert.Equal(10, result.ImageId);
        Assert.True(result.IsActive);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<UlbImageMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNullImageId_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateUlbImageMasterDto { ImageType = "Banner", ImageId = null, CreatedBy = 1 };
        var entity = new UlbImageMasterEntity { Id = 2, ImageType = "Banner", ImageId = null, IsActive = true };
        var returnDto = new UlbImageMasterDto { Id = 2, ImageType = "Banner", ImageId = null, IsActive = true };

        _mapperMock.Setup(x => x.Map<UlbImageMasterEntity>(createDto)).Returns(entity);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<UlbImageMasterDto>(entity)).Returns(returnDto);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ImageId);
    }

    [Fact]
    public async Task CreateAsync_WithMaxLengthImageType_CreatesSuccessfully()
    {
        // Arrange
        var imageType = new string('A', 50);
        var createDto = new CreateUlbImageMasterDto { ImageType = imageType, CreatedBy = 1 };
        var entity = new UlbImageMasterEntity { Id = 1, ImageType = imageType, IsActive = true };
        var returnDto = new UlbImageMasterDto { Id = 1, ImageType = imageType, IsActive = true };

        _mapperMock.Setup(x => x.Map<UlbImageMasterEntity>(createDto)).Returns(entity);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<UlbImageMasterDto>(entity)).Returns(returnDto);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(imageType, result.ImageType);
    }

    [Fact]
    public async Task CreateAsync_MultipleRecords_AllCreatedSuccessfully()
    {
        // Arrange — create DTOs once; reuse the same instances in both setup and act
        var createDtos = new[]
        {
            new CreateUlbImageMasterDto { ImageType = "Logo",   CreatedBy = 1 },
            new CreateUlbImageMasterDto { ImageType = "Banner", CreatedBy = 1 },
            new CreateUlbImageMasterDto { ImageType = "Icon",   CreatedBy = 1 }
        };

        for (int i = 0; i < createDtos.Length; i++)
        {
            var entity    = new UlbImageMasterEntity { Id = i + 1, ImageType = createDtos[i].ImageType, IsActive = true };
            var returnDto = new UlbImageMasterDto    { Id = i + 1, ImageType = createDtos[i].ImageType, IsActive = true };

            _mapperMock.Setup(x => x.Map<UlbImageMasterEntity>(createDtos[i])).Returns(entity);
            _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
            _mapperMock.Setup(x => x.Map<UlbImageMasterDto>(entity)).Returns(returnDto);
        }
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act & Assert
        foreach (var createDto in createDtos)
        {
            var result = await _service.CreateAsync(createDto);
            Assert.NotNull(result);
            Assert.Equal(createDto.ImageType, result.ImageType);
        }
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesAndReturnsDto()
    {
        // Arrange
        var updateDto = new UpdateUlbImageMasterDto
        {
            ImageType = "Banner",
            ImageId = 20,
            UpdatedBy = 2
        };
        var existingEntity = new UlbImageMasterEntity
        {
            Id = 1,
            ImageType = "Logo",
            ImageId = 10,
            IsActive = true
        };
        var returnDto = new UlbImageMasterDto
        {
            Id = 1,
            ImageType = "Banner",
            ImageId = 20,
            IsActive = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mapperMock.Setup(x => x.Map(updateDto, existingEntity)).Returns(existingEntity);
        _repositoryMock.Setup(x => x.UpdateAsync(existingEntity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<UlbImageMasterDto>(existingEntity)).Returns(returnDto);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Banner", result.ImageType);
        Assert.Equal(20, result.ImageId);
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<UlbImageMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateUlbImageMasterDto { ImageType = "Banner", UpdatedBy = 2 };

        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UlbImageMasterEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto);

        // Assert
        Assert.Null(result);
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<UlbImageMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithNullImageId_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateUlbImageMasterDto { ImageType = "Logo", ImageId = null, UpdatedBy = 2 };
        var existingEntity = new UlbImageMasterEntity { Id = 1, ImageType = "Logo", ImageId = 5, IsActive = true };
        var returnDto = new UlbImageMasterDto { Id = 1, ImageType = "Logo", ImageId = null, IsActive = true };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mapperMock.Setup(x => x.Map(updateDto, existingEntity)).Returns(existingEntity);
        _repositoryMock.Setup(x => x.UpdateAsync(existingEntity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<UlbImageMasterDto>(existingEntity)).Returns(returnDto);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ImageId);
    }

    [Fact]
    public async Task UpdateAsync_WithImageTypeChange_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateUlbImageMasterDto { ImageType = "NewType", ImageId = 10, UpdatedBy = 2 };
        var existingEntity = new UlbImageMasterEntity { Id = 1, ImageType = "OldType", ImageId = 10, IsActive = true };
        var returnDto = new UlbImageMasterDto { Id = 1, ImageType = "NewType", ImageId = 10, IsActive = true };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mapperMock.Setup(x => x.Map(updateDto, existingEntity)).Returns(existingEntity);
        _repositoryMock.Setup(x => x.UpdateAsync(existingEntity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<UlbImageMasterDto>(existingEntity)).Returns(returnDto);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("NewType", result.ImageType);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        var entity = new UlbImageMasterEntity { Id = 1, ImageType = "Logo", IsActive = true };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _repositoryMock.Setup(x => x.DeleteAsync(entity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        Assert.True(result);
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<UlbImageMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UlbImageMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        Assert.False(result);
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<UlbImageMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithZeroId_ReturnsFalse()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UlbImageMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(0);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_MultipleIds_AllDeleted()
    {
        // Arrange
        var ids = new[] { 1, 2, 3 };
        foreach (var id in ids)
        {
            var entity = new UlbImageMasterEntity { Id = id, ImageType = $"Type{id}", IsActive = true };
            _repositoryMock.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
            _repositoryMock.Setup(x => x.DeleteAsync(entity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        }
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act & Assert
        foreach (var id in ids)
        {
            var result = await _service.DeleteAsync(id);
            Assert.True(result);
        }
    }

    #endregion

    #region Edge Cases and Integration Tests

    [Fact]
    public async Task CompleteWorkflow_CreateUpdateDelete_AllSuccessful()
    {
        // Arrange - Create
        var createDto = new CreateUlbImageMasterDto { ImageType = "Logo", ImageId = 1, CreatedBy = 1 };
        var entity = new UlbImageMasterEntity { Id = 1, ImageType = "Logo", ImageId = 1, IsActive = true };
        var createdDto = new UlbImageMasterDto { Id = 1, ImageType = "Logo", ImageId = 1, IsActive = true };

        _mapperMock.Setup(x => x.Map<UlbImageMasterEntity>(createDto)).Returns(entity);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<UlbImageMasterDto>(entity)).Returns(createdDto);

        var created = await _service.CreateAsync(createDto);
        Assert.NotNull(created);

        // Arrange - Update
        var updateDto = new UpdateUlbImageMasterDto { ImageType = "Banner", ImageId = 2, UpdatedBy = 2 };
        var updatedDto = new UlbImageMasterDto { Id = 1, ImageType = "Banner", ImageId = 2, IsActive = true };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mapperMock.Setup(x => x.Map(updateDto, entity)).Returns(entity);
        _repositoryMock.Setup(x => x.UpdateAsync(entity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mapperMock.Setup(x => x.Map<UlbImageMasterDto>(entity)).Returns(updatedDto);

        var updated = await _service.UpdateAsync(1, updateDto);
        Assert.NotNull(updated);
        Assert.Equal("Banner", updated.ImageType);

        // Arrange - Delete
        _repositoryMock.Setup(x => x.DeleteAsync(entity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var deleted = await _service.DeleteAsync(1);
        Assert.True(deleted);
    }

    [Fact]
    public async Task GetByIdAsync_AfterCreate_ReturnsCreatedData()
    {
        // Arrange - Create
        var createDto = new CreateUlbImageMasterDto { ImageType = "Logo", CreatedBy = 1 };
        var entity = new UlbImageMasterEntity { Id = 1, ImageType = "Logo", IsActive = true };
        var createdDto = new UlbImageMasterDto { Id = 1, ImageType = "Logo", IsActive = true };

        _mapperMock.Setup(x => x.Map<UlbImageMasterEntity>(createDto)).Returns(entity);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<UlbImageMasterDto>(entity)).Returns(createdDto);

        await _service.CreateAsync(createDto);

        // Arrange - GetById
        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mapperMock.Setup(x => x.Map<UlbImageMasterDto>(entity)).Returns(createdDto);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdDto.ImageType, result.ImageType);
    }

    #endregion
}
