using NtisPlatform.Application.DTOs.Master.PropertyDescriptionAndTypeOfUseValidation;

namespace NtisPlatform.Tests.Application.DTOs;

public class PropertyDescriptionAndTypeOfUseValidationDtosTests
{
    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationDto_AllProperties_GetSet_WorksCorrectly()
    {
        var now = DateTime.Now;
        var dto = new PropertyDescriptionAndTypeOfUseValidationDto
        {
            Id = 1,
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = true,
            CreatedDate = now,
            UpdatedDate = now.AddHours(1)
        };

        Assert.Equal(1, dto.Id);
        Assert.Equal(5, dto.PropertyTypeId);
        Assert.Equal(10, dto.TypeOfUseId);
        Assert.True(dto.IsActive);
        Assert.Equal(now, dto.CreatedDate);
        Assert.Equal(now.AddHours(1), dto.UpdatedDate);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationDto_InheritsFromBaseDtos()
    {
        var dto = new PropertyDescriptionAndTypeOfUseValidationDto();
        Assert.IsAssignableFrom<NtisPlatform.Application.DTOs.BaseDtos>(dto);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationDto_DefaultValues_SetCorrectly()
    {
        var dto = new PropertyDescriptionAndTypeOfUseValidationDto();

        Assert.Equal(0, dto.Id);
        Assert.Equal(0, dto.PropertyTypeId);
        Assert.Equal(0, dto.TypeOfUseId);
        Assert.False(dto.IsActive);
        Assert.Null(dto.CreatedDate);
        Assert.Null(dto.UpdatedDate);
    }

    [Fact]
    public void CreatePropertyDescriptionAndTypeOfUseValidationDto_AllProperties_GetSet_WorksCorrectly()
    {
        var dto = new CreatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = true,
            CreatedBy = 1
        };

        Assert.Equal(5, dto.PropertyTypeId);
        Assert.Equal(10, dto.TypeOfUseId);
        Assert.True(dto.IsActive);
        Assert.Equal(1, dto.CreatedBy);
    }

    [Fact]
    public void CreatePropertyDescriptionAndTypeOfUseValidationDto_InheritsFromCreateBaseDtos()
    {
        var dto = new CreatePropertyDescriptionAndTypeOfUseValidationDto();
        Assert.IsAssignableFrom<NtisPlatform.Application.DTOs.CreateBaseDtos>(dto);
    }

    [Fact]
    public void CreatePropertyDescriptionAndTypeOfUseValidationDto_DefaultValues_SetCorrectly()
    {
        var dto = new CreatePropertyDescriptionAndTypeOfUseValidationDto();

        Assert.Equal(0, dto.PropertyTypeId);
        Assert.Equal(0, dto.TypeOfUseId);
        Assert.False(dto.IsActive);
        Assert.Null(dto.CreatedBy);
    }

    [Fact]
    public void UpdatePropertyDescriptionAndTypeOfUseValidationDto_AllProperties_GetSet_WorksCorrectly()
    {
        var dto = new UpdatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 6,
            TypeOfUseId = 11,
            IsActive = false,
            UpdatedBy = 2
        };

        Assert.Equal(6, dto.PropertyTypeId);
        Assert.Equal(11, dto.TypeOfUseId);
        Assert.False(dto.IsActive);
        Assert.Equal(2, dto.UpdatedBy);
    }

    [Fact]
    public void UpdatePropertyDescriptionAndTypeOfUseValidationDto_InheritsFromUpdateBaseDtos()
    {
        var dto = new UpdatePropertyDescriptionAndTypeOfUseValidationDto();
        Assert.IsAssignableFrom<NtisPlatform.Application.DTOs.UpdateBaseDtos>(dto);
    }

    [Fact]
    public void UpdatePropertyDescriptionAndTypeOfUseValidationDto_DefaultValues_SetCorrectly()
    {
        var dto = new UpdatePropertyDescriptionAndTypeOfUseValidationDto();

        Assert.Equal(0, dto.PropertyTypeId);
        Assert.Equal(0, dto.TypeOfUseId);
        Assert.False(dto.IsActive);
        Assert.Null(dto.UpdatedBy);
    }

    [Fact]
    public void PropertyDescriptionAndTypeOfUseValidationDto_CanBeUsedInCollections()
    {
        var dtos = new List<PropertyDescriptionAndTypeOfUseValidationDto>
        {
            new() { Id = 1, PropertyTypeId = 5, TypeOfUseId = 10 },
            new() { Id = 2, PropertyTypeId = 6, TypeOfUseId = 11 }
        };

        Assert.Equal(2, dtos.Count);
        Assert.Equal(5, dtos[0].PropertyTypeId);
        Assert.Equal(6, dtos[1].PropertyTypeId);
    }

    [Fact]
    public void CreatePropertyDescriptionAndTypeOfUseValidationDto_RequiredFields_MustBeSet()
    {
        var dto = new CreatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 5,
            TypeOfUseId = 10
        };

        Assert.True(dto.PropertyTypeId > 0);
        Assert.True(dto.TypeOfUseId > 0);
    }

    [Fact]
    public void UpdatePropertyDescriptionAndTypeOfUseValidationDto_CanUpdateIndividualFields()
    {
        var dto = new UpdatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 5,
            TypeOfUseId = 10
        };

        dto.PropertyTypeId = 6;
        Assert.Equal(6, dto.PropertyTypeId);

        dto.TypeOfUseId = 11;
        Assert.Equal(11, dto.TypeOfUseId);

        dto.IsActive = true;
        Assert.True(dto.IsActive);
    }
}
