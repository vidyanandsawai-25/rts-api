using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Asset_Management.AssetAgeFactorCVMaster;

namespace NtisPlatform.Tests.Application.DTOs;

/// <summary>
/// DataAnnotations validation tests for AssetAgeFactorCVMaster Create/Update DTOs.
/// ConstructionTypeId/AgeFrom/AgeTo/YearRangeCVId are nullable int so [Required] can actually
/// detect an omitted field — a plain non-nullable int always defaults to 0 and never trips
/// [Required], which is why these were converted from int to int?.
/// </summary>
public class AssetAgeFactorCVMasterDtoValidationTests
{
    #region CreateAssetAgeFactorCVMasterDto

    [Fact]
    public void Create_WithValidData_IsValid()
    {
        var dto = new CreateAssetAgeFactorCVMasterDto
        {
            ConstructionTypeId = 1,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 1.25m,
            YearRangeCVId = 1
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithMissingConstructionTypeId_IsInvalid()
    {
        var dto = new CreateAssetAgeFactorCVMasterDto { AgeFrom = 0, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetAgeFactorCVMasterDto.ConstructionTypeId))
            && r.ErrorMessage == "AgeFactorCV_ConstructionTypeId_Required");
    }

    [Fact]
    public void Create_WithZeroConstructionTypeId_IsInvalid()
    {
        var dto = new CreateAssetAgeFactorCVMasterDto { ConstructionTypeId = 0, AgeFrom = 0, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetAgeFactorCVMasterDto.ConstructionTypeId))
            && r.ErrorMessage == "AgeFactorCV_ConstructionTypeId_Invalid");
    }

    [Fact]
    public void Create_WithNegativeConstructionTypeId_IsInvalid()
    {
        var dto = new CreateAssetAgeFactorCVMasterDto { ConstructionTypeId = -1, AgeFrom = 0, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetAgeFactorCVMasterDto.ConstructionTypeId)));
    }

    [Fact]
    public void Create_WithMissingYearRangeCVId_IsInvalid()
    {
        var dto = new CreateAssetAgeFactorCVMasterDto { ConstructionTypeId = 1, AgeFrom = 0, AgeTo = 5, Factor = 1.0m };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetAgeFactorCVMasterDto.YearRangeCVId))
            && r.ErrorMessage == "AgeFactorCV_YearRangeCVId_Required");
    }

    [Fact]
    public void Create_WithAgeFromZero_IsValid()
    {
        // AgeFrom = 0 is a legitimate value (a brand-new-construction age band) — must not be rejected.
        var dto = new CreateAssetAgeFactorCVMasterDto { ConstructionTypeId = 1, AgeFrom = 0, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1 };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithMissingAgeFrom_IsInvalid()
    {
        var dto = new CreateAssetAgeFactorCVMasterDto { ConstructionTypeId = 1, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetAgeFactorCVMasterDto.AgeFrom))
            && r.ErrorMessage == "AgeFactorCV_AgeFrom_Required");
    }

    [Fact]
    public void Create_WithNegativeAgeFrom_IsInvalid()
    {
        var dto = new CreateAssetAgeFactorCVMasterDto { ConstructionTypeId = 1, AgeFrom = -1, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetAgeFactorCVMasterDto.AgeFrom))
            && r.ErrorMessage == "AgeFactorCV_AgeFrom_Invalid");
    }

    [Fact]
    public void Create_WithMissingAgeTo_IsInvalid()
    {
        var dto = new CreateAssetAgeFactorCVMasterDto { ConstructionTypeId = 1, AgeFrom = 0, Factor = 1.0m, YearRangeCVId = 1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetAgeFactorCVMasterDto.AgeTo))
            && r.ErrorMessage == "AgeFactorCV_AgeTo_Required");
    }

    [Fact]
    public void Create_WithNegativeAgeTo_IsInvalid()
    {
        var dto = new CreateAssetAgeFactorCVMasterDto { ConstructionTypeId = 1, AgeFrom = 0, AgeTo = -5, Factor = 1.0m, YearRangeCVId = 1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetAgeFactorCVMasterDto.AgeTo))
            && r.ErrorMessage == "AgeFactorCV_AgeTo_Invalid");
    }

    [Fact]
    public void Create_WithZeroFactor_IsInvalid()
    {
        // Factor = 0 would zero out every calculation using it — Range excludes zero (min 0.01).
        var dto = new CreateAssetAgeFactorCVMasterDto { ConstructionTypeId = 1, AgeFrom = 0, AgeTo = 5, Factor = 0m, YearRangeCVId = 1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetAgeFactorCVMasterDto.Factor))
            && r.ErrorMessage == "AgeFactorCV_Factor_Range");
    }

    [Fact]
    public void Create_WithFactorExceedingDbPrecision_IsInvalid()
    {
        // Column is decimal(5,2) — max 999.99.
        var dto = new CreateAssetAgeFactorCVMasterDto { ConstructionTypeId = 1, AgeFrom = 0, AgeTo = 5, Factor = 1000m, YearRangeCVId = 1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetAgeFactorCVMasterDto.Factor)));
    }

    [Fact]
    public void Create_WithMultipleMissingFields_ReturnsMultipleErrors()
    {
        var dto = new CreateAssetAgeFactorCVMasterDto();

        var results = ValidateModel(dto);

        Assert.True(results.Count >= 5);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetAgeFactorCVMasterDto.ConstructionTypeId)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetAgeFactorCVMasterDto.AgeFrom)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetAgeFactorCVMasterDto.AgeTo)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetAgeFactorCVMasterDto.Factor)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetAgeFactorCVMasterDto.YearRangeCVId)));
    }

    #endregion

    #region UpdateAssetAgeFactorCVMasterDto

    [Fact]
    public void Update_WithValidData_IsValid()
    {
        var dto = new UpdateAssetAgeFactorCVMasterDto
        {
            ConstructionTypeId = 1,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 1.25m,
            YearRangeCVId = 1,
            IsActive = true
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_WithMissingConstructionTypeId_IsInvalid()
    {
        var dto = new UpdateAssetAgeFactorCVMasterDto { AgeFrom = 0, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetAgeFactorCVMasterDto.ConstructionTypeId))
            && r.ErrorMessage == "AgeFactorCV_ConstructionTypeId_Required");
    }

    [Fact]
    public void Update_WithZeroYearRangeCVId_IsInvalid()
    {
        var dto = new UpdateAssetAgeFactorCVMasterDto { ConstructionTypeId = 1, AgeFrom = 0, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 0 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetAgeFactorCVMasterDto.YearRangeCVId))
            && r.ErrorMessage == "AgeFactorCV_YearRangeCVId_Invalid");
    }

    [Fact]
    public void Update_WithIsActiveFalse_IsValid()
    {
        var dto = new UpdateAssetAgeFactorCVMasterDto
        {
            ConstructionTypeId = 1,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = false
        };

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
