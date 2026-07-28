using AutoMapper;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Application.Mappings;

/// <summary>
/// Mapping tests for AssetApplicationTypeMappingProfile, following the same create-vs-update
/// IsActive contract as the sibling AMS module mapping profiles (AssetDesignation,
/// AssetConditionMaster): create honors client-supplied IsActive rather than silently defaulting.
/// </summary>
public class AssetApplicationTypeMappingProfileTests
{
    private readonly IMapper _mapper;
    private readonly MapperConfiguration _configuration;

    public AssetApplicationTypeMappingProfileTests()
    {
        _configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetApplicationTypeMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        _mapper = _configuration.CreateMapper();
    }

    // Note: this profile is also covered by the project-wide
    // AutoMapperValidationTest.AutoMapper_Configuration_ShouldBeValid_WithDocumentedUnmappedProperties,
    // which validates every profile via an allowlist of intentionally-unmapped members (Id, CreatedBy,
    // UpdatedBy, etc. — populated by the repository/base service, not by AutoMapper).

    #region CreateDto to Entity - IsActive Contract

    [Fact]
    public void Map_CreateDtoToEntity_WithIsActiveTrue_MapsTrue()
    {
        var createDto = new CreateAssetApplicationTypeDto
        {
            ApplicationTypeCode = "NEW",
            ApplicationTypeName = "New Construction",
            IsActive = true,
            CreatedBy = 100
        };

        var entity = _mapper.Map<AssetApplicationTypeEntity>(createDto);

        Assert.True(entity.IsActive);
    }

    [Fact]
    public void Map_CreateDtoToEntity_WithIsActiveFalse_MapsFalse()
    {
        var createDto = new CreateAssetApplicationTypeDto
        {
            ApplicationTypeCode = "NEW",
            ApplicationTypeName = "New Construction",
            IsActive = false,
            CreatedBy = 100
        };

        var entity = _mapper.Map<AssetApplicationTypeEntity>(createDto);

        Assert.False(entity.IsActive);
    }

    [Fact]
    public void Map_CreateDtoToEntity_WithDefaultDto_IsActiveDefaultsToBoolDefault()
    {
        // CreateBaseDtos.IsActive is a plain bool (not bool?), so an omitted value in a JSON
        // request binds to false — this documents that contract rather than assuming true.
        var createDto = new CreateAssetApplicationTypeDto
        {
            ApplicationTypeCode = "NEW",
            ApplicationTypeName = "New Construction"
        };

        var entity = _mapper.Map<AssetApplicationTypeEntity>(createDto);

        Assert.False(entity.IsActive);
    }

    #endregion

    #region CreateDto to Entity - Other Ignored/Mapped Members Preserved

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresCreatedDate()
    {
        var createDto = new CreateAssetApplicationTypeDto { ApplicationTypeCode = "NEW", ApplicationTypeName = "New Construction" };

        var entity = _mapper.Map<AssetApplicationTypeEntity>(createDto);

        Assert.Null(entity.CreatedDate);
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresUpdatedDate()
    {
        var createDto = new CreateAssetApplicationTypeDto { ApplicationTypeCode = "NEW", ApplicationTypeName = "New Construction" };

        var entity = _mapper.Map<AssetApplicationTypeEntity>(createDto);

        Assert.Null(entity.UpdatedDate);
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresMarkedForDeletion()
    {
        var createDto = new CreateAssetApplicationTypeDto { ApplicationTypeCode = "NEW", ApplicationTypeName = "New Construction" };

        var entity = _mapper.Map<AssetApplicationTypeEntity>(createDto);

        Assert.False(entity.MarkedForDeletion);
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresMarkedForDeletionDate()
    {
        var createDto = new CreateAssetApplicationTypeDto { ApplicationTypeCode = "NEW", ApplicationTypeName = "New Construction" };

        var entity = _mapper.Map<AssetApplicationTypeEntity>(createDto);

        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Map_CreateDtoToEntity_MapsCreatedBy()
    {
        var createDto = new CreateAssetApplicationTypeDto { ApplicationTypeCode = "NEW", ApplicationTypeName = "New Construction", CreatedBy = 999 };

        var entity = _mapper.Map<AssetApplicationTypeEntity>(createDto);

        Assert.Equal(999, entity.CreatedBy);
    }

    [Fact]
    public void Map_CreateDtoToEntity_MapsAllProperties()
    {
        var createDto = new CreateAssetApplicationTypeDto
        {
            ApplicationTypeCode = "NEW",
            ApplicationTypeName = "New Construction",
            Description = "Permits issued for new construction",
            DisplayOrder = 1,
            IsActive = true,
            CreatedBy = 100
        };

        var entity = _mapper.Map<AssetApplicationTypeEntity>(createDto);

        Assert.Equal("NEW", entity.ApplicationTypeCode);
        Assert.Equal("New Construction", entity.ApplicationTypeName);
        Assert.Equal("Permits issued for new construction", entity.Description);
        Assert.Equal(1, entity.DisplayOrder);
    }

    #endregion

    #region UpdateDto to Entity - IsActive Still Honored

    [Fact]
    public void Map_UpdateDtoToEntity_MapsIsActive()
    {
        var updateDto = new UpdateAssetApplicationTypeDto { ApplicationTypeCode = "NEW", ApplicationTypeName = "New Construction", IsActive = false, UpdatedBy = 200 };
        var existingEntity = new AssetApplicationTypeEntity { Id = 1, ApplicationTypeCode = "NEW", ApplicationTypeName = "New Construction", IsActive = true };

        _mapper.Map(updateDto, existingEntity);

        Assert.False(existingEntity.IsActive);
    }

    [Fact]
    public void Map_UpdateDtoToEntity_IgnoresCreatedDate()
    {
        var updateDto = new UpdateAssetApplicationTypeDto { ApplicationTypeCode = "NEW", ApplicationTypeName = "New Construction", UpdatedBy = 200 };
        var existingEntity = new AssetApplicationTypeEntity { Id = 1, ApplicationTypeCode = "NEW", ApplicationTypeName = "New Construction", CreatedDate = DateTime.Now.AddDays(-1) };
        var originalCreatedDate = existingEntity.CreatedDate;

        _mapper.Map(updateDto, existingEntity);

        Assert.Equal(originalCreatedDate, existingEntity.CreatedDate);
    }

    [Fact]
    public void Map_UpdateDtoToEntity_MapsUpdatedBy()
    {
        var updateDto = new UpdateAssetApplicationTypeDto { ApplicationTypeCode = "NEW", ApplicationTypeName = "New Construction", UpdatedBy = 300 };
        var existingEntity = new AssetApplicationTypeEntity { Id = 1, ApplicationTypeCode = "NEW", ApplicationTypeName = "New Construction" };

        _mapper.Map(updateDto, existingEntity);

        Assert.Equal(300, existingEntity.UpdatedBy);
    }

    #endregion

    #region Entity to Dto

    [Fact]
    public void Map_EntityToDto_MapsAllProperties()
    {
        var entity = new AssetApplicationTypeEntity
        {
            Id = 1,
            ApplicationTypeCode = "NEW",
            ApplicationTypeName = "New Construction",
            Description = "Permits issued for new construction",
            DisplayOrder = 1,
            IsActive = true
        };

        var dto = _mapper.Map<AssetApplicationTypeDto>(entity);

        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.ApplicationTypeCode, dto.ApplicationTypeCode);
        Assert.Equal(entity.ApplicationTypeName, dto.ApplicationTypeName);
        Assert.Equal(entity.Description, dto.Description);
        Assert.Equal(entity.DisplayOrder, dto.DisplayOrder);
        Assert.Equal(entity.IsActive, dto.IsActive);
    }

    #endregion
}
