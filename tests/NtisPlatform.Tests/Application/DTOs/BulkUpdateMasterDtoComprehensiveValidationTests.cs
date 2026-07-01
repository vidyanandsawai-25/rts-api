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
            DisplaySequence = 1,
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
            DisplaySequence = 1,
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
            DisplaySequence = 1,
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
            DisplaySequence = 1,
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_UpdateName_MaxLen_200"));
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithMaxLengthUpdateNameMarathi_PassesValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "VALID_CODE",
            UpdateName = "Valid Name",
            UpdateNameMarathi = new string('म', 200), // Max length
            ReferenceTableName = "ValidTable",
            DisplaySequence = 1,
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithExceedingUpdateNameMarathiLength_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "VALID_CODE",
            UpdateName = "Valid Name",
            UpdateNameMarathi = new string('म', 201), // Exceeds max length
            ReferenceTableName = "ValidTable",
            DisplaySequence = 1,
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_UpdateNameMarathi_MaxLen_200"));
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
            DisplaySequence = 1,
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
            DisplaySequence = 1,
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_ReferenceTableName_MaxLen_200"));
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithMaxLengthDescription_PassesValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "VALID_CODE",
            UpdateName = "Valid Name",
            ReferenceTableName = "ValidTable",
            DisplaySequence = 1,
            Description = new string('D', 1000), // Max length
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithExceedingDescriptionLength_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "VALID_CODE",
            UpdateName = "Valid Name",
            ReferenceTableName = "ValidTable",
            DisplaySequence = 1,
            Description = new string('D', 1001), // Exceeds max length
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_Description_MaxLen_1000"));
    }

    #endregion

    #region CreateBulkUpdateMasterDto - DisplaySequence Range Tests

    [Fact]
    public void CreateBulkUpdateMasterDto_WithDisplaySequenceZero_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "VALID_CODE",
            UpdateName = "Valid Name",
            ReferenceTableName = "ValidTable",
            DisplaySequence = 0, // Below minimum
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_DisplaySequence_Range"));
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithDisplaySequenceNegative_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "VALID_CODE",
            UpdateName = "Valid Name",
            ReferenceTableName = "ValidTable",
            DisplaySequence = -1, // Negative
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_DisplaySequence_Range"));
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithDisplaySequence10000_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "VALID_CODE",
            UpdateName = "Valid Name",
            ReferenceTableName = "ValidTable",
            DisplaySequence = 10000, // Exceeds maximum
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_DisplaySequence_Range"));
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithDisplaySequence1_PassesValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "VALID_CODE",
            UpdateName = "Valid Name",
            ReferenceTableName = "ValidTable",
            DisplaySequence = 1, // Minimum valid
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithDisplaySequence9999_PassesValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "VALID_CODE",
            UpdateName = "Valid Name",
            ReferenceTableName = "ValidTable",
            DisplaySequence = 9999, // Maximum valid
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
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
            UpdateNameMarathi = "वैध नाव",
            ReferenceTableName = "ValidTable",
            DisplaySequence = 5,
            Description = "Valid description",
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
            DisplaySequence = 0,
            UpdatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("Required") || vr.ErrorMessage!.Contains("Range"));
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
            DisplaySequence = 1,
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithSpecialCharactersInDescription_PassesValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "VALID_CODE",
            UpdateName = "Valid Name",
            ReferenceTableName = "ValidTable",
            DisplaySequence = 1,
            Description = "Description with special chars: @#$%^&*()_+-=[]{}|;:',.<>?/~`",
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithMixedLanguages_PassesValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "MIXED_LANG",
            UpdateName = "English Name",
            UpdateNameMarathi = "मराठी नाव with English मिश्रित",
            ReferenceTableName = "ValidTable",
            DisplaySequence = 1,
            Description = "Mixed: English and मराठी together",
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
            DisplaySequence = 1,
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
            DisplaySequence = 1,
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
            DisplaySequence = 0, // Out of range
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().HaveCountGreaterThan(2);
    }

    #endregion

    #region Null Description Tests

    [Fact]
    public void CreateBulkUpdateMasterDto_WithNullDescription_PassesValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "VALID_CODE",
            UpdateName = "Valid Name",
            ReferenceTableName = "ValidTable",
            DisplaySequence = 1,
            Description = null, // Optional field
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void UpdateBulkUpdateMasterDto_WithNullDescription_PassesValidation()
    {
        // Arrange
        var dto = new UpdateBulkUpdateMasterDto
        {
            UpdateCode = "VALID_CODE",
            UpdateName = "Valid Name",
            ReferenceTableName = "ValidTable",
            DisplaySequence = 1,
            Description = null, // Optional field
            UpdatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    #endregion

    #region Empty Optional String Tests

    [Fact]
    public void CreateBulkUpdateMasterDto_WithEmptyOptionalStrings_PassesValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "VALID_CODE",
            UpdateName = "Valid Name",
            UpdateNameMarathi = string.Empty, // Optional
            ReferenceTableName = "ValidTable",
            DisplaySequence = 1,
            Description = string.Empty, // Optional
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    #endregion
}
