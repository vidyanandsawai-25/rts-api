using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Core.Entities;
using Xunit;

namespace NtisPlatform.Tests.Application.Mappings;

/// <summary>
/// Comprehensive mapping tests for TypeOfUseGroupCVMappingProfile to achieve 100% code coverage
/// </summary>
public class TypeOfUseGroupCVMappingProfileTests
{
    private readonly IMapper _mapper;
    private readonly MapperConfiguration _configuration;

    public TypeOfUseGroupCVMappingProfileTests()
    {
        _configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TypeOfUseGroupCVMappingProfile>();
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
        var entity = new TypeOfUseGroupCVEntity
        {
            Id = 1,
            TypeOfUseGroupCVCode = "RES",
            GroupName = "Residential",
            GroupIcon = "home-icon",
            IsFloorWiseRateApplicable = true,
            IsActive = true,
            CreatedBy = 100,
            CreatedDate = DateTime.Now,
            UpdatedBy = 200,
            UpdatedDate = DateTime.Now.AddHours(1)
        };

        // Act
        var dto = _mapper.Map<TypeOfUseGroupCVDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.TypeOfUseGroupCVCode, dto.TypeOfUseGroupCVCode);
        Assert.Equal(entity.GroupName, dto.GroupName);
        Assert.Equal(entity.GroupIcon, dto.GroupIcon);
        Assert.Equal(entity.IsFloorWiseRateApplicable, dto.IsFloorWiseRateApplicable);
        Assert.Equal(entity.IsActive, dto.IsActive);
        Assert.Equal(entity.CreatedDate, dto.CreatedDate);
        Assert.Equal(entity.UpdatedDate, dto.UpdatedDate);
    }

    [Fact]
    public void Map_EntityToDto_WithNullOptionalFields_MapsCorrectly()
    {
        // Arrange
        var entity = new TypeOfUseGroupCVEntity
        {
            Id = 1,
            TypeOfUseGroupCVCode = "COM",
            GroupName = "Commercial",
            GroupIcon = "",
            IsFloorWiseRateApplicable = false,
            IsActive = true,
            CreatedBy = null,
            CreatedDate = null,
            UpdatedBy = null,
            UpdatedDate = null
        };

        // Act
        var dto = _mapper.Map<TypeOfUseGroupCVDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(1, dto.Id);
        Assert.Equal("COM", dto.TypeOfUseGroupCVCode);
        Assert.Equal("Commercial", dto.GroupName);
        Assert.Equal("", dto.GroupIcon);
        Assert.False(dto.IsFloorWiseRateApplicable);
    }

    [Fact]
    public void Map_EntityToDto_MultipleEntities_MapsAllCorrectly()
    {
        // Arrange
        var entities = new List<TypeOfUseGroupCVEntity>
        {
            new() { Id = 1, TypeOfUseGroupCVCode = "RES", GroupName = "Residential", IsFloorWiseRateApplicable = true, IsActive = true },
            new() { Id = 2, TypeOfUseGroupCVCode = "COM", GroupName = "Commercial", IsFloorWiseRateApplicable = false, IsActive = true },
            new() { Id = 3, TypeOfUseGroupCVCode = "IND", GroupName = "Industrial", IsFloorWiseRateApplicable = true, IsActive = false }
        };

        // Act
        var dtos = _mapper.Map<List<TypeOfUseGroupCVDto>>(entities);

        // Assert
        Assert.NotNull(dtos);
        Assert.Equal(3, dtos.Count);
        Assert.Equal("RES", dtos[0].TypeOfUseGroupCVCode);
        Assert.True(dtos[0].IsFloorWiseRateApplicable);
        Assert.Equal("COM", dtos[1].TypeOfUseGroupCVCode);
        Assert.False(dtos[1].IsFloorWiseRateApplicable);
        Assert.Equal("IND", dtos[2].TypeOfUseGroupCVCode);
        Assert.False(dtos[2].IsActive);
    }

    [Fact]
    public void Map_EntityToDto_IgnoresTypeOfUseCollection()
    {
        // Arrange
        var entity = new TypeOfUseGroupCVEntity
        {
            Id = 1,
            TypeOfUseGroupCVCode = "RES",
            GroupName = "Residential",
            TypeOfUse = new List<TypeOfUseEntity>
            {
                new TypeOfUseEntity { Id = 10, Description = "Apartment" },
                new TypeOfUseEntity { Id = 11, Description = "Villa" }
            }
        };

        // Act
        var dto = _mapper.Map<TypeOfUseGroupCVDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("RES", dto.TypeOfUseGroupCVCode);
        // TypeOfUse collection should not be mapped to DTO
    }

    #endregion

    #region CreateDto to Entity Mapping Tests

    [Fact]
    public void Map_CreateDtoToEntity_MapsAllProperties()
    {
        // Arrange
        var createDto = new CreateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "AGR",
            GroupName = "Agriculture",
            GroupIcon = "farm-icon",
            IsFloorWiseRateApplicable = false,
            CreatedBy = 100
        };

        // Act
        var entity = _mapper.Map<TypeOfUseGroupCVEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal(createDto.TypeOfUseGroupCVCode, entity.TypeOfUseGroupCVCode);
        Assert.Equal(createDto.GroupName, entity.GroupName);
        Assert.Equal(createDto.GroupIcon, entity.GroupIcon);
        Assert.Equal(createDto.IsFloorWiseRateApplicable, entity.IsFloorWiseRateApplicable);
        Assert.Equal(createDto.CreatedBy, entity.CreatedBy);
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresId()
    {
        // Arrange
        var createDto = new CreateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "EDU",
            GroupName = "Educational",
            CreatedBy = 100
        };

        // Act
        var entity = _mapper.Map<TypeOfUseGroupCVEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal(0, entity.Id); // Should be default value
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresCreatedDate()
    {
        // Arrange
        var createDto = new CreateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "HEA",
            GroupName = "Healthcare",
            CreatedBy = 100
        };

        // Act
        var entity = _mapper.Map<TypeOfUseGroupCVEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Null(entity.CreatedDate); // Should be ignored by mapping
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresUpdatedDate()
    {
        // Arrange
        var createDto = new CreateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "RET",
            GroupName = "Retail",
            CreatedBy = 100
        };

        // Act
        var entity = _mapper.Map<TypeOfUseGroupCVEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Null(entity.UpdatedDate); // Should be ignored by mapping
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresUpdatedBy()
    {
        // Arrange
        var createDto = new CreateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "OFF",
            GroupName = "Office",
            CreatedBy = 100
        };

        // Act
        var entity = _mapper.Map<TypeOfUseGroupCVEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Null(entity.UpdatedBy); // Should be ignored by mapping
    }

    [Fact]
    public void Map_CreateDtoToEntity_PreservesCreatedBy()
    {
        // Arrange
        var createDto = new CreateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "MIX",
            GroupName = "Mixed Use",
            CreatedBy = 999
        };

        // Act
        var entity = _mapper.Map<TypeOfUseGroupCVEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal(999, entity.CreatedBy);
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresTypeOfUseCollection()
    {
        // Arrange
        var createDto = new CreateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "PUB",
            GroupName = "Public",
            CreatedBy = 50
        };

        // Act
        var entity = _mapper.Map<TypeOfUseGroupCVEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Empty(entity.TypeOfUse); // Collection should be empty (default)
    }

    [Fact]
    public void Map_CreateDtoToEntity_WithFloorWiseRateApplicableTrue()
    {
        // Arrange
        var createDto = new CreateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "HOT",
            GroupName = "Hospitality",
            IsFloorWiseRateApplicable = true,
            CreatedBy = 25
        };

        // Act
        var entity = _mapper.Map<TypeOfUseGroupCVEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.True(entity.IsFloorWiseRateApplicable);
    }

    #endregion

    #region UpdateDto to Entity Mapping Tests

    [Fact]
    public void Map_UpdateDtoToEntity_MapsAllProperties()
    {
        // Arrange
        var updateDto = new UpdateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "RES-UPD",
            GroupName = "Updated Residential",
            GroupIcon = "new-icon",
            IsFloorWiseRateApplicable = false,
            UpdatedBy = 200
        };

        var existingEntity = new TypeOfUseGroupCVEntity
        {
            Id = 1,
            TypeOfUseGroupCVCode = "RES",
            GroupName = "Residential",
            GroupIcon = "old-icon",
            IsFloorWiseRateApplicable = true,
            IsActive = true,
            CreatedBy = 100,
            CreatedDate = DateTime.Now.AddDays(-1)
        };

        // Act
        var entity = _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal(updateDto.TypeOfUseGroupCVCode, entity.TypeOfUseGroupCVCode);
        Assert.Equal(updateDto.GroupName, entity.GroupName);
        Assert.Equal(updateDto.GroupIcon, entity.GroupIcon);
        Assert.Equal(updateDto.IsFloorWiseRateApplicable, entity.IsFloorWiseRateApplicable);
        Assert.Equal(updateDto.UpdatedBy, entity.UpdatedBy);
        Assert.Equal(1, entity.Id); // Should preserve existing Id
    }

    [Fact]
    public void Map_UpdateDtoToEntity_IgnoresId()
    {
        // Arrange
        var updateDto = new UpdateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "NEW",
            GroupName = "New Name",
            UpdatedBy = 200
        };

        var existingEntity = new TypeOfUseGroupCVEntity
        {
            Id = 42,
            TypeOfUseGroupCVCode = "OLD"
        };

        // Act
        _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.Equal(42, existingEntity.Id); // Should remain unchanged
    }

    [Fact]
    public void Map_UpdateDtoToEntity_IgnoresCreatedDate()
    {
        // Arrange
        var updateDto = new UpdateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "MOD",
            GroupName = "Modified",
            UpdatedBy = 200
        };

        var existingEntity = new TypeOfUseGroupCVEntity
        {
            Id = 1,
            TypeOfUseGroupCVCode = "OLD",
            CreatedDate = DateTime.Now.AddDays(-10)
        };

        var originalCreatedDate = existingEntity.CreatedDate;

        // Act
        _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.Equal(originalCreatedDate, existingEntity.CreatedDate); // Should remain unchanged
    }

    [Fact]
    public void Map_UpdateDtoToEntity_IgnoresCreatedBy()
    {
        // Arrange
        var updateDto = new UpdateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "CHG",
            GroupName = "Changed",
            UpdatedBy = 300
        };

        var existingEntity = new TypeOfUseGroupCVEntity
        {
            Id = 5,
            CreatedBy = 100
        };

        // Act
        _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.Equal(100, existingEntity.CreatedBy); // Should remain unchanged
    }

    [Fact]
    public void Map_UpdateDtoToEntity_IgnoresUpdatedDate()
    {
        // Arrange
        var updateDto = new UpdateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "TST",
            GroupName = "Test",
            UpdatedBy = 200
        };

        var existingEntity = new TypeOfUseGroupCVEntity
        {
            Id = 1,
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
        var updateDto = new UpdateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "ACT",
            GroupName = "Active",
            UpdatedBy = 200
        };

        var existingEntity = new TypeOfUseGroupCVEntity
        {
            Id = 1,
            IsActive = true
        };

        // Act
        _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.True(existingEntity.IsActive); // Should remain unchanged
    }

    [Fact]
    public void Map_UpdateDtoToEntity_IgnoresTypeOfUseCollection()
    {
        // Arrange
        var updateDto = new UpdateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "UPD",
            GroupName = "Updated Group",
            UpdatedBy = 150
        };

        var existingEntity = new TypeOfUseGroupCVEntity
        {
            Id = 3,
            TypeOfUse = new List<TypeOfUseEntity>
            {
                new TypeOfUseEntity { Id = 20, Description = "Existing Use" }
            }
        };

        var originalCount = existingEntity.TypeOfUse.Count;

        // Act
        _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.Equal(originalCount, existingEntity.TypeOfUse.Count); // Collection should remain unchanged
    }

    [Fact]
    public void Map_UpdateDtoToEntity_UpdatesOnlyAllowedFields()
    {
        // Arrange
        var updateDto = new UpdateTypeOfUseGroupCVDto
        {
            TypeOfUseGroupCVCode = "ALL",
            GroupName = "All Changed",
            GroupIcon = "changed-icon",
            IsFloorWiseRateApplicable = false,
            UpdatedBy = 500
        };

        var existingEntity = new TypeOfUseGroupCVEntity
        {
            Id = 5,
            TypeOfUseGroupCVCode = "ORIG",
            GroupName = "Original",
            GroupIcon = "orig-icon",
            IsFloorWiseRateApplicable = true,
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
        Assert.Equal("ALL", existingEntity.TypeOfUseGroupCVCode); // Updated
        Assert.Equal("All Changed", existingEntity.GroupName); // Updated
        Assert.Equal("changed-icon", existingEntity.GroupIcon); // Updated
        Assert.False(existingEntity.IsFloorWiseRateApplicable); // Updated
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
        TypeOfUseGroupCVEntity? entity = null;

        // Act
        var dto = _mapper.Map<TypeOfUseGroupCVDto>(entity);

        // Assert
        Assert.Null(dto);
    }

    [Fact]
    public void Map_NullCreateDto_ReturnsNull()
    {
        // Arrange
        CreateTypeOfUseGroupCVDto? createDto = null;

        // Act
        var entity = _mapper.Map<TypeOfUseGroupCVEntity>(createDto);

        // Assert
        Assert.Null(entity);
    }

    [Fact]
    public void Map_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var entities = new List<TypeOfUseGroupCVEntity>();

        // Act
        var dtos = _mapper.Map<List<TypeOfUseGroupCVDto>>(entities);

        // Assert
        Assert.NotNull(dtos);
        Assert.Empty(dtos);
    }

    #endregion
}
