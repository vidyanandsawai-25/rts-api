using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Master.PropertyPhotoType;
using NtisPlatform.Application.Services.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive service tests for PropertyPhotoTypeService to achieve 100% code coverage
/// </summary>
public class PropertyPhotoTypeServiceTests
{
    private readonly Mock<IRepository<PropertyPhotoTypeEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PropertyPhotoTypeService _service;

    public PropertyPhotoTypeServiceTests()
    {
        _mockRepository = new Mock<IRepository<PropertyPhotoTypeEntity, int>>();
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

        _service = new PropertyPhotoTypeService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new PropertyPhotoTypeEntity
        {
            Id = 1,
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front View",
            Description = "Front facade of the property",
            DisplayOrder = 1,
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<PropertyPhotoTypeDto>(It.IsAny<PropertyPhotoTypeEntity>()))
            .Returns(new PropertyPhotoTypeDto
            {
                Id = 1,
                PhotoTypeCode = "FRONT",
                PhotoTypeName = "Front View",
                Description = "Front facade of the property",
                DisplayOrder = 1,
                IsActive = true
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("FRONT", result.PhotoTypeCode);
        Assert.Equal("Front View", result.PhotoTypeName);
        Assert.Equal("Front facade of the property", result.Description);
        Assert.Equal(1, result.DisplayOrder);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyPhotoTypeEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(9999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var entity = new PropertyPhotoTypeEntity { Id = 1, PhotoTypeCode = "TEST", PhotoTypeName = "Test" };

        _mockRepository.Setup(r => r.GetByIdAsync(1, cancellationToken))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<PropertyPhotoTypeDto>(entity))
            .Returns(new PropertyPhotoTypeDto { Id = 1 });

        // Act
        await _service.GetByIdAsync(1, cancellationToken);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(1, cancellationToken), Times.Once);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<PropertyPhotoTypeEntity>
        {
            new() { Id = 1, PhotoTypeCode = "FRONT", PhotoTypeName = "Front View", IsActive = true },
            new() { Id = 2, PhotoTypeCode = "BACK", PhotoTypeName = "Back View", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyPhotoTypeEntity, PropertyPhotoTypeDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new PropertyPhotoTypeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new PropertyPhotoTypeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And,
            SearchTerm = null!,
            SortBy = null!
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.PhotoTypeCode == "FRONT");
        Assert.Contains(items, x => x.PhotoTypeCode == "BACK");
    }

    [Fact]
    public async Task GetAllAsync_WithSearchTerm_FiltersResults()
    {
        // Arrange
        var entities = new List<PropertyPhotoTypeEntity>
        {
            new() { Id = 1, PhotoTypeCode = "FRONT", PhotoTypeName = "Front View", Description = "Front", IsActive = true },
            new() { Id = 2, PhotoTypeCode = "BACK", PhotoTypeName = "Back View", Description = "Back", IsActive = true },
            new() { Id = 3, PhotoTypeCode = "LEFT", PhotoTypeName = "Left Side", Description = "Left", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyPhotoTypeEntity, PropertyPhotoTypeDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();

        var service = new PropertyPhotoTypeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new PropertyPhotoTypeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "Front",
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And,
            SortBy = null!
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Single(items);
        Assert.Equal("FRONT", items[0].PhotoTypeCode);
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var entities = new List<PropertyPhotoTypeEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyPhotoTypeEntity, PropertyPhotoTypeDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();

        var service = new PropertyPhotoTypeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new PropertyPhotoTypeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front View",
            Description = "Front facade",
            DisplayOrder = 1,
            IsActive = true,
            CreatedBy = 100
        };

        _mockMapper
            .Setup(m => m.Map<PropertyPhotoTypeEntity>(It.IsAny<CreatePropertyPhotoTypeDto>()))
            .Returns((CreatePropertyPhotoTypeDto dto) => new PropertyPhotoTypeEntity
            {
                PhotoTypeCode = dto.PhotoTypeCode,
                PhotoTypeName = dto.PhotoTypeName,
                Description = dto.Description,
                DisplayOrder = dto.DisplayOrder,
                CreatedBy = dto.CreatedBy,
                CreatedDate = DateTime.Now,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<PropertyPhotoTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyPhotoTypeEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<PropertyPhotoTypeDto>(It.IsAny<PropertyPhotoTypeEntity>()))
            .Returns((PropertyPhotoTypeEntity e) => new PropertyPhotoTypeDto
            {
                PhotoTypeCode = e.PhotoTypeCode,
                PhotoTypeName = e.PhotoTypeName,
                Description = e.Description,
                DisplayOrder = e.DisplayOrder,
                IsActive = true
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("FRONT", result.PhotoTypeCode);
        Assert.Equal("Front View", result.PhotoTypeName);
        Assert.Equal("Front facade", result.Description);
        Assert.Equal(1, result.DisplayOrder);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<PropertyPhotoTypeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNullOptionalFields_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front View",
            Description = null,
            DisplayOrder = null,
            IsActive = true,
            CreatedBy = 100
        };

        _mockMapper
            .Setup(m => m.Map<PropertyPhotoTypeEntity>(It.IsAny<CreatePropertyPhotoTypeDto>()))
            .Returns(new PropertyPhotoTypeEntity
            {
                PhotoTypeCode = "FRONT",
                PhotoTypeName = "Front View",
                Description = null,
                DisplayOrder = null,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<PropertyPhotoTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyPhotoTypeEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<PropertyPhotoTypeDto>(It.IsAny<PropertyPhotoTypeEntity>()))
            .Returns(new PropertyPhotoTypeDto
            {
                PhotoTypeCode = "FRONT",
                PhotoTypeName = "Front View",
                Description = null,
                DisplayOrder = null,
                IsActive = true
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Description);
        Assert.Null(result.DisplayOrder);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "BACK",
            PhotoTypeName = "Back View Updated",
            Description = "Updated description",
            DisplayOrder = 5,
            IsActive = true,
            UpdatedBy = 200
        };

        var existingEntity = new PropertyPhotoTypeEntity
        {
            Id = 1,
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front View",
            Description = "Old description",
            DisplayOrder = 1,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<PropertyPhotoTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdatePropertyPhotoTypeDto>(), It.IsAny<PropertyPhotoTypeEntity>()))
            .Callback((UpdatePropertyPhotoTypeDto src, PropertyPhotoTypeEntity dest) =>
            {
                dest.PhotoTypeCode = src.PhotoTypeCode;
                dest.PhotoTypeName = src.PhotoTypeName;
                dest.Description = src.Description;
                dest.DisplayOrder = src.DisplayOrder;
            });

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertyPhotoTypeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal("BACK", existingEntity.PhotoTypeCode);
        Assert.Equal("Back View Updated", existingEntity.PhotoTypeName);
        Assert.Equal("Updated description", existingEntity.Description);
        Assert.Equal(5, existingEntity.DisplayOrder);
        Assert.True(existingEntity.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
    {
        // Arrange
        var updateDto = new UpdatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "TEST",
            PhotoTypeName = "Test",
            IsActive = true,
            UpdatedBy = 200
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyPhotoTypeEntity?)null);

        // Act
        await _service.UpdateAsync(9999, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertyPhotoTypeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_UpdateIsActiveFalse_WorksCorrectly()
    {
        // Arrange
        var updateDto = new UpdatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front View",
            IsActive = false,
            UpdatedBy = 200
        };

        var existingEntity = new PropertyPhotoTypeEntity
        {
            Id = 1,
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front View",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<PropertyPhotoTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdatePropertyPhotoTypeDto>(), It.IsAny<PropertyPhotoTypeEntity>()))
            .Callback((UpdatePropertyPhotoTypeDto src, PropertyPhotoTypeEntity dest) =>
            {
                dest.IsActive = src.IsActive;
            });

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.False(existingEntity.IsActive);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingEntity_ReturnsTrue()
    {
        // Arrange
        var entity = new PropertyPhotoTypeEntity
        {
            Id = 1,
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front View",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<PropertyPhotoTypeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(1, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertyPhotoTypeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        // Arrange
        int idToDelete = 9999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyPhotoTypeEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertyPhotoTypeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var service = new PropertyPhotoTypeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);

        // Assert
        Assert.NotNull(service);
    }

    #endregion
}
