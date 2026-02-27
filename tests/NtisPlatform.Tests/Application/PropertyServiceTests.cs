using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NtisPlatform.Tests.Application;

#region PropertyEntity Tests

public class PropertyEntityTests
{
    [Fact]
    public void PropertyEntity_Properties_GetSet_WorksCorrectly()
    {
        var entity = new PropertyEntity
        {
            OwnerID = 1001,
            WardNo = "01",
            PropertyNo = "100",
            PartitionNo = "A",
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now,
            UpdatedBy = 2,
            UpdatedDate = DateTime.Now
        };

        Assert.Equal(1001, entity.OwnerID);
        Assert.Equal("01", entity.WardNo);
        Assert.Equal("100", entity.PropertyNo);
        Assert.Equal("A", entity.PartitionNo);
        Assert.True(entity.IsActive);
        Assert.Equal(1, entity.CreatedBy);
        Assert.NotNull(entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.NotNull(entity.UpdatedDate);
    }

    [Fact]
    public void PropertyEntity_PartitionNo_CanBeNull()
    {
        var entity = new PropertyEntity
        {
            OwnerID = 1001,
            WardNo = "01",
            PropertyNo = "100",
            PartitionNo = null
        };

        Assert.Null(entity.PartitionNo);
    }

    [Fact]
    public void PropertyEntity_InheritsFromCommonBaseEntity()
    {
        var entity = new PropertyEntity();
        Assert.IsAssignableFrom<CommonBaseEntity>(entity);
    }
}

#endregion

#region PropertyDto Tests

public class PropertyDtoTests
{
    [Fact]
    public void PropertyDto_Properties_GetSet_WorksCorrectly()
    {
        var dto = new PropertyDto
        {
            OwnerID = 1001,
            WardNo = "01",
            PropertyNo = "100",
            PartitionNo = "A",
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };

        Assert.Equal(1001, dto.OwnerID);
        Assert.Equal("01", dto.WardNo);
        Assert.Equal("100", dto.PropertyNo);
        Assert.Equal("A", dto.PartitionNo);
        Assert.Equal("100-A", dto.DisplayProperty);
        Assert.True(dto.IsActive);
        Assert.NotNull(dto.CreatedDate);
        Assert.NotNull(dto.UpdatedDate);
    }

    [Fact]
    public void PropertyDto_InheritsFromCommonBaseDtos()
    {
        var dto = new PropertyDto();
        Assert.IsAssignableFrom<CommonBaseDtos>(dto);
    }

    [Fact]
    public void PropertyDto_PartitionNo_CanBeNull()
    {
        var dto = new PropertyDto
        {
            OwnerID = 1001,
            WardNo = "01",
            PropertyNo = "100",
            PartitionNo = null
        };

        Assert.Null(dto.PartitionNo);
    }
    [Fact]
    public void PropertyDto_DisplayProperty_WhenPropertyNoIsNull_ReturnsHyphenPartition()
    {
        var dto = new PropertyDto
        {
            PropertyNo = null,
            PartitionNo = "A"
        };

        Assert.Equal("-A", dto.DisplayProperty);
    }
}

#endregion

#region CreatePropertyDto Tests

public class CreatePropertyDtoTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void CreatePropertyDto_ValidData_PassesValidation()
    {
        var dto = new CreatePropertyDto
        {
            WardNo = "01",
            PropertyNo = "100",
            PartitionNo = "A",
            CreatedBy = 1
        };

        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void CreatePropertyDto_WardNoRequired_FailsValidation()
    {
        var dto = new CreatePropertyDto
        {
            WardNo = "",
            PropertyNo = "100"
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "Property_WardNo_Required");
    }

    [Fact]
    public void CreatePropertyDto_WardNoMaxLength_FailsValidation()
    {
        var dto = new CreatePropertyDto
        {
            WardNo = new string('X', 11),
            PropertyNo = "100"
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "Property_WardNo_MaxLen_10");
    }

    [Fact]
    public void CreatePropertyDto_PropertyNoRequired_FailsValidation()
    {
        var dto = new CreatePropertyDto
        {
            WardNo = "01",
            PropertyNo = ""
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "Property_PropertyNo_Required");
    }

    [Fact]
    public void CreatePropertyDto_PropertyNoMaxLength_FailsValidation()
    {
        var dto = new CreatePropertyDto
        {
            WardNo = "01",
            PropertyNo = new string('Y', 11)
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "Property_PropertyNo_MaxLen_10");
    }

    [Fact]
    public void CreatePropertyDto_PartitionNoMaxLength_FailsValidation()
    {
        var dto = new CreatePropertyDto
        {
            WardNo = "01",
            PropertyNo = "100",
            PartitionNo = new string('Z', 11)
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "Property_PartitionNo_MaxLen_10");
    }

    [Fact]
    public void CreatePropertyDto_PartitionNoCanBeNull_PassesValidation()
    {
        var dto = new CreatePropertyDto
        {
            WardNo = "01",
            PropertyNo = "100",
            PartitionNo = null
        };

        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void CreatePropertyDto_InheritsFromCreateCommonBaseDtos()
    {
        var dto = new CreatePropertyDto();
        Assert.IsAssignableFrom<CreateCommonBaseDtos>(dto);
    }

    [Fact]
    public void CreatePropertyDto_AllProperties_GetSet_WorksCorrectly()
    {
        var dto = new CreatePropertyDto
        {
            WardNo = "01",
            PropertyNo = "100",
            PartitionNo = "A",
            CreatedBy = 1
        };

        Assert.Equal("01", dto.WardNo);
        Assert.Equal("100", dto.PropertyNo);
        Assert.Equal("A", dto.PartitionNo);
        Assert.Equal(1, dto.CreatedBy);
    }
}

#endregion

#region UpdatePropertyDto Tests

public class UpdatePropertyDtoTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void UpdatePropertyDto_ValidData_PassesValidation()
    {
        var dto = new UpdatePropertyDto
        {
            WardNo = "01",
            PropertyNo = "100",
            PartitionNo = "A",
            UpdatedBy = 1,
            IsActive = true
        };

        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdatePropertyDto_WardNoRequired_FailsValidation()
    {
        var dto = new UpdatePropertyDto
        {
            WardNo = "",
            PropertyNo = "100"
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void UpdatePropertyDto_WardNoMaxLength_FailsValidation()
    {
        var dto = new UpdatePropertyDto
        {
            WardNo = new string('X', 11),
            PropertyNo = "100"
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void UpdatePropertyDto_PropertyNoRequired_FailsValidation()
    {
        var dto = new UpdatePropertyDto
        {
            WardNo = "01",
            PropertyNo = ""
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void UpdatePropertyDto_PropertyNoMaxLength_FailsValidation()
    {
        var dto = new UpdatePropertyDto
        {
            WardNo = "01",
            PropertyNo = new string('Y', 11)
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void UpdatePropertyDto_Touches_Nullables_And_Validates_Success()
    {
        var dto = new UpdatePropertyDto();
        dto.WardNo = "01";
        dto.PropertyNo = "100";
        dto.PartitionNo = "A";
        dto.UpdatedBy = 123;
        dto.IsActive = true;

        var partitionNo = dto.PartitionNo;
        var updatedBy = dto.UpdatedBy;

        Assert.Equal("01", dto.WardNo);
        Assert.Equal("100", dto.PropertyNo);
        Assert.Equal("A", partitionNo);
        Assert.Equal(123, updatedBy);
        Assert.True(dto.IsActive);

        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdatePropertyDto_Touches_Null_Assignments_And_Validates_Fail()
    {
        var dto = new UpdatePropertyDto();
        dto.WardNo = "";
        dto.PropertyNo = "";
        dto.PartitionNo = null;
        dto.UpdatedBy = null;

        _ = dto.PartitionNo;
        _ = dto.UpdatedBy;
        var results = Validate(dto);

        Assert.NotEmpty(results);
    }

    [Fact]
    public void UpdatePropertyDto_DefaultConstructor_Executes_DefaultInitializers()
    {
        var dto = new UpdatePropertyDto();

        Assert.False(dto.IsActive);
        Assert.Null(dto.PartitionNo);
        Assert.Null(dto.UpdatedBy);
        _ = dto.WardNo;
        _ = dto.PropertyNo;
    }

    [Fact]
    public void UpdatePropertyDto_InheritsFromUpdateCommonBaseDtos()
    {
        var dto = new UpdatePropertyDto();
        Assert.IsAssignableFrom<UpdateCommonBaseDtos>(dto);
    }
}

#endregion

#region PropertyQueryParameters Tests

public class PropertyQueryParametersTests
{
    #region Property Getter/Setter Coverage Tests

    [Fact]
    public void AllPropertyGetters_ExplicitlyInvoked_ReturnExpectedValues()
    {
        var queryParams = new PropertyQueryParameters();

        queryParams.PropertyNo = "100";
        queryParams.PartitionNo = "A";

        string? propertyNo = queryParams.PropertyNo;
        string? partitionNo = queryParams.PartitionNo;

        Assert.Equal("100", propertyNo);
        Assert.Equal("A", partitionNo);
    }

    [Fact]
    public void AllPropertyGetters_WithNullValues_ReturnNull()
    {
        var queryParams = new PropertyQueryParameters();

        string? propertyNo = queryParams.PropertyNo;
        string? partitionNo = queryParams.PartitionNo;

        Assert.Null(propertyNo);
        Assert.Null(partitionNo);
    }

    [Fact]
    public void PropertyGetters_CalledMultipleTimes_ReturnConsistentValues()
    {
        var queryParams = new PropertyQueryParameters
        {
            PropertyNo = "100",
            PartitionNo = "A"
        };

        var result1 = queryParams.PropertyNo;
        var result2 = queryParams.PropertyNo;
        var result3 = queryParams.PartitionNo;
        var result4 = queryParams.PartitionNo;

        Assert.Equal(result1, result2);
        Assert.Equal(result3, result4);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void PropertyNo_GetSet_WorksCorrectly()
    {
        var queryParams = new PropertyQueryParameters();
        queryParams.PropertyNo = "100";
        var result = queryParams.PropertyNo;

        Assert.Equal("100", result);
    }

    [Fact]
    public void PropertyNo_DefaultValue_IsNull()
    {
        var queryParams = new PropertyQueryParameters();
        var result = queryParams.PropertyNo;

        Assert.Null(result);
    }

    [Fact]
    public void PartitionNo_GetSet_WorksCorrectly()
    {
        var queryParams = new PropertyQueryParameters();
        queryParams.PartitionNo = "A";
        var result = queryParams.PartitionNo;

        Assert.Equal("A", result);
    }

    [Fact]
    public void PartitionNo_DefaultValue_IsNull()
    {
        var queryParams = new PropertyQueryParameters();
        var result = queryParams.PartitionNo;

        Assert.Null(result);
    }

    #endregion

    #region Attribute Tests

    [Fact]
    public void PropertyNo_HasFilterableSearchableSortableAttributes()
    {
        var propertyInfo = typeof(PropertyQueryParameters).GetProperty(nameof(PropertyQueryParameters.PropertyNo));

        var filterableAttr = propertyInfo?.GetCustomAttribute<FilterableAttribute>();
        var searchableAttr = propertyInfo?.GetCustomAttribute<SearchableAttribute>();
        var sortableAttr = propertyInfo?.GetCustomAttribute<SortableAttribute>();

        Assert.NotNull(filterableAttr);
        Assert.NotNull(searchableAttr);
        Assert.NotNull(sortableAttr);
    }

    [Fact]
    public void PartitionNo_HasFilterableSearchableSortableAttributes()
    {
        var propertyInfo = typeof(PropertyQueryParameters).GetProperty(nameof(PropertyQueryParameters.PartitionNo));

        var filterableAttr = propertyInfo?.GetCustomAttribute<FilterableAttribute>();
        var searchableAttr = propertyInfo?.GetCustomAttribute<SearchableAttribute>();
        var sortableAttr = propertyInfo?.GetCustomAttribute<SortableAttribute>();

        Assert.NotNull(filterableAttr);
        Assert.NotNull(searchableAttr);
        Assert.NotNull(sortableAttr);
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void PropertyQueryParameters_InheritsFromBaseQueryParameters()
    {
        var queryParams = new PropertyQueryParameters();
        Assert.IsAssignableFrom<NtisPlatform.Application.DTOs.Queries.BaseQueryParameters>(queryParams);
    }

    [Fact]
    public void PropertyQueryParameters_CanSetBaseProperties()
    {
        var queryParams = new PropertyQueryParameters();

        queryParams.PageNumber = 2;
        queryParams.PageSize = 25;
        queryParams.SearchTerm = "test";
        queryParams.SortBy = "PropertyNo";
        queryParams.SortOrder = "desc";

        Assert.Equal(2, queryParams.PageNumber);
        Assert.Equal(25, queryParams.PageSize);
        Assert.Equal("test", queryParams.SearchTerm);
        Assert.Equal("PropertyNo", queryParams.SortBy);
        Assert.Equal("desc", queryParams.SortOrder);
    }

    #endregion

    #region Combined Property Tests

    [Fact]
    public void AllProperties_SetTogether_RetainValues()
    {
        var queryParams = new PropertyQueryParameters
        {
            PropertyNo = "100",
            PartitionNo = "A",
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "search"
        };

        var pn = queryParams.PropertyNo;
        var pt = queryParams.PartitionNo;

        Assert.Equal("100", pn);
        Assert.Equal("A", pt);
        Assert.Equal(1, queryParams.PageNumber);
        Assert.Equal(10, queryParams.PageSize);
        Assert.Equal("search", queryParams.SearchTerm);
    }

    #endregion
}

#endregion

#region PropertyService Tests

public class PropertyServiceTests
{
    private readonly Mock<IRepository<PropertyEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PropertyService _service;

    public PropertyServiceTests()
    {
        _mockRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new PropertyService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new PropertyEntity
        {
            OwnerID = 1001,
            WardNo = "01",
            PropertyNo = "100",
            PartitionNo = "A",
            CreatedDate = DateTime.Now,
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1001, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var dto = new PropertyDto
        {
            OwnerID = 1001,
            WardNo = "01",
            PropertyNo = "100",
            PartitionNo = "A"
        };

        _mockMapper.Setup(m => m.Map<PropertyDto>(It.IsAny<PropertyEntity>()))
            .Returns(dto);

        var result = await _service.GetByIdAsync(1001);

        Assert.NotNull(result);
        Assert.Equal(1001, result.OwnerID);
        Assert.Equal("01", result.WardNo);
        Assert.Equal("100", result.PropertyNo);
        Assert.Equal("A", result.PartitionNo);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyEntity?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        var entities = new List<PropertyEntity>
        {
            new() { OwnerID = 1, WardNo = "01", PropertyNo = "100", PartitionNo = "A", IsActive = true },
            new() { OwnerID = 2, WardNo = "01", PropertyNo = "101", PartitionNo = null, IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        // Simple one-way mapping - DisplayProperty is computed automatically
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyEntity, PropertyDto>();
        });
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new PropertyService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var qp = new PropertyQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        var result = await service.GetAllAsync(qp, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, x => x.OwnerID == 1);
        Assert.Contains(result.Items, x => x.OwnerID == 2);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var createDto = new CreatePropertyDto
        {
            WardNo = "01",
            PropertyNo = "100",
            PartitionNo = "A"
        };

        _mockMapper
            .Setup(m => m.Map<PropertyEntity>(It.IsAny<CreatePropertyDto>()))
            .Returns((CreatePropertyDto dto) => new PropertyEntity
            {
                WardNo = dto.WardNo,
                PropertyNo = dto.PropertyNo,
                PartitionNo = dto.PartitionNo,
                CreatedBy = dto.CreatedBy,
                CreatedDate = DateTime.Now,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyEntity e, CancellationToken _) =>
            {
                e.OwnerID = 1001;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<PropertyDto>(It.IsAny<PropertyEntity>()))
            .Returns((PropertyEntity e) => new PropertyDto
            {
                OwnerID = e.OwnerID,
                WardNo = e.WardNo,
                PropertyNo = e.PropertyNo,
                PartitionNo = e.PartitionNo
            });

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1001, result.OwnerID);
        Assert.Equal("01", result.WardNo);
        Assert.Equal("100", result.PropertyNo);
        Assert.Equal("A", result.PartitionNo);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var updateDto = new UpdatePropertyDto
        {
            WardNo = "01",
            PropertyNo = "100",
            PartitionNo = "B"
        };

        var existingEntity = new PropertyEntity
        {
            OwnerID = 1001,
            WardNo = "01",
            PropertyNo = "100",
            PartitionNo = "A",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1001, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdatePropertyDto>(), It.IsAny<PropertyEntity>()))
            .Callback((UpdatePropertyDto src, PropertyEntity dest) =>
            {
                dest.PartitionNo = src.PartitionNo;
            });

        await _service.UpdateAsync(1001, updateDto, CancellationToken.None);

        _mockRepository.Verify(r => r.GetByIdAsync(1001, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal("B", existingEntity.PartitionNo);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
    {
        var updateDto = new UpdatePropertyDto
        {
            WardNo = "01",
            PropertyNo = "100"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyEntity?)null);

        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyEntity?)null);

        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        Assert.False(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        var idToDelete = 1001;

        var existingEntity = new PropertyEntity
        {
            OwnerID = idToDelete,
            WardNo = "01",
            PropertyNo = "100",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(idToDelete, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        Assert.True(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion
