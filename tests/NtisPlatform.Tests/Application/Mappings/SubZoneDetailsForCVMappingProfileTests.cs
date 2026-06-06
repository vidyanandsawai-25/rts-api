using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Application.Mappings;

public class SubZoneDetailsForCVMappingProfileTests
{
    private readonly IMapper _mapper;
    private readonly MapperConfiguration _configuration;

    public SubZoneDetailsForCVMappingProfileTests()
    {
        _configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<SubZoneDetailsForCVMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        _mapper = _configuration.CreateMapper();
    }

    [Fact]
    public void MappingProfile_Configuration_IsValid()
    {
        // Act & Assert
        _configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_EntityToDto_MapsAllProperties()
    {
        // Arrange
        var entity = new SubZoneDetailsForCVEntity
        {
            Id = 1,
            MoujaId = 10,
            SubZoneNo = "SZ001",
            SubZoneName = "Zone A",
            IsActive = true,
            CreatedDate = DateTime.Now,
            Mouja = new MoujaEntity { Id = 10, MoujaName = "Mouja A" }
        };

        // Act
        var dto = _mapper.Map<SubZoneDetailsForCVDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.MoujaId, dto.MoujaId);
        Assert.Equal(entity.SubZoneNo, dto.SubZoneNo);
        Assert.Equal(entity.SubZoneName, dto.SubZoneName);
        Assert.Equal("Mouja A", dto.MoujaName);
    }

    [Fact]
    public void Map_EntityToDto_WithNullMouja_MapsCorrectly()
    {
        // Arrange
        var entity = new SubZoneDetailsForCVEntity
        {
            Id = 1,
            MoujaId = 10,
            SubZoneNo = "SZ002",
            SubZoneName = "Zone B",
            Mouja = null
        };

        // Act
        var dto = _mapper.Map<SubZoneDetailsForCVDto>(entity);

        // Assert
        Assert.NotNull(dto);
        Assert.Null(dto.MoujaName);
    }

    [Fact]
    public void Map_CreateDtoToEntity_MapsAllProperties()
    {
        // Arrange
        var createDto = new CreateSubZoneDetailsForCVDto
        {
            MoujaId = 10,
            SubZoneNo = "SZ003",
            SubZoneName = "Zone C",
            CreatedBy = 100
        };

        // Act
        var entity = _mapper.Map<SubZoneDetailsForCVEntity>(createDto);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal(createDto.MoujaId, entity.MoujaId);
        Assert.Equal(createDto.SubZoneNo, entity.SubZoneNo);
        Assert.Equal(createDto.SubZoneName, entity.SubZoneName);
        Assert.Equal(createDto.CreatedBy, entity.CreatedBy);
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresBaseProperties()
    {
        // Arrange
        var createDto = new CreateSubZoneDetailsForCVDto
        {
            MoujaId = 10,
            SubZoneNo = "SZ004",
            SubZoneName = "Zone D",
            CreatedBy = 100
        };

        // Act
        var entity = _mapper.Map<SubZoneDetailsForCVEntity>(createDto);

        // Assert
        Assert.Equal(0, entity.Id);
        Assert.Null(entity.CreatedDate);
        Assert.Null(entity.UpdatedDate);
        Assert.Null(entity.UpdatedBy);
        Assert.Null(entity.Mouja);
    }

    [Fact]
    public void Map_UpdateDtoToEntity_MapsAllProperties()
    {
        // Arrange
        var updateDto = new UpdateSubZoneDetailsForCVDto
        {
            MoujaId = 10,
            SubZoneNo = "SZ001-UPD",
            SubZoneName = "Zone A Updated",
            UpdatedBy = 200
        };

        var existingEntity = new SubZoneDetailsForCVEntity
        {
            Id = 1,
            MoujaId = 10,
            SubZoneNo = "SZ001",
            SubZoneName = "Zone A",
            IsActive = true,
            CreatedBy = 100,
            CreatedDate = DateTime.Now.AddDays(-1)
        };

        // Act
        var entity = _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal(updateDto.MoujaId, entity.MoujaId);
        Assert.Equal(updateDto.SubZoneNo, entity.SubZoneNo);
        Assert.Equal(updateDto.SubZoneName, entity.SubZoneName);
        Assert.Equal(updateDto.UpdatedBy, entity.UpdatedBy);
        Assert.Equal(1, entity.Id); // Preserved
    }

    [Fact]
    public void Map_UpdateDtoToEntity_PreservesBaseProperties()
    {
        // Arrange
        var updateDto = new UpdateSubZoneDetailsForCVDto
        {
            MoujaId = 15,
            SubZoneNo = "NEW",
            SubZoneName = "New Zone",
            UpdatedBy = 300
        };

        var existingEntity = new SubZoneDetailsForCVEntity
        {
            Id = 5,
            MoujaId = 10,
            IsActive = true,
            CreatedBy = 100,
            CreatedDate = DateTime.Now.AddDays(-10)
        };

        var originalCreatedBy = existingEntity.CreatedBy;
        var originalCreatedDate = existingEntity.CreatedDate;
        var originalId = existingEntity.Id;

        // Act
        _mapper.Map(updateDto, existingEntity);

        // Assert
        Assert.Equal(originalId, existingEntity.Id);
        Assert.Equal(originalCreatedBy, existingEntity.CreatedBy);
        Assert.Equal(originalCreatedDate, existingEntity.CreatedDate);
    }

    [Fact]
    public void Map_NullEntity_ReturnsNull()
    {
        // Arrange
        SubZoneDetailsForCVEntity? entity = null;

        // Act
        var dto = _mapper.Map<SubZoneDetailsForCVDto>(entity);

        // Assert
        Assert.Null(dto);
    }
}
