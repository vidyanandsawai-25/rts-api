using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using NtisPlatform.Application.DTOs.CommonDetails;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs;

public class BulkUpdateMasterDtoValidationTests
{
    private static List<ValidationResult> ValidateDto(object dto)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(dto);
        Validator.TryValidateObject(dto, validationContext, validationResults, true);
        return validationResults;
    }

    #region CreateBulkUpdateMasterDto Tests

    [Fact]
    public void CreateBulkUpdateMasterDto_WithValidData_PassesValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update",
            ReferenceTableName = "PropertyTypeMaster",
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithMissingUpdateCode_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = string.Empty, // Missing
            UpdateName = "Property Type Update",
            ReferenceTableName = "PropertyTypeMaster",
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_UpdateCode_Required"));
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithMissingUpdateName_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "PROP_TYPE",
            UpdateName = string.Empty, // Missing
            ReferenceTableName = "PropertyTypeMaster",
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_UpdateName_Required"));
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithMissingReferenceTableName_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update",
            ReferenceTableName = string.Empty, // Missing
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_ReferenceTableName_Required"));
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithUpdateCodeTooLong_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = new string('A', 51), // Exceeds 50 characters
            UpdateName = "Property Type Update",
            ReferenceTableName = "PropertyTypeMaster",
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_UpdateCode_MaxLen_50"));
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithUpdateNameTooLong_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "PROP_TYPE",
            UpdateName = new string('A', 201), // Exceeds 200 characters
            ReferenceTableName = "PropertyTypeMaster",
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_UpdateName_MaxLen_200"));
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithReferenceTableNameTooLong_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update",
            ReferenceTableName = new string('A', 201), // Exceeds 200 characters
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_ReferenceTableName_MaxLen_200"));
    }

    #endregion

    #region UpdateBulkUpdateMasterDto Tests

    [Fact]
    public void UpdateBulkUpdateMasterDto_WithValidData_PassesValidation()
    {
        // Arrange
        var dto = new UpdateBulkUpdateMasterDto
        {
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update (Modified)",
            ReferenceTableName = "PropertyTypeMaster",
            UpdatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void UpdateBulkUpdateMasterDto_WithMissingUpdateCode_FailsValidation()
    {
        // Arrange
        var dto = new UpdateBulkUpdateMasterDto
        {
            UpdateCode = string.Empty, // Missing
            UpdateName = "Property Type Update",
            ReferenceTableName = "PropertyTypeMaster",
            UpdatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_UpdateCode_Required"));
    }

    [Fact]
    public void UpdateBulkUpdateMasterDto_WithAllOptionalFieldsNull_PassesValidation()
    {
        // Arrange
        var dto = new UpdateBulkUpdateMasterDto
        {
            UpdateCode = "SIMPLE",
            UpdateName = "Simple Update",
            ReferenceTableName = "SimpleTable",
            IsApprovalRequired = null,
            UpdatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    #endregion

    #region Property Value Tests

    [Fact]
    public void BulkUpdateMasterDto_Properties_AreSetCorrectly()
    {
        // Arrange & Act
        var dto = new BulkUpdateMasterDto
        {
            Id = 1,
            UpdateCode = "TEST_CODE",
            UpdateName = "Test Update",
            ReferenceTableName = "TestTable",
            IsActive = true
        };

        // Assert
        dto.Id.Should().Be(1);
        dto.UpdateCode.Should().Be("TEST_CODE");
        dto.UpdateName.Should().Be("Test Update");
        dto.ReferenceTableName.Should().Be("TestTable");
        dto.IsActive.Should().BeTrue();
    }

    #endregion
}
