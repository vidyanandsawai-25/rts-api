using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Application.Mappings;

/// <summary>
/// Comprehensive mapping tests for FloorGroupMappingProfile to achieve 100% code coverage
/// </summary>
public class FloorGroupMappingProfileTests
{
    private readonly IMapper _mapper;
    private readonly MapperConfiguration _configuration;

    public FloorGroupMappingProfileTests()
    {
        _configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<FloorGroupMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        _mapper = _configuration.CreateMapper();
    }

    #region Configuration Tests

    [Fact]
    public void MappingProfile_Configuration_IsValid()
    {
        // Act & Assert
        _configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void MappingProfile_CanCreateMapper()
    {
        // Act
        var mapper = _configuration.CreateMapper();

        // Assert
        Assert.NotNull(mapper);
    }

    #endregion

    #region Entity to Dto Mapping Tests

    [Fact]
    public void Map_EntityToDto_MapsAllProperties()
    {
        // Arrange
        var entity = new FloorGroupMasterEntity
        {
            Id = 1,
            FloorGroup = "Ground Floor",
            IsActive = true,
            CreatedBy = 100,
            CreatedDate = DateTime.Now,
            UpdatedBy = 200,
            UpdatedDate = DateTime.Now.AddHours(1)
        };

        // Act
        var dto = _mapper.Map<FloorGroupDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.FloorGroup, dto.FloorGroup);
        Assert.Equal(entity.IsActive, dto.IsActive);
        Assert.Equal(entity.CreatedDate, dto.CreatedDate);
        Assert.Equal(entity.UpdatedDate, dto.UpdatedDate);
    }

    [Fact]
    public void Map_EntityToDto_WithNullOptionalFields_MapsCorrectly()
    {
        // Arrange
        var entity = new FloorGroupMasterEntity
        {
            Id = 1,
            FloorGroup = "First Floor",
            IsActive = true,
            CreatedBy = null,
            CreatedDate = null,
            UpdatedBy = null,
            UpdatedDate = null
        };

        // Act
        var dto = _mapper.Map<FloorGroupDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(1, dto.Id);
        Assert.Equal("First Floor", dto.FloorGroup);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public void Map_EntityToDto_MultipleEntities_MapsAllCorrectly()
    {
        // Arrange
        var entities = new List<FloorGroupMasterEntity>
        {
            new() { Id = 1, FloorGroup = "Ground Floor", IsActive = true },
            new() { Id = 2, FloorGroup = "First Floor", IsActive = true },
            new() { Id = 3, FloorGroup = "Second Floor", IsActive = false }
        };

        // Act
        var dtos = _mapper.Map<List<FloorGroupDto>>(entities);

        // Assert
        Assert.NotNull(dtos);
        Assert.Equal(3, dtos.Count);
        Assert.Equal("Ground Floor", dtos[0].FloorGroup);
        Assert.Equal("First Floor", dtos[1].FloorGroup);
        Assert.Equal("Second Floor", dtos[2].FloorGroup);
        Assert.False(dtos[2].IsActive);
    }

    [Fact]
    public void Map_EntityToDto_WithEmptyString_MapsCorrectly()
    {
        // Arrange
        var entity = new FloorGroupMasterEntity
        {
            Id = 1,
            FloorGroup = "",
            IsActive = true
        };

        // Act
        var dto = _mapper.Map<FloorGroupDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("", dto.FloorGroup);
    }

    #endregion

    #region CreateDto to Entity Mapping Tests

    [Fact]
    public void Map_CreateDtoToEntity_MapsAllProperties()
    {
        // Arrange
        var createDto = new CreateFloorGroupDto
        {
            FloorGroup = "Basement",
            CreatedBy = 100
        };

        // Act
        var entity = _mapper.Map<FloorGroupMasterEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal(createDto.FloorGroup, entity.FloorGroup);
        Assert.Equal(createDto.CreatedBy, entity.CreatedBy);
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresCreatedDate()
    {
        // Arrange
        var createDto = new CreateFloorGroupDto
        {
            FloorGroup = "Terrace",
            CreatedBy = 100
        };

        // Act
        var entity = _mapper.Map<FloorGroupMasterEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Null(entity.CreatedDate); // Should be ignored by mapping
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresUpdatedDate()
    {
        // Arrange
        var createDto = new CreateFloorGroupDto
        {
            FloorGroup = "Mezzanine",
            CreatedBy = 100
        };

        // Act
        var entity = _mapper.Map<FloorGroupMasterEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Null(entity.UpdatedDate); // Should be ignored by mapping
    }

    [Fact]
    public void Map_CreateDtoToEntity_PreservesCreatedBy()
    {
        // Arrange
        var createDto = new CreateFloorGroupDto
        {
            FloorGroup = "Attic",
            CreatedBy = 999
        };

        // Act
        var entity = _mapper.Map<FloorGroupMasterEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal(999, entity.CreatedBy);
    }

    #endregion

    #region UpdateDto to Entity Mapping Tests

    [Fact]
    public void Map_UpdateDtoToEntity_MapsAllProperties()
    {
        // Arrange
        var updateDto = new UpdateFloorGroupDto
        {
            FloorGroup = "Updated Floor",
            UpdatedBy = 200
        };

        var existingEntity = new FloorGroupMasterEntity
        {
            Id = 1,
            FloorGroup = "Original Floor",
            IsActive = true,
            CreatedBy = 100,
            CreatedDate = DateTime.Now.AddDays(-1)
        };

        // Act
        var entity = _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal(updateDto.FloorGroup, entity.FloorGroup);
        Assert.Equal(updateDto.UpdatedBy, entity.UpdatedBy);
        Assert.Equal(1, entity.Id); // Should preserve existing Id
    }

    [Fact]
    public void Map_UpdateDtoToEntity_IgnoresCreatedDate()
    {
        // Arrange
        var updateDto = new UpdateFloorGroupDto
        {
            FloorGroup = "Modified Floor",
            UpdatedBy = 200
        };

        var existingEntity = new FloorGroupMasterEntity
        {
            Id = 1,
            FloorGroup = "Old Floor",
            CreatedDate = DateTime.Now.AddDays(-10)
        };

        var originalCreatedDate = existingEntity.CreatedDate;

        // Act
        _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.Equal(originalCreatedDate, existingEntity.CreatedDate); // Should remain unchanged
    }

    [Fact]
    public void Map_UpdateDtoToEntity_IgnoresUpdatedDate()
    {
        // Arrange
        var updateDto = new UpdateFloorGroupDto
        {
            FloorGroup = "New Name",
            UpdatedBy = 200
        };

        var existingEntity = new FloorGroupMasterEntity
        {
            Id = 1,
            FloorGroup = "Old Name",
            UpdatedDate = DateTime.Now.AddHours(-5)
        };

        var originalUpdatedDate = existingEntity.UpdatedDate;

        // Act
        _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.Equal(originalUpdatedDate, existingEntity.UpdatedDate); // Should remain unchanged
    }

    [Fact]
    public void Map_UpdateDtoToEntity_IgnoresIsActive()
    {
        // Arrange
        var updateDto = new UpdateFloorGroupDto
        {
            FloorGroup = "Active Floor",
            UpdatedBy = 200
        };

        var existingEntity = new FloorGroupMasterEntity
        {
            Id = 1,
            FloorGroup = "Original Floor",
            IsActive = true
        };

        // Act
        _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.True(existingEntity.IsActive); // Should remain unchanged
    }

    [Fact]
    public void Map_UpdateDtoToEntity_PreservesExistingId()
    {
        // Arrange
        var updateDto = new UpdateFloorGroupDto
        {
            FloorGroup = "Some Floor",
            UpdatedBy = 300
        };

        var existingEntity = new FloorGroupMasterEntity
        {
            Id = 42,
            FloorGroup = "Existing Floor"
        };

        // Act
        _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.Equal(42, existingEntity.Id);
    }

    [Fact]
    public void Map_UpdateDtoToEntity_UpdatesOnlyAllowedFields()
    {
        // Arrange
        var updateDto = new UpdateFloorGroupDto
        {
            FloorGroup = "Changed Floor Group",
            UpdatedBy = 500
        };

        var existingEntity = new FloorGroupMasterEntity
        {
            Id = 5,
            FloorGroup = "Original Value",
            IsActive = true,
            CreatedBy = 100,
            CreatedDate = DateTime.Now.AddMonths(-1),
            UpdatedBy = 200,
            UpdatedDate = DateTime.Now.AddDays(-2)
        };

        var originalCreatedBy = existingEntity.CreatedBy;
        var originalCreatedDate = existingEntity.CreatedDate;
        var originalIsActive = existingEntity.IsActive;
        var originalId = existingEntity.Id;

        // Act
        _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.Equal("Changed Floor Group", existingEntity.FloorGroup); // Updated
        Assert.Equal(500, existingEntity.UpdatedBy); // Updated
        Assert.Equal(originalId, existingEntity.Id); // Preserved
        Assert.Equal(originalCreatedBy, existingEntity.CreatedBy); // Preserved
        Assert.Equal(originalCreatedDate, existingEntity.CreatedDate); // Preserved
        Assert.Equal(originalIsActive, existingEntity.IsActive); // Preserved
    }

    #endregion

    #region Null Handling Tests

    [Fact]
    public void Map_NullEntity_ReturnsNull()
    {
        // Arrange
        FloorGroupMasterEntity? entity = null;

        // Act
        var dto = _mapper.Map<FloorGroupDto>(entity);

        // Assert
        Assert.Null(dto);
    }

    [Fact]
    public void Map_NullCreateDto_ReturnsNull()
    {
        // Arrange
        CreateFloorGroupDto? createDto = null;

        // Act
        var entity = _mapper.Map<FloorGroupMasterEntity>(createDto);

        // Assert
        Assert.Null(entity);
    }

    [Fact]
    public void Map_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var entities = new List<FloorGroupMasterEntity>();

        // Act
        var dtos = _mapper.Map<List<FloorGroupDto>>(entities);

        // Assert
        Assert.NotNull(dtos);
        Assert.Empty(dtos);
    }

    #endregion
}
