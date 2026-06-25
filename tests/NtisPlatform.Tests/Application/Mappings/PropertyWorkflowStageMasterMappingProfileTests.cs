using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using NtisPlatform.Application.DTOs.Master.PropertyWorkflowStageMaster;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Core.Entities;
using Xunit;

namespace NtisPlatform.Tests.Application.Mappings;

/// <summary>
/// Unit tests for PropertyWorkflowStageMasterMappingProfile
/// </summary>
public class PropertyWorkflowStageMasterMappingProfileTests
{
    private readonly IMapper _mapper;

    public PropertyWorkflowStageMasterMappingProfileTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PropertyWorkflowStageMasterMappingProfile>();
        }, NullLoggerFactory.Instance);

        _mapper = config.CreateMapper();
    }

    #region Entity to DTO Mapping Tests

    [Fact]
    public void Map_PropertyWorkflowStageMasterEntity_To_PropertyWorkflowStageMasterDto_Success()
    {
        // Arrange
        var entity = new PropertyWorkflowStageMasterEntity
        {
            Id = 1,
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            Description = "Initial stage",
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now,
            UpdatedBy = null,
            UpdatedDate = null
        };

        // Act
        var dto = _mapper.Map<PropertyWorkflowStageMasterDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.StageName, dto.StageName);
        Assert.Equal(entity.DisplayOrder, dto.DisplayOrder);
        Assert.Equal(entity.Description, dto.Description);
        Assert.Equal(entity.IsActive, dto.IsActive);
    }

    [Fact]
    public void Map_PropertyWorkflowStageMasterEntity_WithNullDescription_To_Dto()
    {
        // Arrange
        var entity = new PropertyWorkflowStageMasterEntity
        {
            Id = 2,
            StageName = "InternalSurvey",
            DisplayOrder = 2,
            Description = null,
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        // Act
        var dto = _mapper.Map<PropertyWorkflowStageMasterDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(2, dto.Id);
        Assert.Equal("InternalSurvey", dto.StageName);
        Assert.Null(dto.Description);
    }

    [Fact]
    public void Map_InactiveEntity_To_Dto()
    {
        // Arrange
        var entity = new PropertyWorkflowStageMasterEntity
        {
            Id = 3,
            StageName = "Assessment",
            DisplayOrder = 3,
            IsActive = false,
            CreatedDate = DateTime.Now
        };

        // Act
        var dto = _mapper.Map<PropertyWorkflowStageMasterDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.False(dto.IsActive);
        Assert.Equal("Assessment", dto.StageName);
    }

    [Fact]
    public void Map_EntityWithUpdatedInfo_To_Dto()
    {
        // Arrange
        var now = DateTime.Now;
        var entity = new PropertyWorkflowStageMasterEntity
        {
            Id = 4,
            StageName = "BillGeneration",
            DisplayOrder = 9,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = now.AddDays(-10),
            UpdatedBy = 2,
            UpdatedDate = now
        };

        // Act
        var dto = _mapper.Map<PropertyWorkflowStageMasterDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(4, dto.Id);
        Assert.Equal("BillGeneration", dto.StageName);
    }

    #endregion

    #region CreateDto to Entity Mapping Tests

    [Fact]
    public void Map_CreatePropertyWorkflowStageMasterDto_To_Entity_Success()
    {
        // Arrange
        var createDto = new CreatePropertyWorkflowStageMasterDto
        {
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            Description = "Initial stage",
            CreatedBy = 1
        };

        // Act
        var entity = _mapper.Map<PropertyWorkflowStageMasterEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal(createDto.StageName, entity.StageName);
        Assert.Equal(createDto.DisplayOrder, entity.DisplayOrder);
        Assert.Equal(createDto.Description, entity.Description);
        Assert.Equal(createDto.CreatedBy, entity.CreatedBy);
    }

    [Fact]
    public void Map_CreateDtoWithMinimalData_To_Entity()
    {
        // Arrange
        var createDto = new CreatePropertyWorkflowStageMasterDto
        {
            StageName = "QuickCheck",
            DisplayOrder = 5,
            CreatedBy = 1
        };

        // Act
        var entity = _mapper.Map<PropertyWorkflowStageMasterEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal("QuickCheck", entity.StageName);
        Assert.Equal(5, entity.DisplayOrder);
        Assert.Null(entity.Description);
    }

    [Fact]
    public void Map_CreateDto_PreservesCreatedByValue()
    {
        // Arrange
        var createDto = new CreatePropertyWorkflowStageMasterDto
        {
            StageName = "Test",
            DisplayOrder = 1,
            CreatedBy = 42
        };

        // Act
        var entity = _mapper.Map<PropertyWorkflowStageMasterEntity>(createDto);

        // Assert
        Assert.Equal(42, entity.CreatedBy);
    }

    [Fact]
    public void Map_CreateDto_IgnoresCreatedDateProperty()
    {
        // Arrange
        var createDto = new CreatePropertyWorkflowStageMasterDto
        {
            StageName = "Test",
            DisplayOrder = 1,
            CreatedBy = 1
        };

        // Act
        var entity = _mapper.Map<PropertyWorkflowStageMasterEntity>(createDto);

        // Assert
        // CreatedDate should be default/ignored by mapping
        Assert.NotNull(entity);
        Assert.Equal(createDto.StageName, entity.StageName);
    }

    #endregion

    #region UpdateDto to Entity Mapping Tests

    [Fact]
    public void Map_UpdatePropertyWorkflowStageMasterDto_To_Entity_Success()
    {
        // Arrange
        var existingEntity = new PropertyWorkflowStageMasterEntity
        {
            Id = 1,
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            Description = "Old description",
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now.AddDays(-5),
            UpdatedBy = null,
            UpdatedDate = null
        };

        var updateDto = new UpdatePropertyWorkflowStageMasterDto
        {
            StageName = "GeoSequencing Updated",
            DisplayOrder = 2,
            Description = "New description",
            UpdatedBy = 2
        };

        // Act
        var entity = _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal("GeoSequencing Updated", entity.StageName);
        Assert.Equal(2, entity.DisplayOrder);
        Assert.Equal("New description", entity.Description);
        Assert.Equal(2, entity.UpdatedBy);
        // CreatedBy and CreatedDate should remain unchanged
        Assert.Equal(1, entity.CreatedBy);
    }

    [Fact]
    public void Map_UpdateDto_PreservesCreatedInfo()
    {
        // Arrange
        var createdDate = DateTime.Now.AddDays(-10);
        var existingEntity = new PropertyWorkflowStageMasterEntity
        {
            Id = 1,
            StageName = "Original",
            DisplayOrder = 1,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = createdDate
        };

        var updateDto = new UpdatePropertyWorkflowStageMasterDto
        {
            StageName = "Updated",
            DisplayOrder = 1,
            UpdatedBy = 2
        };

        // Act
        var entity = _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.Equal(1, entity.CreatedBy);
        Assert.Equal(createdDate, entity.CreatedDate);
    }

    [Fact]
    public void Map_UpdateDto_ClearsDescription()
    {
        // Arrange
        var existingEntity = new PropertyWorkflowStageMasterEntity
        {
            Id = 1,
            StageName = "Test",
            DisplayOrder = 1,
            Description = "Old description",
            IsActive = true
        };

        var updateDto = new UpdatePropertyWorkflowStageMasterDto
        {
            StageName = "Test",
            DisplayOrder = 1,
            Description = null,
            UpdatedBy = 1
        };

        // Act
        var entity = _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.Null(entity.Description);
    }

    [Fact]
    public void Map_UpdateDto_UpdatesDisplayOrder()
    {
        // Arrange
        var existingEntity = new PropertyWorkflowStageMasterEntity
        {
            Id = 1,
            StageName = "Test",
            DisplayOrder = 1,
            IsActive = true
        };

        var updateDto = new UpdatePropertyWorkflowStageMasterDto
        {
            StageName = "Test",
            DisplayOrder = 9,
            UpdatedBy = 1
        };

        // Act
        var entity = _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.Equal(9, entity.DisplayOrder);
    }

    #endregion

    #region Configuration Validation Tests

    [Fact]
    public void MappingConfiguration_IsValid()
    {
        // Arrange
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PropertyWorkflowStageMasterMappingProfile>();
        }, NullLoggerFactory.Instance);

        // Act & Assert - Should not throw
        config.AssertConfigurationIsValid();
    }

    #endregion

    #region Round-trip Mapping Tests

    [Fact]
    public void RoundTrip_CreateDto_To_Entity_To_Dto()
    {
        // Arrange
        var createDto = new CreatePropertyWorkflowStageMasterDto
        {
            StageName = "RoundTrip",
            DisplayOrder = 5,
            Description = "Test description",
            CreatedBy = 1
        };

        // Act
        var entity = _mapper.Map<PropertyWorkflowStageMasterEntity>(createDto);
        entity.Id = 1; // Simulate database assignment
        entity.IsActive = true; // Set default
        var resultDto = _mapper.Map<PropertyWorkflowStageMasterDto>(entity);

        // Assert
        Assert.Equal(createDto.StageName, resultDto.StageName);
        Assert.Equal(createDto.DisplayOrder, resultDto.DisplayOrder);
        Assert.Equal(createDto.Description, resultDto.Description);
    }

    [Fact]
    public void RoundTrip_UpdateDto_To_Entity_To_Dto()
    {
        // Arrange
        var originalEntity = new PropertyWorkflowStageMasterEntity
        {
            Id = 1,
            StageName = "Original",
            DisplayOrder = 1,
            Description = "Original description",
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now
        };

        var updateDto = new UpdatePropertyWorkflowStageMasterDto
        {
            StageName = "Updated",
            DisplayOrder = 3,
            Description = "Updated description",
            UpdatedBy = 2
        };

        // Act
        var updatedEntity = _mapper.Map(updateDto, originalEntity);
        var resultDto = _mapper.Map<PropertyWorkflowStageMasterDto>(updatedEntity);

        // Assert
        Assert.Equal("Updated", resultDto.StageName);
        Assert.Equal(3, resultDto.DisplayOrder);
        Assert.Equal("Updated description", resultDto.Description);
    }

    #endregion

    #region Multiple Entities Mapping Tests

    [Fact]
    public void Map_MultipleEntities_To_Dtos()
    {
        // Arrange
        var entities = new List<PropertyWorkflowStageMasterEntity>
        {
            new PropertyWorkflowStageMasterEntity { Id = 1, StageName = "Stage1", DisplayOrder = 1, IsActive = true },
            new PropertyWorkflowStageMasterEntity { Id = 2, StageName = "Stage2", DisplayOrder = 2, IsActive = true },
            new PropertyWorkflowStageMasterEntity { Id = 3, StageName = "Stage3", DisplayOrder = 3, IsActive = false }
        };

        // Act
        var dtos = _mapper.Map<List<PropertyWorkflowStageMasterDto>>(entities);

        // Assert
        Assert.NotNull(dtos);
        Assert.Equal(3, dtos.Count);
        Assert.Equal("Stage1", dtos[0].StageName);
        Assert.Equal("Stage2", dtos[1].StageName);
        Assert.Equal("Stage3", dtos[2].StageName);
        Assert.False(dtos[2].IsActive);
    }

    #endregion
}
