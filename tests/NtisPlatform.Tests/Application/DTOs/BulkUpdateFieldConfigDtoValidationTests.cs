using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using NtisPlatform.Application.DTOs.CommonDetails;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs;

public class BulkUpdateFieldConfigDtoValidationTests
{
    private static List<ValidationResult> ValidateDto(object dto)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(dto);
        Validator.TryValidateObject(dto, validationContext, validationResults, true);
        return validationResults;
    }

    #region CreateBulkUpdateFieldConfigDto Tests

    [Fact]
    public void CreateBulkUpdateFieldConfigDto_WithValidData_PassesValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = "Property Type",
            ControlType = "Dropdown",
            DataType = "String",
            Placeholder = "Select Property Type",
            IsRequired = true,
            MaxLength = 100,
            ValidationRegex = null,
            DefaultValue = null,
            SequenceNo = 1,
            BindApi = "/api/PropertyType",
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void CreateBulkUpdateFieldConfigDto_WithMissingFieldName_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = string.Empty, // Missing
            DisplayName = "Property Type",
            ControlType = "Dropdown",
            DataType = "String",
            SequenceNo = 1,
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateFieldConfig_FieldName_Required"));
    }

    [Fact]
    public void CreateBulkUpdateFieldConfigDto_WithMissingDisplayName_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = string.Empty, // Missing
            ControlType = "Dropdown",
            DataType = "String",
            SequenceNo = 1,
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateFieldConfig_DisplayName_Required"));
    }

    [Fact]
    public void CreateBulkUpdateFieldConfigDto_WithMissingControlType_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = "Property Type",
            ControlType = string.Empty, // Missing
            DataType = "String",
            SequenceNo = 1,
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateFieldConfig_ControlType_Required"));
    }

    [Fact]
    public void CreateBulkUpdateFieldConfigDto_WithMissingDataType_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = "Property Type",
            ControlType = "Dropdown",
            DataType = string.Empty, // Missing
            SequenceNo = 1,
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateFieldConfig_DataType_Required"));
    }

    [Fact]
    public void CreateBulkUpdateFieldConfigDto_WithFieldNameTooLong_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = new string('A', 101), // Exceeds 100 characters
            DisplayName = "Property Type",
            ControlType = "Dropdown",
            DataType = "String",
            SequenceNo = 1,
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateFieldConfig_FieldName_MaxLen_100"));
    }

    [Fact]
    public void CreateBulkUpdateFieldConfigDto_WithDisplayNameTooLong_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = new string('A', 201), // Exceeds 200 characters
            ControlType = "Dropdown",
            DataType = "String",
            SequenceNo = 1,
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateFieldConfig_DisplayName_MaxLen_200"));
    }

    [Fact]
    public void CreateBulkUpdateFieldConfigDto_WithSequenceNoOutOfRange_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = "Property Type",
            ControlType = "Dropdown",
            DataType = "String",
            SequenceNo = 10000, // Exceeds max range
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateFieldConfig_SequenceNo_Range"));
    }

    [Fact]
    public void CreateBulkUpdateFieldConfigDto_WithSequenceNoZero_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = "Property Type",
            ControlType = "Dropdown",
            DataType = "String",
            SequenceNo = 0, // Below min range
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateFieldConfig_SequenceNo_Range"));
    }

    [Fact]
    public void CreateBulkUpdateFieldConfigDto_WithPlaceholderTooLong_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = "Property Type",
            ControlType = "Dropdown",
            DataType = "String",
            Placeholder = new string('A', 501), // Exceeds 500 characters
            SequenceNo = 1,
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateFieldConfig_Placeholder_MaxLen_500"));
    }

    [Fact]
    public void CreateBulkUpdateFieldConfigDto_WithValidationRegexTooLong_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "Email",
            DisplayName = "Email",
            ControlType = "TextBox",
            DataType = "String",
            ValidationRegex = new string('A', 501), // Exceeds 500 characters
            SequenceNo = 1,
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateFieldConfig_ValidationRegex_MaxLen_500"));
    }

    [Fact]
    public void CreateBulkUpdateFieldConfigDto_WithBindApiTooLong_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = "Property Type",
            ControlType = "Dropdown",
            DataType = "String",
            BindApi = new string('A', 501), // Exceeds 500 characters
            SequenceNo = 1,
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateFieldConfig_BindApi_MaxLen_500"));
    }

    #endregion

    #region UpdateBulkUpdateFieldConfigDto Tests

    [Fact]
    public void UpdateBulkUpdateFieldConfigDto_WithValidData_PassesValidation()
    {
        // Arrange
        var dto = new UpdateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = "Property Type (Updated)",
            ControlType = "Dropdown",
            DataType = "String",
            Placeholder = "Please Select Property Type",
            IsRequired = true,
            MaxLength = 150,
            ValidationRegex = null,
            DefaultValue = null,
            SequenceNo = 1,
            BindApi = "/api/PropertyType/GetAll",
            UpdatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void UpdateBulkUpdateFieldConfigDto_WithMissingFieldName_FailsValidation()
    {
        // Arrange
        var dto = new UpdateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = string.Empty, // Missing
            DisplayName = "Property Type",
            ControlType = "Dropdown",
            DataType = "String",
            SequenceNo = 1,
            UpdatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateFieldConfig_FieldName_Required"));
    }

    [Fact]
    public void UpdateBulkUpdateFieldConfigDto_WithAllOptionalFieldsNull_PassesValidation()
    {
        // Arrange
        var dto = new UpdateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "Status",
            DisplayName = "Status",
            ControlType = "Checkbox",
            DataType = "Boolean",
            Placeholder = null,
            IsRequired = false,
            MaxLength = null,
            ValidationRegex = null,
            DefaultValue = null,
            SequenceNo = 1,
            BindApi = null,
            UpdatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void UpdateBulkUpdateFieldConfigDto_WithSequenceNoOutOfRange_FailsValidation()
    {
        // Arrange
        var dto = new UpdateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = "Property Type",
            ControlType = "Dropdown",
            DataType = "String",
            SequenceNo = -1, // Below min range
            UpdatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateFieldConfig_SequenceNo_Range"));
    }

    #endregion

    #region Property Value Tests

    [Fact]
    public void BulkUpdateFieldConfigDto_Properties_AreSetCorrectly()
    {
        // Arrange & Act
        var dto = new BulkUpdateFieldConfigDto
        {
            Id = 1,
            BulkUpdateMasterId = 2,
            FieldName = "TestField",
            DisplayName = "Test Field",
            ControlType = "TextBox",
            DataType = "String",
            Placeholder = "Enter value",
            IsRequired = true,
            MaxLength = 200,
            ValidationRegex = @"^\d+$",
            DefaultValue = "Default",
            SequenceNo = 5,
            IsActive = true,
            BindApi = "/api/test"
        };

        // Assert
        dto.Id.Should().Be(1);
        dto.BulkUpdateMasterId.Should().Be(2);
        dto.FieldName.Should().Be("TestField");
        dto.DisplayName.Should().Be("Test Field");
        dto.ControlType.Should().Be("TextBox");
        dto.DataType.Should().Be("String");
        dto.Placeholder.Should().Be("Enter value");
        dto.IsRequired.Should().BeTrue();
        dto.MaxLength.Should().Be(200);
        dto.ValidationRegex.Should().Be(@"^\d+$");
        dto.DefaultValue.Should().Be("Default");
        dto.SequenceNo.Should().Be(5);
        dto.IsActive.Should().BeTrue();
        dto.BindApi.Should().Be("/api/test");
    }

    #endregion
}
