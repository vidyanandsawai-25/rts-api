using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Asset_Management.AssetAssessmentYearRangeMasterCV;

namespace NtisPlatform.Tests.Application.DTOs;

/// <summary>
/// DataAnnotations validation tests for AssetAssessmentYearRangeMasterCV Create/Update DTOs.
/// FromYear/ToYear are nullable int so [Required] can actually detect an omitted field.
/// Cross-field validation (FromYear &lt;= ToYear, duplicate range) stays in
/// AssetAssessmentYearRangeCVService — not something DataAnnotations should attempt.
/// </summary>
public class AssetAssessmentYearRangeMasterCVDtoValidationTests
{
    #region CreateAssetAssessmentYearRangeMasterCVDto

    [Fact]
    public void Create_WithValidData_IsValid()
    {
        var dto = new CreateAssetAssessmentYearRangeMasterCVDto { FromYear = 2000, ToYear = 2005 };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithMissingFromYear_IsInvalid()
    {
        var dto = new CreateAssetAssessmentYearRangeMasterCVDto { ToYear = 2005 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetAssessmentYearRangeMasterCVDto.FromYear))
            && r.ErrorMessage == "AssessmentYearRangeCV_FromYear_Required");
    }

    [Fact]
    public void Create_WithMissingToYear_IsInvalid()
    {
        var dto = new CreateAssetAssessmentYearRangeMasterCVDto { FromYear = 2000 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetAssessmentYearRangeMasterCVDto.ToYear))
            && r.ErrorMessage == "AssessmentYearRangeCV_ToYear_Required");
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(10000)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithFromYearOutOfRange_IsInvalid(int fromYear)
    {
        var dto = new CreateAssetAssessmentYearRangeMasterCVDto { FromYear = fromYear, ToYear = 2005 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetAssessmentYearRangeMasterCVDto.FromYear))
            && r.ErrorMessage == "AssessmentYearRangeCV_FromYear_Invalid");
    }

    [Theory]
    [InlineData(1900)]
    [InlineData(9999)]
    public void Create_WithFromYearAtBoundary_IsValid(int fromYear)
    {
        var dto = new CreateAssetAssessmentYearRangeMasterCVDto { FromYear = fromYear, ToYear = fromYear };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Create_WithMultipleMissingFields_ReturnsMultipleErrors()
    {
        var dto = new CreateAssetAssessmentYearRangeMasterCVDto();

        var results = ValidateModel(dto);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetAssessmentYearRangeMasterCVDto.FromYear)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetAssessmentYearRangeMasterCVDto.ToYear)));
    }

    #endregion

    #region UpdateAssetAssessmentYearRangeMasterCVDto

    [Fact]
    public void Update_WithValidData_IsValid()
    {
        var dto = new UpdateAssetAssessmentYearRangeMasterCVDto { FromYear = 2000, ToYear = 2005, IsActive = true };

        Assert.Empty(ValidateModel(dto));
    }

    [Fact]
    public void Update_WithMissingToYear_IsInvalid()
    {
        var dto = new UpdateAssetAssessmentYearRangeMasterCVDto { FromYear = 2000 };

        var results = ValidateModel(dto);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetAssessmentYearRangeMasterCVDto.ToYear))
            && r.ErrorMessage == "AssessmentYearRangeCV_ToYear_Required");
    }

    [Fact]
    public void Update_WithIsActiveFalse_IsValid()
    {
        var dto = new UpdateAssetAssessmentYearRangeMasterCVDto { FromYear = 2000, ToYear = 2005, IsActive = false };

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
