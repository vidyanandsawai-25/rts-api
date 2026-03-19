using AutoMapper;
using Moq;
using MockQueryable;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

public class PropertyCategoryServiceTests
{
    private readonly Mock<IRepository<PropertyCategoryEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PropertyCategoryService _service;

    public PropertyCategoryServiceTests()
    {
        _mockRepository = new Mock<IRepository<PropertyCategoryEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new PropertyCategoryService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new PropertyCategoryEntity
        {
            PropertyCategoryId = 1,
            PropertyCategoryName = "Test Category",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 1
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<PropertyCategoryDto>(It.IsAny<PropertyCategoryEntity>()))
            .Returns(new PropertyCategoryDto
            {
                PropertyCategoryId = 1,
                PropertyCategoryName = "Test Category",
                IsActive = true
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.PropertyCategoryId);
        Assert.Equal("Test Category", result.PropertyCategoryName);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyCategoryEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<PropertyCategoryEntity>
        {
            new() { PropertyCategoryId = 1, PropertyCategoryName = "Category1", CreatedBy = 1, CreatedDate = DateTime.Now, IsActive = true },
            new() { PropertyCategoryId = 2, PropertyCategoryName = "Category2", CreatedBy = 1, CreatedDate = DateTime.Now, IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyCategoryEntity, PropertyCategoryDto>();
        });
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new PropertyCategoryService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new PropertyCategoryQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.PropertyCategoryName == "Category1");
        Assert.Contains(items, x => x.PropertyCategoryName == "Category2");
    }

    [Fact]
    public async Task GetAllAsync_WithFilters_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<PropertyCategoryEntity>
        {
            new() { PropertyCategoryId = 1, PropertyCategoryName = "Active Category", CreatedBy = 1, CreatedDate = DateTime.Now, IsActive = true },
            new() { PropertyCategoryId = 2, PropertyCategoryName = "Inactive Category", CreatedBy = 1, CreatedDate = DateTime.Now, IsActive = false },
            new() { PropertyCategoryId = 3, PropertyCategoryName = "Another Active", CreatedBy = 1, CreatedDate = DateTime.Now, IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyCategoryEntity, PropertyCategoryDto>();
        });
        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new PropertyCategoryService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new PropertyCategoryQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            IsActive = true
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.True(item.IsActive));
    }

    [Fact]
    public async Task GetAllAsync_WithPropertyCategoryIdFilter_ReturnsSpecificCategory()
    {
        // Arrange
        var entities = new List<PropertyCategoryEntity>
        {
            new() { PropertyCategoryId = 1, PropertyCategoryName = "Category1", CreatedBy = 1, CreatedDate = DateTime.Now, IsActive = true },
            new() { PropertyCategoryId = 2, PropertyCategoryName = "Category2", CreatedBy = 1, CreatedDate = DateTime.Now, IsActive = true },
            new() { PropertyCategoryId = 3, PropertyCategoryName = "Category3", CreatedBy = 1, CreatedDate = DateTime.Now, IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyCategoryEntity, PropertyCategoryDto>();
        });
        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new PropertyCategoryService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new PropertyCategoryQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            PropertyCategoryId = 2
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        var item = result.Items.Single();
        Assert.Equal(2, item.PropertyCategoryId);
        Assert.Equal("Category2", item.PropertyCategoryName);
    }

    [Fact]
    public async Task GetAllAsync_WithSearchTerm_ReturnsMatchingEntities()
    {
        // Arrange
        var entities = new List<PropertyCategoryEntity>
        {
            new() { PropertyCategoryId = 1, PropertyCategoryName = "TestCategory", CreatedBy = 1, CreatedDate = DateTime.Now, IsActive = true },
            new() { PropertyCategoryId = 2, PropertyCategoryName = "OtherCategory", CreatedBy = 1, CreatedDate = DateTime.Now, IsActive = true },
            new() { PropertyCategoryId = 3, PropertyCategoryName = "TestAnother", CreatedBy = 1, CreatedDate = DateTime.Now, IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyCategoryEntity, PropertyCategoryDto>();
        });
        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new PropertyCategoryService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new PropertyCategoryQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "Test"
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Contains("Test", item.PropertyCategoryName));
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var entities = new List<PropertyCategoryEntity>
        {
            new() { PropertyCategoryId = 1, PropertyCategoryName = "Category1", CreatedBy = 1, CreatedDate = DateTime.Now, IsActive = true },
            new() { PropertyCategoryId = 2, PropertyCategoryName = "Category2", CreatedBy = 1, CreatedDate = DateTime.Now, IsActive = true },
            new() { PropertyCategoryId = 3, PropertyCategoryName = "Category3", CreatedBy = 1, CreatedDate = DateTime.Now, IsActive = true },
            new() { PropertyCategoryId = 4, PropertyCategoryName = "Category4", CreatedBy = 1, CreatedDate = DateTime.Now, IsActive = true },
            new() { PropertyCategoryId = 5, PropertyCategoryName = "Category5", CreatedBy = 1, CreatedDate = DateTime.Now, IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyCategoryEntity, PropertyCategoryDto>();
        });
        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new PropertyCategoryService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new PropertyCategoryQueryParameters
        {
            PageNumber = 2,
            PageSize = 2
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PropertyCategoryMappingProfile>();
        });

        IMapper mapper = mapperConfig.CreateMapper();

        var service = new PropertyCategoryService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var createDto = new PropertyCategoryCreateDto
        {
            PropertyCategoryName = "New Category",
            CreatedBy = 5
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<PropertyCategoryEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyCategoryEntity e, CancellationToken _) =>
            {
                e.PropertyCategoryId = 1;
                return e;
            });

        var result = await service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("New Category", result.PropertyCategoryName);

        // Verify that CreatedBy is mapped from the create DTO according to the mapping profile
        _mockRepository.Verify(r => r.AddAsync(
            It.Is<PropertyCategoryEntity>(e => e.CreatedBy == 5),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyCategoryEntity, PropertyCategoryDto>();
            cfg.CreateMap<PropertyCategoryUpdateDto, PropertyCategoryEntity>()
                .ForMember(dest => dest.PropertyCategoryId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
        });

        IMapper realMapper = mapperConfig.CreateMapper();

        var service = new PropertyCategoryService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            realMapper);

        var updateDto = new PropertyCategoryUpdateDto
        {
            PropertyCategoryName = "Updated Category",
            IsActive = true,
            UpdatedBy = 10
        };

        var existingEntity = new PropertyCategoryEntity
        {
            PropertyCategoryId = 1,
            PropertyCategoryName = "Old",
            IsActive = false,
            CreatedBy = 1,
            CreatedDate = DateTime.Now.AddDays(-1)
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<PropertyCategoryEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Updated Category", result.PropertyCategoryName);
        Assert.True(result.IsActive);

        Assert.Equal(10, existingEntity.UpdatedBy);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new PropertyCategoryUpdateDto
        {
            PropertyCategoryName = "Updated Category",
            IsActive = true,
            UpdatedBy = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyCategoryEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertyCategoryEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndReturnsTrue()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new PropertyCategoryEntity
        {
            PropertyCategoryId = idToDelete,
            PropertyCategoryName = "Category to Delete",
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(idToDelete, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyCategoryEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}