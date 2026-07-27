using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Asset_Management.AssetNatureFactorCVMaster;

namespace NtisPlatform.Tests.Application.DTOs;

/// <summary>
/// DataAnnotations validation tests for AssetNatureFactorCVMaster Create/Update DTOs.
/// ConstructionTypeId/YearRangeCVId are nullable int so [Required] can actually detect an
/// omitted field.
/// </summary>
public class AssetNatureFactorCVMasterDtoValidationTests
{
    #region CreateAssetNatureFactorCVMasterDto

    [Fact]
    public void Create_WithValidData_IsValid()
    {
        var dto = new CreateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, Factor = 1.5m, YearRangeCVId = 1 };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithMissingConstructionTypeId_IsInvalid()
    {
        var dto = new CreateAssetNatureFactorCVMasterDto { Factor = 1.0m, YearRangeCVId = 1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetNatureFactorCVMasterDto.ConstructionTypeId))
            && r.ErrorMessage == "NatureFactorCV_ConstructionTypeId_Required");
    }

    [Fact]
    public void Create_WithZeroConstructionTypeId_IsInvalid()
    {
        var dto = new CreateAssetNatureFactorCVMasterDto { ConstructionTypeId = 0, Factor = 1.0m, YearRangeCVId = 1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetNatureFactorCVMasterDto.ConstructionTypeId))
            && r.ErrorMessage == "NatureFactorCV_ConstructionTypeId_Invalid");
    }

    [Fact]
    public void Create_WithNegativeYearRangeCVId_IsInvalid()
    {
        var dto = new CreateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, Factor = 1.0m, YearRangeCVId = -1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetNatureFactorCVMasterDto.YearRangeCVId))
            && r.ErrorMessage == "NatureFactorCV_YearRangeCVId_Invalid");
    }

    [Fact]
    public void Create_WithMissingYearRangeCVId_IsInvalid()
    {
        var dto = new CreateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, Factor = 1.0m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetNatureFactorCVMasterDto.YearRangeCVId))
            && r.ErrorMessage == "NatureFactorCV_YearRangeCVId_Required");
    }

    [Fact]
    public void Create_WithZeroFactor_IsInvalid()
    {
        var dto = new CreateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, Factor = 0m, YearRangeCVId = 1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetNatureFactorCVMasterDto.Factor))
            && r.ErrorMessage == "NatureFactorCV_Factor_Range");
    }

    [Fact]
    public void Create_WithMultipleMissingFields_ReturnsMultipleErrors()
    {
        var dto = new CreateAssetNatureFactorCVMasterDto();

        var results = ValidateModel(dto);

        Assert.True(results.Count >= 3);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetNatureFactorCVMasterDto.ConstructionTypeId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetNatureFactorCVMasterDto.Factor)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetNatureFactorCVMasterDto.YearRangeCVId)));
    }

    #endregion

    #region UpdateAssetNatureFactorCVMasterDto

    [Fact]
    public void Update_WithValidData_IsValid()
    {
        var dto = new UpdateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, Factor = 1.5m, YearRangeCVId = 1, IsActive = true };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_WithMissingYearRangeCVId_IsInvalid()
    {
        var dto = new UpdateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, Factor = 1.0m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetNatureFactorCVMasterDto.YearRangeCVId))
            && r.ErrorMessage == "NatureFactorCV_YearRangeCVId_Required");
    }

    [Fact]
    public void Update_WithIsActiveFalse_IsValid()
    {
        var dto = new UpdateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, Factor = 1.0m, YearRangeCVId = 1, IsActive = false };

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
