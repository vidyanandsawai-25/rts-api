using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using NtisPlatform.Application.DTOs.CommonDetails;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs;

/// <summary>
/// Comprehensive validation tests for BulkUpdateMaster DTOs covering
/// all validation attributes, edge cases, and boundary conditions.
/// </summary>
public class BulkUpdateMasterDtoComprehensiveValidationTests
{
    private static List<ValidationResult> ValidateDto(object dto)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(dto);
        Validator.TryValidateObject(dto, validationContext, validationResults, true);
        return validationResults;
    }

    #region CreateBulkUpdateMasterDto - String Length Tests

    [Fact]
    public void CreateBulkUpdateMasterDto_WithMaxLengthUpdateCode_PassesValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = new string('A', 50), // Max length
            UpdateName = "Valid Name",
            ReferenceTableName = "ValidTable",
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithExceedingUpdateCodeLength_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = new string('A', 51), // Exceeds max length
            UpdateName = "Valid Name",
            ReferenceTableName = "ValidTable",
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_UpdateCode_MaxLen_50"));
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithMaxLengthUpdateName_PassesValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "VALID_CODE",
            UpdateName = new string('B', 200), // Max length
            ReferenceTableName = "ValidTable",
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithExceedingUpdateNameLength_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "VALID_CODE",
            UpdateName = new string('B', 201), // Exceeds max length
            ReferenceTableName = "ValidTable",
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_UpdateName_MaxLen_200"));
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithMaxLengthReferenceTableName_PassesValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "VALID_CODE",
            UpdateName = "Valid Name",
            ReferenceTableName = new string('T', 200), // Max length
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithExceedingReferenceTableNameLength_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "VALID_CODE",
            UpdateName = "Valid Name",
            ReferenceTableName = new string('T', 201), // Exceeds max length
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_ReferenceTableName_MaxLen_200"));
    }

    #endregion

    #region UpdateBulkUpdateMasterDto - Validation Tests

    [Fact]
    public void UpdateBulkUpdateMasterDto_WithValidData_PassesValidation()
    {
        // Arrange
        var dto = new UpdateBulkUpdateMasterDto
        {
            UpdateCode = "VALID_CODE",
            UpdateName = "Valid Name",
            ReferenceTableName = "ValidTable",
            UpdatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void UpdateBulkUpdateMasterDto_WithMissingRequiredFields_FailsValidation()
    {
        // Arrange
        var dto = new UpdateBulkUpdateMasterDto
        {
            UpdateCode = string.Empty,
            UpdateName = string.Empty,
            ReferenceTableName = string.Empty,
            UpdatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("Required"));
    }

    #endregion

    #region Special Characters and Encoding Tests

    [Fact]
    public void CreateBulkUpdateMasterDto_WithSpecialCharactersInUpdateCode_PassesValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "CODE_WITH-DASH_AND.DOT",
            UpdateName = "Valid Name",
            ReferenceTableName = "ValidTable",
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithMixedLanguageUpdateName_PassesValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "MIXED_LANG",
            UpdateName = "मराठी नाव with English मिश्रित",
            ReferenceTableName = "ValidTable",
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    #endregion

    #region Whitespace Handling Tests

    [Fact]
    public void CreateBulkUpdateMasterDto_WithWhitespaceOnlyUpdateCode_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "   ", // Whitespace only
            UpdateName = "Valid Name",
            ReferenceTableName = "ValidTable",
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_UpdateCode_Required"));
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithLeadingTrailingWhitespace_PassesValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "  VALID_CODE  ",
            UpdateName = "  Valid Name  ",
            ReferenceTableName = "  ValidTable  ",
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    #endregion

    #region Multiple Validation Errors Tests

    [Fact]
    public void CreateBulkUpdateMasterDto_WithMultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = new string('A', 51), // Too long
            UpdateName = string.Empty, // Required
            ReferenceTableName = new string('T', 201), // Too long
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().HaveCountGreaterThan(1);
    }

    #endregion
}
