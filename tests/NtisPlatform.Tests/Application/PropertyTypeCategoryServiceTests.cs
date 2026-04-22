using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MockQueryable;
using Moq;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace NtisPlatform.Tests.Application;

#region PropertyTypeCategoryEntity Tests

public class PropertyTypeCategoryEntityTests
{
    [Fact]
    public void PropertyTypeCategoryEntity_Properties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var entity = new PropertyTypeCategoryEntity
        {
            Id = 1,
            PropertyTypeCategory = "Residential",
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now,
            UpdatedBy = 2,
            UpdatedDate = DateTime.Now
        };

        // Assert
        Assert.Equal(1, entity.Id);
        Assert.Equal("Residential", entity.PropertyTypeCategory);
        Assert.True(entity.IsActive);
        Assert.Equal(1, entity.CreatedBy);
        Assert.NotNull(entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.NotNull(entity.UpdatedDate);
    }

    [Fact]
    public void PropertyTypeCategoryEntity_PropertyTypeCategory_CanBeNull()
    {
        // Arrange & Act
        var entity = new PropertyTypeCategoryEntity
        {
            Id = 1,
            PropertyTypeCategory = null
        };

        // Assert
        Assert.Null(entity.PropertyTypeCategory);
    }

    [Fact]
    public void PropertyTypeCategoryEntity_InheritsFromBaseEntity()
    {
        // Arrange & Act
        var entity = new PropertyTypeCategoryEntity();

        // Assert
        Assert.IsAssignableFrom<BaseEntity>(entity);
    }
}

#endregion

#region PropertyTypeCategoryDto Tests

public class PropertyTypeCategoryDtoTests
{
    [Fact]
    public void PropertyTypeCategoryDto_Properties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var dto = new PropertyTypeCategoryDto
        {
            Id = 1,
            PropertyTypeCategory = "Commercial",
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal("Commercial", dto.PropertyTypeCategory);
        Assert.True(dto.IsActive);
        Assert.NotNull(dto.CreatedDate);
        Assert.NotNull(dto.UpdatedDate);
    }

    [Fact]
    public void PropertyTypeCategoryDto_InheritsFromBaseDtos()
    {
        // Arrange & Act
        var dto = new PropertyTypeCategoryDto();

        // Assert
        Assert.IsAssignableFrom<BaseDtos>(dto);
    }

    [Fact]
    public void PropertyTypeCategoryDto_PropertyTypeCategory_CanBeNull()
    {
        // Arrange & Act
        var dto = new PropertyTypeCategoryDto
        {
            Id = 1,
            PropertyTypeCategory = null
        };

        // Assert
        Assert.Null(dto.PropertyTypeCategory);
    }
}

#endregion

#region CreatePropertyTypeCategoryDto Tests

public class CreatePropertyTypeCategoryDtoTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void CreatePropertyTypeCategoryDto_ValidData_PassesValidation()
    {
        // Arrange
        var dto = new CreatePropertyTypeCategoryDto
        {
            PropertyTypeCategory = "Industrial",
            CreatedBy = 1
        };

        // Act
        var results = Validate(dto);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void CreatePropertyTypeCategoryDto_PropertyTypeCategoryRequired_FailsValidation()
    {
        // Arrange
        var dto = new CreatePropertyTypeCategoryDto
        {
            PropertyTypeCategory = null
        };

        // Act
        var results = Validate(dto);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "PropertyTypeCategory_Required");
    }

    [Fact]
    public void CreatePropertyTypeCategoryDto_PropertyTypeCategoryMaxLength_FailsValidation()
    {
        // Arrange
        var dto = new CreatePropertyTypeCategoryDto
        {
            PropertyTypeCategory = new string('X', 101) // 101 characters, max is 100
        };

        // Act
        var results = Validate(dto);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "PropertyTypeCategory_MaxLen_100");
    }

    [Fact]
    public void CreatePropertyTypeCategoryDto_PropertyTypeCategory_TrimmedCorrectly()
    {
        // Arrange
        var dto = new CreatePropertyTypeCategoryDto
        {
            PropertyTypeCategory = "  Residential  "
        };

        // Act
        var result = dto.PropertyTypeCategory;

        // Assert
        Assert.Equal("Residential", result);
    }

    [Fact]
    public void CreatePropertyTypeCategoryDto_PropertyTypeCategoryWhitespace_SetsToNull()
    {
        // Arrange
        var dto = new CreatePropertyTypeCategoryDto
        {
            PropertyTypeCategory = "   "
        };

        // Act
        var result = dto.PropertyTypeCategory;

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CreatePropertyTypeCategoryDto_InheritsFromCreateBaseDtos()
    {
        // Arrange & Act
        var dto = new CreatePropertyTypeCategoryDto();

        // Assert
        Assert.IsAssignableFrom<CreateBaseDtos>(dto);
    }

    [Fact]
    public void CreatePropertyTypeCategoryDto_AllProperties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var dto = new CreatePropertyTypeCategoryDto
        {
            PropertyTypeCategory = "Mixed Use",
            CreatedBy = 5
        };

        // Assert
        Assert.Equal("Mixed Use", dto.PropertyTypeCategory);
        Assert.Equal(5, dto.CreatedBy);
    }
}

#endregion

#region UpdatePropertyTypeCategoryDto Tests

public class UpdatePropertyTypeCategoryDtoTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void UpdatePropertyTypeCategoryDto_ValidData_PassesValidation()
    {
        // Arrange
        var dto = new UpdatePropertyTypeCategoryDto
        {
            PropertyTypeCategory = "Agricultural",
            UpdatedBy = 1,
            IsActive = true
        };

        // Act
        var results = Validate(dto);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void UpdatePropertyTypeCategoryDto_PropertyTypeCategoryRequired_FailsValidation()
    {
        // Arrange
        var dto = new UpdatePropertyTypeCategoryDto
        {
            PropertyTypeCategory = null
        };

        // Act
        var results = Validate(dto);

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public void UpdatePropertyTypeCategoryDto_PropertyTypeCategoryMaxLength_FailsValidation()
    {
        // Arrange
        var dto = new UpdatePropertyTypeCategoryDto
        {
            PropertyTypeCategory = new string('Y', 101)
        };

        // Act
        var results = Validate(dto);

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public void UpdatePropertyTypeCategoryDto_PropertyTypeCategory_TrimmedCorrectly()
    {
        // Arrange
        var dto = new UpdatePropertyTypeCategoryDto
        {
            PropertyTypeCategory = "  Commercial  "
        };

        // Act
        var result = dto.PropertyTypeCategory;

        // Assert
        Assert.Equal("Commercial", result);
    }

    [Fact]
    public void UpdatePropertyTypeCategoryDto_InheritsFromUpdateBaseDtos()
    {
        // Arrange & Act
        var dto = new UpdatePropertyTypeCategoryDto();

        // Assert
        Assert.IsAssignableFrom<UpdateBaseDtos>(dto);
    }

    [Fact]
    public void UpdatePropertyTypeCategoryDto_AllProperties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var dto = new UpdatePropertyTypeCategoryDto
        {
            PropertyTypeCategory = "Special Purpose",
            UpdatedBy = 10,
            IsActive = false
        };

        // Assert
        Assert.Equal("Special Purpose", dto.PropertyTypeCategory);
        Assert.Equal(10, dto.UpdatedBy);
        Assert.False(dto.IsActive);
    }
}

#endregion

#region PropertyTypeCategoryQueryParameters Tests

public class PropertyTypeCategoryQueryParametersTests
{
    [Fact]
    public void PropertyTypeCategoryQueryParameters_PropertyTypeCategory_GetSet_WorksCorrectly()
    {
        // Arrange
        var queryParams = new PropertyTypeCategoryQueryParameters();

        // Act
        queryParams.PropertyTypeCategory = "Residential";
        var result = queryParams.PropertyTypeCategory;

        // Assert
        Assert.Equal("Residential", result);
    }

    [Fact]
    public void PropertyTypeCategoryQueryParameters_PropertyTypeCategory_DefaultValue_IsNull()
    {
        // Arrange & Act
        var queryParams = new PropertyTypeCategoryQueryParameters();
        var result = queryParams.PropertyTypeCategory;

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PropertyTypeCategoryQueryParameters_PropertyTypeCategory_HasFilterableSearchableSortableAttributes()
    {
        // Arrange
        var propertyInfo = typeof(PropertyTypeCategoryQueryParameters).GetProperty(nameof(PropertyTypeCategoryQueryParameters.PropertyTypeCategory));

        // Act
        var filterableAttr = propertyInfo?.GetCustomAttribute<FilterableAttribute>();
        var searchableAttr = propertyInfo?.GetCustomAttribute<SearchableAttribute>();
        var sortableAttr = propertyInfo?.GetCustomAttribute<SortableAttribute>();

        // Assert
        Assert.NotNull(filterableAttr);
        Assert.NotNull(searchableAttr);
        Assert.NotNull(sortableAttr);
    }

    [Fact]
    public void PropertyTypeCategoryQueryParameters_InheritsFromBaseQueryParameters()
    {
        // Arrange & Act
        var queryParams = new PropertyTypeCategoryQueryParameters();

        // Assert
        Assert.IsAssignableFrom<NtisPlatform.Application.DTOs.Queries.BaseQueryParameters>(queryParams);
    }

    [Fact]
    public void PropertyTypeCategoryQueryParameters_CanSetBaseProperties()
    {
        // Arrange
        var queryParams = new PropertyTypeCategoryQueryParameters();

        // Act
        queryParams.PageNumber = 2;
        queryParams.PageSize = 25;
        queryParams.SearchTerm = "test";
        queryParams.SortBy = "PropertyTypeCategory";
        queryParams.SortOrder = "desc";

        // Assert
        Assert.Equal(2, queryParams.PageNumber);
        Assert.Equal(25, queryParams.PageSize);
        Assert.Equal("test", queryParams.SearchTerm);
        Assert.Equal("PropertyTypeCategory", queryParams.SortBy);
        Assert.Equal("desc", queryParams.SortOrder);
    }

    [Fact]
    public void PropertyTypeCategoryQueryParameters_AllProperties_SetTogether_RetainValues()
    {
        // Arrange & Act
        var queryParams = new PropertyTypeCategoryQueryParameters
        {
            PropertyTypeCategory = "Industrial",
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "search"
        };

        // Assert
        Assert.Equal("Industrial", queryParams.PropertyTypeCategory);
        Assert.Equal(1, queryParams.PageNumber);
        Assert.Equal(10, queryParams.PageSize);
        Assert.Equal("search", queryParams.SearchTerm);
    }
}

#endregion

#region PropertyTypeCategoryService Tests

public class PropertyTypeCategoryServiceTests
{
    private readonly Mock<IRepository<PropertyTypeCategoryEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PropertyTypeCategoryService _service;

    public PropertyTypeCategoryServiceTests()
    {
        _mockRepository = new Mock<IRepository<PropertyTypeCategoryEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new PropertyTypeCategoryService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new PropertyTypeCategoryEntity
        {
            Id = 1,
            PropertyTypeCategory = "Test Category",
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<PropertyTypeCategoryDto>(It.IsAny<PropertyTypeCategoryEntity>()))
            .Returns(new PropertyTypeCategoryDto
            {
                Id = 1,
                PropertyTypeCategory = "Test Category"
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Category", result.PropertyTypeCategory);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyTypeCategoryEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<PropertyTypeCategoryEntity>
        {
            new() { Id = 1, PropertyTypeCategory = "Category1", IsActive = true },
            new() { Id = 2, PropertyTypeCategory = "Category2", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyTypeCategoryEntity, PropertyTypeCategoryDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new PropertyTypeCategoryService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var qp = new PropertyTypeCategoryQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, x => x.PropertyTypeCategory == "Category1");
        Assert.Contains(result.Items, x => x.PropertyTypeCategory == "Category2");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreatePropertyTypeCategoryDto
        {
            PropertyTypeCategory = "New Category"
        };

        _mockMapper
            .Setup(m => m.Map<PropertyTypeCategoryEntity>(It.IsAny<CreatePropertyTypeCategoryDto>()))
            .Returns((CreatePropertyTypeCategoryDto dto) => new PropertyTypeCategoryEntity
            {
                PropertyTypeCategory = dto.PropertyTypeCategory,
                CreatedBy = dto.CreatedBy,
                CreatedDate = DateTime.Now,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<PropertyTypeCategoryEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyTypeCategoryEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<PropertyTypeCategoryDto>(It.IsAny<PropertyTypeCategoryEntity>()))
            .Returns((PropertyTypeCategoryEntity e) => new PropertyTypeCategoryDto
            {
                Id = e.Id,
                PropertyTypeCategory = e.PropertyTypeCategory
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("New Category", result.PropertyTypeCategory);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<PropertyTypeCategoryEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdatePropertyTypeCategoryDto
        {
            PropertyTypeCategory = "Updated Category"
        };

        var existingEntity = new PropertyTypeCategoryEntity
        {
            Id = 1,
            PropertyTypeCategory = "Old Category",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<PropertyTypeCategoryEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdatePropertyTypeCategoryDto>(), It.IsAny<PropertyTypeCategoryEntity>()))
            .Callback((UpdatePropertyTypeCategoryDto src, PropertyTypeCategoryEntity dest) =>
            {
                dest.PropertyTypeCategory = src.PropertyTypeCategory;
            });

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertyTypeCategoryEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal("Updated Category", existingEntity.PropertyTypeCategory);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
    {
        // Arrange
        var updateDto = new UpdatePropertyTypeCategoryDto
        {
            PropertyTypeCategory = "Test"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyTypeCategoryEntity?)null);

        // Act
        await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertyTypeCategoryEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new PropertyTypeCategoryEntity
        {
            Id = idToDelete,
            PropertyTypeCategory = "Category to Delete",
            IsActive = true
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
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyTypeCategoryEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

#endregion
