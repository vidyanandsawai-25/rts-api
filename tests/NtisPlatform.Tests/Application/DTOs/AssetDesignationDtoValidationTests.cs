using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Master;

namespace NtisPlatform.Tests.Application.DTOs;

/// <summary>
/// DataAnnotations validation tests for AssetDesignation Create/Update DTOs.
/// OwningDepartmentId is nullable int so [Required] can actually detect an omitted field.
/// </summary>
public class AssetDesignationDtoValidationTests
{
    #region CreateAssetDesignationDto

    [Fact]
    public void Create_WithValidData_IsValid()
    {
        var dto = new CreateAssetDesignationDto
        {
            OwningDepartmentId = 1,
            DesignationCode = "ENG",
            DesignationName = "Engineer"
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithMissingOwningDepartmentId_IsInvalid()
    {
        var dto = new CreateAssetDesignationDto { DesignationCode = "ENG", DesignationName = "Engineer" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetDesignationDto.OwningDepartmentId))
            && r.ErrorMessage == "Designation_OwningDepartmentId_Required");
    }

    [Fact]
    public void Create_WithZeroOwningDepartmentId_IsInvalid()
    {
        var dto = new CreateAssetDesignationDto { OwningDepartmentId = 0, DesignationCode = "ENG", DesignationName = "Engineer" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetDesignationDto.OwningDepartmentId))
            && r.ErrorMessage == "Designation_OwningDepartmentId_Invalid");
    }

    [Fact]
    public void Create_WithNegativeOwningDepartmentId_IsInvalid()
    {
        var dto = new CreateAssetDesignationDto { OwningDepartmentId = -1, DesignationCode = "ENG", DesignationName = "Engineer" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetDesignationDto.OwningDepartmentId))
            && r.ErrorMessage == "Designation_OwningDepartmentId_Invalid");
    }

    [Fact]
    public void Create_WithEmptyDesignationCode_IsInvalid()
    {
        var dto = new CreateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = string.Empty, DesignationName = "Engineer" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetDesignationDto.DesignationCode))
            && r.ErrorMessage == "Designation_DesignationCode_Required");
    }

    [Fact]
    public void Create_WithWhitespaceOnlyDesignationCode_IsInvalid()
    {
        var dto = new CreateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = "   ", DesignationName = "Engineer" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetDesignationDto.DesignationCode))
            && r.ErrorMessage == "Designation_DesignationCode_Required");
    }

    [Fact]
    public void Create_WithDesignationCodeExceeding50Characters_IsInvalid()
    {
        var dto = new CreateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = new string('A', 51), DesignationName = "Engineer" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetDesignationDto.DesignationCode))
            && r.ErrorMessage == "Designation_DesignationCode_MaxLengthExceeded_50");
    }

    [Fact]
    public void Create_WithEmptyDesignationName_IsInvalid()
    {
        var dto = new CreateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = string.Empty };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetDesignationDto.DesignationName))
            && r.ErrorMessage == "Designation_DesignationName_Required");
    }

    [Fact]
    public void Create_WithInvalidCharactersInDesignationName_IsInvalid()
    {
        var dto = new CreateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer#123" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetDesignationDto.DesignationName))
            && r.ErrorMessage == "Designation_DesignationName_Invalid");
    }

    [Fact]
    public void Create_WithNullOptionalFields_IsValid()
    {
        var dto = new CreateAssetDesignationDto
        {
            OwningDepartmentId = 1,
            DesignationCode = "ENG",
            DesignationName = "Engineer",
            DesignationLocal = null,
            DesignationDescription = null
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithMultipleMissingFields_ReturnsMultipleErrors()
    {
        var dto = new CreateAssetDesignationDto();

        var results = ValidateModel(dto);

        Assert.True(results.Count >= 3);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetDesignationDto.OwningDepartmentId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetDesignationDto.DesignationCode)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetDesignationDto.DesignationName)));
    }

    #endregion

    #region UpdateAssetDesignationDto

    [Fact]
    public void Update_WithValidData_IsValid()
    {
        var dto = new UpdateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Senior Engineer", IsActive = true };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_WithMissingOwningDepartmentId_IsInvalid()
    {
        var dto = new UpdateAssetDesignationDto { DesignationCode = "ENG", DesignationName = "Senior Engineer" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetDesignationDto.OwningDepartmentId))
            && r.ErrorMessage == "Designation_OwningDepartmentId_Required");
    }

    [Fact]
    public void Update_WithIsActiveFalse_IsValid()
    {
        var dto = new UpdateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Senior Engineer", IsActive = false };

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
