using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Master.PropertyDescriptionAndTypeOfUseValidation;

namespace NtisPlatform.Tests.Application.DTOs;

public class PropertyDescriptionAndTypeOfUseValidationDtoValidationTests
{
    private static IList<ValidationResult> ValidateDto<T>(T dto)
    {
        var validationContext = new ValidationContext(dto!, null, null);
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(dto!, validationContext, validationResults, true);
        return validationResults;
    }

    [Fact]
    public void CreateDto_WithValidData_PassesValidation()
    {
        var dto = new CreatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = true,
            CreatedBy = 1
        };

        var validationResults = ValidateDto(dto);

        Assert.Empty(validationResults);
    }

    [Fact]
    public void CreateDto_WithoutPropertyTypeId_FailsValidation()
    {
        var dto = new CreatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 0,
            TypeOfUseId = 10,
            IsActive = true
        };

        var validationResults = ValidateDto(dto);

        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, v => v.MemberNames.Contains("PropertyTypeId"));
    }

    [Fact]
    public void CreateDto_WithoutTypeOfUseId_FailsValidation()
    {
        var dto = new CreatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 5,
            TypeOfUseId = 0,
            IsActive = true
        };

        var validationResults = ValidateDto(dto);

        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, v => v.MemberNames.Contains("TypeOfUseId"));
    }

    [Fact]
    public void UpdateDto_WithValidData_PassesValidation()
    {
        var dto = new UpdatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 6,
            TypeOfUseId = 11,
            IsActive = false,
            UpdatedBy = 2
        };

        var validationResults = ValidateDto(dto);

        Assert.Empty(validationResults);
    }

    [Fact]
    public void UpdateDto_WithoutPropertyTypeId_FailsValidation()
    {
        var dto = new UpdatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 0,
            TypeOfUseId = 10,
            IsActive = true
        };

        var validationResults = ValidateDto(dto);

        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, v => v.MemberNames.Contains("PropertyTypeId"));
    }

    [Fact]
    public void UpdateDto_WithoutTypeOfUseId_FailsValidation()
    {
        var dto = new UpdatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 5,
            TypeOfUseId = 0,
            IsActive = true
        };

        var validationResults = ValidateDto(dto);

        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, v => v.MemberNames.Contains("TypeOfUseId"));
    }

    [Fact]
    public void CreateDto_IsActiveTrue_WorksCorrectly()
    {
        var dto = new CreatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = true
        };

        Assert.True(dto.IsActive);
    }

    [Fact]
    public void CreateDto_IsActiveFalse_WorksCorrectly()
    {
        var dto = new CreatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = false
        };

        Assert.False(dto.IsActive);
    }

    [Fact]
    public void UpdateDto_IsActiveTrue_WorksCorrectly()
    {
        var dto = new UpdatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = true
        };

        Assert.True(dto.IsActive);
    }

    [Fact]
    public void UpdateDto_IsActiveFalse_WorksCorrectly()
    {
        var dto = new UpdatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = false
        };

        Assert.False(dto.IsActive);
    }

    [Fact]
    public void CreateDto_CreatedBy_CanBeNull()
    {
        var dto = new CreatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            CreatedBy = null
        };

        Assert.Null(dto.CreatedBy);
    }

    [Fact]
    public void UpdateDto_UpdatedBy_CanBeNull()
    {
        var dto = new UpdatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            UpdatedBy = null
        };

        Assert.Null(dto.UpdatedBy);
    }

    [Fact]
    public void CreateDto_WithLargeIds_WorksCorrectly()
    {
        var dto = new CreatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = int.MaxValue,
            TypeOfUseId = int.MaxValue,
            IsActive = true
        };

        Assert.Equal(int.MaxValue, dto.PropertyTypeId);
        Assert.Equal(int.MaxValue, dto.TypeOfUseId);
    }

    [Fact]
    public void UpdateDto_WithLargeIds_WorksCorrectly()
    {
        var dto = new UpdatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = int.MaxValue,
            TypeOfUseId = int.MaxValue,
            IsActive = true
        };

        Assert.Equal(int.MaxValue, dto.PropertyTypeId);
        Assert.Equal(int.MaxValue, dto.TypeOfUseId);
    }
}
