using AutoMapper;
using Moq;
using NtisPlatform.Application.DTOs.Master.PropertyWorkflowStageMaster;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Unit tests for PropertyWorkflowStageMasterService
/// </summary>
public class PropertyWorkflowStageMasterServiceTests
{
    private readonly Mock<IRepository<PropertyWorkflowStageMasterEntity, int>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IConfigurationProvider> _configurationProviderMock;
    private readonly PropertyWorkflowStageMasterService _service;

    public PropertyWorkflowStageMasterServiceTests()
    {
        _repositoryMock = new Mock<IRepository<PropertyWorkflowStageMasterEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _configurationProviderMock = new Mock<IConfigurationProvider>();

        _mapperMock.Setup(m => m.ConfigurationProvider).Returns(_configurationProviderMock.Object);

        _service = new PropertyWorkflowStageMasterService(
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
        var entity = new PropertyWorkflowStageMasterEntity
        {
            Id = 1,
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            Description = "Initial stage",
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now
        };

        var dto = new PropertyWorkflowStageMasterDto
        {
            Id = 1,
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            Description = "Initial stage",
            IsActive = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowStageMasterDto>(entity))
            .Returns(dto);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("GeoSequencing", result.StageName);
        Assert.Equal(1, result.DisplayOrder);
        Assert.True(result.IsActive);
        _repositoryMock.Verify(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyWorkflowStageMasterEntity?)null);

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
            .ReturnsAsync((PropertyWorkflowStageMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(0);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_MultipleIds_ReturnsDifferentDtos()
    {
        // Arrange
        var entity1 = new PropertyWorkflowStageMasterEntity { Id = 1, StageName = "Stage1", DisplayOrder = 1, IsActive = true };
        var entity2 = new PropertyWorkflowStageMasterEntity { Id = 2, StageName = "Stage2", DisplayOrder = 2, IsActive = true };
        var dto1 = new PropertyWorkflowStageMasterDto { Id = 1, StageName = "Stage1", DisplayOrder = 1, IsActive = true };
        var dto2 = new PropertyWorkflowStageMasterDto { Id = 2, StageName = "Stage2", DisplayOrder = 2, IsActive = true };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity1);
        _repositoryMock.Setup(x => x.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(entity2);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowStageMasterDto>(entity1)).Returns(dto1);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowStageMasterDto>(entity2)).Returns(dto2);

        // Act
        var result1 = await _service.GetByIdAsync(1);
        var result2 = await _service.GetByIdAsync(2);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal("Stage1", result1.StageName);
        Assert.Equal("Stage2", result2.StageName);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesAndReturnsDto()
    {
        // Arrange
        var createDto = new CreatePropertyWorkflowStageMasterDto
        {
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            Description = "Initial stage",
            CreatedBy = 1
        };

        var entity = new PropertyWorkflowStageMasterEntity
        {
            Id = 1,
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            Description = "Initial stage",
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now
        };

        var returnDto = new PropertyWorkflowStageMasterDto
        {
            Id = 1,
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            Description = "Initial stage",
            IsActive = true
        };

        _mapperMock.Setup(x => x.Map<PropertyWorkflowStageMasterEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowStageMasterDto>(entity))
            .Returns(returnDto);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("GeoSequencing", result.StageName);
        Assert.Equal(1, result.DisplayOrder);
        Assert.True(result.IsActive);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<PropertyWorkflowStageMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithMinimalData_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreatePropertyWorkflowStageMasterDto
        {
            StageName = "InternalSurvey",
            DisplayOrder = 2,
            CreatedBy = 1
        };

        var entity = new PropertyWorkflowStageMasterEntity
        {
            Id = 2,
            StageName = "InternalSurvey",
            DisplayOrder = 2,
            IsActive = true,
            CreatedBy = 1
        };

        var returnDto = new PropertyWorkflowStageMasterDto
        {
            Id = 2,
            StageName = "InternalSurvey",
            DisplayOrder = 2,
            IsActive = true
        };

        _mapperMock.Setup(x => x.Map<PropertyWorkflowStageMasterEntity>(createDto)).Returns(entity);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowStageMasterDto>(entity)).Returns(returnDto);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal("InternalSurvey", result.StageName);
    }

    [Fact]
    public async Task CreateAsync_MultipleRecords_AllCreatedSuccessfully()
    {
        // Arrange
        var createDtos = new[]
        {
            new CreatePropertyWorkflowStageMasterDto { StageName = "Stage1", DisplayOrder = 1, CreatedBy = 1 },
            new CreatePropertyWorkflowStageMasterDto { StageName = "Stage2", DisplayOrder = 2, CreatedBy = 1 },
            new CreatePropertyWorkflowStageMasterDto { StageName = "Stage3", DisplayOrder = 3, CreatedBy = 1 }
        };

        for (int i = 0; i < createDtos.Length; i++)
        {
            var entity = new PropertyWorkflowStageMasterEntity { Id = i + 1, StageName = createDtos[i].StageName, DisplayOrder = createDtos[i].DisplayOrder, IsActive = true };
            var returnDto = new PropertyWorkflowStageMasterDto { Id = i + 1, StageName = createDtos[i].StageName, DisplayOrder = createDtos[i].DisplayOrder, IsActive = true };

            _mapperMock.Setup(x => x.Map<PropertyWorkflowStageMasterEntity>(createDtos[i])).Returns(entity);
            _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
            _mapperMock.Setup(x => x.Map<PropertyWorkflowStageMasterDto>(entity)).Returns(returnDto);
        }

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act & Assert
        foreach (var createDto in createDtos)
        {
            var result = await _service.CreateAsync(createDto);
            Assert.NotNull(result);
            Assert.Equal(createDto.StageName, result.StageName);
        }
    }

    [Fact]
    public async Task CreateAsync_WithMaxLengthStageName_CreatesSuccessfully()
    {
        // Arrange
        var longStageName = new string('A', 100);
        var createDto = new CreatePropertyWorkflowStageMasterDto
        {
            StageName = longStageName,
            DisplayOrder = 1,
            CreatedBy = 1
        };

        var entity = new PropertyWorkflowStageMasterEntity { Id = 1, StageName = longStageName, DisplayOrder = 1, IsActive = true };
        var returnDto = new PropertyWorkflowStageMasterDto { Id = 1, StageName = longStageName, DisplayOrder = 1, IsActive = true };

        _mapperMock.Setup(x => x.Map<PropertyWorkflowStageMasterEntity>(createDto)).Returns(entity);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowStageMasterDto>(entity)).Returns(returnDto);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(longStageName, result.StageName);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesAndReturnsDto()
    {
        // Arrange
        var updateDto = new UpdatePropertyWorkflowStageMasterDto
        {
            StageName = "GeoSequencing Updated",
            DisplayOrder = 1,
            Description = "Updated description",
            UpdatedBy = 2
        };

        var existingEntity = new PropertyWorkflowStageMasterEntity
        {
            Id = 1,
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            Description = "Initial stage",
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now
        };

        var returnDto = new PropertyWorkflowStageMasterDto
        {
            Id = 1,
            StageName = "GeoSequencing Updated",
            DisplayOrder = 1,
            Description = "Updated description",
            IsActive = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _mapperMock.Setup(x => x.Map(updateDto, existingEntity))
            .Returns(existingEntity);
        _repositoryMock.Setup(x => x.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowStageMasterDto>(existingEntity))
            .Returns(returnDto);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("GeoSequencing Updated", result.StageName);
        Assert.Equal("Updated description", result.Description);
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<PropertyWorkflowStageMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdatePropertyWorkflowStageMasterDto
        {
            StageName = "Updated",
            DisplayOrder = 1,
            UpdatedBy = 2
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyWorkflowStageMasterEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto);

        // Assert
        Assert.Null(result);
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<PropertyWorkflowStageMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithDisplayOrderChange_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdatePropertyWorkflowStageMasterDto
        {
            StageName = "GeoSequencing",
            DisplayOrder = 5,
            UpdatedBy = 2
        };

        var existingEntity = new PropertyWorkflowStageMasterEntity
        {
            Id = 1,
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            IsActive = true
        };

        var returnDto = new PropertyWorkflowStageMasterDto
        {
            Id = 1,
            StageName = "GeoSequencing",
            DisplayOrder = 5,
            IsActive = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mapperMock.Setup(x => x.Map(updateDto, existingEntity)).Returns(existingEntity);
        _repositoryMock.Setup(x => x.UpdateAsync(existingEntity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowStageMasterDto>(existingEntity)).Returns(returnDto);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.DisplayOrder);
    }

    [Fact]
    public async Task UpdateAsync_WithDescriptionClear_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdatePropertyWorkflowStageMasterDto
        {
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            Description = null,
            UpdatedBy = 2
        };

        var existingEntity = new PropertyWorkflowStageMasterEntity
        {
            Id = 1,
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            Description = "Old description",
            IsActive = true
        };

        var returnDto = new PropertyWorkflowStageMasterDto
        {
            Id = 1,
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            Description = null,
            IsActive = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mapperMock.Setup(x => x.Map(updateDto, existingEntity)).Returns(existingEntity);
        _repositoryMock.Setup(x => x.UpdateAsync(existingEntity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowStageMasterDto>(existingEntity)).Returns(returnDto);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Description);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        var entity = new PropertyWorkflowStageMasterEntity
        {
            Id = 1,
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            IsActive = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(x => x.DeleteAsync(It.IsAny<PropertyWorkflowStageMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        Assert.True(result);
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<PropertyWorkflowStageMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyWorkflowStageMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        Assert.False(result);
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<PropertyWorkflowStageMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithZeroId_ReturnsFalse()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyWorkflowStageMasterEntity?)null);

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
            var entity = new PropertyWorkflowStageMasterEntity { Id = id, StageName = $"Stage{id}", DisplayOrder = id, IsActive = true };
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
        var createDto = new CreatePropertyWorkflowStageMasterDto { StageName = "Geo", DisplayOrder = 1, CreatedBy = 1 };
        var entity = new PropertyWorkflowStageMasterEntity { Id = 1, StageName = "Geo", DisplayOrder = 1, IsActive = true };
        var createdDto = new PropertyWorkflowStageMasterDto { Id = 1, StageName = "Geo", DisplayOrder = 1, IsActive = true };

        _mapperMock.Setup(x => x.Map<PropertyWorkflowStageMasterEntity>(createDto)).Returns(entity);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowStageMasterDto>(entity)).Returns(createdDto);

        // Act - Create
        var created = await _service.CreateAsync(createDto);
        Assert.NotNull(created);

        // Arrange - Update
        var updateDto = new UpdatePropertyWorkflowStageMasterDto { StageName = "Geo Updated", DisplayOrder = 1, UpdatedBy = 2 };
        var updatedDto = new PropertyWorkflowStageMasterDto { Id = 1, StageName = "Geo Updated", DisplayOrder = 1, IsActive = true };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mapperMock.Setup(x => x.Map(updateDto, entity)).Returns(entity);
        _repositoryMock.Setup(x => x.UpdateAsync(entity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowStageMasterDto>(entity)).Returns(updatedDto);

        // Act - Update
        var updated = await _service.UpdateAsync(1, updateDto);
        Assert.NotNull(updated);

        // Arrange - Delete
        _repositoryMock.Setup(x => x.DeleteAsync(entity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act - Delete
        var deleted = await _service.DeleteAsync(1);
        Assert.True(deleted);
    }

    [Fact]
    public async Task GetByIdAsync_AfterCreate_ReturnsCreatedData()
    {
        // Arrange - Create
        var createDto = new CreatePropertyWorkflowStageMasterDto { StageName = "Test", DisplayOrder = 1, CreatedBy = 1 };
        var entity = new PropertyWorkflowStageMasterEntity { Id = 1, StageName = "Test", DisplayOrder = 1, IsActive = true };
        var createdDto = new PropertyWorkflowStageMasterDto { Id = 1, StageName = "Test", DisplayOrder = 1, IsActive = true };

        _mapperMock.Setup(x => x.Map<PropertyWorkflowStageMasterEntity>(createDto)).Returns(entity);
        _repositoryMock.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowStageMasterDto>(entity)).Returns(createdDto);

        // Act - Create
        var created = await _service.CreateAsync(createDto);

        // Arrange - Get
        var getDto = new PropertyWorkflowStageMasterDto { Id = 1, StageName = "Test", DisplayOrder = 1, IsActive = true };
        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mapperMock.Setup(x => x.Map<PropertyWorkflowStageMasterDto>(entity)).Returns(getDto);

        // Act - Get
        var retrieved = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(created.StageName, retrieved.StageName);
    }

    #endregion
}
