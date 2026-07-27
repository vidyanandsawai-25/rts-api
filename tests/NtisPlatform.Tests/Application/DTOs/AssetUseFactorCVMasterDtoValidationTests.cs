using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Asset_Management.AssetUseFactorCVMaster;

namespace NtisPlatform.Tests.Application.DTOs;

/// <summary>
/// DataAnnotations validation tests for AssetUseFactorCVMaster Create/Update DTOs.
/// TypeOfUseId/SubTypeOfUseId/YearRangeCVId are nullable int so [Required] can actually detect an
/// omitted field.
/// </summary>
public class AssetUseFactorCVMasterDtoValidationTests
{
    #region CreateAssetUseFactorCVMasterDto

    [Fact]
    public void Create_WithValidData_IsValid()
    {
        var dto = new CreateAssetUseFactorCVMasterDto { TypeOfUseId = 1, SubTypeOfUseId = 1, Factor = 1.5m, YearRangeCVId = 1 };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithMissingTypeOfUseId_IsInvalid()
    {
        var dto = new CreateAssetUseFactorCVMasterDto { SubTypeOfUseId = 1, Factor = 1.0m, YearRangeCVId = 1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetUseFactorCVMasterDto.TypeOfUseId))
            && r.ErrorMessage == "AssetUseFactorCV_TypeOfUseId_Required");
    }

    [Fact]
    public void Create_WithZeroTypeOfUseId_IsInvalid()
    {
        var dto = new CreateAssetUseFactorCVMasterDto { TypeOfUseId = 0, SubTypeOfUseId = 1, Factor = 1.0m, YearRangeCVId = 1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetUseFactorCVMasterDto.TypeOfUseId))
            && r.ErrorMessage == "UseFactorCV_TypeOfUseId_Invalid");
    }

    [Fact]
    public void Create_WithMissingSubTypeOfUseId_IsInvalid()
    {
        var dto = new CreateAssetUseFactorCVMasterDto { TypeOfUseId = 1, Factor = 1.0m, YearRangeCVId = 1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetUseFactorCVMasterDto.SubTypeOfUseId))
            && r.ErrorMessage == "AssetUseFactorCV_SubTypeOfUseId_Required");
    }

    [Fact]
    public void Create_WithNegativeSubTypeOfUseId_IsInvalid()
    {
        var dto = new CreateAssetUseFactorCVMasterDto { TypeOfUseId = 1, SubTypeOfUseId = -1, Factor = 1.0m, YearRangeCVId = 1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetUseFactorCVMasterDto.SubTypeOfUseId))
            && r.ErrorMessage == "UseFactorCV_SubTypeOfUseId_Invalid");
    }

    [Fact]
    public void Create_WithMissingYearRangeCVId_IsInvalid()
    {
        var dto = new CreateAssetUseFactorCVMasterDto { TypeOfUseId = 1, SubTypeOfUseId = 1, Factor = 1.0m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetUseFactorCVMasterDto.YearRangeCVId))
            && r.ErrorMessage == "AssetUseFactorCV_YearRangeCVId_Required");
    }

    [Fact]
    public void Create_WithNegativeYearRangeCVId_IsInvalid()
    {
        var dto = new CreateAssetUseFactorCVMasterDto { TypeOfUseId = 1, SubTypeOfUseId = 1, Factor = 1.0m, YearRangeCVId = -1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetUseFactorCVMasterDto.YearRangeCVId))
            && r.ErrorMessage == "UseFactorCV_YearRangeCVId_Invalid");
    }

    [Fact]
    public void Create_WithZeroFactor_IsInvalid()
    {
        var dto = new CreateAssetUseFactorCVMasterDto { TypeOfUseId = 1, SubTypeOfUseId = 1, Factor = 0m, YearRangeCVId = 1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetUseFactorCVMasterDto.Factor))
            && r.ErrorMessage == "UseFactorCV_Factor_Range");
    }

    [Fact]
    public void Create_WithMultipleMissingFields_ReturnsMultipleErrors()
    {
        var dto = new CreateAssetUseFactorCVMasterDto();

        var results = ValidateModel(dto);

        Assert.True(results.Count >= 4);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetUseFactorCVMasterDto.TypeOfUseId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetUseFactorCVMasterDto.SubTypeOfUseId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetUseFactorCVMasterDto.Factor)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetUseFactorCVMasterDto.YearRangeCVId)));
    }

    #endregion

    #region UpdateAssetUseFactorCVMasterDto

    [Fact]
    public void Update_WithValidData_IsValid()
    {
        var dto = new UpdateAssetUseFactorCVMasterDto { TypeOfUseId = 1, SubTypeOfUseId = 1, Factor = 1.5m, YearRangeCVId = 1, IsActive = true };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_WithMissingSubTypeOfUseId_IsInvalid()
    {
        var dto = new UpdateAssetUseFactorCVMasterDto { TypeOfUseId = 1, Factor = 1.0m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetUseFactorCVMasterDto.SubTypeOfUseId))
            && r.ErrorMessage == "AssetUseFactorCV_SubTypeOfUseId_Required");
    }

    [Fact]
    public void Update_WithIsActiveFalse_IsValid()
    {
        var dto = new UpdateAssetUseFactorCVMasterDto { TypeOfUseId = 1, SubTypeOfUseId = 1, Factor = 1.0m, YearRangeCVId = 1, IsActive = false };

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
