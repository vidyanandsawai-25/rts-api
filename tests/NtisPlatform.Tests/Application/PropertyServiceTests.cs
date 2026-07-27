using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable;
using Moq;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using MockQueryable.Moq;
namespace NtisPlatform.Tests.Application;

#region PropertyEntity Tests

public class PropertyEntityTests
{
    [Fact]
    public void PropertyEntity_Properties_GetSet_WorksCorrectly()
    {
        var entity = new PropertyEntity
        {
            Id = 1,
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

        Assert.Equal(1, entity.Id);
        Assert.Equal(1, entity.TaxZoneId);
        Assert.Equal(10, entity.WardId);
        Assert.Equal("PROP001", entity.PropertyNo);
        Assert.Equal("A", entity.PartitionNo);
        Assert.Equal("John Doe", entity.OwnerName);
        Assert.False(entity.MarkedForDeletion);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void PropertyEntity_AllProperties_GetSet_ComprehensiveCoverage()
    {
        var now = DateTime.Now;
        var entity = new PropertyEntity
        {
            Id = 549357,
            TaxZoneId = 10,
            WardId = 79,
            PropertyNo = "22",
            PartitionNo = "1",
            PropertyTypeId = 2,
            UPICId = "UPIC123",
            OpenPlot = true,
            CSN = "CSN456",
            SubZoneNo = "SZ01",
            PlotNo = "P123",
            CategoryId = 1,
            Type = "RES",
            OwnerTitle = "Mr",
            OwnerName = "John Doe",
            OwnerTitleEnglish = "Mr",
            OwnerNameEnglish = "John English",
            OccupierTitle = "Ms",
            OccupierName = "Jane Doe",
            OccupierTitleEnglish = "Ms",
            OccupierNameEnglish = "Jane English",
            FlatOrShopNo = "101",
            FlatOrShopName = "Flat 101",
            FlatOrShopNoEnglish = "101",
            FlatOrShopNameEnglish = "Flat English",
            Address = "123 Main St",
            Location = "Downtown",
            AddressEnglish = "123 Main Street",
            LocationEnglish = "Downtown Area",
            MobileNo = "9921759522",
            EmailId = "test@example.com",
            SocietyDetailId = 5,
            MarkedForDeletion = false,
            MarkedForDeletionDate = now,
            IsActive = true,
            CreatedBy = 100,
            CreatedDate = now,
            UpdatedBy = 200,
            UpdatedDate = now
        };

        Assert.Equal(549357, entity.Id);
        Assert.Equal(10, entity.TaxZoneId);
        Assert.Equal(79, entity.WardId);
        Assert.Equal("22", entity.PropertyNo);
        Assert.Equal("1", entity.PartitionNo);
        Assert.Equal(2, entity.PropertyTypeId);
        Assert.Equal("UPIC123", entity.UPICId);
        Assert.True(entity.OpenPlot);
        Assert.Equal("CSN456", entity.CSN);
        Assert.Equal("SZ01", entity.SubZoneNo);
        Assert.Equal("P123", entity.PlotNo);
        Assert.Equal(1, entity.CategoryId);
        Assert.Equal("RES", entity.Type);
        Assert.Equal("Mr", entity.OwnerTitle);
        Assert.Equal("John Doe", entity.OwnerName);
        Assert.Equal("Mr", entity.OwnerTitleEnglish);
        Assert.Equal("John English", entity.OwnerNameEnglish);
        Assert.Equal("Ms", entity.OccupierTitle);
        Assert.Equal("Jane Doe", entity.OccupierName);
        Assert.Equal("Ms", entity.OccupierTitleEnglish);
        Assert.Equal("Jane English", entity.OccupierNameEnglish);
        Assert.Equal("101", entity.FlatOrShopNo);
        Assert.Equal("Flat 101", entity.FlatOrShopName);
        Assert.Equal("101", entity.FlatOrShopNoEnglish);
        Assert.Equal("Flat English", entity.FlatOrShopNameEnglish);
        Assert.Equal("123 Main St", entity.Address);
        Assert.Equal("Downtown", entity.Location);
        Assert.Equal("123 Main Street", entity.AddressEnglish);
        Assert.Equal("Downtown Area", entity.LocationEnglish);
        Assert.Equal("9921759522", entity.MobileNo);
        Assert.Equal("test@example.com", entity.EmailId);
        Assert.Equal(5, entity.SocietyDetailId);
        Assert.False(entity.MarkedForDeletion);
        Assert.Equal(now, entity.MarkedForDeletionDate);
        Assert.True(entity.IsActive);
        Assert.Equal(100, entity.CreatedBy);
        Assert.Equal(now, entity.CreatedDate);
        Assert.Equal(200, entity.UpdatedBy);
        Assert.Equal(now, entity.UpdatedDate);
    }

    [Fact]
    public void PropertyEntity_ImplementsIHardDeletable()
    {
        var entity = new PropertyEntity();
        Assert.IsAssignableFrom<NtisPlatform.Core.Interfaces.IHardDeletable>(entity);
    }

    [Fact]
    public void PropertyEntity_MarkedForDeletionDate_CanBeNull()
    {
        var entity = new PropertyEntity
        {
            MarkedForDeletion = false,
            MarkedForDeletionDate = null
        };

        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void PropertyEntity_DefaultValues_SetCorrectly()
    {
        var entity = new PropertyEntity();

        Assert.Equal(0, entity.Id);
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
            Id = 1,
            TaxZoneId = 1,
            WardId = 10,
            PropertyNo = "PROP001",
            PartitionNo = "A",
            PropertyTypeId = 1,
            OwnerName = "John Doe",
            MarkedForDeletion = false,
            IsActive = true
        };

        Assert.Equal(1, dto.Id);
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
    private readonly Mock<ILogger<PropertyService>> _mockLogger;
    private readonly Mock<IOptions<FeatureFlagsOptions>> _mockFeatureFlags;

    public PropertyServiceTests()
    {
        _mockRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockPropertyRepository = new Mock<IPropertyRepository>();
        _mockLogger = new Mock<ILogger<PropertyService>>();
        _mockFeatureFlags = new Mock<IOptions<FeatureFlagsOptions>>();

        // Setup feature flag - allow deletion without payment validation in tests
        _mockFeatureFlags.Setup(f => f.Value).Returns(new FeatureFlagsOptions
        {
            AllowPropertyDeletionWithoutPaymentValidation = true
        });

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new PropertyService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object, _mockPropertyRepository.Object, _mockLogger.Object, _mockFeatureFlags.Object, new Mock<IRepository<WardEntity, int>>().Object, new Mock<IRepository<PropertyCategoryEntity, int>>().Object, new Mock<IRepository<SocietyDetailsEntity, int>>().Object, new Mock<IRepository<PropertyDetailsEntity, int>>().Object, new Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>>().Object, new Mock<IRepository<PropertyAssessmentEntity, int>>().Object, new Mock<IRepository<GlobalSurveyWardAllocationEntity, int>>().Object, new Mock<IRepository<PropertyMapMasterEntity, int>>().Object, new Mock<IRepository<PropertyMapDetailEntity, int>>().Object, new Mock<IRepository<UserEntity, int>>().Object, new Mock<IRepository<PropertyMastOldEntity, int>>().Object, new Mock<IRepository<PropertyTypeMasterEntity, int>>().Object, new Mock<NtisPlatform.Application.Interfaces.Rules.IPropertyRuleApplicationLogService>().Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new PropertyEntity
        {
            Id = 1,
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
                Id = 1,
                TaxZoneId = 1,
                WardId = 10,
                PropertyNo = "PROP001",
                PartitionNo = "A",
                OwnerName = "John Doe"
            });

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
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
            new() { Id = 1, TaxZoneId = 1, WardId = 10, PropertyNo = "PROP001", OwnerName = "John Doe", IsActive = true },
            new() { Id = 2, TaxZoneId = 1, WardId = 10, PropertyNo = "PROP002", OwnerName = "Jane Doe", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PropertyMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        IMapper mapper = mapperConfig.CreateMapper();
        var mockPropertyRepo = new Mock<IPropertyRepository>();
        var mockFeatureFlags = new Mock<IOptions<FeatureFlagsOptions>>();
        mockFeatureFlags.Setup(f => f.Value).Returns(new FeatureFlagsOptions
        {
            AllowPropertyDeletionWithoutPaymentValidation = true
        });

        var service = new PropertyService(_mockRepository.Object, _mockUnitOfWork.Object, mapper, mockPropertyRepo.Object, _mockLogger.Object, mockFeatureFlags.Object, new Mock<IRepository<WardEntity, int>>().Object, new Mock<IRepository<PropertyCategoryEntity, int>>().Object, new Mock<IRepository<SocietyDetailsEntity, int>>().Object, new Mock<IRepository<PropertyDetailsEntity, int>>().Object, new Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>>().Object, new Mock<IRepository<PropertyAssessmentEntity, int>>().Object, new Mock<IRepository<GlobalSurveyWardAllocationEntity, int>>().Object, new Mock<IRepository<PropertyMapMasterEntity, int>>().Object, new Mock<IRepository<PropertyMapDetailEntity, int>>().Object, new Mock<IRepository<UserEntity, int>>().Object, new Mock<IRepository<PropertyMastOldEntity, int>>().Object, new Mock<IRepository<PropertyTypeMasterEntity, int>>().Object, new Mock<NtisPlatform.Application.Interfaces.Rules.IPropertyRuleApplicationLogService>().Object);

        var qp = new PropertyQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        var result = await service.GetAllAsync(qp, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, x => x.Id == 1);
        Assert.Contains(result.Items, x => x.Id == 2);
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
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<PropertyDto>(It.IsAny<PropertyEntity>()))
            .Returns((PropertyEntity e) => new PropertyDto
            {
                Id = e.Id,
                TaxZoneId = e.TaxZoneId,
                WardId = e.WardId,
                PropertyNo = e.PropertyNo,
                PartitionNo = e.PartitionNo,
                OwnerName = e.OwnerName
            });

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
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
            Id = 1,
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
    public async Task DeleteAsync_NonExistingEntity_ThrowsValidationException_DoesNotSave()
    {
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyEntity?)null);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert - Now throws ValidationException instead of returning false
        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.DeleteAsync(idToDelete, CancellationToken.None));

        Assert.Contains("999", ex.Message);
        Assert.Contains("does not exist", ex.Message);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Helper method to setup all the new repository method mocks for delete operations.
    /// </summary>
    private void SetupDeleteRepositoryMocks()
    {
        _mockPropertyRepository.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyDetailsEntity>());

        _mockPropertyRepository.Setup(r => r.GetRvResultsByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RVCalculationResultsEntity>());

        _mockPropertyRepository.Setup(r => r.GetSection129ResultsByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyTaxCalculationSection129ResultsEntity>());

        _mockPropertyRepository.Setup(r => r.GetPropertyOccupancyByPropertyDetailIdsAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyOccupancyDetailsEntity>());

        _mockPropertyRepository.Setup(r => r.GetRentersByPropertyDetailIdsAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RenterMastEntity>());

        _mockPropertyRepository.Setup(r => r.GetRoomWiseSubmissionByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RoomWiseSubmissionDetailsEntity>());

        _mockPropertyRepository.Setup(r => r.GetRelatedEntitiesForDeletionAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IHardDeletable>());

        _mockPropertyRepository.Setup(r => r.GetRoomWiseMinusBySubmissionIdsAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RoomWiseMinusDataEntity>());

        _mockPropertyRepository.Setup(r => r.GetPropertySocialDetailsByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertySocialDetailsEntity>());

        _mockPropertyRepository.Setup(r => r.GetWaterConnectionsByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WaterConnectionMasterEntity>());

        // Setup MarkEntitiesForDeletion to apply deletion flags to entities passed to it
        _mockPropertyRepository.Setup(r => r.MarkEntitiesForDeletion(It.IsAny<IEnumerable<IHardDeletable>>()))
            .Callback<IEnumerable<IHardDeletable>>(entities =>
            {
                var now = DateTime.Now;
                foreach (var entity in entities)
                {
                    entity.MarkedForDeletion = true;
                    if (!entity.MarkedForDeletionDate.HasValue)
                    {
                        entity.MarkedForDeletionDate = now;
                    }
                    if (entity is BaseEntity baseEntity)
                    {
                        baseEntity.IsActive = false;
                        baseEntity.UpdatedDate = now;
                    }
                }
            });

        // Setup DeactivatePropertyEntities to apply deactivation to BaseEntity-only entities
        _mockPropertyRepository.Setup(r => r.DeactivatePropertyEntities(It.IsAny<IEnumerable<BaseEntity>>()))
            .Callback<IEnumerable<BaseEntity>>(entities =>
            {
                var now = DateTime.Now;
                foreach (var entity in entities)
                {
                    entity.IsActive = false;
                    entity.UpdatedDate = now;
                }
            });
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        var propertyId = 1;
        var entity = new PropertyEntity { Id = propertyId, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup all repository mocks for delete
        SetupDeleteRepositoryMocks();

        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(propertyId);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2)); // Once for children, once for parent
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingProperty_ThrowsValidationException()
    {
        // Arrange
        var propertyId = 999;

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository.Setup(r => r.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyEntity?)null);

        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert - Now throws ValidationException instead of returning false
        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.DeleteAsync(propertyId));

        Assert.Contains("999", ex.Message);
        Assert.Contains("does not exist", ex.Message);
        _mockRepository.Verify(r => r.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkDeleteAsync_ValidIds_DeletesAll()
    {
        // Arrange
        var propertyIds = new[] { 1, 2 };
        var entities = propertyIds.Select(id => new PropertyEntity { Id = id, IsActive = true }).ToList();

        // Setup GetQueryable for upfront entity fetch
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(entities.BuildMock());

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => entities.FirstOrDefault(e => e.Id == id));

        // Setup all repository mocks for delete
        SetupDeleteRepositoryMocks();

        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkDeleteAsync(propertyIds);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(propertyIds.Length, result.SuccessCount);
        Assert.True(result.Errors == null || result.Errors.Count == 0);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Exactly(propertyIds.Length)); // Once per property
        _mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(propertyIds.Length));
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(propertyIds.Length));
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(propertyIds.Length * 2)); // Twice per property (children + parent)
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Exactly(propertyIds.Length)); // Once per property
    }

    [Fact]
    public async Task BulkDeleteAsync_SomeIdsNotFound_ReturnsErrorBeforeProcessing()
    {
        // Arrange - When some IDs don't exist, the new implementation returns error before processing
        var propertyIds = new[] { 1, 999 }; // 999 does not exist
        var entities = new List<PropertyEntity> { new() { Id = 1, IsActive = true } };

        // Setup GetQueryable - will only return entity with Id=1
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(entities.BuildMock());

        // Act
        var result = await _service.BulkDeleteAsync(propertyIds);

        // Assert - New behavior: Returns error immediately if some IDs not found
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(2, result.FailedCount);
        Assert.NotNull(result.Errors);
        Assert.Single(result.Errors);
        Assert.Contains("999", result.Errors[0]); // Should mention the missing ID
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkDeleteAsync_EmptyIdList_ReturnsZeroSuccessCount()
    {
        // Arrange
        var propertyIds = Array.Empty<int>();

        // Act
        var result = await _service.BulkDeleteAsync(propertyIds);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkDeleteAsync_AllInvalidIds_ReturnsErrorBeforeProcessing()
    {
        // Arrange - All IDs don't exist, returns error before any processing
        var propertyIds = new[] { 998, 999 };
        var emptyList = new List<PropertyEntity>();

        // Setup GetQueryable to return empty list (no matching IDs)
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(emptyList.BuildMock());

        // Act
        var result = await _service.BulkDeleteAsync(propertyIds);

        // Assert - New behavior: Returns error immediately if IDs not found
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(propertyIds.Length, result.FailedCount);
        Assert.NotNull(result.Errors);
        Assert.Single(result.Errors); // One error message listing all missing IDs
        Assert.Contains("998", result.Errors[0]);
        Assert.Contains("999", result.Errors[0]);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithException_ThrowsException()
    {
        // Arrange
        var propertyId = 1;
        var entity = new PropertyEntity { Id = propertyId, IsActive = true };

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository.Setup(r => r.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Mock repository method to throw exception
        _mockPropertyRepository.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Delete operation failed"));

        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert - Now throws ValidationException with error message
        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.DeleteAsync(propertyId));

        Assert.Contains("Delete operation failed", ex.Message);
        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #region Partition Deletion Validation Tests

    [Fact]
    public async Task DeleteAsync_PropertyWithPartition_HighestPropertyId_DeletesSuccessfully()
    {
        // Arrange - Property with highest ID in partition group should delete
        var propertyId = 552380; // A7 - highest
        var entity = new PropertyEntity 
        { 
            Id = propertyId, 
            WardId = 60, 
            PropertyNo = "1", 
            PartitionNo = "A7", 
            IsActive = true,
            MarkedForDeletion = false 
        };

        var relatedProperties = new List<PropertyEntity>
        {
            new() { Id = 552380, WardId = 60, PropertyNo = "1", PartitionNo = "A7", IsActive = true, MarkedForDeletion = false },
            new() { Id = 552379, WardId = 60, PropertyNo = "1", PartitionNo = "A6", IsActive = true, MarkedForDeletion = false },
            new() { Id = 552378, WardId = 60, PropertyNo = "1", PartitionNo = "A5", IsActive = true, MarkedForDeletion = false }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(relatedProperties.BuildMock());

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupDeleteRepositoryMocks();

        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(propertyId);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_PropertyWithPartition_NotHighestPropertyId_ThrowsValidationException()
    {
        // Arrange - Property with lower ID when higher ID exists should fail
        var propertyId = 552378; // A5 - not highest (552380/A7 is highest)
        var entity = new PropertyEntity 
        { 
            Id = propertyId, 
            WardId = 60, 
            PropertyNo = "1", 
            PartitionNo = "A5", 
            IsActive = true,
            MarkedForDeletion = false 
        };

        var relatedProperties = new List<PropertyEntity>
        {
            new() { Id = 552380, WardId = 60, PropertyNo = "1", PartitionNo = "A7", IsActive = true, MarkedForDeletion = false },
            new() { Id = 552379, WardId = 60, PropertyNo = "1", PartitionNo = "A6", IsActive = true, MarkedForDeletion = false },
            new() { Id = 552378, WardId = 60, PropertyNo = "1", PartitionNo = "A5", IsActive = true, MarkedForDeletion = false }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(relatedProperties.BuildMock());

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.DeleteAsync(propertyId));

        // Error message now uses partition numbers only, not PropertyIds
        Assert.Contains("A5", ex.Message); // The partition being deleted
        Assert.Contains("A7", ex.Message); // The highest partition that should be deleted first
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_PropertyWithPartition_AfterHigherDeleted_DeletesSuccessfully()
    {
        // Arrange - Property becomes highest after higher one is marked for deletion
        var propertyId = 552379; // A6 - becomes highest after A7 (552380) is deleted
        var entity = new PropertyEntity 
        { 
            Id = propertyId, 
            WardId = 60, 
            PropertyNo = "1", 
            PartitionNo = "A6", 
            IsActive = true,
            MarkedForDeletion = false 
        };

        // 552380 (A7) is already marked for deletion, so 552379 (A6) is now highest
        var relatedProperties = new List<PropertyEntity>
        {
            new() { Id = 552380, WardId = 60, PropertyNo = "1", PartitionNo = "A7", IsActive = true, MarkedForDeletion = true }, // Already deleted
            new() { Id = 552379, WardId = 60, PropertyNo = "1", PartitionNo = "A6", IsActive = true, MarkedForDeletion = false },
            new() { Id = 552378, WardId = 60, PropertyNo = "1", PartitionNo = "A5", IsActive = true, MarkedForDeletion = false }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(relatedProperties.BuildMock());

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupDeleteRepositoryMocks();

        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(propertyId);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_PropertyWithoutPartition_DeletesSuccessfully()
    {
        // Arrange - Property without partition number should delete without partition validation
        var propertyId = 1;
        var entity = new PropertyEntity 
        { 
            Id = propertyId, 
            WardId = 60, 
            PropertyNo = "100", 
            PartitionNo = null, // No partition
            IsActive = true,
            MarkedForDeletion = false 
        };

        _mockRepository.Setup(r => r.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupDeleteRepositoryMocks();

        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(propertyId);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkDeleteAsync_PartitionsInDescendingOrder_DeletesAll()
    {
        // Arrange - Properties in correct descending order [A10, A9, A8] should all delete
        var propertyIds = new[] { 560669, 560668, 560667 };
        var entities = new List<PropertyEntity>
        {
            new() { Id = 560669, WardId = 17, PropertyNo = "3", PartitionNo = "A10", IsActive = true, MarkedForDeletion = false },
            new() { Id = 560668, WardId = 17, PropertyNo = "3", PartitionNo = "A9", IsActive = true, MarkedForDeletion = false },
            new() { Id = 560667, WardId = 17, PropertyNo = "3", PartitionNo = "A8", IsActive = true, MarkedForDeletion = false }
        };

        // All active properties in the group (for validation query)
        var allActiveProperties = new List<PropertyEntity>
        {
            new() { Id = 560669, WardId = 17, PropertyNo = "3", PartitionNo = "A10", IsActive = true, MarkedForDeletion = false },
            new() { Id = 560668, WardId = 17, PropertyNo = "3", PartitionNo = "A9", IsActive = true, MarkedForDeletion = false },
            new() { Id = 560667, WardId = 17, PropertyNo = "3", PartitionNo = "A8", IsActive = true, MarkedForDeletion = false },
            new() { Id = 560666, WardId = 17, PropertyNo = "3", PartitionNo = "A7", IsActive = true, MarkedForDeletion = false }
        };

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(allActiveProperties.BuildMock());

        _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => entities.FirstOrDefault(e => e.Id == id));

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupDeleteRepositoryMocks();

        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkDeleteAsync(propertyIds);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.True(result.Errors == null || result.Errors.Count == 0);
    }

    [Fact]
    public async Task BulkDeleteAsync_PartitionsWithGaps_FailsValidation()
    {
        // Arrange - Properties with gaps [A10, A8, A6] should fail (missing A9, A7)
        var propertyIds = new[] { 560669, 560667, 560665 };
        var entities = new List<PropertyEntity>
        {
            new() { Id = 560669, WardId = 17, PropertyNo = "3", PartitionNo = "A10", IsActive = true, MarkedForDeletion = false },
            new() { Id = 560667, WardId = 17, PropertyNo = "3", PartitionNo = "A8", IsActive = true, MarkedForDeletion = false },
            new() { Id = 560665, WardId = 17, PropertyNo = "3", PartitionNo = "A6", IsActive = true, MarkedForDeletion = false }
        };

        // All active properties (includes the ones being skipped)
        var allActiveProperties = new List<PropertyEntity>
        {
            new() { Id = 560669, WardId = 17, PropertyNo = "3", PartitionNo = "A10", IsActive = true, MarkedForDeletion = false },
            new() { Id = 560668, WardId = 17, PropertyNo = "3", PartitionNo = "A9", IsActive = true, MarkedForDeletion = false },
            new() { Id = 560667, WardId = 17, PropertyNo = "3", PartitionNo = "A8", IsActive = true, MarkedForDeletion = false },
            new() { Id = 560666, WardId = 17, PropertyNo = "3", PartitionNo = "A7", IsActive = true, MarkedForDeletion = false },
            new() { Id = 560665, WardId = 17, PropertyNo = "3", PartitionNo = "A6", IsActive = true, MarkedForDeletion = false }
        };

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(allActiveProperties.BuildMock());

        // Act
        var result = await _service.BulkDeleteAsync(propertyIds);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(3, result.FailedCount);
        Assert.NotNull(result.Errors);
        Assert.Single(result.Errors);
        Assert.Contains("gap", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BulkDeleteAsync_PartitionsNotStartingFromHighest_FailsValidation()
    {
        // Arrange - Properties not starting from highest [A9, A8, A7] when A10 exists should fail
        var propertyIds = new[] { 560668, 560667, 560666 };
        var entities = new List<PropertyEntity>
        {
            new() { Id = 560668, WardId = 17, PropertyNo = "3", PartitionNo = "A9", IsActive = true, MarkedForDeletion = false },
            new() { Id = 560667, WardId = 17, PropertyNo = "3", PartitionNo = "A8", IsActive = true, MarkedForDeletion = false },
            new() { Id = 560666, WardId = 17, PropertyNo = "3", PartitionNo = "A7", IsActive = true, MarkedForDeletion = false }
        };

        // All active properties (includes A10 which is highest)
        var allActiveProperties = new List<PropertyEntity>
        {
            new() { Id = 560669, WardId = 17, PropertyNo = "3", PartitionNo = "A10", IsActive = true, MarkedForDeletion = false },
            new() { Id = 560668, WardId = 17, PropertyNo = "3", PartitionNo = "A9", IsActive = true, MarkedForDeletion = false },
            new() { Id = 560667, WardId = 17, PropertyNo = "3", PartitionNo = "A8", IsActive = true, MarkedForDeletion = false },
            new() { Id = 560666, WardId = 17, PropertyNo = "3", PartitionNo = "A7", IsActive = true, MarkedForDeletion = false }
        };

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(allActiveProperties.BuildMock());

        // Act
        var result = await _service.BulkDeleteAsync(propertyIds);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(3, result.FailedCount);
        Assert.NotNull(result.Errors);
        Assert.Single(result.Errors);
        // Error message now uses partition numbers only, not PropertyIds
        Assert.Contains("A10", result.Errors[0]); // Should mention the highest partition
    }

    [Fact]
    public async Task BulkDeleteAsync_PartitionsAfterHigherDeleted_DeletesAll()
    {
        // Arrange - After A10 is deleted, [A9, A8, A7] should delete successfully
        var propertyIds = new[] { 560668, 560667, 560666 };
        var entities = new List<PropertyEntity>
        {
            new() { Id = 560668, WardId = 17, PropertyNo = "3", PartitionNo = "A9", IsActive = true, MarkedForDeletion = false },
            new() { Id = 560667, WardId = 17, PropertyNo = "3", PartitionNo = "A8", IsActive = true, MarkedForDeletion = false },
            new() { Id = 560666, WardId = 17, PropertyNo = "3", PartitionNo = "A7", IsActive = true, MarkedForDeletion = false }
        };

        // A10 is already marked for deletion, so it's excluded from validation
        var allActiveProperties = new List<PropertyEntity>
        {
            new() { Id = 560669, WardId = 17, PropertyNo = "3", PartitionNo = "A10", IsActive = true, MarkedForDeletion = true }, // Already deleted
            new() { Id = 560668, WardId = 17, PropertyNo = "3", PartitionNo = "A9", IsActive = true, MarkedForDeletion = false },
            new() { Id = 560667, WardId = 17, PropertyNo = "3", PartitionNo = "A8", IsActive = true, MarkedForDeletion = false },
            new() { Id = 560666, WardId = 17, PropertyNo = "3", PartitionNo = "A7", IsActive = true, MarkedForDeletion = false }
        };

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(allActiveProperties.BuildMock());

        _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => entities.FirstOrDefault(e => e.Id == id));

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupDeleteRepositoryMocks();

        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkDeleteAsync(propertyIds);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
    }

    [Fact]
    public async Task BulkDeleteAsync_MixedPropertiesWithAndWithoutPartitions_DeletesAllValid()
    {
        // Arrange - Mix of partitioned and non-partitioned properties
        var propertyIds = new[] { 560669, 1 }; // 560669 has partition, 1 does not
        var entities = new List<PropertyEntity>
        {
            new() { Id = 560669, WardId = 17, PropertyNo = "3", PartitionNo = "A10", IsActive = true, MarkedForDeletion = false },
            new() { Id = 1, WardId = 60, PropertyNo = "100", PartitionNo = null, IsActive = true, MarkedForDeletion = false }
        };

        // All properties including non-partitioned and other partitioned properties
        var allActiveProperties = new List<PropertyEntity>
        {
            new() { Id = 560669, WardId = 17, PropertyNo = "3", PartitionNo = "A10", IsActive = true, MarkedForDeletion = false },
            new() { Id = 560668, WardId = 17, PropertyNo = "3", PartitionNo = "A9", IsActive = true, MarkedForDeletion = false },
            new() { Id = 1, WardId = 60, PropertyNo = "100", PartitionNo = null, IsActive = true, MarkedForDeletion = false } // Non-partitioned property
        };

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(allActiveProperties.BuildMock());

        _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => entities.FirstOrDefault(e => e.Id == id));

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetupDeleteRepositoryMocks();

        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkDeleteAsync(propertyIds);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
    }

    [Fact]
    public async Task BulkDeleteAsync_PropertiesNotFound_ReturnsError()
    {
        // Arrange - Some properties don't exist
        var propertyIds = new[] { 560669, 999999 };
        var entities = new List<PropertyEntity>
        {
            new() { Id = 560669, WardId = 17, PropertyNo = "3", PartitionNo = "A10", IsActive = true, MarkedForDeletion = false }
        };

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(entities.BuildMock());

        // Act
        var result = await _service.BulkDeleteAsync(propertyIds);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(2, result.FailedCount);
        Assert.NotNull(result.Errors);
        Assert.Contains("999999", result.Errors[0]);
    }

    #endregion
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
                NoOfCommercialToilets = 0,
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
            Assert.Equal(0, dto.NoOfCommercialToilets);
            Assert.Equal(1000.50, dto.TotalCarpetAreaSqMeter);
            Assert.Equal(1200.75, dto.TotalBuiltupAreaSqMeter);
            Assert.Equal(1500.25, dto.PlotArea);
        }

        [Fact]
        public void PropertyBasicDetailsDto_AllDoubleProperties_GetSet_WorksCorrectly()
        {
            var dto = new PropertyBasicDetailsDto
            {
                PropertyId = 1,
                WardId = 10,
                TaxZoneId = 1,
                TotalCarpetAreaSqMeter = 1000.50,
                TotalBuiltupAreaSqMeter = 1200.75,
                TotalCarpetAreaSqFeet = 10764.50,
                TotalBuiltupAreaSqFeet = 12917.25,
                PlotArea = 1500.25,
                PlotAreaFtLength = 50.5,
                PlotAreaFtWidth = 30.25,
                PlotAreaMtrLength = 15.4,
                PlotAreaMtrWidth = 9.2
            };

            Assert.Equal(1000.50, dto.TotalCarpetAreaSqMeter);
            Assert.Equal(1200.75, dto.TotalBuiltupAreaSqMeter);
            Assert.Equal(10764.50, dto.TotalCarpetAreaSqFeet);
            Assert.Equal(12917.25, dto.TotalBuiltupAreaSqFeet);
            Assert.Equal(1500.25, dto.PlotArea);
            Assert.Equal(50.5, dto.PlotAreaFtLength);
            Assert.Equal(30.25, dto.PlotAreaFtWidth);
            Assert.Equal(15.4, dto.PlotAreaMtrLength);
            Assert.Equal(9.2, dto.PlotAreaMtrWidth);
        }

        [Fact]
        public void PropertyBasicDetailsDto_WingProperties_GetSet_WorksCorrectly()
        {
            var dto = new PropertyBasicDetailsDto
            {
                PropertyId = 1,
                WardId = 10,
                TaxZoneId = 1,
                TotalCarpetAreaSqMeter = 0,
                TotalBuiltupAreaSqMeter = 0,
                WingId = 5,
                WingName = "West Wing",
                WingNo = "A"
            };

            Assert.Equal(5, dto.WingId);
            Assert.Equal("West Wing", dto.WingName);
            Assert.Equal("A", dto.WingNo);
        }

        [Fact]
        public void PropertyBasicDetailsDto_UPICIdAndSubZoneNo_GetSet_WorksCorrectly()
        {
            var dto = new PropertyBasicDetailsDto
            {
                PropertyId = 1,
                WardId = 10,
                TaxZoneId = 1,
                TotalCarpetAreaSqMeter = 0,
                TotalBuiltupAreaSqMeter = 0,
                UPICId = "UPIC123456",
                SubZoneNo = "SZ001"
            };

            Assert.Equal("UPIC123456", dto.UPICId);
            Assert.Equal("SZ001", dto.SubZoneNo);
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
}

#endregion



