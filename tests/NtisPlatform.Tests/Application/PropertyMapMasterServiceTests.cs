using AutoMapper;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Master.PropertyMapMaster;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class PropertyMapMasterServiceTests
{
    private readonly Mock<IRepository<PropertyMapMasterEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PropertyMapMasterService _service;

    public PropertyMapMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<PropertyMapMasterEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        // Setup SaveChangesAsync
        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Optional transaction setups
        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new PropertyMapMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new PropertyMapMasterEntity
        {
            Id = 1,
            ModuleId = 100,
            ParentPropertyMapId = null,
            VersionNo = 1,
            MappingCategory = "ONE_TO_ONE",
            ChangeReason = "Initial mapping",
            Remark = "Test remark",
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<PropertyMapMasterDtos>(It.IsAny<PropertyMapMasterEntity>()))
            .Returns(new PropertyMapMasterDtos
            {
                Id = 1,
                ModuleId = 100,
                ParentPropertyMapId = null,
                VersionNo = 1,
                MappingCategory = "ONE_TO_ONE",
                ChangeReason = "Initial mapping",
                Remark = "Test remark",
                IsActive = true
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(100, result.ModuleId);
        Assert.Equal("ONE_TO_ONE", result.MappingCategory);
        Assert.Equal("Initial mapping", result.ChangeReason);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<PropertyMapMasterDtos>(entity), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyMapMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<PropertyMapMasterDtos>(It.IsAny<PropertyMapMasterEntity>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetByIdAsync_InvalidId_ReturnsNull(int invalidId)
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyMapMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(invalidId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<PropertyMapMasterEntity>
        {
            new() { Id = 1, ModuleId = 100, VersionNo = 1, MappingCategory = "ONE_TO_ONE", IsActive = true },
            new() { Id = 2, ModuleId = 101, VersionNo = 1, MappingCategory = "SPLIT", IsActive = true },
            new() { Id = 3, ModuleId = 102, VersionNo = 2, MappingCategory = "MERGE", IsActive = false }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyMapMasterEntity, PropertyMapMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new PropertyMapMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new PropertyMapQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(3, items.Count);
        Assert.Contains(items, x => x.MappingCategory == "ONE_TO_ONE");
        Assert.Contains(items, x => x.MappingCategory == "SPLIT");
        Assert.Contains(items, x => x.MappingCategory == "MERGE");
    }

    [Fact]
    public async Task GetAllAsync_WithMappingCategoryFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<PropertyMapMasterEntity>
        {
            new() { Id = 1, ModuleId = 100, VersionNo = 1, MappingCategory = "ONE_TO_ONE", IsActive = true },
            new() { Id = 2, ModuleId = 101, VersionNo = 1, MappingCategory = "SPLIT", IsActive = true },
            new() { Id = 3, ModuleId = 102, VersionNo = 1, MappingCategory = "MERGE", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyMapMasterEntity, PropertyMapMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new PropertyMapMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new PropertyMapQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            MappingCategory = "SPLIT"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item =>
             Assert.Contains("SPLIT", item.MappingCategory, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAllAsync_WithModuleIdFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<PropertyMapMasterEntity>
        {
            new() { Id = 1, ModuleId = 100, VersionNo = 1, MappingCategory = "ONE_TO_ONE", IsActive = true },
            new() { Id = 2, ModuleId = 100, VersionNo = 2, MappingCategory = "SPLIT", IsActive = true },
            new() { Id = 3, ModuleId = 101, VersionNo = 1, MappingCategory = "MERGE", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyMapMasterEntity, PropertyMapMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new PropertyMapMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new PropertyMapQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            ModuleId = 100
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal(100, item.ModuleId));
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var entities = new List<PropertyMapMasterEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyMapMasterEntity, PropertyMapMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new PropertyMapMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new PropertyMapQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var entities = Enumerable.Range(1, 25)
            .Select(i => new PropertyMapMasterEntity
            {
                Id = i,
                ModuleId = 100 + i,
                VersionNo = 1,
                MappingCategory = "ONE_TO_ONE",
                IsActive = true
            })
            .ToList();

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyMapMasterEntity, PropertyMapMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new PropertyMapMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new PropertyMapQueryParameters
        {
            PageNumber = 2,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
    }

    [Fact]
    public async Task GetAllAsync_WithSearchTerm_ReturnsMatchingEntities()
    {
        // Arrange
        var entities = new List<PropertyMapMasterEntity>
    {
        new() { Id = 1, ModuleId = 100, VersionNo = 1, MappingCategory = "ONE_TO_ONE", ChangeReason = "Initial setup", Remark = "Test remark 1", IsActive = true },
        new() { Id = 2, ModuleId = 101, VersionNo = 1, MappingCategory = "SPLIT", ChangeReason = "Property division", Remark = "Test remark 2", IsActive = true },
        new() { Id = 3, ModuleId = 102, VersionNo = 1, MappingCategory = "MERGE", ChangeReason = "Merge reason", Remark = "Merged properties", IsActive = true }
    };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyMapMasterEntity, PropertyMapMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new PropertyMapMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new PropertyMapQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "division"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Property division", result.Items.Single().ChangeReason);
    }

    [Fact]
    public async Task GetAllAsync_WithVersionNoFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<PropertyMapMasterEntity>
        {
            new() { Id = 1, ModuleId = 100, VersionNo = 1, MappingCategory = "ONE_TO_ONE", IsActive = true },
            new() { Id = 2, ModuleId = 100, VersionNo = 2, MappingCategory = "SPLIT", IsActive = true },
            new() { Id = 3, ModuleId = 100, VersionNo = 2, MappingCategory = "MERGE", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyMapMasterEntity, PropertyMapMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new PropertyMapMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new PropertyMapQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            VersionNo = 2
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal(2, item.VersionNo));
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreatePropertyMapMasterDto
        {
            ModuleId = 100,
            ParentPropertyMapId = null,
            VersionNo = 1,
            MappingCategory = "ONE_TO_ONE",
            ChangeReason = "Initial mapping",
            Remark = "Test remark",
            IsActive = true,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<PropertyMapMasterEntity>(It.IsAny<CreatePropertyMapMasterDto>()))
            .Returns((CreatePropertyMapMasterDto dto) => new PropertyMapMasterEntity
            {
                ModuleId = dto.ModuleId,
                ParentPropertyMapId = dto.ParentPropertyMapId,
                VersionNo = dto.VersionNo,
                MappingCategory = dto.MappingCategory,
                ChangeReason = dto.ChangeReason,
                Remark = dto.Remark,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<PropertyMapMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyMapMasterEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<PropertyMapMasterDtos>(It.IsAny<PropertyMapMasterEntity>()))
            .Returns((PropertyMapMasterEntity e) => new PropertyMapMasterDtos
            {
                Id = e.Id,
                ModuleId = e.ModuleId,
                VersionNo = e.VersionNo,
                MappingCategory = e.MappingCategory,
                ChangeReason = e.ChangeReason,
                Remark = e.Remark,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(100, result.ModuleId);
        Assert.Equal("ONE_TO_ONE", result.MappingCategory);
        Assert.Equal("Initial mapping", result.ChangeReason);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(
            It.Is<PropertyMapMasterEntity>(e => 
                e.MappingCategory == "ONE_TO_ONE" && 
                e.ModuleId == 100),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("ONE_TO_ONE")]
    [InlineData("SPLIT")]
    [InlineData("MERGE")]
    public async Task CreateAsync_ValidMappingCategories_ReturnsCreatedDto(string mappingCategory)
    {
        // Arrange
        var createDto = new CreatePropertyMapMasterDto
        {
            ModuleId = 100,
            VersionNo = 1,
            MappingCategory = mappingCategory,
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<PropertyMapMasterEntity>(It.IsAny<CreatePropertyMapMasterDto>()))
            .Returns(new PropertyMapMasterEntity
            {
                ModuleId = 100,
                VersionNo = 1,
                MappingCategory = mappingCategory,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<PropertyMapMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyMapMasterEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<PropertyMapMasterDtos>(It.IsAny<PropertyMapMasterEntity>()))
            .Returns(new PropertyMapMasterDtos
            {
                Id = 1,
                ModuleId = 100,
                VersionNo = 1,
                MappingCategory = mappingCategory,
                IsActive = true
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mappingCategory, result.MappingCategory);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithParentPropertyMapId_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreatePropertyMapMasterDto
        {
            ModuleId = 100,
            ParentPropertyMapId = 5,
            VersionNo = 2,
            MappingCategory = "SPLIT",
            ChangeReason = "Child property map",
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<PropertyMapMasterEntity>(It.IsAny<CreatePropertyMapMasterDto>()))
            .Returns(new PropertyMapMasterEntity
            {
                ModuleId = 100,
                ParentPropertyMapId = 5,
                VersionNo = 2,
                MappingCategory = "SPLIT"
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<PropertyMapMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyMapMasterEntity e, CancellationToken _) =>
            {
                e.Id = 10;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<PropertyMapMasterDtos>(It.IsAny<PropertyMapMasterEntity>()))
            .Returns(new PropertyMapMasterDtos
            {
                Id = 10,
                ModuleId = 100,
                ParentPropertyMapId = 5,
                VersionNo = 2,
                MappingCategory = "SPLIT"
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.ParentPropertyMapId);
        Assert.Equal(2, result.VersionNo);
    }

    [Fact]
    public async Task CreateAsync_InactivePropertyMap_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreatePropertyMapMasterDto
        {
            ModuleId = 100,
            VersionNo = 1,
            MappingCategory = "ONE_TO_ONE",
            IsActive = false
        };

        _mockMapper
            .Setup(m => m.Map<PropertyMapMasterEntity>(It.IsAny<CreatePropertyMapMasterDto>()))
            .Returns(new PropertyMapMasterEntity
            {
                Id = 0,
                ModuleId = 100,
                VersionNo = 1,
                MappingCategory = "ONE_TO_ONE",
                IsActive = false
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<PropertyMapMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyMapMasterEntity e, CancellationToken _) =>
            {
                e.Id = 2;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<PropertyMapMasterDtos>(It.IsAny<PropertyMapMasterEntity>()))
            .Returns(new PropertyMapMasterDtos
            {
                Id = 2,
                ModuleId = 100,
                VersionNo = 1,
                MappingCategory = "ONE_TO_ONE",
                IsActive = false
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdatePropertyMapMasterDto
        {
            ModuleId = 100,
            ParentPropertyMapId = null,
            VersionNo = 2,
            MappingCategory = "SPLIT",
            ChangeReason = "Updated mapping",
            Remark = "Updated remark",
            IsActive = true,
            UpdatedBy = 1
        };

        var existingEntity = new PropertyMapMasterEntity
        {
            Id = 1,
            ModuleId = 100,
            VersionNo = 1,
            MappingCategory = "ONE_TO_ONE",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<PropertyMapMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdatePropertyMapMasterDto>(), It.IsAny<PropertyMapMasterEntity>()))
            .Callback((UpdatePropertyMapMasterDto src, PropertyMapMasterEntity dest) =>
            {
                dest.VersionNo = src.VersionNo;
                dest.MappingCategory = src.MappingCategory;
                dest.ChangeReason = src.ChangeReason;
                dest.Remark = src.Remark;
            });

        _mockMapper
            .Setup(m => m.Map<PropertyMapMasterDtos>(It.IsAny<PropertyMapMasterEntity>()))
            .Returns((PropertyMapMasterEntity e) => new PropertyMapMasterDtos
            {
                Id = e.Id,
                ModuleId = e.ModuleId,
                VersionNo = e.VersionNo,
                MappingCategory = e.MappingCategory,
                ChangeReason = e.ChangeReason,
                Remark = e.Remark,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SPLIT", result.MappingCategory);
        Assert.Equal(2, result.VersionNo);
        Assert.Equal("Updated mapping", result.ChangeReason);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdatePropertyMapMasterDto
        {
            ModuleId = 100,
            VersionNo = 1,
            MappingCategory = "ONE_TO_ONE",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyMapMasterEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertyMapMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ChangeMappingCategory_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdatePropertyMapMasterDto
        {
            ModuleId = 100,
            VersionNo = 1,
            MappingCategory = "MERGE",
            ChangeReason = "Changed from SPLIT to MERGE",
            IsActive = true
        };

        var existingEntity = new PropertyMapMasterEntity
        {
            Id = 1,
            ModuleId = 100,
            VersionNo = 1,
            MappingCategory = "SPLIT",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<PropertyMapMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdatePropertyMapMasterDto>(), It.IsAny<PropertyMapMasterEntity>()))
            .Callback((UpdatePropertyMapMasterDto src, PropertyMapMasterEntity dest) =>
            {
                dest.MappingCategory = src.MappingCategory;
                dest.ChangeReason = src.ChangeReason;
            });

        _mockMapper
            .Setup(m => m.Map<PropertyMapMasterDtos>(It.IsAny<PropertyMapMasterEntity>()))
            .Returns((PropertyMapMasterEntity e) => new PropertyMapMasterDtos
            {
                Id = e.Id,
                MappingCategory = e.MappingCategory,
                ChangeReason = e.ChangeReason
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("MERGE", result.MappingCategory);
        Assert.Equal("Changed from SPLIT to MERGE", result.ChangeReason);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSavesSuccessfully()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new PropertyMapMasterEntity
        {
            Id = idToDelete,
            ModuleId = 100,
            VersionNo = 1,
            MappingCategory = "ONE_TO_ONE",
            IsActive = false
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<PropertyMapMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertyMapMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ActivePropertyMap_ShouldStillDelete()
    {
        // Arrange - Even active property maps can be deleted
        var idToDelete = 1;

        var existingEntity = new PropertyMapMasterEntity
        {
            Id = idToDelete,
            ModuleId = 100,
            VersionNo = 1,
            MappingCategory = "ONE_TO_ONE",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<PropertyMapMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertyMapMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyMapMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertyMapMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Transaction Tests

    [Fact]
    public async Task CreateAsync_VerifiesNoExplicitTransactionUsed()
    {
        // Arrange
        var createDto = new CreatePropertyMapMasterDto
        {
            ModuleId = 100,
            VersionNo = 1,
            MappingCategory = "ONE_TO_ONE",
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<PropertyMapMasterEntity>(It.IsAny<CreatePropertyMapMasterDto>()))
            .Returns(new PropertyMapMasterEntity { ModuleId = 100, VersionNo = 1, MappingCategory = "ONE_TO_ONE" });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<PropertyMapMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyMapMasterEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<PropertyMapMasterDtos>(It.IsAny<PropertyMapMasterEntity>()))
            .Returns(new PropertyMapMasterDtos { Id = 1, ModuleId = 100, VersionNo = 1, MappingCategory = "ONE_TO_ONE" });

        // Act
        await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert - Verify NO explicit transaction management in simple CRUD
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_VerifiesNoExplicitTransactionUsed()
    {
        // Arrange
        var updateDto = new UpdatePropertyMapMasterDto
        {
            ModuleId = 100,
            VersionNo = 2,
            MappingCategory = "SPLIT",
            IsActive = true
        };

        var existingEntity = new PropertyMapMasterEntity
        {
            Id = 1,
            ModuleId = 100,
            VersionNo = 1,
            MappingCategory = "ONE_TO_ONE"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<PropertyMapMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdatePropertyMapMasterDto>(), It.IsAny<PropertyMapMasterEntity>()))
            .Callback((UpdatePropertyMapMasterDto src, PropertyMapMasterEntity dest) =>
            {
                dest.VersionNo = src.VersionNo;
                dest.MappingCategory = src.MappingCategory;
            });

        _mockMapper
            .Setup(m => m.Map<PropertyMapMasterDtos>(It.IsAny<PropertyMapMasterEntity>()))
            .Returns(new PropertyMapMasterDtos { Id = 1, ModuleId = 100, VersionNo = 2, MappingCategory = "SPLIT" });

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_VerifiesNoExplicitTransactionUsed()
    {
        // Arrange
        var existingEntity = new PropertyMapMasterEntity
        {
            Id = 1,
            ModuleId = 100,
            VersionNo = 1,
            MappingCategory = "ONE_TO_ONE"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertyMapMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(1, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task CreateAsync_WithLongChangeReason_CreatesSuccessfully()
    {
        // Arrange - ChangeReason max length is 500
        var longChangeReason = new string('A', 500);
        var createDto = new CreatePropertyMapMasterDto
        {
            ModuleId = 100,
            VersionNo = 1,
            MappingCategory = "ONE_TO_ONE",
            ChangeReason = longChangeReason,
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<PropertyMapMasterEntity>(It.IsAny<CreatePropertyMapMasterDto>()))
            .Returns(new PropertyMapMasterEntity { ModuleId = 100, VersionNo = 1, MappingCategory = "ONE_TO_ONE", ChangeReason = longChangeReason });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<PropertyMapMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyMapMasterEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<PropertyMapMasterDtos>(It.IsAny<PropertyMapMasterEntity>()))
            .Returns(new PropertyMapMasterDtos { Id = 1, ModuleId = 100, VersionNo = 1, MappingCategory = "ONE_TO_ONE", ChangeReason = longChangeReason });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(longChangeReason, result.ChangeReason);
        Assert.Equal(500, result.ChangeReason!.Length);
    }

    [Fact]
    public async Task CreateAsync_WithLongRemark_CreatesSuccessfully()
    {
        // Arrange - Remark max length is 500
        var longRemark = new string('B', 500);
        var createDto = new CreatePropertyMapMasterDto
        {
            ModuleId = 100,
            VersionNo = 1,
            MappingCategory = "SPLIT",
            Remark = longRemark,
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<PropertyMapMasterEntity>(It.IsAny<CreatePropertyMapMasterDto>()))
            .Returns(new PropertyMapMasterEntity { ModuleId = 100, VersionNo = 1, MappingCategory = "SPLIT", Remark = longRemark });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<PropertyMapMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyMapMasterEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<PropertyMapMasterDtos>(It.IsAny<PropertyMapMasterEntity>()))
            .Returns(new PropertyMapMasterDtos { Id = 1, ModuleId = 100, VersionNo = 1, MappingCategory = "SPLIT", Remark = longRemark });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(longRemark, result.Remark);
        Assert.Equal(500, result.Remark!.Length);
    }

    [Fact]
    public async Task GetAllAsync_WithIsActiveFilter_ReturnsOnlyActiveEntities()
    {
        // Arrange
        var entities = new List<PropertyMapMasterEntity>
        {
            new() { Id = 1, ModuleId = 100, VersionNo = 1, MappingCategory = "ONE_TO_ONE", IsActive = true },
            new() { Id = 2, ModuleId = 101, VersionNo = 1, MappingCategory = "SPLIT", IsActive = false },
            new() { Id = 3, ModuleId = 102, VersionNo = 1, MappingCategory = "MERGE", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyMapMasterEntity, PropertyMapMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new PropertyMapMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new PropertyMapQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            IsActive = true
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.True(item.IsActive));
    }

    [Fact]
    public async Task GetAllAsync_WithParentPropertyMapIdFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<PropertyMapMasterEntity>
        {
            new() { Id = 1, ModuleId = 100, ParentPropertyMapId = null, VersionNo = 1, MappingCategory = "ONE_TO_ONE", IsActive = true },
            new() { Id = 2, ModuleId = 100, ParentPropertyMapId = 1, VersionNo = 2, MappingCategory = "SPLIT", IsActive = true },
            new() { Id = 3, ModuleId = 100, ParentPropertyMapId = 1, VersionNo = 2, MappingCategory = "SPLIT", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyMapMasterEntity, PropertyMapMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new PropertyMapMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new PropertyMapQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            ParentPropertyMapId = 1
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal(1, item.ParentPropertyMapId));
    }

    #endregion

    #region SearchPropertyMappingsAsync Tests

    [Fact]
    public async Task SearchPropertyMappingsAsync_ShouldSearchByUnifiedSearchTerm_ForOldProperties()
    {
        // Arrange
        var mockPmmRepo = new Mock<IRepository<PropertyMapMasterEntity, int>>();
        var mockPmdRepo = new Mock<IRepository<PropertyMapDetailEntity, int>>();
        var mockPmRepo = new Mock<IRepository<PropertyEntity, int>>();
        var mockPmoRepo = new Mock<IRepository<PropertyMastOldEntity, int>>();
        var mockUow = new Mock<IUnitOfWork>();
        var mapper = NtisPlatform.Tests.Helpers.AutoMapperTestHelper.CreateMapper();

        var pmmList = new List<PropertyMapMasterEntity>().BuildMock();
        var pmdList = new List<PropertyMapDetailEntity>().BuildMock();

        var pmList = new List<PropertyEntity>
        {
            new()
            {
                Id = 20,
                PropertyNo = "PROP-NEW-99",
                OwnerName = "Jane Smith New",
                MobileNo = "9998887770",
                IsActive = true
            }
        }.BuildMock();

        var pmoList = new List<PropertyMastOldEntity>
        {
            new()
            {
                Id = 10,
                OldPropertyNo = "PROP-OLD-88",
                OldOwnerName = "Jane Smith Old",
                OldMobileNo = "9998887770",
                IsActive = true
            }
        }.BuildMock();

        mockPmmRepo.Setup(r => r.GetQueryable()).Returns(pmmList);
        mockPmdRepo.Setup(r => r.GetQueryable()).Returns(pmdList);
        mockPmRepo.Setup(r => r.GetQueryable()).Returns(pmList);
        mockPmoRepo.Setup(r => r.GetQueryable()).Returns(pmoList);

        var service = new PropertyMapMasterService(
            mockPmmRepo.Object,
            mockUow.Object,
            mapper,
            mockPmdRepo.Object,
            mockPmRepo.Object,
            mockPmoRepo.Object
        );

        var q = new PropertyMapDetailQueryParameters
        {
            SearchTerm = "Jane Smith" // Single unified search term
        };

        // Act
        var result = await service.SearchPropertyMappingsAsync(q, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.OldPropertySuggestions);
        Assert.Equal("Jane Smith Old", result.OldPropertySuggestions.First().OldOwnerName);
    }

    [Fact]
    public async Task SearchPropertyMappingsAsync_ShouldSearchByUnifiedSearchTerm_MatchingOldWardNo_OldPropertyNo_AndOldPartitionNo()
    {
        // Arrange
        var mockPmmRepo = new Mock<IRepository<PropertyMapMasterEntity, int>>();
        var mockPmdRepo = new Mock<IRepository<PropertyMapDetailEntity, int>>();
        var mockPmRepo = new Mock<IRepository<PropertyEntity, int>>();
        var mockPmoRepo = new Mock<IRepository<PropertyMastOldEntity, int>>();
        var mockUow = new Mock<IUnitOfWork>();
        var mapper = NtisPlatform.Tests.Helpers.AutoMapperTestHelper.CreateMapper();

        var pmmList = new List<PropertyMapMasterEntity>().BuildMock();
        var pmdList = new List<PropertyMapDetailEntity>().BuildMock();
        var pmList = new List<PropertyEntity>().BuildMock();

        var pmoList = new List<PropertyMastOldEntity>
        {
            new()
            {
                Id = 10,
                OldPropertyNo = "PROP-111",
                OldWardNo = "Ward-X",
                OldPartitionNo = "Part-Y",
                IsActive = true
            },
            new()
            {
                Id = 11,
                OldPropertyNo = "PROP-222",
                OldWardNo = "Ward-Z",
                OldPartitionNo = "Part-W",
                IsActive = true
            }
        }.BuildMock();

        mockPmmRepo.Setup(r => r.GetQueryable()).Returns(pmmList);
        mockPmdRepo.Setup(r => r.GetQueryable()).Returns(pmdList);
        mockPmRepo.Setup(r => r.GetQueryable()).Returns(pmList);
        mockPmoRepo.Setup(r => r.GetQueryable()).Returns(pmoList);

        var service = new PropertyMapMasterService(
            mockPmmRepo.Object,
            mockUow.Object,
            mapper,
            mockPmdRepo.Object,
            mockPmRepo.Object,
            mockPmoRepo.Object
        );

        // Test matching old partition number
        var qPartition = new PropertyMapDetailQueryParameters { SearchTerm = "Part-Y" };
        var resultPartition = await service.SearchPropertyMappingsAsync(qPartition, CancellationToken.None);
        Assert.NotNull(resultPartition);
        Assert.Single(resultPartition.OldPropertySuggestions);
        Assert.Equal(10, resultPartition.OldPropertySuggestions.First().Id);

        // Test matching old property number
        var qProp = new PropertyMapDetailQueryParameters { SearchTerm = "PROP-222" };
        var resultProp = await service.SearchPropertyMappingsAsync(qProp, CancellationToken.None);
        Assert.NotNull(resultProp);
        Assert.Single(resultProp.OldPropertySuggestions);
        Assert.Equal(11, resultProp.OldPropertySuggestions.First().Id);

        // Test matching old ward number
        var qWard = new PropertyMapDetailQueryParameters { SearchTerm = "Ward-Z" };
        var resultWard = await service.SearchPropertyMappingsAsync(qWard, CancellationToken.None);
        Assert.NotNull(resultWard);
        Assert.Single(resultWard.OldPropertySuggestions);
        Assert.Equal(11, resultWard.OldPropertySuggestions.First().Id);
    }

    [Fact]
    public async Task SearchPropertyMappingsAsync_ShouldReturnPropertyDetailsOld_ForSuggestedProperties()
    {
        // Arrange
        var mockPmmRepo = new Mock<IRepository<PropertyMapMasterEntity, int>>();
        var mockPmdRepo = new Mock<IRepository<PropertyMapDetailEntity, int>>();
        var mockPmRepo = new Mock<IRepository<PropertyEntity, int>>();
        var mockPmoRepo = new Mock<IRepository<PropertyMastOldEntity, int>>();
        var mockPdoRepo = new Mock<IRepository<PropertyDetailsOldEntity, int>>();
        var mockUow = new Mock<IUnitOfWork>();
        var mapper = NtisPlatform.Tests.Helpers.AutoMapperTestHelper.CreateMapper();

        var pmmList = new List<PropertyMapMasterEntity>().BuildMock();
        var pmdList = new List<PropertyMapDetailEntity>().BuildMock();
        var pmList = new List<PropertyEntity>().BuildMock();

        var pmoList = new List<PropertyMastOldEntity>
        {
            new()
            {
                Id = 10,
                OldPropertyNo = "PROP-111",
                IsActive = true
            }
        }.BuildMock();

        var pdoList = new List<PropertyDetailsOldEntity>
        {
            new()
            {
                Id = 100,
                PropertyMastOldId = 10,
                OldFloorId = 1,
                OldConstructionYear = "2015",
                IsActive = true,
                MarkedForDeletion = false
            }
        }.BuildMock();

        mockPmmRepo.Setup(r => r.GetQueryable()).Returns(pmmList);
        mockPmdRepo.Setup(r => r.GetQueryable()).Returns(pmdList);
        mockPmRepo.Setup(r => r.GetQueryable()).Returns(pmList);
        mockPmoRepo.Setup(r => r.GetQueryable()).Returns(pmoList);
        mockPdoRepo.Setup(r => r.GetQueryable()).Returns(pdoList);

        var service = new PropertyMapMasterService(
            mockPmmRepo.Object,
            mockUow.Object,
            mapper,
            mockPmdRepo.Object,
            mockPmRepo.Object,
            mockPmoRepo.Object,
            mockPdoRepo.Object
        );

        var q = new PropertyMapDetailQueryParameters { SearchTerm = "PROP-111" };

        // Act
        var result = await service.SearchPropertyMappingsAsync(q, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.OldPropertySuggestions);
        var sug = result.OldPropertySuggestions.First();
        Assert.Equal(10, sug.Id);
        Assert.Single(sug.PropertyDetailsOld);
        var detail = sug.PropertyDetailsOld.First();
        Assert.Equal(100, detail.Id);
        Assert.Equal(10, detail.PropertyId);
        Assert.Equal(1, detail.OldFloorId);
        Assert.Equal("2015", detail.OldConstructionYear);
    }

    [Fact]
    public async Task SearchPropertyMappingsAsync_ShouldReturnMappedNewPropertyNo_WhenAlreadyMapped()
    {
        // Arrange
        var mockPmmRepo = new Mock<IRepository<PropertyMapMasterEntity, int>>();
        var mockPmdRepo = new Mock<IRepository<PropertyMapDetailEntity, int>>();
        var mockPmRepo = new Mock<IRepository<PropertyEntity, int>>();
        var mockPmoRepo = new Mock<IRepository<PropertyMastOldEntity, int>>();
        var mockPdoRepo = new Mock<IRepository<PropertyDetailsOldEntity, int>>();
        var mockUow = new Mock<IUnitOfWork>();
        var mapper = NtisPlatform.Tests.Helpers.AutoMapperTestHelper.CreateMapper();

        var pmmList = new List<PropertyMapMasterEntity>
        {
            new()
            {
                Id = 1,
                IsActive = true
            }
        }.BuildMock();
        
        var pmdList = new List<PropertyMapDetailEntity>
        {
            new()
            {
                Id = 1,
                PropertyMapId = 1,
                PropertyIdOld = 10,
                PropertyIdNew = 20,
                IsActive = true
            }
        }.BuildMock();

        var pmList = new List<PropertyEntity>
        {
            new()
            {
                Id = 20,
                PropertyNo = "PROP-NEW-123",
                PartitionNo = "PART-A",
                WardId = 5,
                IsActive = true,
                Ward = new WardEntity { Id = 5, WardNo = "WARD-5" }
            }
        }.BuildMock();

        var pmoList = new List<PropertyMastOldEntity>
        {
            new()
            {
                Id = 10,
                OldPropertyNo = "PROP-OLD-111",
                IsActive = true
            }
        }.BuildMock();

        var pdoList = new List<PropertyDetailsOldEntity>().BuildMock();

        mockPmmRepo.Setup(r => r.GetQueryable()).Returns(pmmList);
        mockPmdRepo.Setup(r => r.GetQueryable()).Returns(pmdList);
        mockPmRepo.Setup(r => r.GetQueryable()).Returns(pmList);
        mockPmoRepo.Setup(r => r.GetQueryable()).Returns(pmoList);
        mockPdoRepo.Setup(r => r.GetQueryable()).Returns(pdoList);

        var service = new PropertyMapMasterService(
            mockPmmRepo.Object,
            mockUow.Object,
            mapper,
            mockPmdRepo.Object,
            mockPmRepo.Object,
            mockPmoRepo.Object,
            mockPdoRepo.Object
        );

        var q = new PropertyMapDetailQueryParameters { SearchTerm = "PROP-OLD-111" };

        // Act
        var result = await service.SearchPropertyMappingsAsync(q, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.OldPropertySuggestions);
        var sug = result.OldPropertySuggestions.First();
        Assert.Equal(10, sug.Id);
        Assert.True(sug.IsMapped);
        Assert.Equal("WARD-5-PROP-NEW-123/PART-A", sug.MappedNewPropertyNo);
    }

    [Fact]
    public async Task SearchPropertyMappingsAsync_ShouldSupportPaginationMetadataAndOffset()
    {
        // Arrange
        var mockPmmRepo = new Mock<IRepository<PropertyMapMasterEntity, int>>();
        var mockPmdRepo = new Mock<IRepository<PropertyMapDetailEntity, int>>();
        var mockPmRepo = new Mock<IRepository<PropertyEntity, int>>();
        var mockPmoRepo = new Mock<IRepository<PropertyMastOldEntity, int>>();
        var mockUow = new Mock<IUnitOfWork>();
        var mapper = NtisPlatform.Tests.Helpers.AutoMapperTestHelper.CreateMapper();

        var pmmList = new List<PropertyMapMasterEntity>().BuildMock();
        var pmdList = new List<PropertyMapDetailEntity>().BuildMock();
        var pmList = new List<PropertyEntity>().BuildMock();

        // Create 25 matching old properties
        var pmoList = Enumerable.Range(1, 25).Select(i => new PropertyMastOldEntity
        {
            Id = i,
            OldPropertyNo = $"PROP-OLD-{i}",
            OldOwnerName = "John Doe",
            IsActive = true
        }).ToList().BuildMock();

        mockPmmRepo.Setup(r => r.GetQueryable()).Returns(pmmList);
        mockPmdRepo.Setup(r => r.GetQueryable()).Returns(pmdList);
        mockPmRepo.Setup(r => r.GetQueryable()).Returns(pmList);
        mockPmoRepo.Setup(r => r.GetQueryable()).Returns(pmoList);

        var service = new PropertyMapMasterService(
            mockPmmRepo.Object,
            mockUow.Object,
            mapper,
            mockPmdRepo.Object,
            mockPmRepo.Object,
            mockPmoRepo.Object
        );

        // Act - Request Page 2 with PageSize = 10
        var qPage2 = new PropertyMapDetailQueryParameters
        {
            SearchTerm = "John Doe",
            PageNumber = 2,
            PageSize = 10
        };

        var resultPage2 = await service.SearchPropertyMappingsAsync(qPage2, CancellationToken.None);

        // Assert
        Assert.NotNull(resultPage2);
        Assert.Equal(25, resultPage2.TotalCount);
        Assert.Equal(2, resultPage2.PageNumber);
        Assert.Equal(10, resultPage2.PageSize);
        Assert.Equal(3, resultPage2.TotalPages);
        Assert.True(resultPage2.HasPrevious);
        Assert.True(resultPage2.HasNext);
        Assert.Equal(10, resultPage2.OldPropertySuggestions.Count);
        // Page 2 items should be ID 11 to 20
        Assert.Equal(11, resultPage2.OldPropertySuggestions.First().Id);
        Assert.Equal(20, resultPage2.OldPropertySuggestions.Last().Id);
    }

    [Fact]
    public async Task SearchPropertyMappingsAsync_ShouldRankExactMatchesTop()
    {
        // Arrange
        var mockPmmRepo = new Mock<IRepository<PropertyMapMasterEntity, int>>();
        var mockPmdRepo = new Mock<IRepository<PropertyMapDetailEntity, int>>();
        var mockPmRepo = new Mock<IRepository<PropertyEntity, int>>();
        var mockPmoRepo = new Mock<IRepository<PropertyMastOldEntity, int>>();
        var mockUow = new Mock<IUnitOfWork>();
        var mapper = NtisPlatform.Tests.Helpers.AutoMapperTestHelper.CreateMapper();

        var pmmList = new List<PropertyMapMasterEntity>().BuildMock();
        var pmdList = new List<PropertyMapDetailEntity>().BuildMock();
        var pmList = new List<PropertyEntity>().BuildMock();

        var pmoList = new List<PropertyMastOldEntity>
        {
            new() { Id = 1, OldOwnerName = "John Doe Senior", IsActive = true },
            new() { Id = 2, OldOwnerName = "John Doe", IsActive = true }, // Exact match
            new() { Id = 3, OldOwnerName = "John Doe Junior", IsActive = true }
        }.BuildMock();

        mockPmmRepo.Setup(r => r.GetQueryable()).Returns(pmmList);
        mockPmdRepo.Setup(r => r.GetQueryable()).Returns(pmdList);
        mockPmRepo.Setup(r => r.GetQueryable()).Returns(pmList);
        mockPmoRepo.Setup(r => r.GetQueryable()).Returns(pmoList);

        var service = new PropertyMapMasterService(
            mockPmmRepo.Object,
            mockUow.Object,
            mapper,
            mockPmdRepo.Object,
            mockPmRepo.Object,
            mockPmoRepo.Object
        );

        var q = new PropertyMapDetailQueryParameters
        {
            OldOwnerName = "John Doe"
        };

        // Act
        var result = await service.SearchPropertyMappingsAsync(q, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.OldPropertySuggestions.Count);
        // Exact match (ID = 2) should be ranked FIRST
        Assert.Equal(2, result.OldPropertySuggestions.First().Id);
        Assert.Equal("John Doe", result.OldPropertySuggestions.First().OldOwnerName);
    }
    [Fact]
    [Trait("Category", "Performance")]
    public async Task SearchPropertyMappingsAsync_PerformanceTest_WithLargeDataset()
    {
        // Arrange
        var mockPmmRepo = new Mock<IRepository<PropertyMapMasterEntity, int>>();
        var mockPmdRepo = new Mock<IRepository<PropertyMapDetailEntity, int>>();
        var mockPmRepo = new Mock<IRepository<PropertyEntity, int>>();
        var mockPmoRepo = new Mock<IRepository<PropertyMastOldEntity, int>>();
        var mockUow = new Mock<IUnitOfWork>();

        var mapper =
            NtisPlatform.Tests.Helpers.AutoMapperTestHelper.CreateMapper();

        var pmmList = new List<PropertyMapMasterEntity>().BuildMock();
        var pmdList = new List<PropertyMapDetailEntity>().BuildMock();
        var pmList = new List<PropertyEntity>().BuildMock();

        // Create 10,000 old property records.
        // 5,000 records will match "Jane Smith".
        var pmoList = Enumerable.Range(1, 10000)
            .Select(i => new PropertyMastOldEntity
            {
                Id = i,
                OldPropertyNo = $"PROP-OLD-{i}",
                OldWardNo = $"Ward-{i % 10}",
                OldOwnerName = i % 2 == 0
                    ? "Jane Smith"
                    : "John Doe",
                OldMobileNo = $"98765{i:D5}",
                IsActive = true
            })
            .ToList()
            .BuildMock();

        mockPmmRepo
            .Setup(r => r.GetQueryable())
            .Returns(pmmList);

        mockPmdRepo
            .Setup(r => r.GetQueryable())
            .Returns(pmdList);

        mockPmRepo
            .Setup(r => r.GetQueryable())
            .Returns(pmList);

        mockPmoRepo
            .Setup(r => r.GetQueryable())
            .Returns(pmoList);

        var service = new PropertyMapMasterService(
            mockPmmRepo.Object,
            mockUow.Object,
            mapper,
            mockPmdRepo.Object,
            mockPmRepo.Object,
            mockPmoRepo.Object
        );

        var queryParameters = new PropertyMapDetailQueryParameters
        {
            SearchTerm = "Jane Smith",
            PageNumber = 50,
            PageSize = 20
        };

        // Warm-up calls to reduce JIT/tiering effects.
        const int warmupIterations = 3;

        for (var i = 0; i < warmupIterations; i++)
        {
            await service.SearchPropertyMappingsAsync(
                queryParameters,
                CancellationToken.None);
        }

        // Act
        // Use an odd number so median calculation is straightforward.
        const int iterations = 11;

        var iterationTimes = new double[iterations];

        PropertyMapSearchResultDto lastResult = null!;

        for (var i = 0; i < iterations; i++)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            lastResult = await service.SearchPropertyMappingsAsync(
                queryParameters,
                CancellationToken.None);

            stopwatch.Stop();

            iterationTimes[i] = stopwatch.Elapsed.TotalMilliseconds;
        }

        // Calculate median.
        Array.Sort(iterationTimes);

        var medianMs = iterationTimes[iterations / 2];

        // Assert result correctness
        Assert.NotNull(lastResult);

        Assert.Equal(
            5000,
            lastResult.TotalCount);

        Assert.Equal(
            50,
            lastResult.PageNumber);

        Assert.Equal(
            20,
            lastResult.PageSize);

        Assert.Equal(
            250,
            lastResult.TotalPages);

        Assert.Equal(
            20,
            lastResult.OldPropertySuggestions.Count);

        // Detect CI environment.
        var isCi =
            !string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("CI")) ||
            !string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||
            !string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("TF_BUILD"));

        // Allow threshold override from CI/environment.
        var configuredThreshold =
            Environment.GetEnvironmentVariable(
                "PROPERTY_MAP_SEARCH_MEDIAN_BUDGET_MS");

        double thresholdMs;

        if (!double.TryParse(
                configuredThreshold,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out thresholdMs))
        {
            // More lenient threshold for CI and virtualized/local test runners.
            thresholdMs = isCi ? 400 : 350;
        }

        // Useful diagnostics when the test fails.
        var timingSamples = string.Join(
            ", ",
            iterationTimes.Select(
                x => x.ToString(
                    "F2",
                    System.Globalization.CultureInfo.InvariantCulture)));

        // Assert median execution time is under threshold.
        Assert.True(
            medianMs < thresholdMs,
            $"Performance test failed: " +
            $"Median search time was {medianMs:F2}ms, " +
            $"expected < {thresholdMs:F2}ms. " +
            $"Environment: {(isCi ? "CI" : "Local")}. " +
            $"Samples: [{timingSamples}]");
    }
    [Fact]
    public async Task GetMappedPropertiesAsync_ShouldFilterBySearchParameters_AndReturnPropertyDetailsOld()
    {
        // Arrange
        var mockPmmRepo = new Mock<IRepository<PropertyMapMasterEntity, int>>();
        var mockPmdRepo = new Mock<IRepository<PropertyMapDetailEntity, int>>();
        var mockPmRepo = new Mock<IRepository<PropertyEntity, int>>();
        var mockPmoRepo = new Mock<IRepository<PropertyMastOldEntity, int>>();
        var mockPdoRepo = new Mock<IRepository<PropertyDetailsOldEntity, int>>();
        var mockUow = new Mock<IUnitOfWork>();
        var mapper = NtisPlatform.Tests.Helpers.AutoMapperTestHelper.CreateMapper();

        var pmmList = new List<PropertyMapMasterEntity>
        {
            new() { Id = 1, MappingCategory = "ONE_TO_ONE", IsActive = true }
        }.BuildMock();

        var pmdList = new List<PropertyMapDetailEntity>
        {
            new() { Id = 5, PropertyMapId = 1, PropertyIdOld = 10, PropertyIdNew = 20, IsActive = true }
        }.BuildMock();

        var pmList = new List<PropertyEntity>
        {
            new() { Id = 20, PropertyNo = "PROP-NEW", OwnerName = "John Doe New", IsActive = true }
        }.BuildMock();

        var pmoList = new List<PropertyMastOldEntity>
        {
            new() { Id = 10, OldPropertyNo = "PROP-OLD", OldOwnerName = "John Doe Old", IsActive = true }
        }.BuildMock();

        var pdoList = new List<PropertyDetailsOldEntity>
        {
            new()
            {
                Id = 100,
                PropertyMastOldId = 10,
                OldFloorId = 1,
                OldConstructionYear = "2015",
                IsActive = true,
                MarkedForDeletion = false
            }
        }.BuildMock();

        mockPmmRepo.Setup(r => r.GetQueryable()).Returns(pmmList);
        mockPmdRepo.Setup(r => r.GetQueryable()).Returns(pmdList);
        mockPmRepo.Setup(r => r.GetQueryable()).Returns(pmList);
        mockPmoRepo.Setup(r => r.GetQueryable()).Returns(pmoList);
        mockPdoRepo.Setup(r => r.GetQueryable()).Returns(pdoList);

        var service = new PropertyMapMasterService(
            mockPmmRepo.Object,
            mockUow.Object,
            mapper,
            mockPmdRepo.Object,
            mockPmRepo.Object,
            mockPmoRepo.Object,
            mockPdoRepo.Object
        );

        var q = new PropertyMapDetailQueryParameters
        {
            PropertyId = 20
        };

        // Act
        var result = await service.GetMappedPropertiesAsync(q, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        var item = result.Items.First();
        Assert.Equal(20, item.PropertyId);
        Assert.Equal("PROP-OLD", item.OldPropertyNo);
        Assert.Equal("John Doe Old", item.OldOwnerName);
        Assert.Equal("ONE_TO_ONE", item.MappingCategory);
        Assert.Single(item.PropertyDetailsOld);
        var detail = item.PropertyDetailsOld.First();
        Assert.Equal(100, detail.Id);
        Assert.Equal(10, detail.PropertyId);
        Assert.Equal("2015", detail.OldConstructionYear);
    }

    [Fact]
    public async Task GetMappedPropertiesAsync_UnmappedProperty_ReturnsPropertyFromPropertyMast()
    {
        // Arrange
        var mockPmmRepo = new Mock<IRepository<PropertyMapMasterEntity, int>>();
        var mockPmdRepo = new Mock<IRepository<PropertyMapDetailEntity, int>>();
        var mockPmRepo = new Mock<IRepository<PropertyEntity, int>>();
        var mockPmoRepo = new Mock<IRepository<PropertyMastOldEntity, int>>();
        var mockPdoRepo = new Mock<IRepository<PropertyDetailsOldEntity, int>>();
        var mockUow = new Mock<IUnitOfWork>();
        var mapper = NtisPlatform.Tests.Helpers.AutoMapperTestHelper.CreateMapper();

        var pmmList = new List<PropertyMapMasterEntity>().BuildMock();
        var pmdList = new List<PropertyMapDetailEntity>().BuildMock(); // No mapping record

        var pmList = new List<PropertyEntity>
        {
            new() { Id = 3740000, PropertyNo = "PROP-3740000", OwnerName = "Unmapped Owner", IsActive = true }
        }.BuildMock();

        var pmoList = new List<PropertyMastOldEntity>().BuildMock();
        var pdoList = new List<PropertyDetailsOldEntity>().BuildMock();

        mockPmmRepo.Setup(r => r.GetQueryable()).Returns(pmmList);
        mockPmdRepo.Setup(r => r.GetQueryable()).Returns(pmdList);
        mockPmRepo.Setup(r => r.GetQueryable()).Returns(pmList);
        mockPmoRepo.Setup(r => r.GetQueryable()).Returns(pmoList);
        mockPdoRepo.Setup(r => r.GetQueryable()).Returns(pdoList);

        var service = new PropertyMapMasterService(
            mockPmmRepo.Object,
            mockUow.Object,
            mapper,
            mockPmdRepo.Object,
            mockPmRepo.Object,
            mockPmoRepo.Object,
            mockPdoRepo.Object
        );

        var q = new PropertyMapDetailQueryParameters
        {
            PropertyId = 3740000
        };

        // Act
        var result = await service.GetMappedPropertiesAsync(q, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        var item = result.Items.First();
        Assert.Equal(3740000, item.PropertyId);
        Assert.NotNull(item.NewPropertyInfo);
        Assert.Equal("PROP-3740000", item.NewPropertyInfo.PropertyNo);
        Assert.Equal("Unmapped Owner", item.NewPropertyInfo.OwnerName);
        Assert.Null(item.OldPropertyNo);
        Assert.Empty(item.MappingCategory);
    }

    #endregion
}