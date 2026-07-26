using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Master;

namespace NtisPlatform.Tests.Application.DTOs;

/// <summary>
/// DataAnnotations validation tests for AssetConditionMaster Create/Update DTOs.
/// CategoryId is nullable int so [Required] can actually detect an omitted field.
/// ConditionFactor/DisplayOrder are genuinely optional (nullable in the entity and DB column),
/// so they intentionally have no [Required] — only a [Range] that applies when a value is supplied.
/// </summary>
public class AssetConditionMasterDtoValidationTests
{
    #region CreateAssetConditionMasterDto

    [Fact]
    public void Create_WithValidData_IsValid()
    {
        var dto = new CreateAssetConditionMasterDto
        {
            ConditionCategory = "Asset",
            CategoryId = 1,
            ConditionName = "Good",
            Description = "Well maintained",
            ConditionFactor = 0.9m,
            DisplayOrder = 1
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithEmptyConditionCategory_IsInvalid()
    {
        var dto = new CreateAssetConditionMasterDto { ConditionCategory = string.Empty, CategoryId = 1, ConditionName = "Good" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetConditionMasterDto.ConditionCategory))
            && r.ErrorMessage == "AssetConditionMaster_ConditionCategory_Required");
    }

    [Fact]
    public void Create_WithWhitespaceOnlyConditionCategory_IsInvalid()
    {
        // [Required] on a string trims before checking emptiness (AllowEmptyStrings defaults to
        // false), so whitespace-only values are already rejected without any extra attribute.
        var dto = new CreateAssetConditionMasterDto { ConditionCategory = "   ", CategoryId = 1, ConditionName = "Good" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetConditionMasterDto.ConditionCategory))
            && r.ErrorMessage == "AssetConditionMaster_ConditionCategory_Required");
    }

    [Fact]
    public void Create_WithConditionCategoryExceeding20Characters_IsInvalid()
    {
        var dto = new CreateAssetConditionMasterDto { ConditionCategory = new string('A', 21), CategoryId = 1, ConditionName = "Good" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetConditionMasterDto.ConditionCategory))
            && r.ErrorMessage == "AssetConditionMaster_ConditionCategory_MaxLengthExceeded_20");
    }

    [Fact]
    public void Create_WithMissingCategoryId_IsInvalid()
    {
        var dto = new CreateAssetConditionMasterDto { ConditionCategory = "Asset", ConditionName = "Good" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetConditionMasterDto.CategoryId))
            && r.ErrorMessage == "AssetConditionMaster_CategoryId_Required");
    }

    [Fact]
    public void Create_WithZeroCategoryId_IsInvalid()
    {
        var dto = new CreateAssetConditionMasterDto { ConditionCategory = "Asset", CategoryId = 0, ConditionName = "Good" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetConditionMasterDto.CategoryId))
            && r.ErrorMessage == "AssetConditionMaster_CategoryId_Invalid");
    }

    [Fact]
    public void Create_WithNegativeCategoryId_IsInvalid()
    {
        var dto = new CreateAssetConditionMasterDto { ConditionCategory = "Asset", CategoryId = -5, ConditionName = "Good" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetConditionMasterDto.CategoryId))
            && r.ErrorMessage == "AssetConditionMaster_CategoryId_Invalid");
    }

    [Fact]
    public void Create_WithEmptyConditionName_IsInvalid()
    {
        var dto = new CreateAssetConditionMasterDto { ConditionCategory = "Asset", CategoryId = 1, ConditionName = string.Empty };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetConditionMasterDto.ConditionName))
            && r.ErrorMessage == "AssetConditionMaster_ConditionName_Required");
    }

    [Fact]
    public void Create_WithNullDescription_IsValid()
    {
        var dto = new CreateAssetConditionMasterDto { ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Good", Description = null };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithNullConditionFactorAndDisplayOrder_IsValid()
    {
        // These are genuinely optional — omitting them must not fail validation.
        var dto = new CreateAssetConditionMasterDto { ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Good" };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithZeroConditionFactor_IsInvalid()
    {
        var dto = new CreateAssetConditionMasterDto { ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Good", ConditionFactor = 0m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetConditionMasterDto.ConditionFactor))
            && r.ErrorMessage == "AssetConditionMaster_ConditionFactor_Range");
    }

    [Fact]
    public void Create_WithNegativeDisplayOrder_IsInvalid()
    {
        var dto = new CreateAssetConditionMasterDto { ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Good", DisplayOrder = -1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetConditionMasterDto.DisplayOrder))
            && r.ErrorMessage == "AssetConditionMaster_DisplayOrder_Invalid");
    }

    [Fact]
    public void Create_WithZeroDisplayOrder_IsValid()
    {
        // Zero is a legitimate first-position display order.
        var dto = new CreateAssetConditionMasterDto { ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Good", DisplayOrder = 0 };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithMultipleInvalidFields_ReturnsMultipleErrors()
    {
        var dto = new CreateAssetConditionMasterDto { ConditionCategory = string.Empty, CategoryId = 0, ConditionName = string.Empty };

        var results = ValidateModel(dto);

        Assert.True(results.Count >= 3);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetConditionMasterDto.ConditionCategory)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetConditionMasterDto.CategoryId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetConditionMasterDto.ConditionName)));
    }

    #endregion

    #region UpdateAssetConditionMasterDto

    [Fact]
    public void Update_WithValidData_IsValid()
    {
        var dto = new UpdateAssetConditionMasterDto { ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Excellent", IsActive = true };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_WithMissingCategoryId_IsInvalid()
    {
        var dto = new UpdateAssetConditionMasterDto { ConditionCategory = "Asset", ConditionName = "Excellent" };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetConditionMasterDto.CategoryId))
            && r.ErrorMessage == "AssetConditionMaster_CategoryId_Required");
    }

    [Fact]
    public void Update_WithIsActiveFalse_IsValid()
    {
        var dto = new UpdateAssetConditionMasterDto { ConditionCategory = "Asset", CategoryId = 1, ConditionName = "Excellent", IsActive = false };

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
