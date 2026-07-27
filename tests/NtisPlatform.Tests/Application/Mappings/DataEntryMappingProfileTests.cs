using AutoMapper;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Core.Entities;
using Xunit;

namespace NtisPlatform.Tests.Application.Mappings;

/// <summary>
/// Tests for DataEntryMappingProfile AutoMapper configuration
/// Verifies all mapping configurations are valid
/// </summary>
public class DataEntryMappingProfileTests
{
    private readonly IMapper _mapper;
    private readonly MapperConfiguration _configuration;

    public DataEntryMappingProfileTests()
    {
        _configuration = new MapperConfiguration(cfg => {
            cfg.AddProfile<DataEntryMappingProfile>();
            cfg.AddProfile<RenterDetailMappingProfile>();
            cfg.AddProfile<RenterMastMappingProfile>();
            cfg.AddProfile<RoomWiseSubmissionDetailsMappingProfile>();
         }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        _mapper = _configuration.CreateMapper();
    }

    [Fact]
    public void DataEntryMappingProfile_Configuration_IsValid()
    {
        // Act & Assert
        _configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_PropertyDetailsEntity_To_PropertyDetailsDto()
    {
        // Arrange
        var entity = new PropertyDetailsEntity
        {
            Id = 1,
            PropertyId = 100,
            FloorId = 1,
            SubFloorId = 1,
            ConstructionTypeId = 1,
            TypeOfUseId = 1,
            SubTypeOfUseId = 1,
            IsActive = true,
            MarkedForDeletion = false,
            RenterDetails = new List<RenterDetailEntity>(),
            Renters = new List<RenterMastEntity>(),
            RoomWiseSubmissionDetails = new List<RoomWiseSubmissionDetailsEntity>()
        };

        // Act
        var dto = _mapper.Map<PropertyDetailsDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.PropertyId, dto.PropertyId);
        Assert.Equal(entity.FloorId, dto.FloorId);
        Assert.Equal(entity.SubFloorId, dto.SubFloorId);
        Assert.Equal(entity.ConstructionTypeId, dto.ConstructionTypeId);
        Assert.Equal(entity.TypeOfUseId, dto.TypeOfUseId);
        Assert.Equal(entity.SubTypeOfUseId, dto.SubTypeOfUseId);
    }

    [Fact]
    public void Map_CreatePropertyDetailsDto_To_PropertyDetailsEntity()
    {
        // Arrange
        var createDto = new CreatePropertyDetailsDto
        {
            PropertyId = 100,
            FloorId = 1,
            SubFloorId = 1,
            ConstructionTypeId = 1,
            TypeOfUseId = 1,
            SubTypeOfUseId = 1
        };

        // Act
        var entity = _mapper.Map<PropertyDetailsEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal(createDto.PropertyId, entity.PropertyId);
        Assert.Equal(createDto.FloorId, entity.FloorId);
        Assert.Equal(createDto.SubFloorId, entity.SubFloorId);
        Assert.Equal(createDto.ConstructionTypeId, entity.ConstructionTypeId);
        Assert.Equal(createDto.TypeOfUseId, entity.TypeOfUseId);
        Assert.Equal(createDto.SubTypeOfUseId, entity.SubTypeOfUseId);
    }

    [Fact]
    public void Map_UpdatePropertyDetailsDto_To_PropertyDetailsEntity()
    {
        // Arrange
        var updateDto = new UpdatePropertyDetailsDto
        {
            PropertyId = 100,
            FloorId = 2,
            SubFloorId = 2,
            ConstructionTypeId = 2,
            TypeOfUseId = 2,
            SubTypeOfUseId = 2
        };

        var existingEntity = new PropertyDetailsEntity
        {
            Id = 1,
            PropertyId = 50,
            FloorId = 1,
            SubFloorId = 1,
            ConstructionTypeId = 1,
            TypeOfUseId = 1,
            SubTypeOfUseId = 1
        };

        // Act
        _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.Equal(1, existingEntity.Id); // ID should not change
        Assert.Equal(updateDto.PropertyId, existingEntity.PropertyId);
        Assert.Equal(updateDto.FloorId, existingEntity.FloorId);
        Assert.Equal(updateDto.SubFloorId, existingEntity.SubFloorId);
        Assert.Equal(updateDto.ConstructionTypeId, existingEntity.ConstructionTypeId);
        Assert.Equal(updateDto.TypeOfUseId, existingEntity.TypeOfUseId);
        Assert.Equal(updateDto.SubTypeOfUseId, existingEntity.SubTypeOfUseId);
    }

    [Fact]
    public void Map_PropertyDetailsEntity_WithNestedCollections_To_PropertyDetailsDto()
    {
        // Arrange
        var entity = new PropertyDetailsEntity
        {
            Id = 1,
            PropertyId = 100,
            RenterDetails = new List<RenterDetailEntity>
            {
                new RenterDetailEntity { Id = 1, PropertyDetailsId = 1 },
                new RenterDetailEntity { Id = 2, PropertyDetailsId = 1 }
            },
            Renters = new List<RenterMastEntity>
            {
                new RenterMastEntity { Id = 1, PropertyDetailsId = 1 }
            },
            RoomWiseSubmissionDetails = new List<RoomWiseSubmissionDetailsEntity>
            {
                new RoomWiseSubmissionDetailsEntity { Id = 1, PropertyDetailsId = 1 }
            }
        };

        // Act
        var dto = _mapper.Map<PropertyDetailsDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.NotNull(dto.RenterDetails);
        Assert.NotNull(dto.Renters);
        Assert.NotNull(dto.RoomWiseSubmissionDetails);
        Assert.Equal(2, dto.RenterDetails.Count);
        Assert.Single(dto.Renters);
        Assert.Single(dto.RoomWiseSubmissionDetails);
    }

    [Fact]
    public void Map_PropertyDetailsEntity_WithNullNavigationProperties_To_PropertyDetailsDto()
    {
        // Arrange
        var entity = new PropertyDetailsEntity
        {
            Id = 1,
            PropertyId = 100,
            FloorId = 1,
            RenterDetails = null,
            Renters = null,
            RoomWiseSubmissionDetails = null
        };

        // Act
        var dto = _mapper.Map<PropertyDetailsDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(1, dto.Id);
        // Nested collections should either be null or empty depending on mapping configuration
    }
}
