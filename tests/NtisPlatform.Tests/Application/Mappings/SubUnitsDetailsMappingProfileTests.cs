using AutoMapper;
using NtisPlatform.Application.DTOs.Asset_Management.SubUnitsDetails;
using NtisPlatform.Application.Mappings.Asset_Management;
using NtisPlatform.Core.Entities.Asset_Management;
using Xunit;

namespace NtisPlatform.Tests.Application.Mappings;

/// <summary>
/// Mapping tests for SubUnitsDetailsMappingProfile.
///
/// Note: Names and SubAssetCount are Ignore()'d here because they are populated by
/// SubUnitsDetailsService's own Include()+Select() projections (GetByIdAsync/GetAllAsync/
/// GetByAssetIdAsync), never by a plain _mapper.Map(entity) call — see
/// SubUnitsDetailsServiceIntegrationTests for coverage of that projection behavior.
/// This profile is also covered by the project-wide AutoMapperValidationTest, which validates
/// every profile via an allowlist of intentionally-unmapped members.
/// </summary>
public class SubUnitsDetailsMappingProfileTests
{
    private readonly IMapper _mapper;

    public SubUnitsDetailsMappingProfileTests()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<SubUnitsDetailsMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        _mapper = configuration.CreateMapper();
    }

    #region CreateDto to Entity

    [Fact]
    public void Map_CreateDtoToEntity_MapsScalarProperties()
    {
        var createDto = new CreateSubUnitsDetailsDto
        {
            AssetId = 10,
            FloorId = 1,
            SubFloorId = 2,
            ConstructionYear = "2020",
            AssessmentYear = "2024",
            ConstructionTypeId = 3,
            TypeOfUseId = 4,
            SubTypeOfUseId = 5,
            CarpetAreaSqMeter = 50m,
            CarpetAreaSqFeet = 538m,
            NoOfRooms = 2,
            CreatedBy = 100
        };

        var entity = _mapper.Map<SubUnitsDetailsEntity>(createDto);

        Assert.Equal(10, entity.AssetId);
        Assert.Equal(1, entity.FloorId);
        Assert.Equal(2, entity.SubFloorId);
        Assert.Equal("2020", entity.ConstructionYear);
        Assert.Equal("2024", entity.AssessmentYear);
        Assert.Equal(3, entity.ConstructionTypeId);
        Assert.Equal(4, entity.TypeOfUseId);
        Assert.Equal(5, entity.SubTypeOfUseId);
        Assert.Equal(50m, entity.CarpetAreaSqMeter);
        Assert.Equal(2, entity.NoOfRooms);
        Assert.Equal(100, entity.CreatedBy);
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresCreatedDate()
    {
        var entity = _mapper.Map<SubUnitsDetailsEntity>(new CreateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1 });

        Assert.Null(entity.CreatedDate);
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresUpdatedDate()
    {
        var entity = _mapper.Map<SubUnitsDetailsEntity>(new CreateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1 });

        Assert.Null(entity.UpdatedDate);
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresUpdatedBy()
    {
        var entity = _mapper.Map<SubUnitsDetailsEntity>(new CreateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1 });

        Assert.Null(entity.UpdatedBy);
    }

    [Fact]
    public void Map_CreateDtoToEntity_AlwaysSetsMarkedForDeletionFalse()
    {
        var entity = _mapper.Map<SubUnitsDetailsEntity>(new CreateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1 });

        Assert.False(entity.MarkedForDeletion);
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresMarkedForDeletionDate()
    {
        var entity = _mapper.Map<SubUnitsDetailsEntity>(new CreateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1 });

        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresIsRented()
    {
        // IsRented isn't user-supplied on create — it's derived server-side elsewhere.
        var entity = _mapper.Map<SubUnitsDetailsEntity>(new CreateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1 });

        Assert.Null(entity.IsRented);
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresAssetNavigationProperty()
    {
        var entity = _mapper.Map<SubUnitsDetailsEntity>(new CreateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1 });

        Assert.Null(entity.Asset);
    }

    #endregion

    #region UpdateDto to Entity

    [Fact]
    public void Map_UpdateDtoToEntity_MapsScalarPropertiesOntoExistingEntity()
    {
        var existingEntity = new SubUnitsDetailsEntity
        {
            Id = 1,
            AssetId = 10,
            FloorId = 1,
            ConstructionTypeId = 1,
            TypeOfUseId = 1,
            CreatedDate = DateTime.UtcNow.AddDays(-1),
            CreatedBy = 5
        };
        var updateDto = new UpdateSubUnitsDetailsDto
        {
            AssetId = 10,
            FloorId = 2,
            ConstructionTypeId = 3,
            TypeOfUseId = 4,
            CarpetAreaSqMeter = 75m,
            UpdatedBy = 200
        };

        _mapper.Map(updateDto, existingEntity);

        Assert.Equal(2, existingEntity.FloorId);
        Assert.Equal(3, existingEntity.ConstructionTypeId);
        Assert.Equal(4, existingEntity.TypeOfUseId);
        Assert.Equal(75m, existingEntity.CarpetAreaSqMeter);
        Assert.Equal(200, existingEntity.UpdatedBy);
    }

    [Fact]
    public void Map_UpdateDtoToEntity_IgnoresCreatedDateAndCreatedBy()
    {
        var originalCreatedDate = DateTime.UtcNow.AddDays(-3);
        var existingEntity = new SubUnitsDetailsEntity
        {
            Id = 1,
            AssetId = 10,
            FloorId = 1,
            ConstructionTypeId = 1,
            TypeOfUseId = 1,
            CreatedDate = originalCreatedDate,
            CreatedBy = 5
        };
        var updateDto = new UpdateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1, UpdatedBy = 200 };

        _mapper.Map(updateDto, existingEntity);

        Assert.Equal(originalCreatedDate, existingEntity.CreatedDate);
        Assert.Equal(5, existingEntity.CreatedBy);
    }

    [Fact]
    public void Map_UpdateDtoToEntity_IgnoresAssetNavigationProperty()
    {
        var existingEntity = new SubUnitsDetailsEntity { Id = 1, AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1, Asset = new AssetMasterEntity { Id = 10, AssetName = "Existing" } };
        var updateDto = new UpdateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1 };

        _mapper.Map(updateDto, existingEntity);

        Assert.NotNull(existingEntity.Asset);
        Assert.Equal("Existing", existingEntity.Asset!.AssetName);
    }

    [Fact]
    public void Map_UpdateDtoToEntity_IgnoresIsRented()
    {
        var existingEntity = new SubUnitsDetailsEntity { Id = 1, AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1, IsRented = true };
        var updateDto = new UpdateSubUnitsDetailsDto { AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1 };

        _mapper.Map(updateDto, existingEntity);

        Assert.True(existingEntity.IsRented);
    }

    #endregion

    #region Entity to Dto

    [Fact]
    public void Map_EntityToDto_MapsScalarProperties()
    {
        var entity = new SubUnitsDetailsEntity
        {
            Id = 1,
            AssetId = 10,
            FloorId = 1,
            ConstructionTypeId = 1,
            TypeOfUseId = 1,
            CarpetAreaSqMeter = 50m,
            CapitalValue = 100000m,
            BaseValue = 90000m,
            IsActive = true,
            MarkedForDeletion = false
        };

        var dto = _mapper.Map<SubUnitsDetailsDto>(entity);

        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.AssetId, dto.AssetId);
        Assert.Equal(entity.FloorId, dto.FloorId);
        Assert.Equal(entity.CarpetAreaSqMeter, dto.CarpetAreaSqMeter);
        Assert.Equal(entity.CapitalValue, dto.CapitalValue);
        Assert.Equal(entity.BaseValue, dto.BaseValue);
        Assert.Equal(entity.IsActive, dto.IsActive);
    }

    [Fact]
    public void Map_EntityToDto_NamesDefaultsToEmptyObject_NotPopulatedByPlainMap()
    {
        // Names is only populated by the service's own Include()+Select() projection
        // (see SubUnitsDetailsServiceIntegrationTests) — a bare _mapper.Map(entity) never
        // resolves display names since AutoMapper Ignore()s the member entirely.
        var entity = new SubUnitsDetailsEntity
        {
            Id = 1,
            AssetId = 10,
            FloorId = 1,
            ConstructionTypeId = 1,
            TypeOfUseId = 1
        };

        var dto = _mapper.Map<SubUnitsDetailsDto>(entity);

        Assert.NotNull(dto.Names);
        Assert.Null(dto.Names.AssetName);
        Assert.Null(dto.Names.FloorName);
    }

    [Fact]
    public void Map_EntityToDto_SubAssetCountDefaultsToZero_NotPopulatedByPlainMap()
    {
        var entity = new SubUnitsDetailsEntity { Id = 1, AssetId = 10, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1 };

        var dto = _mapper.Map<SubUnitsDetailsDto>(entity);

        Assert.Equal(0, dto.SubAssetCount);
    }

    #endregion
}
