using AutoMapper;
using NtisPlatform.Application.DTOs.Master.PropertyAssessmentStatus;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Application.Mappings;

/// <summary>
/// Tests for PropertyAssessmentStatusMappingProfile AutoMapper configuration
/// Verifies all mapping configurations are valid and cover 100% of mapping scenarios
/// </summary>
public class PropertyAssessmentStatusMappingProfileTests
{
    private readonly IMapper _mapper;
    private readonly MapperConfiguration _configuration;

    public PropertyAssessmentStatusMappingProfileTests()
    {
        _configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PropertyAssessmentStatusMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        _mapper = _configuration.CreateMapper();
    }

    #region Configuration Tests

    [Fact]
    public void PropertyAssessmentStatusMappingProfile_Configuration_IsValid()
    {
        // Act & Assert - This will throw if configuration is invalid
        _configuration.AssertConfigurationIsValid();
    }

    #endregion

    #region Entity to Dto Mapping Tests

    [Fact]
    public void Map_PropertyAssessmentStatusEntity_To_PropertyAssessmentStatusDto()
    {
        // Arrange
        var entity = new PropertyAssessmentStatusEntity
        {
            Id = 1,
            StatusName = "Pending Assessment",
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now,
            CreatedBy = 1,
            UpdatedBy = 2
        };

        // Act
        var dto = _mapper.Map<PropertyAssessmentStatusDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.StatusName, dto.StatusName);
        Assert.Equal(entity.IsActive, dto.IsActive);
        Assert.Equal(entity.CreatedDate, dto.CreatedDate);
        Assert.Equal(entity.UpdatedDate, dto.UpdatedDate);
    }

    [Fact]
    public void Map_PropertyAssessmentStatusEntity_WithNullDates_To_PropertyAssessmentStatusDto()
    {
        // Arrange
        var entity = new PropertyAssessmentStatusEntity
        {
            Id = 2,
            StatusName = "Approved",
            IsActive = true,
            CreatedDate = null,
            UpdatedDate = null
        };

        // Act
        var dto = _mapper.Map<PropertyAssessmentStatusDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(entity.Id, dto.Id);
        Assert.Null(dto.CreatedDate);
        Assert.Null(dto.UpdatedDate);
    }

    [Fact]
    public void Map_PropertyAssessmentStatusEntity_WithInactiveStatus_To_PropertyAssessmentStatusDto()
    {
        // Arrange
        var entity = new PropertyAssessmentStatusEntity
        {
            Id = 3,
            StatusName = "Rejected",
            IsActive = false,
            CreatedDate = DateTime.Now
        };

        // Act
        var dto = _mapper.Map<PropertyAssessmentStatusDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.False(dto.IsActive);
        Assert.Equal("Rejected", dto.StatusName);
    }

    #endregion

    #region Dto to Entity Mapping Tests

    [Fact]
    public void Map_PropertyAssessmentStatusDto_To_PropertyAssessmentStatusEntity()
    {
        // Arrange
        var dto = new PropertyAssessmentStatusDto
        {
            Id = 1,
            StatusName = "Under Review",
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };

        // Act
        var entity = _mapper.Map<PropertyAssessmentStatusEntity>(dto);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal(dto.Id, entity.Id);
        Assert.Equal(dto.StatusName, entity.StatusName);
        Assert.Equal(dto.IsActive, entity.IsActive);
    }

    #endregion

    #region CreateDto to Entity Mapping Tests

    [Fact]
    public void Map_CreatePropertyAssessmentStatusDto_To_PropertyAssessmentStatusEntity()
    {
        // Arrange
        var createDto = new CreatePropertyAssessmentStatusDto
        {
            StatusName = "New Status",
            IsActive = true,
            CreatedBy = 1
        };

        // Act
        var entity = _mapper.Map<PropertyAssessmentStatusEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal(createDto.StatusName, entity.StatusName);
        Assert.Equal(createDto.IsActive, entity.IsActive);
        Assert.Equal(createDto.CreatedBy, entity.CreatedBy);
        Assert.Equal(0, entity.Id); // Should be ignored/default
    }

    [Fact]
    public void Map_CreatePropertyAssessmentStatusDto_IgnoresIdAndDates()
    {
        // Arrange
        var createDto = new CreatePropertyAssessmentStatusDto
        {
            StatusName = "Test Status",
            IsActive = true,
            CreatedBy = 5
        };

        // Act
        var entity = _mapper.Map<PropertyAssessmentStatusEntity>(createDto);

        // Assert
        Assert.Equal(0, entity.Id); // Default value, not set from DTO
        Assert.Null(entity.CreatedDate); // Should be ignored
        Assert.Null(entity.UpdatedDate); // Should be ignored
    }

    [Fact]
    public void Map_CreatePropertyAssessmentStatusDto_WithInactive_To_PropertyAssessmentStatusEntity()
    {
        // Arrange
        var createDto = new CreatePropertyAssessmentStatusDto
        {
            StatusName = "Inactive Status",
            IsActive = false,
            CreatedBy = 2
        };

        // Act
        var entity = _mapper.Map<PropertyAssessmentStatusEntity>(createDto);

        // Assert
        Assert.False(entity.IsActive);
        Assert.Equal("Inactive Status", entity.StatusName);
    }

    [Fact]
    public void Map_CreatePropertyAssessmentStatusDto_WithTrimmedName_To_PropertyAssessmentStatusEntity()
    {
        // Arrange
        var createDto = new CreatePropertyAssessmentStatusDto
        {
            StatusName = "  Trimmed Status  ", // Setter should trim this
            IsActive = true
        };

        // Act
        var entity = _mapper.Map<PropertyAssessmentStatusEntity>(createDto);

        // Assert
        Assert.Equal("Trimmed Status", entity.StatusName);
    }

    #endregion

    #region UpdateDto to Entity Mapping Tests

    [Fact]
    public void Map_UpdatePropertyAssessmentStatusDto_To_PropertyAssessmentStatusEntity()
    {
        // Arrange
        var updateDto = new UpdatePropertyAssessmentStatusDto
        {
            StatusName = "Updated Status",
            IsActive = false,
            UpdatedBy = 3
        };

        // Act
        var entity = _mapper.Map<PropertyAssessmentStatusEntity>(updateDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal(updateDto.StatusName, entity.StatusName);
        Assert.Equal(updateDto.IsActive, entity.IsActive);
        Assert.Equal(updateDto.UpdatedBy, entity.UpdatedBy);
        Assert.Equal(0, entity.Id); // Should be ignored/default
    }

    [Fact]
    public void Map_UpdatePropertyAssessmentStatusDto_IgnoresIdAndDates()
    {
        // Arrange
        var updateDto = new UpdatePropertyAssessmentStatusDto
        {
            StatusName = "Changed Status",
            IsActive = true,
            UpdatedBy = 7
        };

        // Act
        var entity = _mapper.Map<PropertyAssessmentStatusEntity>(updateDto);

        // Assert
        Assert.Equal(0, entity.Id); // Default value
        Assert.Null(entity.CreatedDate); // Should be ignored
        Assert.Null(entity.UpdatedDate); // Should be ignored
    }

    [Fact]
    public void Map_UpdatePropertyAssessmentStatusDto_WithActivation_To_PropertyAssessmentStatusEntity()
    {
        // Arrange
        var updateDto = new UpdatePropertyAssessmentStatusDto
        {
            StatusName = "Reactivated",
            IsActive = true,
            UpdatedBy = 4
        };

        // Act
        var entity = _mapper.Map<PropertyAssessmentStatusEntity>(updateDto);

        // Assert
        Assert.True(entity.IsActive);
        Assert.Equal(4, entity.UpdatedBy);
    }

    [Fact]
    public void Map_UpdatePropertyAssessmentStatusDto_WithDeactivation_To_PropertyAssessmentStatusEntity()
    {
        // Arrange
        var updateDto = new UpdatePropertyAssessmentStatusDto
        {
            StatusName = "Deactivated",
            IsActive = false,
            UpdatedBy = 6
        };

        // Act
        var entity = _mapper.Map<PropertyAssessmentStatusEntity>(updateDto);

        // Assert
        Assert.False(entity.IsActive);
        Assert.Equal(6, entity.UpdatedBy);
    }

    [Fact]
    public void Map_UpdatePropertyAssessmentStatusDto_WithTrimmedName_To_PropertyAssessmentStatusEntity()
    {
        // Arrange
        var updateDto = new UpdatePropertyAssessmentStatusDto
        {
            StatusName = "  Trimmed Update  ", // Setter should trim this
            IsActive = true
        };

        // Act
        var entity = _mapper.Map<PropertyAssessmentStatusEntity>(updateDto);

        // Assert
        Assert.Equal("Trimmed Update", entity.StatusName);
    }

    #endregion

    #region Collection Mapping Tests

    [Fact]
    public void Map_ListOfEntities_To_ListOfDtos()
    {
        // Arrange
        var entities = new List<PropertyAssessmentStatusEntity>
        {
            new PropertyAssessmentStatusEntity { Id = 1, StatusName = "Status 1", IsActive = true },
            new PropertyAssessmentStatusEntity { Id = 2, StatusName = "Status 2", IsActive = false },
            new PropertyAssessmentStatusEntity { Id = 3, StatusName = "Status 3", IsActive = true }
        };

        // Act
        var dtos = _mapper.Map<List<PropertyAssessmentStatusDto>>(entities);

        // Assert
        Assert.NotNull(dtos);
        Assert.Equal(3, dtos.Count);
        Assert.Equal("Status 1", dtos[0].StatusName);
        Assert.Equal("Status 2", dtos[1].StatusName);
        Assert.Equal("Status 3", dtos[2].StatusName);
    }

    [Fact]
    public void Map_EmptyListOfEntities_To_EmptyListOfDtos()
    {
        // Arrange
        var entities = new List<PropertyAssessmentStatusEntity>();

        // Act
        var dtos = _mapper.Map<List<PropertyAssessmentStatusDto>>(entities);

        // Assert
        Assert.NotNull(dtos);
        Assert.Empty(dtos);
    }

    #endregion

    #region Reverse Mapping Tests

    [Fact]
    public void Map_EntityToDto_ThenDtoToEntity_PreservesData()
    {
        // Arrange
        var originalEntity = new PropertyAssessmentStatusEntity
        {
            Id = 1,
            StatusName = "Round Trip",
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        // Act
        var dto = _mapper.Map<PropertyAssessmentStatusDto>(originalEntity);
        var entityFromDto = _mapper.Map<PropertyAssessmentStatusEntity>(dto);

        // Assert
        Assert.Equal(originalEntity.Id, entityFromDto.Id);
        Assert.Equal(originalEntity.StatusName, entityFromDto.StatusName);
        Assert.Equal(originalEntity.IsActive, entityFromDto.IsActive);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Map_EntityWithMaxLengthStatusName_To_Dto()
    {
        // Arrange - Max length is 30 according to database schema
        var entity = new PropertyAssessmentStatusEntity
        {
            Id = 1,
            StatusName = "123456789012345678901234567890", // 30 characters
            IsActive = true
        };

        // Act
        var dto = _mapper.Map<PropertyAssessmentStatusDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(30, dto.StatusName.Length);
    }

    [Fact]
    public void Map_CreateDtoWithEmptyStatusName_To_Entity()
    {
        // Arrange
        var createDto = new CreatePropertyAssessmentStatusDto
        {
            StatusName = string.Empty,
            IsActive = true
        };

        // Act
        var entity = _mapper.Map<PropertyAssessmentStatusEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal(string.Empty, entity.StatusName);
    }

    [Fact]
    public void Map_NullEntity_ReturnsNull()
    {
        // Arrange
        PropertyAssessmentStatusEntity? entity = null;

        // Act
        var dto = _mapper.Map<PropertyAssessmentStatusDto>(entity);

        // Assert
        Assert.Null(dto);
    }

    [Fact]
    public void Map_NullDto_ReturnsNull()
    {
        // Arrange
        PropertyAssessmentStatusDto? dto = null;

        // Act
        var entity = _mapper.Map<PropertyAssessmentStatusEntity>(dto);

        // Assert
        Assert.Null(entity);
    }

    #endregion
}
