using AutoMapper;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Application.Mappings;

/// <summary>
/// Mapping tests for AssetDesignationMappingProfile, focused on the create-vs-update IsActive
/// contract: create now honors client-supplied IsActive (previously silently ignored, forcing
/// every new designation to the entity's true default) and matches the sibling AMS module
/// mapping profiles (AgeFactorCV, AssessmentYearRangeCV, NatureFactorCV, RoomType), all of which
/// let IsActive flow through by convention on create.
/// </summary>
public class AssetDesignationMappingProfileTests
{
    private readonly IMapper _mapper;
    private readonly MapperConfiguration _configuration;

    public AssetDesignationMappingProfileTests()
    {
        _configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetDesignationMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        _mapper = _configuration.CreateMapper();
    }

    // Note: this profile is also covered by the project-wide
    // AutoMapperValidationTest.AutoMapper_Configuration_ShouldBeValid_WithDocumentedUnmappedProperties,
    // which validates every profile via an allowlist of intentionally-unmapped members (Id, CreatedBy,
    // UpdatedBy, etc. — populated by the repository/base service, not by AutoMapper). A bare
    // AssertConfigurationIsValid() here would fail on those pre-existing, already-documented gaps,
    // which are unrelated to the IsActive behavior this file actually verifies.

    #region CreateDto to Entity - IsActive Contract

    [Fact]
    public void Map_CreateDtoToEntity_WithIsActiveTrue_MapsTrue()
    {
        var createDto = new CreateAssetDesignationDto
        {
            OwningDepartmentId = 1,
            DesignationCode = "ENG",
            DesignationName = "Engineer",
            IsActive = true,
            CreatedBy = 100
        };

        var entity = _mapper.Map<AssetDesignationEntity>(createDto);

        Assert.True(entity.IsActive);
    }

    [Fact]
    public void Map_CreateDtoToEntity_WithIsActiveFalse_MapsFalse()
    {
        // Previously ignored: create always produced IsActive = true (the entity's CLR default)
        // regardless of what the client sent. Now the client-supplied value is honored, matching
        // every other AMS create mapping profile and the Update mapping's existing behavior.
        var createDto = new CreateAssetDesignationDto
        {
            OwningDepartmentId = 1,
            DesignationCode = "ENG",
            DesignationName = "Engineer",
            IsActive = false,
            CreatedBy = 100
        };

        var entity = _mapper.Map<AssetDesignationEntity>(createDto);

        Assert.False(entity.IsActive);
    }

    [Fact]
    public void Map_CreateDtoToEntity_WithDefaultDto_IsActiveDefaultsToBoolDefault()
    {
        // CreateBaseDtos.IsActive is a plain bool (not bool?), so an omitted value in a JSON
        // request binds to false — this documents that contract rather than assuming true.
        var createDto = new CreateAssetDesignationDto
        {
            OwningDepartmentId = 1,
            DesignationCode = "ENG",
            DesignationName = "Engineer"
        };

        var entity = _mapper.Map<AssetDesignationEntity>(createDto);

        Assert.False(entity.IsActive);
    }

    #endregion

    #region CreateDto to Entity - Other Ignored/Mapped Members Preserved

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresCreatedDate()
    {
        var createDto = new CreateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer" };

        var entity = _mapper.Map<AssetDesignationEntity>(createDto);

        Assert.Null(entity.CreatedDate);
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresUpdatedDate()
    {
        var createDto = new CreateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer" };

        var entity = _mapper.Map<AssetDesignationEntity>(createDto);

        Assert.Null(entity.UpdatedDate);
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresMarkedForDeletion()
    {
        var createDto = new CreateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer" };

        var entity = _mapper.Map<AssetDesignationEntity>(createDto);

        Assert.False(entity.MarkedForDeletion);
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresMarkedForDeletionDate()
    {
        var createDto = new CreateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer" };

        var entity = _mapper.Map<AssetDesignationEntity>(createDto);

        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Map_CreateDtoToEntity_MapsCreatedBy()
    {
        var createDto = new CreateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer", CreatedBy = 999 };

        var entity = _mapper.Map<AssetDesignationEntity>(createDto);

        Assert.Equal(999, entity.CreatedBy);
    }

    [Fact]
    public void Map_CreateDtoToEntity_MapsAllProperties()
    {
        var createDto = new CreateAssetDesignationDto
        {
            OwningDepartmentId = 1,
            DesignationCode = "ENG",
            DesignationName = "Engineer",
            DesignationLocal = "इंजीनियर",
            DesignationDescription = "Engineering staff",
            IsActive = true,
            CreatedBy = 100
        };

        var entity = _mapper.Map<AssetDesignationEntity>(createDto);

        Assert.Equal(1, entity.OwningDepartmentId);
        Assert.Equal("ENG", entity.DesignationCode);
        Assert.Equal("Engineer", entity.DesignationName);
        Assert.Equal("इंजीनियर", entity.DesignationLocal);
        Assert.Equal("Engineering staff", entity.DesignationDescription);
    }

    #endregion

    #region UpdateDto to Entity - IsActive Still Honored

    [Fact]
    public void Map_UpdateDtoToEntity_MapsIsActive()
    {
        var updateDto = new UpdateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer", IsActive = false, UpdatedBy = 200 };
        var existingEntity = new AssetDesignationEntity { Id = 1, OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer", IsActive = true };

        _mapper.Map(updateDto, existingEntity);

        Assert.False(existingEntity.IsActive);
    }

    [Fact]
    public void Map_UpdateDtoToEntity_IgnoresCreatedDate()
    {
        var updateDto = new UpdateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer", UpdatedBy = 200 };
        var existingEntity = new AssetDesignationEntity { Id = 1, OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer", CreatedDate = DateTime.Now.AddDays(-1) };
        var originalCreatedDate = existingEntity.CreatedDate;

        _mapper.Map(updateDto, existingEntity);

        Assert.Equal(originalCreatedDate, existingEntity.CreatedDate);
    }

    [Fact]
    public void Map_UpdateDtoToEntity_MapsUpdatedBy()
    {
        var updateDto = new UpdateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer", UpdatedBy = 300 };
        var existingEntity = new AssetDesignationEntity { Id = 1, OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer" };

        _mapper.Map(updateDto, existingEntity);

        Assert.Equal(300, existingEntity.UpdatedBy);
    }

    #endregion

    #region Entity to Dto

    [Fact]
    public void Map_EntityToDto_MapsAllProperties()
    {
        var entity = new AssetDesignationEntity
        {
            Id = 1,
            OwningDepartmentId = 1,
            DesignationCode = "ENG",
            DesignationName = "Engineer",
            IsActive = true
        };

        var dto = _mapper.Map<AssetDesignationDto>(entity);

        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.DesignationCode, dto.DesignationCode);
        Assert.Equal(entity.DesignationName, dto.DesignationName);
        Assert.Equal(entity.IsActive, dto.IsActive);
    }

    #endregion
}
