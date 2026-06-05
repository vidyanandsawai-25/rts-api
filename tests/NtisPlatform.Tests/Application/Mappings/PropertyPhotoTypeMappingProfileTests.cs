using AutoMapper;
using NtisPlatform.Application.DTOs.Master.PropertyPhotoType;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Tests.Application.Mappings;

/// <summary>
/// Comprehensive mapping tests for PropertyPhotoTypeMappingProfile to achieve 100% code coverage
/// </summary>
public class PropertyPhotoTypeMappingProfileTests
{
    private readonly IMapper _mapper;
    private readonly MapperConfiguration _configuration;

    public PropertyPhotoTypeMappingProfileTests()
    {
        _configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PropertyPhotoTypeMappingProfile>();
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
        var entity = new PropertyPhotoTypeEntity
        {
            Id = 1,
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front View",
            Description = "Front facade of the property",
            DisplayOrder = 1,
            IsActive = true,
            CreatedBy = 100,
            CreatedDate = DateTime.Now,
            UpdatedBy = 200,
            UpdatedDate = DateTime.Now.AddHours(1)
        };

        // Act
        var dto = _mapper.Map<PropertyPhotoTypeDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.PhotoTypeCode, dto.PhotoTypeCode);
        Assert.Equal(entity.PhotoTypeName, dto.PhotoTypeName);
        Assert.Equal(entity.Description, dto.Description);
        Assert.Equal(entity.DisplayOrder, dto.DisplayOrder);
        Assert.Equal(entity.IsActive, dto.IsActive);
        Assert.Equal(entity.CreatedDate, dto.CreatedDate);
        Assert.Equal(entity.UpdatedDate, dto.UpdatedDate);
    }

    [Fact]
    public void Map_EntityToDto_WithNullOptionalFields_MapsCorrectly()
    {
        // Arrange
        var entity = new PropertyPhotoTypeEntity
        {
            Id = 1,
            PhotoTypeCode = "TEST",
            PhotoTypeName = "Test Name",
            Description = null,
            DisplayOrder = null,
            IsActive = true,
            CreatedBy = null,
            CreatedDate = null,
            UpdatedBy = null,
            UpdatedDate = null
        };

        // Act
        var dto = _mapper.Map<PropertyPhotoTypeDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(1, dto.Id);
        Assert.Equal("TEST", dto.PhotoTypeCode);
        Assert.Equal("Test Name", dto.PhotoTypeName);
        Assert.Null(dto.Description);
        Assert.Null(dto.DisplayOrder);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public void Map_EntityToDto_MultipleEntities_MapsAllCorrectly()
    {
        // Arrange
        var entities = new List<PropertyPhotoTypeEntity>
        {
            new() { Id = 1, PhotoTypeCode = "FRONT", PhotoTypeName = "Front View", IsActive = true },
            new() { Id = 2, PhotoTypeCode = "BACK", PhotoTypeName = "Back View", IsActive = true },
            new() { Id = 3, PhotoTypeCode = "LEFT", PhotoTypeName = "Left Side", IsActive = false }
        };

        // Act
        var dtos = _mapper.Map<List<PropertyPhotoTypeDto>>(entities);

        // Assert
        Assert.NotNull(dtos);
        Assert.Equal(3, dtos.Count);
        Assert.Equal("FRONT", dtos[0].PhotoTypeCode);
        Assert.Equal("BACK", dtos[1].PhotoTypeCode);
        Assert.Equal("LEFT", dtos[2].PhotoTypeCode);
        Assert.False(dtos[2].IsActive);
    }

    #endregion

    #region CreateDto to Entity Mapping Tests

    [Fact]
    public void Map_CreateDtoToEntity_MapsAllProperties()
    {
        // Arrange
        var createDto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front View",
            Description = "Front facade",
            DisplayOrder = 1,
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var entity = _mapper.Map<PropertyPhotoTypeEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal(createDto.PhotoTypeCode, entity.PhotoTypeCode);
        Assert.Equal(createDto.PhotoTypeName, entity.PhotoTypeName);
        Assert.Equal(createDto.Description, entity.Description);
        Assert.Equal(createDto.DisplayOrder, entity.DisplayOrder);
        Assert.Equal(createDto.IsActive, entity.IsActive);
        Assert.Equal(createDto.CreatedBy, entity.CreatedBy);
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresCreatedDate()
    {
        // Arrange
        var createDto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "TEST",
            PhotoTypeName = "Test",
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var entity = _mapper.Map<PropertyPhotoTypeEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Null(entity.CreatedDate); // Should be ignored by mapping
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresUpdatedDate()
    {
        // Arrange
        var createDto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "TEST",
            PhotoTypeName = "Test",
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var entity = _mapper.Map<PropertyPhotoTypeEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Null(entity.UpdatedDate); // Should be ignored by mapping
    }

    [Fact]
    public void Map_CreateDtoToEntity_WithNullOptionalFields_MapsCorrectly()
    {
        // Arrange
        var createDto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "MIN",
            PhotoTypeName = "Minimal",
            Description = null,
            DisplayOrder = null,
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var entity = _mapper.Map<PropertyPhotoTypeEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal("MIN", entity.PhotoTypeCode);
        Assert.Equal("Minimal", entity.PhotoTypeName);
        Assert.Null(entity.Description);
        Assert.Null(entity.DisplayOrder);
    }

    #endregion

    #region UpdateDto to Entity Mapping Tests

    [Fact]
    public void Map_UpdateDtoToEntity_MapsAllProperties()
    {
        // Arrange
        var updateDto = new UpdatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "UPDATED",
            PhotoTypeName = "Updated Name",
            Description = "Updated description",
            DisplayOrder = 5,
            IsActive = false,
            UpdatedBy = 200
        };

        var existingEntity = new PropertyPhotoTypeEntity
        {
            Id = 1,
            PhotoTypeCode = "OLD",
            PhotoTypeName = "Old Name",
            Description = "Old description",
            DisplayOrder = 1,
            IsActive = true,
            CreatedBy = 100,
            CreatedDate = DateTime.Now.AddDays(-1)
        };

        // Act
        _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.Equal(updateDto.PhotoTypeCode, existingEntity.PhotoTypeCode);
        Assert.Equal(updateDto.PhotoTypeName, existingEntity.PhotoTypeName);
        Assert.Equal(updateDto.Description, existingEntity.Description);
        Assert.Equal(updateDto.DisplayOrder, existingEntity.DisplayOrder);
        Assert.Equal(updateDto.IsActive, existingEntity.IsActive);
        Assert.Equal(updateDto.UpdatedBy, existingEntity.UpdatedBy);
    }

    [Fact]
    public void Map_UpdateDtoToEntity_IgnoresCreatedDate()
    {
        // Arrange
        var originalDate = DateTime.Now.AddDays(-10);
        var updateDto = new UpdatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "TEST",
            PhotoTypeName = "Test",
            IsActive = true,
            UpdatedBy = 200
        };

        var existingEntity = new PropertyPhotoTypeEntity
        {
            Id = 1,
            PhotoTypeCode = "OLD",
            PhotoTypeName = "Old",
            CreatedDate = originalDate,
            IsActive = true
        };

        // Act
        _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.Equal(originalDate, existingEntity.CreatedDate); // Should remain unchanged
    }

    [Fact]
    public void Map_UpdateDtoToEntity_IgnoresUpdatedDate()
    {
        // Arrange
        var originalUpdatedDate = DateTime.Now.AddDays(-1);
        var updateDto = new UpdatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "TEST",
            PhotoTypeName = "Test",
            IsActive = true,
            UpdatedBy = 200
        };

        var existingEntity = new PropertyPhotoTypeEntity
        {
            Id = 1,
            PhotoTypeCode = "OLD",
            PhotoTypeName = "Old",
            UpdatedDate = originalUpdatedDate,
            IsActive = true
        };

        // Act
        _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.Equal(originalUpdatedDate, existingEntity.UpdatedDate); // Should remain unchanged
    }

    [Fact]
    public void Map_UpdateDtoToEntity_PreservesId()
    {
        // Arrange
        var updateDto = new UpdatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "TEST",
            PhotoTypeName = "Test",
            IsActive = true,
            UpdatedBy = 200
        };

        var existingEntity = new PropertyPhotoTypeEntity
        {
            Id = 99,
            PhotoTypeCode = "OLD",
            PhotoTypeName = "Old",
            IsActive = true
        };

        // Act
        _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.Equal(99, existingEntity.Id); // ID should not change
    }

    [Fact]
    public void Map_UpdateDtoToEntity_UpdatesIsActiveToFalse()
    {
        // Arrange
        var updateDto = new UpdatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "TEST",
            PhotoTypeName = "Test",
            IsActive = false,
            UpdatedBy = 200
        };

        var existingEntity = new PropertyPhotoTypeEntity
        {
            Id = 1,
            PhotoTypeCode = "OLD",
            PhotoTypeName = "Old",
            IsActive = true
        };

        // Act
        _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.False(existingEntity.IsActive);
    }

    [Fact]
    public void Map_UpdateDtoToEntity_WithNullOptionalFields_ClearsExistingValues()
    {
        // Arrange
        var updateDto = new UpdatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "TEST",
            PhotoTypeName = "Test",
            Description = null,
            DisplayOrder = null,
            IsActive = true,
            UpdatedBy = 200
        };

        var existingEntity = new PropertyPhotoTypeEntity
        {
            Id = 1,
            PhotoTypeCode = "OLD",
            PhotoTypeName = "Old",
            Description = "Some description",
            DisplayOrder = 5,
            IsActive = true
        };

        // Act
        _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.Null(existingEntity.Description);
        Assert.Null(existingEntity.DisplayOrder);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Map_EntityWithMaxLengthValues_ToDto_MapsCorrectly()
    {
        // Arrange
        var entity = new PropertyPhotoTypeEntity
        {
            Id = 1,
            PhotoTypeCode = new string('A', 50),
            PhotoTypeName = new string('B', 200),
            Description = new string('C', 500),
            IsActive = true
        };

        // Act
        var dto = _mapper.Map<PropertyPhotoTypeDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(50, dto.PhotoTypeCode.Length);
        Assert.Equal(200, dto.PhotoTypeName.Length);
        Assert.Equal(500, dto.Description!.Length);
    }

    [Fact]
    public void Map_NullEntity_ToDto_ReturnsNull()
    {
        // Arrange
        PropertyPhotoTypeEntity? entity = null;

        // Act
        var dto = _mapper.Map<PropertyPhotoTypeDto>(entity);

        // Assert
        Assert.Null(dto);
    }

    [Fact]
    public void Map_EmptyStrings_MapsCorrectly()
    {
        // Arrange
        var entity = new PropertyPhotoTypeEntity
        {
            Id = 1,
            PhotoTypeCode = string.Empty,
            PhotoTypeName = string.Empty,
            Description = string.Empty,
            IsActive = true
        };

        // Act
        var dto = _mapper.Map<PropertyPhotoTypeDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(string.Empty, dto.PhotoTypeCode);
        Assert.Equal(string.Empty, dto.PhotoTypeName);
        Assert.Equal(string.Empty, dto.Description);
    }

    #endregion
}
