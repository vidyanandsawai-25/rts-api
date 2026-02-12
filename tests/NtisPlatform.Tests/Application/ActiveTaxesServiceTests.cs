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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NtisPlatform.Tests.Application;

#region Test Helpers

/// <summary>
/// Shared test utility class for validation testing across DTOs
/// </summary>
internal static class ValidationTestHelper
{
    /// <summary>
    /// Validates a model using DataAnnotations and returns validation results
    /// </summary>
    /// <param name="model">The model to validate</param>
    /// <returns>List of validation results</returns>
    public static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }
}

#endregion

#region ActiveTaxesEntity Tests

public class ActiveTaxesEntityTests
{
    [Fact]
    public void ActiveTaxesEntity_Properties_GetSet_WorksCorrectly()
    {
        var entity = new ActiveTaxesEntity
        {
            TaxNameID = 1,
            TaxName = "GeneralTax",
            TaxNameAlias = "General Tax",
            TaxNameOrder = 1,
            ActiveTaxHeadsOnly = true,
            DisplayOrder = 1,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now,
            UpdatedBy = 2,
            UpdatedDate = DateTime.Now
        };

        Assert.Equal(1, entity.TaxNameID);
        Assert.Equal("GeneralTax", entity.TaxName);
        Assert.Equal("General Tax", entity.TaxNameAlias);
        Assert.Equal(1, entity.TaxNameOrder);
        Assert.True(entity.ActiveTaxHeadsOnly);
        Assert.Equal(1, entity.DisplayOrder);
        Assert.True(entity.IsActive);
        Assert.Equal(1, entity.CreatedBy);
        Assert.NotNull(entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.NotNull(entity.UpdatedDate);
    }

    [Fact]
    public void ActiveTaxesEntity_TaxNameAlias_CanBeNull()
    {
        var entity = new ActiveTaxesEntity
        {
            TaxNameID = 1,
            TaxName = "GeneralTax",
            TaxNameAlias = null
        };

        Assert.Null(entity.TaxNameAlias);
    }

    [Fact]
    public void ActiveTaxesEntity_TaxName_CanBeNull()
    {
        var entity = new ActiveTaxesEntity
        {
            TaxNameID = 1,
            TaxName = null
        };

        Assert.Null(entity.TaxName);
    }

    [Fact]
    public void ActiveTaxesEntity_ActiveTaxHeadsOnly_GetSet_WorksCorrectly()
    {
        var entity = new ActiveTaxesEntity
        {
            TaxNameID = 1,
            TaxName = "GeneralTax",
            ActiveTaxHeadsOnly = false
        };

        Assert.False(entity.ActiveTaxHeadsOnly);

        entity.ActiveTaxHeadsOnly = true;
        Assert.True(entity.ActiveTaxHeadsOnly);

        entity.ActiveTaxHeadsOnly = null;
        Assert.Null(entity.ActiveTaxHeadsOnly);
    }

    [Fact]
    public void ActiveTaxesEntity_TaxNameOrderAndDisplayOrder_GetSet_WithNullValues()
    {
        var entity = new ActiveTaxesEntity
        {
            TaxNameID = 1,
            TaxName = "GeneralTax",
            TaxNameOrder = null,
            DisplayOrder = null
        };

        Assert.Null(entity.TaxNameOrder);
        Assert.Null(entity.DisplayOrder);
    }

    [Fact]
    public void ActiveTaxesEntity_InheritsFromCommonBaseEntity()
    {
        var entity = new ActiveTaxesEntity();
        Assert.IsAssignableFrom<CommonBaseEntity>(entity);
    }
}

#endregion

#region ActiveTaxesDto Tests

public class ActiveTaxesDtoTests
{
    [Fact]
    public void ActiveTaxesDto_Properties_GetSet_WorksCorrectly()
    {
        var dto = new ActiveTaxesDto
        {
            TaxNameID = 1,
            TaxName = "GeneralTax",
            TaxNameAlias = "General Tax",
            TaxNameOrder = 1,
            ActiveTaxHeadsOnly = true,
            DisplayOrder = 1,
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };

        Assert.Equal(1, dto.TaxNameID);
        Assert.Equal("GeneralTax", dto.TaxName);
        Assert.Equal("General Tax", dto.TaxNameAlias);
        Assert.Equal(1, dto.TaxNameOrder);
        Assert.True(dto.ActiveTaxHeadsOnly);
        Assert.Equal(1, dto.DisplayOrder);
        Assert.True(dto.IsActive);
        Assert.NotNull(dto.CreatedDate);
        Assert.NotNull(dto.UpdatedDate);
    }

    [Fact]
    public void ActiveTaxesDto_InheritsFromCommonBaseDtos()
    {
        var dto = new ActiveTaxesDto();
        Assert.IsAssignableFrom<CommonBaseDtos>(dto);
    }

    [Fact]
    public void ActiveTaxesDto_TaxNameAlias_CanBeNull()
    {
        var dto = new ActiveTaxesDto
        {
            TaxNameID = 1,
            TaxName = "GeneralTax",
            TaxNameAlias = null
        };

        Assert.Null(dto.TaxNameAlias);
    }

    [Fact]
    public void ActiveTaxesDto_ActiveTaxHeadsOnly_GetSet_WorksCorrectly()
    {
        var dto = new ActiveTaxesDto
        {
            TaxNameID = 1,
            TaxName = "GeneralTax",
            ActiveTaxHeadsOnly = false
        };

        Assert.False(dto.ActiveTaxHeadsOnly);

        dto.ActiveTaxHeadsOnly = true;
        Assert.True(dto.ActiveTaxHeadsOnly);

        dto.ActiveTaxHeadsOnly = null;
        Assert.Null(dto.ActiveTaxHeadsOnly);
    }

    [Fact]
    public void ActiveTaxesDto_TaxNameOrderAndDisplayOrder_GetSet_WithNullValues()
    {
        var dto = new ActiveTaxesDto
        {
            TaxNameID = 1,
            TaxName = "GeneralTax",
            TaxNameOrder = null,
            DisplayOrder = null
        };

        Assert.Null(dto.TaxNameOrder);
        Assert.Null(dto.DisplayOrder);
    }
}

#endregion

#region CreateActiveTaxesDto Tests

public class CreateActiveTaxesDtoTests
{
    [Fact]
    public void CreateActiveTaxesDto_ValidData_PassesValidation()
    {
        var dto = new CreateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            TaxNameAlias = "General Tax",
            TaxNameOrder = 1,
            DisplayOrder = 1,
            CreatedBy = 1
        };

        var results = ValidationTestHelper.Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void CreateActiveTaxesDto_TaxNameRequired_FailsValidation()
    {
        var dto = new CreateActiveTaxesDto
        {
            TaxName = "",
            TaxNameAlias = "Alias"
        };

        var results = ValidationTestHelper.Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "ActiveTaxes_TaxName_Required");
    }

    [Fact]
    public void CreateActiveTaxesDto_TaxNameMaxLength_FailsValidation()
    {
        var dto = new CreateActiveTaxesDto
        {
            TaxName = new string('X', 201),
            TaxNameAlias = "Alias"
        };

        var results = ValidationTestHelper.Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "ActiveTaxes_TaxName_MaxLen_200");
    }

    [Fact]
    public void CreateActiveTaxesDto_TaxNameAliasMaxLength_FailsValidation()
    {
        var dto = new CreateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            TaxNameAlias = new string('Y', 201)
        };

        var results = ValidationTestHelper.Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "ActiveTaxes_TaxNameAlias_MaxLen_200");
    }

    [Fact]
    public void CreateActiveTaxesDto_TaxNameOrderRange_FailsValidation()
    {
        var dto = new CreateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            TaxNameOrder = 0
        };

        var results = ValidationTestHelper.Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "ActiveTaxes_TaxNameOrder_Range");
    }

    [Fact]
    public void CreateActiveTaxesDto_DisplayOrderRange_FailsValidation()
    {
        var dto = new CreateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            DisplayOrder = 0
        };

        var results = ValidationTestHelper.Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "ActiveTaxes_DisplayOrder_Range");
    }

    [Fact]
    public void CreateActiveTaxesDto_TaxNameAliasCanBeNull_PassesValidation()
    {
        var dto = new CreateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            TaxNameAlias = null
        };

        var results = ValidationTestHelper.Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void CreateActiveTaxesDto_InheritsFromCreateCommonBaseDtos()
    {
        var dto = new CreateActiveTaxesDto();
        Assert.IsAssignableFrom<CreateCommonBaseDtos>(dto);
    }

    [Fact]
    public void CreateActiveTaxesDto_AllProperties_GetSet_WorksCorrectly()
    {
        var dto = new CreateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            TaxNameAlias = "General Tax",
            TaxNameOrder = 1,
            ActiveTaxHeadsOnly = true,
            DisplayOrder = 1,
            CreatedBy = 1
        };

        Assert.Equal("GeneralTax", dto.TaxName);
        Assert.Equal("General Tax", dto.TaxNameAlias);
        Assert.Equal(1, dto.TaxNameOrder);
        Assert.True(dto.ActiveTaxHeadsOnly);
        Assert.Equal(1, dto.DisplayOrder);
        Assert.Equal(1, dto.CreatedBy);
    }

    [Fact]
    public void CreateActiveTaxesDto_ActiveTaxHeadsOnly_GetSet_WorksCorrectly()
    {
        var dto = new CreateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            ActiveTaxHeadsOnly = false
        };

        Assert.False(dto.ActiveTaxHeadsOnly);

        dto.ActiveTaxHeadsOnly = true;
        Assert.True(dto.ActiveTaxHeadsOnly);

        dto.ActiveTaxHeadsOnly = null;
        Assert.Null(dto.ActiveTaxHeadsOnly);
    }

    [Fact]
    public void CreateActiveTaxesDto_TaxNameOrderAndDisplayOrder_GetSet_WithNullValues()
    {
        var dto = new CreateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            TaxNameOrder = null,
            DisplayOrder = null
        };

        Assert.Null(dto.TaxNameOrder);
        Assert.Null(dto.DisplayOrder);
    }
}

#endregion

#region UpdateActiveTaxesDto Tests

public class UpdateActiveTaxesDtoTests
{
    [Fact]
    public void UpdateActiveTaxesDto_ValidData_PassesValidation()
    {
        var dto = new UpdateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            TaxNameAlias = "General Tax Updated",
            TaxNameOrder = 2,
            DisplayOrder = 2,
            UpdatedBy = 1,
            IsActive = true
        };

        var results = ValidationTestHelper.Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateActiveTaxesDto_TaxNameRequired_FailsValidation()
    {
        var dto = new UpdateActiveTaxesDto
        {
            TaxName = "",
            TaxNameAlias = "Alias"
        };

        var results = ValidationTestHelper.Validate(dto);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void UpdateActiveTaxesDto_TaxNameMaxLength_FailsValidation()
    {
        var dto = new UpdateActiveTaxesDto
        {
            TaxName = new string('X', 201),
            TaxNameAlias = "Alias"
        };

        var results = ValidationTestHelper.Validate(dto);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void UpdateActiveTaxesDto_TaxNameAliasMaxLength_FailsValidation()
    {
        var dto = new UpdateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            TaxNameAlias = new string('Y', 201)
        };

        var results = ValidationTestHelper.Validate(dto);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void UpdateActiveTaxesDto_Touches_Nullables_And_Validates_Success()
    {
        var dto = new UpdateActiveTaxesDto();
        dto.TaxName = "GeneralTax";
        dto.TaxNameAlias = "Alias";
        dto.TaxNameOrder = 1;
        dto.UpdatedBy = 123;
        dto.IsActive = true;

        var taxNameAlias = dto.TaxNameAlias;
        var updatedBy = dto.UpdatedBy;

        Assert.Equal("GeneralTax", dto.TaxName);
        Assert.Equal("Alias", taxNameAlias);
        Assert.Equal(123, updatedBy);
        Assert.True(dto.IsActive);

        var results = ValidationTestHelper.Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateActiveTaxesDto_Touches_Null_Assignments_And_Validates_Fail()
    {
        var dto = new UpdateActiveTaxesDto();
        dto.TaxName = "";
        dto.TaxNameAlias = null;
        dto.UpdatedBy = null;

        _ = dto.TaxNameAlias;
        _ = dto.UpdatedBy;
        var results = ValidationTestHelper.Validate(dto);

        Assert.NotEmpty(results);
    }

    [Fact]
    public void UpdateActiveTaxesDto_DefaultConstructor_Executes_DefaultInitializers()
    {
        var dto = new UpdateActiveTaxesDto();

        Assert.False(dto.IsActive);
        Assert.Null(dto.TaxNameAlias);
        Assert.Null(dto.UpdatedBy);
        Assert.Null(dto.TaxNameOrder);
        Assert.Null(dto.ActiveTaxHeadsOnly);
        Assert.Null(dto.DisplayOrder);
        _ = dto.TaxName;
    }

    [Fact]
    public void UpdateActiveTaxesDto_AllProperties_GetSet_WorksCorrectly()
    {
        var dto = new UpdateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            TaxNameAlias = "General Tax Updated",
            TaxNameOrder = 2,
            ActiveTaxHeadsOnly = true,
            DisplayOrder = 2,
            UpdatedBy = 1,
            IsActive = true
        };

        Assert.Equal("GeneralTax", dto.TaxName);
        Assert.Equal("General Tax Updated", dto.TaxNameAlias);
        Assert.Equal(2, dto.TaxNameOrder);
        Assert.True(dto.ActiveTaxHeadsOnly);
        Assert.Equal(2, dto.DisplayOrder);
        Assert.Equal(1, dto.UpdatedBy);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public void UpdateActiveTaxesDto_TaxNameOrderRange_FailsValidation()
    {
        var dto = new UpdateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            TaxNameOrder = 0
        };

        var results = ValidationTestHelper.Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "ActiveTaxes_TaxNameOrder_Range");
    }

    [Fact]
    public void UpdateActiveTaxesDto_DisplayOrderRange_FailsValidation()
    {
        var dto = new UpdateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            DisplayOrder = 0
        };

        var results = ValidationTestHelper.Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "ActiveTaxes_DisplayOrder_Range");
    }

    [Fact]
    public void UpdateActiveTaxesDto_ActiveTaxHeadsOnly_GetSet_WorksCorrectly()
    {
        var dto = new UpdateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            ActiveTaxHeadsOnly = false
        };

        Assert.False(dto.ActiveTaxHeadsOnly);

        dto.ActiveTaxHeadsOnly = true;
        Assert.True(dto.ActiveTaxHeadsOnly);

        dto.ActiveTaxHeadsOnly = null;
        Assert.Null(dto.ActiveTaxHeadsOnly);
    }

    [Fact]
    public void UpdateActiveTaxesDto_TaxNameOrderAndDisplayOrder_GetSet_WithNullValues()
    {
        var dto = new UpdateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            TaxNameOrder = null,
            DisplayOrder = null
        };

        Assert.Null(dto.TaxNameOrder);
        Assert.Null(dto.DisplayOrder);
    }

    [Fact]
    public void UpdateActiveTaxesDto_ValidData_WithAllOptionalFields_PassesValidation()
    {
        var dto = new UpdateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            TaxNameAlias = "General Tax",
            TaxNameOrder = 5,
            ActiveTaxHeadsOnly = false,
            DisplayOrder = 10,
            UpdatedBy = 100,
            IsActive = false
        };

        var results = ValidationTestHelper.Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateActiveTaxesDto_InheritsFromUpdateCommonBaseDtos()
    {
        var dto = new UpdateActiveTaxesDto();
        Assert.IsAssignableFrom<UpdateCommonBaseDtos>(dto);
    }
}

#endregion

#region ActiveTaxesQueryParameters Tests

public class ActiveTaxesQueryParametersTests
{
    #region Property Getter/Setter Coverage Tests

    [Fact]
    public void AllPropertyGetters_ExplicitlyInvoked_ReturnExpectedValues()
    {
        var queryParams = new ActiveTaxesQueryParameters();

        queryParams.TaxName = "GeneralTax";
        queryParams.TaxNameAlias = "General Tax";
        queryParams.TaxNameOrder = 1;
        queryParams.DisplayOrder = 1;

        string? taxName = queryParams.TaxName;
        string? taxNameAlias = queryParams.TaxNameAlias;
        int? taxNameOrder = queryParams.TaxNameOrder;
        int? displayOrder = queryParams.DisplayOrder;

        Assert.Equal("GeneralTax", taxName);
        Assert.Equal("General Tax", taxNameAlias);
        Assert.Equal(1, taxNameOrder);
        Assert.Equal(1, displayOrder);
    }

    [Fact]
    public void AllPropertyGetters_WithNullValues_ReturnNull()
    {
        var queryParams = new ActiveTaxesQueryParameters();

        string? taxName = queryParams.TaxName;
        string? taxNameAlias = queryParams.TaxNameAlias;
        int? taxNameOrder = queryParams.TaxNameOrder;
        int? displayOrder = queryParams.DisplayOrder;

        Assert.Null(taxName);
        Assert.Null(taxNameAlias);
        Assert.Null(taxNameOrder);
        Assert.Null(displayOrder);
    }

    #endregion

    #region Attribute Tests

    [Fact]
    public void TaxName_HasFilterableSearchableSortableAttributes()
    {
        var propertyInfo = typeof(ActiveTaxesQueryParameters).GetProperty(nameof(ActiveTaxesQueryParameters.TaxName));

        var filterableAttr = propertyInfo?.GetCustomAttribute<FilterableAttribute>();
        var searchableAttr = propertyInfo?.GetCustomAttribute<SearchableAttribute>();
        var sortableAttr = propertyInfo?.GetCustomAttribute<SortableAttribute>();

        Assert.NotNull(filterableAttr);
        Assert.NotNull(searchableAttr);
        Assert.NotNull(sortableAttr);
    }

    [Fact]
    public void TaxNameAlias_HasFilterableSearchableSortableAttributes()
    {
        var propertyInfo = typeof(ActiveTaxesQueryParameters).GetProperty(nameof(ActiveTaxesQueryParameters.TaxNameAlias));

        var filterableAttr = propertyInfo?.GetCustomAttribute<FilterableAttribute>();
        var searchableAttr = propertyInfo?.GetCustomAttribute<SearchableAttribute>();
        var sortableAttr = propertyInfo?.GetCustomAttribute<SortableAttribute>();

        Assert.NotNull(filterableAttr);
        Assert.NotNull(searchableAttr);
        Assert.NotNull(sortableAttr);
        Assert.Equal(FilterOperator.Contains, filterableAttr.Operator);
    }

    [Fact]
    public void TaxNameOrder_HasFilterableSortableAttributes()
    {
        var propertyInfo = typeof(ActiveTaxesQueryParameters).GetProperty(nameof(ActiveTaxesQueryParameters.TaxNameOrder));

        var filterableAttr = propertyInfo?.GetCustomAttribute<FilterableAttribute>();
        var sortableAttr = propertyInfo?.GetCustomAttribute<SortableAttribute>();

        Assert.NotNull(filterableAttr);
        Assert.NotNull(sortableAttr);
    }

    [Fact]
    public void DisplayOrder_HasFilterableAttribute()
    {
        var propertyInfo = typeof(ActiveTaxesQueryParameters).GetProperty(nameof(ActiveTaxesQueryParameters.DisplayOrder));

        var filterableAttr = propertyInfo?.GetCustomAttribute<FilterableAttribute>();

        Assert.NotNull(filterableAttr);
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void ActiveTaxesQueryParameters_InheritsFromBaseQueryParameters()
    {
        var queryParams = new ActiveTaxesQueryParameters();
        Assert.IsAssignableFrom<NtisPlatform.Application.DTOs.Queries.BaseQueryParameters>(queryParams);
    }

    [Fact]
    public void ActiveTaxesQueryParameters_CanSetBaseProperties()
    {
        var queryParams = new ActiveTaxesQueryParameters();

        queryParams.PageNumber = 2;
        queryParams.PageSize = 25;
        queryParams.SearchTerm = "test";
        queryParams.SortBy = "TaxName";
        queryParams.SortOrder = "desc";

        Assert.Equal(2, queryParams.PageNumber);
        Assert.Equal(25, queryParams.PageSize);
        Assert.Equal("test", queryParams.SearchTerm);
        Assert.Equal("TaxName", queryParams.SortBy);
        Assert.Equal("desc", queryParams.SortOrder);
    }

    #endregion
}

#endregion

#region IActiveTaxesService Interface Tests

public class IActiveTaxesServiceTests
{
    [Fact]
    public void IActiveTaxesService_InheritsFromICommonCrudService()
    {
        var serviceType = typeof(NtisPlatform.Application.Interfaces.IActiveTaxesService);
        var interfaceType = typeof(NtisPlatform.Application.Interfaces.ICommonCrudService<ActiveTaxesEntity, ActiveTaxesDto, CreateActiveTaxesDto, UpdateActiveTaxesDto, ActiveTaxesQueryParameters, int>);

        Assert.True(interfaceType.IsAssignableFrom(serviceType));
    }

    [Fact]
    public void ActiveTaxesService_ImplementsIActiveTaxesService()
    {
        var serviceType = typeof(NtisPlatform.Application.Services.ActiveTaxesService);
        var interfaceType = typeof(NtisPlatform.Application.Interfaces.IActiveTaxesService);

        Assert.True(interfaceType.IsAssignableFrom(serviceType));
    }

    [Fact]
    public void ActiveTaxesService_Constructor_InitializesCorrectly()
    {
        var mockRepository = new Mock<IRepository<ActiveTaxesEntity, int>>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockMapper = new Mock<IMapper>();

        var service = new ActiveTaxesService(mockRepository.Object, mockUnitOfWork.Object, mockMapper.Object);

        Assert.NotNull(service);
    }
}

#endregion


#region ActiveTaxesService Tests

public class ActiveTaxesServiceTests
{
    private readonly Mock<IRepository<ActiveTaxesEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly ActiveTaxesService _service;

    public ActiveTaxesServiceTests()
    {
        _mockRepository = new Mock<IRepository<ActiveTaxesEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new ActiveTaxesService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new ActiveTaxesEntity
        {
            TaxNameID = 1,
            TaxName = "GeneralTax",
            TaxNameAlias = "General Tax",
            TaxNameOrder = 1,
            DisplayOrder = 1,
            CreatedDate = DateTime.Now,
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<ActiveTaxesDto>(It.IsAny<ActiveTaxesEntity>()))
            .Returns(new ActiveTaxesDto
            {
                TaxNameID = 1,
                TaxName = "GeneralTax",
                TaxNameAlias = "General Tax",
                TaxNameOrder = 1,
                DisplayOrder = 1
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TaxNameID);
        Assert.Equal("GeneralTax", result.TaxName);
        Assert.Equal("General Tax", result.TaxNameAlias);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActiveTaxesEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<ActiveTaxesEntity>
        {
            new() { TaxNameID = 1, TaxName = "GeneralTax", TaxNameAlias = "General", DisplayOrder = 1, IsActive = true },
            new() { TaxNameID = 2, TaxName = "RoadCess", TaxNameAlias = "Road", DisplayOrder = 2, IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<NtisPlatform.Application.Mappings.ActiveTaxesMappingProfile>();
        });
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new ActiveTaxesService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var qp = new ActiveTaxesQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, x => x.TaxNameID == 1);
        Assert.Contains(result.Items, x => x.TaxNameID == 2);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            TaxNameAlias = "General Tax",
            TaxNameOrder = 1,
            DisplayOrder = 1
        };

        _mockMapper
            .Setup(m => m.Map<ActiveTaxesEntity>(It.IsAny<CreateActiveTaxesDto>()))
            .Returns((CreateActiveTaxesDto dto) => new ActiveTaxesEntity
            {
                TaxName = dto.TaxName,
                TaxNameAlias = dto.TaxNameAlias,
                TaxNameOrder = dto.TaxNameOrder,
                DisplayOrder = dto.DisplayOrder,
                CreatedBy = dto.CreatedBy,
                CreatedDate = DateTime.Now,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<ActiveTaxesEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActiveTaxesEntity e, CancellationToken _) =>
            {
                e.TaxNameID = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<ActiveTaxesDto>(It.IsAny<ActiveTaxesEntity>()))
            .Returns((ActiveTaxesEntity e) => new ActiveTaxesDto
            {
                TaxNameID = e.TaxNameID,
                TaxName = e.TaxName,
                TaxNameAlias = e.TaxNameAlias,
                TaxNameOrder = e.TaxNameOrder,
                DisplayOrder = e.DisplayOrder
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TaxNameID);
        Assert.Equal("GeneralTax", result.TaxName);
        Assert.Equal("General Tax", result.TaxNameAlias);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ActiveTaxesEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            TaxNameAlias = "General Tax Updated",
            TaxNameOrder = 2,
            DisplayOrder = 2
        };

        var existingEntity = new ActiveTaxesEntity
        {
            TaxNameID = 1,
            TaxName = "GeneralTax",
            TaxNameAlias = "General Tax",
            TaxNameOrder = 1,
            DisplayOrder = 1,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ActiveTaxesEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateActiveTaxesDto>(), It.IsAny<ActiveTaxesEntity>()))
            .Callback((UpdateActiveTaxesDto src, ActiveTaxesEntity dest) =>
            {
                dest.TaxNameAlias = src.TaxNameAlias;
                dest.TaxNameOrder = src.TaxNameOrder;
                dest.DisplayOrder = src.DisplayOrder;
            });

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ActiveTaxesEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal("General Tax Updated", existingEntity.TaxNameAlias);
        Assert.Equal(2, existingEntity.TaxNameOrder);
        Assert.Equal(2, existingEntity.DisplayOrder);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
    {
        // Arrange
        var updateDto = new UpdateActiveTaxesDto
        {
            TaxName = "GeneralTax",
            TaxNameAlias = "Alias"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActiveTaxesEntity?)null);

        // Act
        await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ActiveTaxesEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActiveTaxesEntity?)null);

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

        var existingEntity = new ActiveTaxesEntity
        {
            TaxNameID = idToDelete,
            TaxName = "GeneralTax",
            TaxNameAlias = "General Tax",
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
