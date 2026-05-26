using NtisPlatform.Application.DTOs.Master.PropertyAssessmentStatus;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs;

/// <summary>
/// Tests for PropertyAssessmentStatus DTOs
/// Ensures validation attributes and property behaviors work correctly
/// </summary>
public class PropertyAssessmentStatusDtoTests
{
    #region PropertyAssessmentStatusDto Tests

    [Fact]
    public void PropertyAssessmentStatusDto_CanSetAndGetAllProperties()
    {
        // Arrange & Act
        var dto = new PropertyAssessmentStatusDto
        {
            Id = 1,
            StatusName = "Test Status",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal("Test Status", dto.StatusName);
        Assert.True(dto.IsActive);
        Assert.NotNull(dto.CreatedDate);
        Assert.NotNull(dto.UpdatedDate);
    }

    [Fact]
    public void PropertyAssessmentStatusDto_DefaultStatusName_IsEmptyString()
    {
        // Arrange & Act
        var dto = new PropertyAssessmentStatusDto();

        // Assert
        Assert.Equal(string.Empty, dto.StatusName);
    }

    #endregion

    #region CreatePropertyAssessmentStatusDto Tests

    [Fact]
    public void CreatePropertyAssessmentStatusDto_StatusName_RequiredValidation()
    {
        // Arrange
        var dto = new CreatePropertyAssessmentStatusDto { StatusName = null! };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreatePropertyAssessmentStatusDto.StatusName)));
    }

    [Fact]
    public void CreatePropertyAssessmentStatusDto_StatusName_MaxLengthValidation()
    {
        // Arrange
        var dto = new CreatePropertyAssessmentStatusDto 
        { 
            StatusName = new string('A', 31) // 31 characters, exceeds max of 30
        };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreatePropertyAssessmentStatusDto.StatusName)));
    }

    [Fact]
    public void CreatePropertyAssessmentStatusDto_StatusName_AcceptsMaxLength()
    {
        // Arrange
        var dto = new CreatePropertyAssessmentStatusDto 
        { 
            StatusName = new string('A', 30), // Exactly 30 characters
            IsActive = true
        };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, context, results, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void CreatePropertyAssessmentStatusDto_StatusName_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new CreatePropertyAssessmentStatusDto
        {
            StatusName = "  Test Status  "
        };

        // Assert
        Assert.Equal("Test Status", dto.StatusName);
    }

    [Fact]
    public void CreatePropertyAssessmentStatusDto_StatusName_HandlesNull()
    {
        // Arrange & Act
        var dto = new CreatePropertyAssessmentStatusDto
        {
            StatusName = null!
        };

        // Assert
        Assert.Equal(string.Empty, dto.StatusName);
    }

    [Fact]
    public void CreatePropertyAssessmentStatusDto_StatusName_HandlesEmptyString()
    {
        // Arrange & Act
        var dto = new CreatePropertyAssessmentStatusDto
        {
            StatusName = string.Empty
        };

        // Assert
        Assert.Equal(string.Empty, dto.StatusName);
    }

    [Fact]
    public void CreatePropertyAssessmentStatusDto_IsActive_DefaultValue()
    {
        // Arrange & Act
        var dto = new CreatePropertyAssessmentStatusDto();

        // Assert - Should have default value from struct (false)
        Assert.False(dto.IsActive);
    }

    [Fact]
    public void CreatePropertyAssessmentStatusDto_IsActive_CanSetTrue()
    {
        // Arrange & Act
        var dto = new CreatePropertyAssessmentStatusDto { IsActive = true };

        // Assert
        Assert.True(dto.IsActive);
    }

    [Fact]
    public void CreatePropertyAssessmentStatusDto_CreatedBy_CanBeSet()
    {
        // Arrange & Act
        var dto = new CreatePropertyAssessmentStatusDto { CreatedBy = 123 };

        // Assert
        Assert.Equal(123, dto.CreatedBy);
    }

    [Fact]
    public void CreatePropertyAssessmentStatusDto_CreatedBy_CanBeNull()
    {
        // Arrange & Act
        var dto = new CreatePropertyAssessmentStatusDto { CreatedBy = null };

        // Assert
        Assert.Null(dto.CreatedBy);
    }

    [Fact]
    public void CreatePropertyAssessmentStatusDto_StatusName_PreservesNonWhitespaceContent()
    {
        // Arrange & Act
        var dto = new CreatePropertyAssessmentStatusDto
        {
            StatusName = "Status With Spaces"
        };

        // Assert
        Assert.Equal("Status With Spaces", dto.StatusName);
    }

    [Fact]
    public void CreatePropertyAssessmentStatusDto_ValidDto_PassesValidation()
    {
        // Arrange
        var dto = new CreatePropertyAssessmentStatusDto
        {
            StatusName = "Valid Status",
            IsActive = true,
            CreatedBy = 1
        };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, context, results, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    #endregion

    #region UpdatePropertyAssessmentStatusDto Tests

    [Fact]
    public void UpdatePropertyAssessmentStatusDto_StatusName_RequiredValidation()
    {
        // Arrange
        var dto = new UpdatePropertyAssessmentStatusDto { StatusName = null! };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdatePropertyAssessmentStatusDto.StatusName)));
    }

    [Fact]
    public void UpdatePropertyAssessmentStatusDto_StatusName_MaxLengthValidation()
    {
        // Arrange
        var dto = new UpdatePropertyAssessmentStatusDto 
        { 
            StatusName = new string('B', 31) // 31 characters, exceeds max of 30
        };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdatePropertyAssessmentStatusDto.StatusName)));
    }

    [Fact]
    public void UpdatePropertyAssessmentStatusDto_StatusName_TrimsWhitespace()
    {
        // Arrange & Act
        var dto = new UpdatePropertyAssessmentStatusDto
        {
            StatusName = "  Updated Status  "
        };

        // Assert
        Assert.Equal("Updated Status", dto.StatusName);
    }

    [Fact]
    public void UpdatePropertyAssessmentStatusDto_StatusName_HandlesNull()
    {
        // Arrange & Act
        var dto = new UpdatePropertyAssessmentStatusDto
        {
            StatusName = null!
        };

        // Assert
        Assert.Equal(string.Empty, dto.StatusName);
    }

    [Fact]
    public void UpdatePropertyAssessmentStatusDto_IsActive_CanSetFalse()
    {
        // Arrange & Act
        var dto = new UpdatePropertyAssessmentStatusDto { IsActive = false };

        // Assert
        Assert.False(dto.IsActive);
    }

    [Fact]
    public void UpdatePropertyAssessmentStatusDto_UpdatedBy_CanBeSet()
    {
        // Arrange & Act
        var dto = new UpdatePropertyAssessmentStatusDto { UpdatedBy = 456 };

        // Assert
        Assert.Equal(456, dto.UpdatedBy);
    }

    [Fact]
    public void UpdatePropertyAssessmentStatusDto_UpdatedBy_CanBeNull()
    {
        // Arrange & Act
        var dto = new UpdatePropertyAssessmentStatusDto { UpdatedBy = null };

        // Assert
        Assert.Null(dto.UpdatedBy);
    }

    [Fact]
    public void UpdatePropertyAssessmentStatusDto_ValidDto_PassesValidation()
    {
        // Arrange
        var dto = new UpdatePropertyAssessmentStatusDto
        {
            StatusName = "Updated Valid Status",
            IsActive = false,
            UpdatedBy = 2
        };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, context, results, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdatePropertyAssessmentStatusDto_StatusName_AcceptsExactly30Characters()
    {
        // Arrange
        var dto = new UpdatePropertyAssessmentStatusDto 
        { 
            StatusName = "123456789012345678901234567890" // Exactly 30 characters
        };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, context, results, true);

        // Assert
        Assert.True(isValid);
    }

    #endregion

    #region PropertyAssessmentStatusQueryParameters Tests

    [Fact]
    public void PropertyAssessmentStatusQueryParameters_CanSetIsActive()
    {
        // Arrange & Act
        var query = new PropertyAssessmentStatusQueryParameters { IsActive = true };

        // Assert
        Assert.True(query.IsActive);
    }

    [Fact]
    public void PropertyAssessmentStatusQueryParameters_IsActive_CanBeNull()
    {
        // Arrange & Act
        var query = new PropertyAssessmentStatusQueryParameters { IsActive = null };

        // Assert
        Assert.Null(query.IsActive);
    }

    [Fact]
    public void PropertyAssessmentStatusQueryParameters_CanSetStatusName()
    {
        // Arrange & Act
        var query = new PropertyAssessmentStatusQueryParameters { StatusName = "Search Term" };

        // Assert
        Assert.Equal("Search Term", query.StatusName);
    }

    [Fact]
    public void PropertyAssessmentStatusQueryParameters_StatusName_CanBeNull()
    {
        // Arrange & Act
        var query = new PropertyAssessmentStatusQueryParameters { StatusName = null };

        // Assert
        Assert.Null(query.StatusName);
    }

    [Fact]
    public void PropertyAssessmentStatusQueryParameters_InheritsFromBaseQueryParameters()
    {
        // Arrange & Act
        var query = new PropertyAssessmentStatusQueryParameters();

        // Assert - Should have base properties like PageNumber, PageSize, etc.
        Assert.IsAssignableFrom<NtisPlatform.Application.DTOs.Queries.BaseQueryParameters>(query);
    }

    [Fact]
    public void PropertyAssessmentStatusQueryParameters_CanSetId()
    {
        // Arrange & Act
        var query = new PropertyAssessmentStatusQueryParameters { Id = 123 };

        // Assert
        Assert.Equal(123, query.Id);
    }

    [Fact]
    public void PropertyAssessmentStatusQueryParameters_Id_CanBeNull()
    {
        // Arrange & Act
        var query = new PropertyAssessmentStatusQueryParameters { Id = null };

        // Assert
        Assert.Null(query.Id);
    }

    #endregion
}
