using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NtisPlatform.Tests.Application;

#region TaxZoneEntity Tests

public class TaxZoneEntityTests
{
    [Fact]
    public void TaxZoneEntity_Properties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var entity = new TaxZoneEntity
        {
            Id = 1,
            TaxZoneNo = "TZ001",
            TaxZoneType = "Urban",
            Remark = "Test Zone",
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now,
            UpdatedBy = 2,
            UpdatedDate = DateTime.Now
        };

        // Assert
        Assert.Equal(1, entity.Id);
        Assert.Equal("TZ001", entity.TaxZoneNo);
        Assert.Equal("Urban", entity.TaxZoneType);
        Assert.Equal("Test Zone", entity.Remark);
        Assert.True(entity.IsActive);
        Assert.Equal(1, entity.CreatedBy);
        Assert.NotNull(entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.NotNull(entity.UpdatedDate);
    }

    [Fact]
    public void TaxZoneEntity_TaxZoneType_CanBeNull()
    {
        // Arrange & Act
        var entity = new TaxZoneEntity
        {
            Id = 1,
            TaxZoneNo = "TZ001",
            TaxZoneType = null,
            Remark = "Test"
        };

        // Assert
        Assert.Null(entity.TaxZoneType);
    }

    [Fact]
    public void TaxZoneEntity_InheritsFromBaseEntity()
    {
        // Arrange & Act
        var entity = new TaxZoneEntity();

        // Assert
        Assert.IsAssignableFrom<BaseEntity>(entity);
    }
}

#endregion

#region TaxZoneDto Tests

public class TaxZoneDtoTests
{
    [Fact]
    public void TaxZoneDto_Properties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var dto = new TaxZoneDto
        {
            Id = 1,
            TaxZoneNo = "TZ001",
            TaxZoneType = "Urban",
            Remark = "Test Zone",
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal("TZ001", dto.TaxZoneNo);
        Assert.Equal("Urban", dto.TaxZoneType);
        Assert.Equal("Test Zone", dto.Remark);
        Assert.True(dto.IsActive);
        Assert.NotNull(dto.CreatedDate);
        Assert.NotNull(dto.UpdatedDate);
    }

    [Fact]
    public void TaxZoneDto_InheritsFromBaseDtos()
    {
        // Arrange & Act
        var dto = new TaxZoneDto();

        // Assert
        Assert.IsAssignableFrom<BaseDtos>(dto);
    }

    [Fact]
    public void TaxZoneDto_TaxZoneType_CanBeNull()
    {
        // Arrange & Act
        var dto = new TaxZoneDto
        {
            Id = 1,
            TaxZoneNo = "TZ001",
            TaxZoneType = null,
            Remark = "Test"
        };

        // Assert
        Assert.Null(dto.TaxZoneType);
    }
}

#endregion

#region CreateTaxZoneDto Tests

public class CreateTaxZoneDtoTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void CreateTaxZoneDto_ValidData_PassesValidation()
    {
        // Arrange
        var dto = new CreateTaxZoneDto
        {
            TaxZoneNo = "TZ001",
            TaxZoneType = "Urban",
            Remark = "Test Zone",
            CreatedBy = 1
        };

        // Act
        var results = Validate(dto);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void CreateTaxZoneDto_TaxZoneNoRequired_FailsValidation()
    {
        // Arrange
        var dto = new CreateTaxZoneDto
        {
            TaxZoneNo = "",
            Remark = "Test"
        };

        // Act
        var results = Validate(dto);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "TaxZoneNo_Required");
    }

    [Fact]
    public void CreateTaxZoneDto_TaxZoneNoMaxLength_FailsValidation()
    {
        // Arrange
        var dto = new CreateTaxZoneDto
        {
            TaxZoneNo = new string('X', 11), // 11 characters, max is 10
            Remark = "Test"
        };

        // Act
        var results = Validate(dto);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "TaxZoneNo_MaxLen_10");
    }

    [Fact]
    public void CreateTaxZoneDto_TaxZoneTypeMaxLength_FailsValidation()
    {
        // Arrange
        var dto = new CreateTaxZoneDto
        {
            TaxZoneNo = "TZ001",
            TaxZoneType = new string('Y', 51), // 51 characters, max is 50
            Remark = "Test"
        };

        // Act
        var results = Validate(dto);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "TaxZoneType_MaxLen_50");
    }

    [Fact]
    public void CreateTaxZoneDto_RemarkRequired_FailsValidation()
    {
        // Arrange
        var dto = new CreateTaxZoneDto
        {
            TaxZoneNo = "TZ001",
            Remark = ""
        };

        // Act
        var results = Validate(dto);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "Remark_Required");
    }

    [Fact]
    public void CreateTaxZoneDto_RemarkMaxLength_FailsValidation()
    {
        // Arrange
        var dto = new CreateTaxZoneDto
        {
            TaxZoneNo = "TZ001",
            Remark = new string('Z', 51) // 51 characters, max is 50
        };

        // Act
        var results = Validate(dto);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "Remark_MaxLen_50");
    }

    [Fact]
    public void CreateTaxZoneDto_TaxZoneTypeCanBeNull_PassesValidation()
    {
        // Arrange
        var dto = new CreateTaxZoneDto
        {
            TaxZoneNo = "TZ001",
            TaxZoneType = null,
            Remark = "Test"
        };

        // Act
        var results = Validate(dto);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void CreateTaxZoneDto_InheritsFromCreateBaseDtos()
    {
        // Arrange & Act
        var dto = new CreateTaxZoneDto();

        // Assert
        Assert.IsAssignableFrom<CreateBaseDtos>(dto);
    }

    [Fact]
    public void CreateTaxZoneDto_AllProperties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var dto = new CreateTaxZoneDto
        {
            TaxZoneNo = "TZ001",
            TaxZoneType = "Urban",
            Remark = "Test Zone",
            CreatedBy = 1
        };

        // Assert
        Assert.Equal("TZ001", dto.TaxZoneNo);
        Assert.Equal("Urban", dto.TaxZoneType);
        Assert.Equal("Test Zone", dto.Remark);
        Assert.Equal(1, dto.CreatedBy);
    }
}

#endregion

#region UpdateTaxZoneDto Tests

public class UpdateTaxZoneDtoTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void UpdateTaxZoneDto_ValidData_PassesValidation()
    {
        // Arrange
        var dto = new UpdateTaxZoneDto
        {
            TaxZoneNo = "TZ001",
            TaxZoneType = "Urban",
            Remark = "Updated Zone",
            UpdatedBy = 1,
            IsActive = true
        };

        // Act
        var results = Validate(dto);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateTaxZoneDto_TaxZoneNoRequired_FailsValidation()
    {
        // Arrange
        var dto = new UpdateTaxZoneDto
        {
            TaxZoneNo = "",
            Remark = "Test"
        };

        // Act
        var results = Validate(dto);

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public void UpdateTaxZoneDto_TaxZoneNoMaxLength_FailsValidation()
    {
        // Arrange
        var dto = new UpdateTaxZoneDto
        {
            TaxZoneNo = new string('X', 11),
            Remark = "Test"
        };

        // Act
        var results = Validate(dto);

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public void UpdateTaxZoneDto_TaxZoneTypeMaxLength_FailsValidation()
    {
        // Arrange
        var dto = new UpdateTaxZoneDto
        {
            TaxZoneNo = "TZ001",
            TaxZoneType = new string('Y', 51),
            Remark = "Test"
        };

        // Act
        var results = Validate(dto);

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public void UpdateTaxZoneDto_RemarkMaxLength_FailsValidation()
    {
        // Arrange
        var dto = new UpdateTaxZoneDto
        {
            TaxZoneNo = "TZ001",
            Remark = new string('Z', 51)
        };

        // Act
        var results = Validate(dto);

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public void UpdateTaxZoneDto_Touches_Nullables_And_Validates_Success()
    {
        // Arrange
        var dto = new UpdateTaxZoneDto();
        dto.TaxZoneNo = "TZ001";
        dto.TaxZoneType = "TYPE";
        dto.Remark = "REMARK";
        dto.UpdatedBy = 123;
        dto.IsActive = true;

        // Act
        var taxZoneType = dto.TaxZoneType;
        var updatedBy = dto.UpdatedBy;

        // Assert
        Assert.Equal("TZ001", dto.TaxZoneNo);
        Assert.Equal("TYPE", taxZoneType);
        Assert.Equal("REMARK", dto.Remark);
        Assert.Equal(123, updatedBy);
        Assert.True(dto.IsActive);

        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateTaxZoneDto_Touches_Null_Assignments_And_Validates_Fail()
    {
        // Arrange
        var dto = new UpdateTaxZoneDto();
        dto.TaxZoneNo = "";
        dto.Remark = "";
        dto.TaxZoneType = null;
        dto.UpdatedBy = null;

        // Act
        _ = dto.TaxZoneType;
        _ = dto.UpdatedBy;
        var results = Validate(dto);

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public void UpdateTaxZoneDto_DefaultConstructor_Executes_DefaultInitializers()
    {
        // Arrange & Act
        var dto = new UpdateTaxZoneDto();

        // Assert
        Assert.False(dto.IsActive);
        Assert.Null(dto.TaxZoneType);
        Assert.Null(dto.UpdatedBy);
        _ = dto.TaxZoneNo;
        _ = dto.Remark;
    }

    [Fact]
    public void UpdateTaxZoneDto_InheritsFromUpdateBaseDtos()
    {
        // Arrange & Act
        var dto = new UpdateTaxZoneDto();

        // Assert
        Assert.IsAssignableFrom<UpdateBaseDtos>(dto);
    }
}

#endregion

#region TaxZoneQueryParameters Tests

public class TaxZoneQueryParametersTests
{
    #region Property Getter/Setter Coverage Tests

    [Fact]
    public void AllPropertyGetters_ExplicitlyInvoked_ReturnExpectedValues()
    {
        // Arrange
        var queryParams = new TaxZoneQueryParameters();

        // Set values
        queryParams.TaxZoneNo = "TZ001";
        queryParams.TaxZoneType = "Urban";
        queryParams.Remark = "Test Remark";

        string? taxZoneNo = queryParams.TaxZoneNo;
        string? taxZoneType = queryParams.TaxZoneType;
        string? remark = queryParams.Remark;

        // Assert
        Assert.Equal("TZ001", taxZoneNo);
        Assert.Equal("Urban", taxZoneType);
        Assert.Equal("Test Remark", remark);
    }

    [Fact]
    public void AllPropertyGetters_WithNullValues_ReturnNull()
    {
        // Arrange
        var queryParams = new TaxZoneQueryParameters();

        // Act
        string? taxZoneNo = queryParams.TaxZoneNo;
        string? taxZoneType = queryParams.TaxZoneType;
        string? remark = queryParams.Remark;

        // Assert
        Assert.Null(taxZoneNo);
        Assert.Null(taxZoneType);
        Assert.Null(remark);
    }

    [Fact]
    public void PropertyGetters_CalledMultipleTimes_ReturnConsistentValues()
    {
        // Arrange
        var queryParams = new TaxZoneQueryParameters
        {
            TaxZoneNo = "TZ100",
            TaxZoneType = "Rural",
            Remark = "Consistent"
        };

        // Act
        var result1 = queryParams.TaxZoneNo;
        var result2 = queryParams.TaxZoneNo;
        var result3 = queryParams.TaxZoneType;
        var result4 = queryParams.TaxZoneType;
        var result5 = queryParams.Remark;
        var result6 = queryParams.Remark;

        // Assert
        Assert.Equal(result1, result2);
        Assert.Equal(result3, result4);
        Assert.Equal(result5, result6);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void TaxZoneNo_GetSet_WorksCorrectly()
    {
        // Arrange
        var queryParams = new TaxZoneQueryParameters();

        // Act
        queryParams.TaxZoneNo = "TZ001";
        var result = queryParams.TaxZoneNo;

        // Assert
        Assert.Equal("TZ001", result);
    }

    [Fact]
    public void TaxZoneNo_DefaultValue_IsNull()
    {
        // Arrange & Act
        var queryParams = new TaxZoneQueryParameters();
        var result = queryParams.TaxZoneNo;

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TaxZoneType_GetSet_WorksCorrectly()
    {
        // Arrange
        var queryParams = new TaxZoneQueryParameters();

        // Act
        queryParams.TaxZoneType = "Urban";
        var result = queryParams.TaxZoneType;

        // Assert
        Assert.Equal("Urban", result);
    }

    [Fact]
    public void TaxZoneType_DefaultValue_IsNull()
    {
        // Arrange & Act
        var queryParams = new TaxZoneQueryParameters();
        var result = queryParams.TaxZoneType;

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Remark_GetSet_WorksCorrectly()
    {
        // Arrange
        var queryParams = new TaxZoneQueryParameters();

        // Act
        queryParams.Remark = "Test Remark";
        var result = queryParams.Remark;

        // Assert
        Assert.Equal("Test Remark", result);
    }

    [Fact]
    public void Remark_DefaultValue_IsNull()
    {
        // Arrange & Act
        var queryParams = new TaxZoneQueryParameters();
        var result = queryParams.Remark;

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Attribute Tests

    [Fact]
    public void TaxZoneNo_HasFilterableSearchableSortableAttributes()
    {
        // Arrange
        var propertyInfo = typeof(TaxZoneQueryParameters).GetProperty(nameof(TaxZoneQueryParameters.TaxZoneNo));

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
    public void TaxZoneType_HasFilterableSearchableSortableAttributes()
    {
        // Arrange
        var propertyInfo = typeof(TaxZoneQueryParameters).GetProperty(nameof(TaxZoneQueryParameters.TaxZoneType));

        // Act
        var filterableAttr = propertyInfo?.GetCustomAttribute<FilterableAttribute>();
        var searchableAttr = propertyInfo?.GetCustomAttribute<SearchableAttribute>();
        var sortableAttr = propertyInfo?.GetCustomAttribute<SortableAttribute>();

        // Assert
        Assert.NotNull(filterableAttr);
        Assert.NotNull(searchableAttr);
        Assert.NotNull(sortableAttr);
        Assert.Equal(FilterOperator.Contains, filterableAttr.Operator);
    }

    [Fact]
    public void Remark_HasFilterableSearchableSortableAttributes()
    {
        // Arrange
        var propertyInfo = typeof(TaxZoneQueryParameters).GetProperty(nameof(TaxZoneQueryParameters.Remark));

        // Act
        var filterableAttr = propertyInfo?.GetCustomAttribute<FilterableAttribute>();
        var searchableAttr = propertyInfo?.GetCustomAttribute<SearchableAttribute>();
        var sortableAttr = propertyInfo?.GetCustomAttribute<SortableAttribute>();

        // Assert
        Assert.NotNull(filterableAttr);
        Assert.NotNull(searchableAttr);
        Assert.NotNull(sortableAttr);
        Assert.Equal(FilterOperator.Contains, filterableAttr.Operator);
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void TaxZoneQueryParameters_InheritsFromBaseQueryParameters()
    {
        // Arrange & Act
        var queryParams = new TaxZoneQueryParameters();

        // Assert
        Assert.IsAssignableFrom<NtisPlatform.Application.DTOs.Queries.BaseQueryParameters>(queryParams);
    }

    [Fact]
    public void TaxZoneQueryParameters_CanSetBaseProperties()
    {
        // Arrange
        var queryParams = new TaxZoneQueryParameters();

        // Act
        queryParams.PageNumber = 2;
        queryParams.PageSize = 25;
        queryParams.SearchTerm = "test";
        queryParams.SortBy = "TaxZoneNo";
        queryParams.SortOrder = "desc";

        // Assert
        Assert.Equal(2, queryParams.PageNumber);
        Assert.Equal(25, queryParams.PageSize);
        Assert.Equal("test", queryParams.SearchTerm);
        Assert.Equal("TaxZoneNo", queryParams.SortBy);
        Assert.Equal("desc", queryParams.SortOrder);
    }

    #endregion

    #region Combined Property Tests

    [Fact]
    public void AllProperties_SetTogether_RetainValues()
    {
        // Arrange & Act
        var queryParams = new TaxZoneQueryParameters
        {
            TaxZoneNo = "TZ001",
            TaxZoneType = "Urban",
            Remark = "Test Zone",
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "search"
        };

        // Assert
        var tz = queryParams.TaxZoneNo;
        var tt = queryParams.TaxZoneType;
        var rm = queryParams.Remark;

        Assert.Equal("TZ001", tz);
        Assert.Equal("Urban", tt);
        Assert.Equal("Test Zone", rm);
        Assert.Equal(1, queryParams.PageNumber);
        Assert.Equal(10, queryParams.PageSize);
        Assert.Equal("search", queryParams.SearchTerm);
    }

    #endregion
}

#endregion

#region TaxZoneService Tests

public class TaxZoneServiceTests
{
    private readonly Mock<IRepository<TaxZoneEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly TaxZoneService _service;

    public TaxZoneServiceTests()
    {
        _mockRepository = new Mock<IRepository<TaxZoneEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new TaxZoneService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new TaxZoneEntity
        {
            Id = 1,
            TaxZoneNo = "TZ1",
            TaxZoneType = "Urban",
            Remark = "Zone 1",
            CreatedDate = DateTime.Now,
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<TaxZoneDto>(It.IsAny<TaxZoneEntity>()))
            .Returns(new TaxZoneDto
            {
                Id = 1,
                TaxZoneNo = "TZ1",
                TaxZoneType = "Urban",
                Remark = "Zone 1"
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("TZ1", result.TaxZoneNo);
        Assert.Equal("Urban", result.TaxZoneType);
        Assert.Equal("Zone 1", result.Remark);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxZoneEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<TaxZoneEntity>
        {
            new() { Id = 1, TaxZoneNo = "TZ1", TaxZoneType = "Urban", Remark = "Zone 1", IsActive = true },
            new() { Id = 2, TaxZoneNo = "TZ2", TaxZoneType = "Rural", Remark = "Zone 2", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<TaxZoneEntity, TaxZoneDto>();
        });
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new TaxZoneService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var qp = new TaxZoneQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, x => x.TaxZoneNo == "TZ1");
        Assert.Contains(result.Items, x => x.TaxZoneNo == "TZ2");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateTaxZoneDto
        {
            TaxZoneNo = "TZ1",
            TaxZoneType = "Urban",
            Remark = "Zone 1"
        };

        _mockMapper
            .Setup(m => m.Map<TaxZoneEntity>(It.IsAny<CreateTaxZoneDto>()))
            .Returns((CreateTaxZoneDto dto) => new TaxZoneEntity
            {
                TaxZoneNo = dto.TaxZoneNo,
                TaxZoneType = dto.TaxZoneType,
                Remark = dto.Remark,
                CreatedBy = dto.CreatedBy,
                CreatedDate = DateTime.Now,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<TaxZoneEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxZoneEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<TaxZoneDto>(It.IsAny<TaxZoneEntity>()))
            .Returns((TaxZoneEntity e) => new TaxZoneDto
            {
                Id = e.Id,
                TaxZoneNo = e.TaxZoneNo,
                TaxZoneType = e.TaxZoneType,
                Remark = e.Remark
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("TZ1", result.TaxZoneNo);
        Assert.Equal("Urban", result.TaxZoneType);
        Assert.Equal("Zone 1", result.Remark);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<TaxZoneEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateTaxZoneDto
        {
            TaxZoneNo = "TZ1",
            TaxZoneType = "Urban",
            Remark = "Updated Zone"
        };

        var existingEntity = new TaxZoneEntity
        {
            Id = 1,
            TaxZoneNo = "TZ1",
            TaxZoneType = "Urban",
            Remark = "Old Remark",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<TaxZoneEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateTaxZoneDto>(), It.IsAny<TaxZoneEntity>()))
            .Callback((UpdateTaxZoneDto src, TaxZoneEntity dest) =>
            {
                dest.Remark = src.Remark;
                dest.TaxZoneType = src.TaxZoneType;
            });

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<TaxZoneEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal("Updated Zone", existingEntity.Remark);
        Assert.Equal("Urban", existingEntity.TaxZoneType);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
    {
        // Arrange
        var updateDto = new UpdateTaxZoneDto
        {
            TaxZoneNo = "ZZZ",
            TaxZoneType = "Urban",
            Remark = "Remark"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxZoneEntity?)null);

        // Act
        await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<TaxZoneEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxZoneEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new TaxZoneEntity
        {
            Id = idToDelete,
            TaxZoneNo = "TZ1",
            TaxZoneType = "Urban",
            Remark = "Zone 1",
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
}

#endregion
