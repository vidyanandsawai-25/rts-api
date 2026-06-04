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
            UpdateNameMarathi = "मालमत्ता प्रकार अद्यतन",
            IconName = "property_icon",
            ReferenceTableName = "PropertyTypeMaster",
            DisplaySequence = 1,
            ApiRoute = "/api/PropertyType/BulkUpdate",
            Description = "Bulk update for property types",
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
            DisplaySequence = 1,
            ApiRoute = "/api/PropertyType/BulkUpdate",
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
            DisplaySequence = 1,
            ApiRoute = "/api/PropertyType/BulkUpdate",
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
            DisplaySequence = 1,
            ApiRoute = "/api/PropertyType/BulkUpdate",
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_ReferenceTableName_Required"));
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithMissingApiRoute_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update",
            ReferenceTableName = "PropertyTypeMaster",
            DisplaySequence = 1,
            ApiRoute = string.Empty, // Missing
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_ApiRoute_Required"));
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
            DisplaySequence = 1,
            ApiRoute = "/api/PropertyType/BulkUpdate",
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
            DisplaySequence = 1,
            ApiRoute = "/api/PropertyType/BulkUpdate",
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
            DisplaySequence = 1,
            ApiRoute = "/api/PropertyType/BulkUpdate",
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_ReferenceTableName_MaxLen_200"));
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithApiRouteTooLong_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update",
            ReferenceTableName = "PropertyTypeMaster",
            DisplaySequence = 1,
            ApiRoute = new string('A', 501), // Exceeds 500 characters
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_ApiRoute_MaxLen_500"));
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithDisplaySequenceOutOfRange_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update",
            ReferenceTableName = "PropertyTypeMaster",
            DisplaySequence = 10000, // Exceeds max range
            ApiRoute = "/api/PropertyType/BulkUpdate",
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_DisplaySequence_Range"));
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithDisplaySequenceZero_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update",
            ReferenceTableName = "PropertyTypeMaster",
            DisplaySequence = 0, // Below min range
            ApiRoute = "/api/PropertyType/BulkUpdate",
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_DisplaySequence_Range"));
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithDescriptionTooLong_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update",
            ReferenceTableName = "PropertyTypeMaster",
            DisplaySequence = 1,
            ApiRoute = "/api/PropertyType/BulkUpdate",
            Description = new string('A', 1001), // Exceeds 1000 characters
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_Description_MaxLen_1000"));
    }

    [Fact]
    public void CreateBulkUpdateMasterDto_WithIconNameTooLong_FailsValidation()
    {
        // Arrange
        var dto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update",
            IconName = new string('A', 101), // Exceeds 100 characters
            ReferenceTableName = "PropertyTypeMaster",
            DisplaySequence = 1,
            ApiRoute = "/api/PropertyType/BulkUpdate",
            CreatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_IconName_MaxLen_100"));
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
            UpdateNameMarathi = "मालमत्ता प्रकार अद्यतन (सुधारित)",
            IconName = "updated_icon",
            ReferenceTableName = "PropertyTypeMaster",
            DisplaySequence = 1,
            ApiRoute = "/api/PropertyType/BulkUpdate/v2",
            Description = "Updated bulk update for property types",
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
            DisplaySequence = 1,
            ApiRoute = "/api/PropertyType/BulkUpdate",
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
            UpdateNameMarathi = string.Empty,
            IconName = string.Empty,
            ReferenceTableName = "SimpleTable",
            DisplaySequence = 1,
            ApiRoute = "/api/Simple",
            Description = null,
            UpdatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void UpdateBulkUpdateMasterDto_WithDisplaySequenceOutOfRange_FailsValidation()
    {
        // Arrange
        var dto = new UpdateBulkUpdateMasterDto
        {
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update",
            ReferenceTableName = "PropertyTypeMaster",
            DisplaySequence = -1, // Below min range
            ApiRoute = "/api/PropertyType/BulkUpdate",
            UpdatedBy = 1
        };

        // Act
        var validationResults = ValidateDto(dto);

        // Assert
        validationResults.Should().NotBeEmpty();
        validationResults.Should().Contain(vr => vr.ErrorMessage!.Contains("BulkUpdateMaster_DisplaySequence_Range"));
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
            UpdateNameMarathi = "चाचणी अद्यतन",
            IconName = "test_icon",
            ReferenceTableName = "TestTable",
            DisplaySequence = 5,
            ApiRoute = "/api/Test/BulkUpdate",
            Description = "Test description",
            IsActive = true
        };

        // Assert
        dto.Id.Should().Be(1);
        dto.UpdateCode.Should().Be("TEST_CODE");
        dto.UpdateName.Should().Be("Test Update");
        dto.UpdateNameMarathi.Should().Be("चाचणी अद्यतन");
        dto.IconName.Should().Be("test_icon");
        dto.ReferenceTableName.Should().Be("TestTable");
        dto.DisplaySequence.Should().Be(5);
        dto.ApiRoute.Should().Be("/api/Test/BulkUpdate");
        dto.Description.Should().Be("Test description");
        dto.IsActive.Should().BeTrue();
    }

    #endregion
}
