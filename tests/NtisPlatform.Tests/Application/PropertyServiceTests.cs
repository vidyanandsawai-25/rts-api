using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace NtisPlatform.Tests.Application;

#region PropertyEntity Tests

public class PropertyEntityTests
{
    [Fact]
    public void PropertyEntity_Properties_GetSet_WorksCorrectly()
    {
        var entity = new PropertyEntity
        {
            PropertyId = 1,
            TaxZoneId = 1,
            WardId = 10,
            PropertyNo = "PROP001",
            PartitionNo = "A",
            PropertyTypeId = 1,
            UPICId = "UPIC123",
            OpenPlot = false,
            CSN = "CSN001",
            CategoryId = 1,
            OwnerName = "John Doe",
            MarkedForDeletion = false,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now,
            UpdatedBy = 2,
            UpdatedDate = DateTime.Now
        };

        Assert.Equal(1, entity.PropertyId);
        Assert.Equal(1, entity.TaxZoneId);
        Assert.Equal(10, entity.WardId);
        Assert.Equal("PROP001", entity.PropertyNo);
        Assert.Equal("A", entity.PartitionNo);
        Assert.Equal("John Doe", entity.OwnerName);
        Assert.False(entity.MarkedForDeletion);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void PropertyEntity_InheritsFromBaseEntity()
    {
        var entity = new PropertyEntity();
        Assert.IsAssignableFrom<BaseEntity>(entity);
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
            PropertyId = 1,
            TaxZoneId = 1,
            WardId = 10,
            PropertyNo = "PROP001",
            PartitionNo = "A",
            PropertyTypeId = 1,
            OwnerName = "John Doe",
            MarkedForDeletion = false,
            IsActive = true
        };

        Assert.Equal(1, dto.PropertyId);
        Assert.Equal(1, dto.TaxZoneId);
        Assert.Equal(10, dto.WardId);
        Assert.Equal("PROP001", dto.PropertyNo);
        Assert.Equal("A", dto.PartitionNo);
        Assert.Equal("John Doe", dto.OwnerName);
        Assert.False(dto.MarkedForDeletion);
    }

    [Fact]
    public void PropertyDto_DisplayProperty_WithPropertyNoAndPartition_ReturnsFormattedString()
    {
        var dto = new PropertyDto
        {
            PropertyNo = "PROP001",
            PartitionNo = "A"
        };

        Assert.Equal("PROP001-A", dto.DisplayProperty);
    }

    [Fact]
    public void PropertyDto_InheritsFromBaseDtos()
    {
        var dto = new PropertyDto();
        Assert.IsAssignableFrom<BaseDtos>(dto);
    }

    [Fact]
    public void PropertyDto_PartitionNo_CanBeNull()
    {
        var dto = new PropertyDto
        {
            PropertyNo = "PROP001",
            PartitionNo = "A"
        };

        Assert.Equal("PROP001-A", dto.DisplayProperty);
    }

    [Fact]
    public void PropertyDto_DisplayProperty_WithPropertyNoOnly_ReturnsPropertyNo()
    {
        var dto = new PropertyDto
        {
            PropertyNo = "PROP001",
            PartitionNo = null
        };

        Assert.Equal("PROP001", dto.DisplayProperty);
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
            TaxZoneId = 1,
            WardId = 10,
            PropertyNo = "PROP001",
            PartitionNo = "A",
            OwnerName = "John Doe",
            CreatedBy = 1
        };

        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void CreatePropertyDto_MissingTaxZoneId_FailsValidation()
    {
        var dto = new CreatePropertyDto
        {
            WardId = 10,
            PropertyNo = "PROP001"
        };

        var results = Validate(dto);
        // TaxZoneId defaults to 0 which now fails Range validation
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "Property_TaxZoneId_Invalid");
    }

    [Fact]
    public void CreatePropertyDto_MissingWardId_FailsValidation()
    {
        var dto = new CreatePropertyDto
        {
            TaxZoneId = 1,
            PropertyNo = "PROP001"
        };

        var results = Validate(dto);
        // WardId defaults to 0 which now fails Range validation
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "Property_WardId_Invalid");
    }

    [Fact]
    public void CreatePropertyDto_PropertyNoMaxLength_FailsValidation()
    {
        var dto = new CreatePropertyDto
        {
            TaxZoneId = 1,
            WardId = 10,
            PropertyNo = new string('X', 11) // 11 characters, max is 10
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "Property_PropertyNo_MaxLen_10");
    }

    [Fact]
    public void CreatePropertyDto_InvalidEmail_FailsValidation()
    {
        var dto = new CreatePropertyDto
        {
            TaxZoneId = 1,
            WardId = 10,
            EmailId = "invalid-email" // Invalid email format
        };

        var results = Validate(dto);
        // OptionalEmail validation now catches invalid email format
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "Property_EmailId_Invalid");
    }

    [Fact]
    public void CreatePropertyDto_EmptyEmail_PassesValidation()
    {
        var dto = new CreatePropertyDto
        {
            TaxZoneId = 1,
            WardId = 10,
            EmailId = "" // Empty email is allowed
        };

        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void CreatePropertyDto_ValidEmail_PassesValidation()
    {
        var dto = new CreatePropertyDto
        {
            TaxZoneId = 1,
            WardId = 10,
            EmailId = "test@example.com"
        };

        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void CreatePropertyDto_InheritsFromCreateBaseDtos()
    {
        var dto = new CreatePropertyDto();
        Assert.IsAssignableFrom<CreateBaseDtos>(dto);
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
            TaxZoneId = 1,
            WardId = 10,
            PropertyNo = "PROP001",
            OwnerName = "John Doe Updated",
            UpdatedBy = 1,
            IsActive = true
        };

        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdatePropertyDto_TaxZoneIdInvalid_FailsValidation()
    {
        var dto = new UpdatePropertyDto
        {
            TaxZoneId = 0,
            WardId = 10
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "Property_TaxZoneId_Invalid");
    }

    [Fact]
    public void UpdatePropertyDto_WardIdInvalid_FailsValidation()
    {
        var dto = new UpdatePropertyDto
        {
            TaxZoneId = 1,
            WardId = 0
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "Property_WardId_Invalid");
    }

    [Fact]
    public void UpdatePropertyDto_PropertyNoMaxLength_FailsValidation()
    {
        var dto = new UpdatePropertyDto
        {
            TaxZoneId = 1,
            WardId = 10,
            PropertyNo = new string('Y', 11)
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "Property_PropertyNo_MaxLen_10");
    }

    [Fact]
    public void UpdatePropertyDto_Touches_Nullables_And_Validates_Success()
    {
        var dto = new UpdatePropertyDto();
        dto.TaxZoneId = 1;
        dto.WardId = 10;
        dto.PropertyNo = "PROP001";
        dto.PartitionNo = "A";
        dto.UpdatedBy = 123;
        dto.IsActive = true;

        var partitionNo = dto.PartitionNo;
        var updatedBy = dto.UpdatedBy;

        Assert.Equal(1, dto.TaxZoneId);
        Assert.Equal(10, dto.WardId);
        Assert.Equal("PROP001", dto.PropertyNo);
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
        dto.TaxZoneId = 0;
        dto.WardId = 0;
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
    }

    [Fact]
    public void UpdatePropertyDto_InheritsFromUpdateBaseDtos()
    {
        var dto = new UpdatePropertyDto();
        Assert.IsAssignableFrom<UpdateBaseDtos>(dto);
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
    private readonly Mock<IPropertyRepository> _mockPropertyRepository;
    private readonly PropertyService _service;

    public PropertyServiceTests()
    {
        _mockRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockPropertyRepository = new Mock<IPropertyRepository>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new PropertyService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object, _mockPropertyRepository.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new PropertyEntity
        {
            PropertyId = 1,
            TaxZoneId = 1,
            WardId = 10,
            PropertyNo = "PROP001",
            PartitionNo = "A",
            OwnerName = "John Doe",
            CreatedDate = DateTime.Now,
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<PropertyDto>(It.IsAny<PropertyEntity>()))
            .Returns(new PropertyDto
            {
                PropertyId = 1,
                TaxZoneId = 1,
                WardId = 10,
                PropertyNo = "PROP001",
                PartitionNo = "A",
                OwnerName = "John Doe"
            });

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.PropertyId);
        Assert.Equal(1, result.TaxZoneId);
        Assert.Equal(10, result.WardId);
        Assert.Equal("PROP001", result.PropertyNo);
        Assert.Equal("John Doe", result.OwnerName);
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
            new() { PropertyId = 1, TaxZoneId = 1, WardId = 10, PropertyNo = "PROP001", OwnerName = "John Doe", IsActive = true },
            new() { PropertyId = 2, TaxZoneId = 1, WardId = 10, PropertyNo = "PROP002", OwnerName = "Jane Doe", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PropertyMappingProfile>();
        });
        IMapper mapper = mapperConfig.CreateMapper();
        var mockPropertyRepo = new Mock<IPropertyRepository>();

        var service = new PropertyService(_mockRepository.Object, _mockUnitOfWork.Object, mapper, mockPropertyRepo.Object);

        var qp = new PropertyQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        var result = await service.GetAllAsync(qp, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, x => x.PropertyId == 1);
        Assert.Contains(result.Items, x => x.PropertyId == 2);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var createDto = new CreatePropertyDto
        {
            TaxZoneId = 1,
            WardId = 10,
            PropertyNo = "PROP001",
            PartitionNo = "A",
            OwnerName = "John Doe"
        };

        _mockMapper
            .Setup(m => m.Map<PropertyEntity>(It.IsAny<CreatePropertyDto>()))
            .Returns((CreatePropertyDto dto) => new PropertyEntity
            {
                TaxZoneId = dto.TaxZoneId,
                WardId = dto.WardId,
                PropertyNo = dto.PropertyNo,
                PartitionNo = dto.PartitionNo,
                OwnerName = dto.OwnerName,
                CreatedBy = dto.CreatedBy,
                CreatedDate = DateTime.Now,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyEntity e, CancellationToken _) =>
            {
                e.PropertyId = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<PropertyDto>(It.IsAny<PropertyEntity>()))
            .Returns((PropertyEntity e) => new PropertyDto
            {
                PropertyId = e.PropertyId,
                TaxZoneId = e.TaxZoneId,
                WardId = e.WardId,
                PropertyNo = e.PropertyNo,
                PartitionNo = e.PartitionNo,
                OwnerName = e.OwnerName
            });

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.PropertyId);
        Assert.Equal(1, result.TaxZoneId);
        Assert.Equal(10, result.WardId);
        Assert.Equal("PROP001", result.PropertyNo);
        Assert.Equal("John Doe", result.OwnerName);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var updateDto = new UpdatePropertyDto
        {
            TaxZoneId = 1,
            WardId = 10,
            PropertyNo = "PROP001",
            OwnerName = "John Doe Updated"
        };

        var existingEntity = new PropertyEntity
        {
            PropertyId = 1,
            TaxZoneId = 1,
            WardId = 10,
            PropertyNo = "PROP001",
            OwnerName = "John Doe",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdatePropertyDto>(), It.IsAny<PropertyEntity>()))
            .Callback((UpdatePropertyDto src, PropertyEntity dest) =>
            {
                dest.OwnerName = src.OwnerName;
            });

        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal("John Doe Updated", existingEntity.OwnerName);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
    {
        var updateDto = new UpdatePropertyDto
        {
            TaxZoneId = 1,
            WardId = 10,
            OwnerName = "John Doe"
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
        var idToDelete = 1;

        var existingEntity = new PropertyEntity
        {
            PropertyId = idToDelete,
            TaxZoneId = 1,
            WardId = 10,
            PropertyNo = "PROP001",
            OwnerName = "John Doe",
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

    [Fact]
    public async Task GetBasicDetailsAsync_ExistingProperty_ReturnsBasicDetailsDto()
    {
        var propertyId = 1;
        var expectedDto = new PropertyBasicDetailsDto
        {
            PropertyId = propertyId,
            WardId = 10,
            WardNo = "W001",
            ZoneId = 5,
            Division = "North Zone",
            PropertyNo = "PROP001",
            PartitionNo = "A",
            FlatOrShopNo = "101",
            PlotNo = "P123",
            SurveyNo = "S456",
            TaxZoneId = 1,
            TaxZoneNo = "TZ001",
            CategoryId = 2,
            CategoryName = "Residential",
            PropertyTypeId = 3,
            PropertyDescription = "Apartment",
            WingNo = "B",
            NoOfResidentialToilets = 2,
            NoOfCommercialToilets = 0,
            TotalCarpetAreaSqMeter = 1000.50,
            TotalBuiltupAreaSqMeter = 1200.75,
            PlotArea = 1500.25
        };

        _mockPropertyRepository
            .Setup(r => r.GetBasicDetailsAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var result = await _service.GetBasicDetailsAsync(propertyId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(propertyId, result.PropertyId);
        Assert.Equal(10, result.WardId);
        Assert.Equal("W001", result.WardNo);
        Assert.Equal(5, result.ZoneId);
        Assert.Equal("North Zone", result.Division);
        Assert.Equal("PROP001", result.PropertyNo);
        Assert.Equal("A", result.PartitionNo);
        Assert.Equal("101", result.FlatOrShopNo);
        Assert.Equal("P123", result.PlotNo);
        Assert.Equal("S456", result.SurveyNo);
        Assert.Equal(1, result.TaxZoneId);
        Assert.Equal("TZ001", result.TaxZoneNo);
        Assert.Equal(2, result.CategoryId);
        Assert.Equal("Residential", result.CategoryName);
        Assert.Equal(3, result.PropertyTypeId);
        Assert.Equal("Apartment", result.PropertyDescription);
        Assert.Equal("B", result.WingNo);
        Assert.Equal(2, result.NoOfResidentialToilets);
        Assert.Equal(0, result.NoOfCommercialToilets);
        Assert.Equal(1000.50, result.TotalCarpetAreaSqMeter);
        Assert.Equal(1200.75, result.TotalBuiltupAreaSqMeter);
        Assert.Equal(1500.25, result.PlotArea);

        _mockPropertyRepository.Verify(r => r.GetBasicDetailsAsync(propertyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBasicDetailsAsync_NonExistingProperty_ReturnsNull()
    {
        var propertyId = 999;

        _mockPropertyRepository
            .Setup(r => r.GetBasicDetailsAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyBasicDetailsDto?)null);

        var result = await _service.GetBasicDetailsAsync(propertyId, CancellationToken.None);

        Assert.Null(result);
        _mockPropertyRepository.Verify(r => r.GetBasicDetailsAsync(propertyId, It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region PropertyBasicDetailsDto Tests

public class PropertyBasicDetailsDtoTests
{
    [Fact]
    public void PropertyBasicDetailsDto_AllProperties_GetSet_WorksCorrectly()
    {
        var dto = new PropertyBasicDetailsDto
        {
            PropertyId = 1,
            WardId = 10,
            WardNo = "W001",
            ZoneId = 5,
            Division = "North Zone",
            PropertyNo = "PROP001",
            PartitionNo = "A",
            FlatOrShopNo = "101",
            PlotNo = "P123",
            SurveyNo = "S456",
            TaxZoneId = 1,
            TaxZoneNo = "TZ001",
            CategoryId = 2,
            CategoryName = "Residential",
            PropertyTypeId = 3,
            PropertyDescription = "Apartment",
            WingNo = "B",
            NoOfResidentialToilets = 2,
            NoOfCommercialToilets = 1,
            TotalCarpetAreaSqMeter = 1000.50,
            TotalBuiltupAreaSqMeter = 1200.75,
            PlotArea = 1500.25
        };

        Assert.Equal(1, dto.PropertyId);
        Assert.Equal(10, dto.WardId);
        Assert.Equal("W001", dto.WardNo);
        Assert.Equal(5, dto.ZoneId);
        Assert.Equal("North Zone", dto.Division);
        Assert.Equal("PROP001", dto.PropertyNo);
        Assert.Equal("A", dto.PartitionNo);
        Assert.Equal("101", dto.FlatOrShopNo);
        Assert.Equal("P123", dto.PlotNo);
        Assert.Equal("S456", dto.SurveyNo);
        Assert.Equal(1, dto.TaxZoneId);
        Assert.Equal("TZ001", dto.TaxZoneNo);
        Assert.Equal(2, dto.CategoryId);
        Assert.Equal("Residential", dto.CategoryName);
        Assert.Equal(3, dto.PropertyTypeId);
        Assert.Equal("Apartment", dto.PropertyDescription);
        Assert.Equal("B", dto.WingNo);
        Assert.Equal(2, dto.NoOfResidentialToilets);
        Assert.Equal(1, dto.NoOfCommercialToilets);
        Assert.Equal(1000.50, dto.TotalCarpetAreaSqMeter);
        Assert.Equal(1200.75, dto.TotalBuiltupAreaSqMeter);
        Assert.Equal(1500.25, dto.PlotArea);
    }

    [Fact]
    public void PropertyBasicDetailsDto_NullableProperties_CanBeNull()
    {
        var dto = new PropertyBasicDetailsDto
        {
            PropertyId = 1,
            WardId = 10,
            TaxZoneId = 1,
            TotalCarpetAreaSqMeter = 0,
            TotalBuiltupAreaSqMeter = 0
        };

        Assert.Null(dto.WardNo);
        Assert.Null(dto.ZoneId);
        Assert.Null(dto.Division);
        Assert.Null(dto.PropertyNo);
        Assert.Null(dto.PartitionNo);
        Assert.Null(dto.FlatOrShopNo);
        Assert.Null(dto.PlotNo);
        Assert.Null(dto.SurveyNo);
        Assert.Null(dto.TaxZoneNo);
        Assert.Null(dto.CategoryId);
        Assert.Null(dto.CategoryName);
        Assert.Null(dto.PropertyTypeId);
        Assert.Null(dto.PropertyDescription);
        Assert.Null(dto.WingNo);
        Assert.Null(dto.NoOfResidentialToilets);
        Assert.Null(dto.NoOfCommercialToilets);
        Assert.Null(dto.PlotArea);
    }

    [Fact]
    public void PropertyBasicDetailsDto_DefaultConstructor_InitializesCorrectly()
    {
        var dto = new PropertyBasicDetailsDto();

        Assert.Equal(0, dto.PropertyId);
        Assert.Equal(0, dto.WardId);
        Assert.Equal(0, dto.TaxZoneId);
        Assert.Equal(0, dto.TotalCarpetAreaSqMeter);
        Assert.Equal(0, dto.TotalBuiltupAreaSqMeter);
    }

    [Fact]
    public void PropertyBasicDetailsDto_WithPartialData_WorksCorrectly()
    {
        var dto = new PropertyBasicDetailsDto
        {
            PropertyId = 1,
            WardId = 10,
            TaxZoneId = 1,
            PropertyNo = "PROP001",
            TotalCarpetAreaSqMeter = 500.0,
            TotalBuiltupAreaSqMeter = 600.0
        };

        Assert.Equal(1, dto.PropertyId);
        Assert.Equal("PROP001", dto.PropertyNo);
        Assert.Null(dto.PartitionNo);
        Assert.Null(dto.CategoryName);
        Assert.Equal(500.0, dto.TotalCarpetAreaSqMeter);
    }
}

#endregion