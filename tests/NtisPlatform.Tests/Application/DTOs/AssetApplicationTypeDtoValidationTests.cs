using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Master;

namespace NtisPlatform.Tests.Application.DTOs;

/// <summary>
/// DataAnnotations validation tests for AssetApplicationType Create/Update DTOs.
/// </summary>
public class AssetApplicationTypeDtoValidationTests
{
    #region CreateAssetApplicationTypeDto

    [Fact]
    public void Create_WithValidData_IsValid()
    {
        var dto = new CreateAssetApplicationTypeDto
        {
            ApplicationTypeCode = "NEW",
            ApplicationTypeName = "New Construction"
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithEmptyApplicationTypeCode_IsInvalid()
    {
        var dto = new CreateAssetApplicationTypeDto { ApplicationTypeCode = string.Empty, ApplicationTypeName = "New Construction" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetApplicationTypeDto.ApplicationTypeCode))
            && r.ErrorMessage == "ApplicationType_ApplicationTypeCode_Required");
    }

    [Fact]
    public void Create_WithWhitespaceOnlyApplicationTypeCode_IsInvalid()
    {
        var dto = new CreateAssetApplicationTypeDto { ApplicationTypeCode = "   ", ApplicationTypeName = "New Construction" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetApplicationTypeDto.ApplicationTypeCode))
            && r.ErrorMessage == "ApplicationType_ApplicationTypeCode_Required");
    }

    [Fact]
    public void Create_WithApplicationTypeCodeExceeding20Characters_IsInvalid()
    {
        var dto = new CreateAssetApplicationTypeDto { ApplicationTypeCode = new string('A', 21), ApplicationTypeName = "New Construction" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetApplicationTypeDto.ApplicationTypeCode))
            && r.ErrorMessage == "ApplicationType_ApplicationTypeCode_MaxLengthExceeded_20");
    }

    [Fact]
    public void Create_WithEmptyApplicationTypeName_IsInvalid()
    {
        var dto = new CreateAssetApplicationTypeDto { ApplicationTypeCode = "NEW", ApplicationTypeName = string.Empty };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetApplicationTypeDto.ApplicationTypeName))
            && r.ErrorMessage == "ApplicationType_ApplicationTypeName_Required");
    }

    [Fact]
    public void Create_WithApplicationTypeNameExceeding100Characters_IsInvalid()
    {
        var dto = new CreateAssetApplicationTypeDto { ApplicationTypeCode = "NEW", ApplicationTypeName = new string('A', 101) };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetApplicationTypeDto.ApplicationTypeName))
            && r.ErrorMessage == "ApplicationType_ApplicationTypeName_MaxLengthExceeded_100");
    }

    [Fact]
    public void Create_WithInvalidCharactersInApplicationTypeName_IsInvalid()
    {
        var dto = new CreateAssetApplicationTypeDto { ApplicationTypeCode = "NEW", ApplicationTypeName = "New#Construction" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetApplicationTypeDto.ApplicationTypeName))
            && r.ErrorMessage == "ApplicationType_ApplicationTypeName_Invalid");
    }

    [Fact]
    public void Create_WithDescriptionExceeding500Characters_IsInvalid()
    {
        var dto = new CreateAssetApplicationTypeDto { ApplicationTypeCode = "NEW", ApplicationTypeName = "New Construction", Description = new string('A', 501) };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetApplicationTypeDto.Description))
            && r.ErrorMessage == "ApplicationType_Description_MaxLengthExceeded_500");
    }

    [Fact]
    public void Create_WithNegativeDisplayOrder_IsInvalid()
    {
        var dto = new CreateAssetApplicationTypeDto { ApplicationTypeCode = "NEW", ApplicationTypeName = "New Construction", DisplayOrder = -1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetApplicationTypeDto.DisplayOrder))
            && r.ErrorMessage == "ApplicationType_DisplayOrder_Invalid");
    }

    [Fact]
    public void Create_WithZeroDisplayOrder_IsValid()
    {
        var dto = new CreateAssetApplicationTypeDto { ApplicationTypeCode = "NEW", ApplicationTypeName = "New Construction", DisplayOrder = 0 };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithNullOptionalFields_IsValid()
    {
        var dto = new CreateAssetApplicationTypeDto
        {
            ApplicationTypeCode = "NEW",
            ApplicationTypeName = "New Construction",
            Description = null
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithMultipleMissingFields_ReturnsMultipleErrors()
    {
        var dto = new CreateAssetApplicationTypeDto();

        var results = ValidateModel(dto);

        Assert.True(results.Count >= 2);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetApplicationTypeDto.ApplicationTypeCode)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetApplicationTypeDto.ApplicationTypeName)));
    }

    #endregion

    #region UpdateAssetApplicationTypeDto

    [Fact]
    public void Update_WithValidData_IsValid()
    {
        var dto = new UpdateAssetApplicationTypeDto { ApplicationTypeCode = "NEW", ApplicationTypeName = "New Construction", IsActive = true };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_WithMissingApplicationTypeCode_IsInvalid()
    {
        var dto = new UpdateAssetApplicationTypeDto { ApplicationTypeName = "New Construction" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetApplicationTypeDto.ApplicationTypeCode))
            && r.ErrorMessage == "ApplicationType_ApplicationTypeCode_Required");
    }

    [Fact]
    public void Update_WithIsActiveFalse_IsValid()
    {
        var dto = new UpdateAssetApplicationTypeDto { ApplicationTypeCode = "NEW", ApplicationTypeName = "New Construction", IsActive = false };

        Assert.Empty(ValidateModel(dto));
    }

    #endregion

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
