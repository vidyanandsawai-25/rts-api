using AutoMapper;
using NtisPlatform.Application.DTOs.Master.PropertyDescriptionAndTypeOfUseValidation;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Tests.Application.Mappings;

public class PropertyDescriptionAndTypeOfUseValidationMappingProfileTests
{
    private readonly IMapper _mapper;

    public PropertyDescriptionAndTypeOfUseValidationMappingProfileTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PropertyDescriptionAndTypeOfUseValidationMappingProfile>();
        });

        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Map_EntityToDto_MapsAllProperties()
    {
        var now = DateTime.Now;
        var entity = new PropertyDescriptionAndTypeOfUseValidationEntity
        {
            Id = 1,
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = true,
            CreatedBy = 100,
            CreatedDate = now,
            UpdatedBy = 200,
            UpdatedDate = now.AddHours(1)
        };

        var dto = _mapper.Map<PropertyDescriptionAndTypeOfUseValidationDto>(entity);

        Assert.NotNull(dto);
        Assert.Equal(1, dto.Id);
        Assert.Equal(5, dto.PropertyTypeId);
        Assert.Equal(10, dto.TypeOfUseId);
        Assert.True(dto.IsActive);
        Assert.Equal(now, dto.CreatedDate);
        Assert.Equal(now.AddHours(1), dto.UpdatedDate);
    }

    [Fact]
    public void Map_CreateDtoToEntity_MapsAllProperties()
    {
        var createDto = new CreatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = true,
            CreatedBy = 1
        };

        var entity = _mapper.Map<PropertyDescriptionAndTypeOfUseValidationEntity>(createDto);

        Assert.NotNull(entity);
        Assert.Equal(5, entity.PropertyTypeId);
        Assert.Equal(10, entity.TypeOfUseId);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void Map_CreateDtoToEntity_IgnoresAuditFields()
    {
        var createDto = new CreatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            CreatedBy = 1
        };

        var entity = _mapper.Map<PropertyDescriptionAndTypeOfUseValidationEntity>(createDto);

        Assert.Null(entity.CreatedDate);
        Assert.Null(entity.UpdatedDate);
    }

    [Fact]
    public void Map_UpdateDtoToEntity_MapsAllProperties()
    {
        var updateDto = new UpdatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 6,
            TypeOfUseId = 11,
            IsActive = false,
            UpdatedBy = 2
        };

        var existingEntity = new PropertyDescriptionAndTypeOfUseValidationEntity
        {
            Id = 1,
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = true,
            CreatedDate = DateTime.Now.AddDays(-1)
        };

        _mapper.Map(updateDto, existingEntity);

        Assert.Equal(6, existingEntity.PropertyTypeId);
        Assert.Equal(11, existingEntity.TypeOfUseId);
        Assert.False(existingEntity.IsActive);
    }

    [Fact]
    public void Map_UpdateDtoToEntity_IgnoresCreatedDate()
    {
        var originalCreatedDate = DateTime.Now.AddDays(-10);
        var updateDto = new UpdatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 6,
            TypeOfUseId = 11,
            UpdatedBy = 2
        };

        var existingEntity = new PropertyDescriptionAndTypeOfUseValidationEntity
        {
            Id = 1,
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            CreatedDate = originalCreatedDate
        };

        _mapper.Map(updateDto, existingEntity);

        Assert.Equal(originalCreatedDate, existingEntity.CreatedDate);
    }

    [Fact]
    public void Map_UpdateDtoToEntity_IgnoresUpdatedDate()
    {
        var updateDto = new UpdatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 6,
            TypeOfUseId = 11,
            UpdatedBy = 2
        };

        var existingEntity = new PropertyDescriptionAndTypeOfUseValidationEntity
        {
            Id = 1,
            PropertyTypeId = 5,
            TypeOfUseId = 10
        };

        _mapper.Map(updateDto, existingEntity);

        Assert.Null(existingEntity.UpdatedDate);
    }

    [Fact]
    public void Map_EntityToDto_WithNullDates_MapsCorrectly()
    {
        var entity = new PropertyDescriptionAndTypeOfUseValidationEntity
        {
            Id = 1,
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = true,
            CreatedDate = null,
            UpdatedDate = null
        };

        var dto = _mapper.Map<PropertyDescriptionAndTypeOfUseValidationDto>(entity);

        Assert.NotNull(dto);
        Assert.Null(dto.CreatedDate);
        Assert.Null(dto.UpdatedDate);
    }

    [Fact]
    public void Map_CreateDtoToEntity_WithIsActiveFalse_MapsCorrectly()
    {
        var createDto = new CreatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = false,
            CreatedBy = 1
        };

        var entity = _mapper.Map<PropertyDescriptionAndTypeOfUseValidationEntity>(createDto);

        Assert.False(entity.IsActive);
    }

    [Fact]
    public void Map_EntityCollection_ToDtoCollection_MapsCorrectly()
    {
        var entities = new List<PropertyDescriptionAndTypeOfUseValidationEntity>
        {
            new() { Id = 1, PropertyTypeId = 5, TypeOfUseId = 10, IsActive = true },
            new() { Id = 2, PropertyTypeId = 6, TypeOfUseId = 11, IsActive = true }
        };

        var dtos = _mapper.Map<List<PropertyDescriptionAndTypeOfUseValidationDto>>(entities);

        Assert.NotNull(dtos);
        Assert.Equal(2, dtos.Count);
        Assert.Equal(5, dtos[0].PropertyTypeId);
        Assert.Equal(6, dtos[1].PropertyTypeId);
    }
}
