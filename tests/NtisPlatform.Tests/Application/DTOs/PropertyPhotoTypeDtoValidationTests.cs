using NtisPlatform.Application.DTOs.Master.PropertyPhotoType;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Tests.Application.DTOs;

/// <summary>
/// Comprehensive validation tests for PropertyPhotoType DTOs to achieve 100% code coverage
/// </summary>
public class PropertyPhotoTypeDtoValidationTests
{
    #region PropertyPhotoTypeDto Tests

    [Fact]
    public void PropertyPhotoTypeDto_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var dto = new PropertyPhotoTypeDto
        {
            Id = 1,
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front View",
            Description = "Front facade of the property",
            DisplayOrder = 1,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal("FRONT", dto.PhotoTypeCode);
        Assert.Equal("Front View", dto.PhotoTypeName);
        Assert.Equal("Front facade of the property", dto.Description);
        Assert.Equal(1, dto.DisplayOrder);
        Assert.True(dto.IsActive);
        Assert.NotNull(dto.CreatedDate);
        Assert.NotNull(dto.UpdatedDate);
    }

    [Fact]
    public void PropertyPhotoTypeDto_DefaultValues()
    {
        // Arrange & Act
        var dto = new PropertyPhotoTypeDto();

        // Assert
        Assert.Equal(0, dto.Id);
        Assert.Equal(string.Empty, dto.PhotoTypeCode);
        Assert.Equal(string.Empty, dto.PhotoTypeName);
        Assert.Null(dto.Description);
        Assert.Null(dto.DisplayOrder);
        Assert.False(dto.IsActive);
    }

    [Fact]
    public void PropertyPhotoTypeDto_OptionalFieldsCanBeNull()
    {
        // Arrange & Act
        var dto = new PropertyPhotoTypeDto
        {
            Id = 1,
            PhotoTypeCode = "TEST",
            PhotoTypeName = "Test Name",
            Description = null,
            DisplayOrder = null,
            IsActive = true
        };

        // Assert
        Assert.Null(dto.Description);
        Assert.Null(dto.DisplayOrder);
    }

    #endregion

    #region CreatePropertyPhotoTypeDto Tests

    [Fact]
    public void CreatePropertyPhotoTypeDto_WithValidData_IsValid()
    {
        // Arrange
        var dto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front View",
            Description = "Front facade of the property",
            DisplayOrder = 1,
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public void CreatePropertyPhotoTypeDto_WithoutPhotoTypeCode_IsInvalid()
    {
        // Arrange
        var dto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = string.Empty,
            PhotoTypeName = "Front View",
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r =>
            r.MemberNames.Contains(nameof(CreatePropertyPhotoTypeDto.PhotoTypeCode)));
    }

    [Fact]
    public void CreatePropertyPhotoTypeDto_WithNullPhotoTypeCode_IsInvalid()
    {
        // Arrange
        var dto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = null!,
            PhotoTypeName = "Front View",
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r =>
            r.MemberNames.Contains(nameof(CreatePropertyPhotoTypeDto.PhotoTypeCode)));
    }

    [Fact]
    public void CreatePropertyPhotoTypeDto_WithPhotoTypeCodeExceeding50Characters_IsInvalid()
    {
        // Arrange
        var dto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = new string('A', 51),
            PhotoTypeName = "Front View",
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r =>
            r.MemberNames.Contains(nameof(CreatePropertyPhotoTypeDto.PhotoTypeCode)));
    }

    [Fact]
    public void CreatePropertyPhotoTypeDto_WithPhotoTypeCodeAt50Characters_IsValid()
    {
        // Arrange
        var dto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = new string('A', 50),
            PhotoTypeName = "Front View",
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public void CreatePropertyPhotoTypeDto_WithoutPhotoTypeName_IsInvalid()
    {
        // Arrange
        var dto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "FRONT",
            PhotoTypeName = string.Empty,
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r =>
            r.MemberNames.Contains(nameof(CreatePropertyPhotoTypeDto.PhotoTypeName)));
    }

    [Fact]
    public void CreatePropertyPhotoTypeDto_WithNullPhotoTypeName_IsInvalid()
    {
        // Arrange
        var dto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "FRONT",
            PhotoTypeName = null!,
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r =>
            r.MemberNames.Contains(nameof(CreatePropertyPhotoTypeDto.PhotoTypeName)));
    }

    [Fact]
    public void CreatePropertyPhotoTypeDto_WithPhotoTypeNameExceeding200Characters_IsInvalid()
    {
        // Arrange
        var dto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "FRONT",
            PhotoTypeName = new string('B', 201),
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r =>
            r.MemberNames.Contains(nameof(CreatePropertyPhotoTypeDto.PhotoTypeName)));
    }

    [Fact]
    public void CreatePropertyPhotoTypeDto_WithPhotoTypeNameAt200Characters_IsValid()
    {
        // Arrange
        var dto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "FRONT",
            PhotoTypeName = new string('B', 200),
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public void CreatePropertyPhotoTypeDto_WithDescriptionExceeding500Characters_IsInvalid()
    {
        // Arrange
        var dto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front View",
            Description = new string('C', 501),
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r =>
            r.MemberNames.Contains(nameof(CreatePropertyPhotoTypeDto.Description)));
    }

    [Fact]
    public void CreatePropertyPhotoTypeDto_WithDescriptionAt500Characters_IsValid()
    {
        // Arrange
        var dto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front View",
            Description = new string('C', 500),
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public void CreatePropertyPhotoTypeDto_WithNullDescription_IsValid()
    {
        // Arrange
        var dto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front View",
            Description = null,
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public void CreatePropertyPhotoTypeDto_WithNullDisplayOrder_IsValid()
    {
        // Arrange
        var dto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front View",
            DisplayOrder = null,
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public void CreatePropertyPhotoTypeDto_WithPositiveDisplayOrder_IsValid()
    {
        // Arrange
        var dto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front View",
            DisplayOrder = 5,
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public void CreatePropertyPhotoTypeDto_WithNegativeDisplayOrder_IsValid()
    {
        // Arrange
        var dto = new CreatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "FRONT",
            PhotoTypeName = "Front View",
            DisplayOrder = -1,
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.Empty(validationResults);
    }

    #endregion

    #region UpdatePropertyPhotoTypeDto Tests

    [Fact]
    public void UpdatePropertyPhotoTypeDto_WithValidData_IsValid()
    {
        // Arrange
        var dto = new UpdatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "BACK",
            PhotoTypeName = "Back View",
            Description = "Back facade of the property",
            DisplayOrder = 2,
            IsActive = true,
            UpdatedBy = 200
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public void UpdatePropertyPhotoTypeDto_WithoutPhotoTypeCode_IsInvalid()
    {
        // Arrange
        var dto = new UpdatePropertyPhotoTypeDto
        {
            PhotoTypeCode = string.Empty,
            PhotoTypeName = "Back View",
            IsActive = true,
            UpdatedBy = 200
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r =>
            r.MemberNames.Contains(nameof(UpdatePropertyPhotoTypeDto.PhotoTypeCode)));
    }

    [Fact]
    public void UpdatePropertyPhotoTypeDto_WithNullPhotoTypeCode_IsInvalid()
    {
        // Arrange
        var dto = new UpdatePropertyPhotoTypeDto
        {
            PhotoTypeCode = null!,
            PhotoTypeName = "Back View",
            IsActive = true,
            UpdatedBy = 200
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r =>
            r.MemberNames.Contains(nameof(UpdatePropertyPhotoTypeDto.PhotoTypeCode)));
    }

    [Fact]
    public void UpdatePropertyPhotoTypeDto_WithPhotoTypeCodeExceeding50Characters_IsInvalid()
    {
        // Arrange
        var dto = new UpdatePropertyPhotoTypeDto
        {
            PhotoTypeCode = new string('A', 51),
            PhotoTypeName = "Back View",
            IsActive = true,
            UpdatedBy = 200
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r =>
            r.MemberNames.Contains(nameof(UpdatePropertyPhotoTypeDto.PhotoTypeCode)));
    }

    [Fact]
    public void UpdatePropertyPhotoTypeDto_WithoutPhotoTypeName_IsInvalid()
    {
        // Arrange
        var dto = new UpdatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "BACK",
            PhotoTypeName = string.Empty,
            IsActive = true,
            UpdatedBy = 200
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r =>
            r.MemberNames.Contains(nameof(UpdatePropertyPhotoTypeDto.PhotoTypeName)));
    }

    [Fact]
    public void UpdatePropertyPhotoTypeDto_WithPhotoTypeNameExceeding200Characters_IsInvalid()
    {
        // Arrange
        var dto = new UpdatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "BACK",
            PhotoTypeName = new string('B', 201),
            IsActive = true,
            UpdatedBy = 200
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r =>
            r.MemberNames.Contains(nameof(UpdatePropertyPhotoTypeDto.PhotoTypeName)));
    }

    [Fact]
    public void UpdatePropertyPhotoTypeDto_WithDescriptionExceeding500Characters_IsInvalid()
    {
        // Arrange
        var dto = new UpdatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "BACK",
            PhotoTypeName = "Back View",
            Description = new string('C', 501),
            IsActive = true,
            UpdatedBy = 200
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r =>
            r.MemberNames.Contains(nameof(UpdatePropertyPhotoTypeDto.Description)));
    }

    [Fact]
    public void UpdatePropertyPhotoTypeDto_WithNullDescription_IsValid()
    {
        // Arrange
        var dto = new UpdatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "BACK",
            PhotoTypeName = "Back View",
            Description = null,
            IsActive = true,
            UpdatedBy = 200
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public void UpdatePropertyPhotoTypeDto_WithIsActiveFalse_IsValid()
    {
        // Arrange
        var dto = new UpdatePropertyPhotoTypeDto
        {
            PhotoTypeCode = "BACK",
            PhotoTypeName = "Back View",
            IsActive = false,
            UpdatedBy = 200
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.Empty(validationResults);
        Assert.False(dto.IsActive);
    }

    #endregion

    #region Helper Methods

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }

    #endregion
}
