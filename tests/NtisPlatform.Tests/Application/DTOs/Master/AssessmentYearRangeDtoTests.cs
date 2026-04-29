using NtisPlatform.Application.DTOs;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Tests.Application.DTOs.Master;

/// <summary>
/// Comprehensive tests for AssessmentYearRange DTOs to achieve 100% code coverage
/// </summary>
public class AssessmentYearRangeDtoTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        
        if (model is IValidatableObject validatable)
        {
            results.AddRange(validatable.Validate(ctx));
        }
        
        return results;
    }

    #region CreateAssessmentYearRangeDto Tests

    [Fact]
    public void CreateAssessmentYearRangeDto_AllProperties_GetSet_WorksCorrectly()
    {
        var dto = new CreateAssessmentYearRangeDto
        {
            FromYear = 2020,
            ToYear = 2025,
            IsActive = true,
            CreatedBy = 1
        };

        Assert.Equal(2020, dto.FromYear);
        Assert.Equal(2025, dto.ToYear);
        Assert.True(dto.IsActive);
        Assert.Equal(1, dto.CreatedBy);
    }

    [Fact]
    public void CreateAssessmentYearRangeDto_ValidData_PassesValidation()
    {
        var dto = new CreateAssessmentYearRangeDto
        {
            FromYear = 2020,
            ToYear = 2025
        };

        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void CreateAssessmentYearRangeDto_FromYearGreaterThanToYear_FailsValidation()
    {
        var dto = new CreateAssessmentYearRangeDto
        {
            FromYear = 2025,
            ToYear = 2020
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "FromYear_MustBeLessThanToYear");
    }

    [Fact]
    public void CreateAssessmentYearRangeDto_FromYearBelowRange_FailsValidation()
    {
        var dto = new CreateAssessmentYearRangeDto
        {
            FromYear = 1699,
            ToYear = 2025
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "FromYear_MustBe4Digits");
    }

    [Fact]
    public void CreateAssessmentYearRangeDto_ToYearAboveRange_FailsValidation()
    {
        var dto = new CreateAssessmentYearRangeDto
        {
            FromYear = 2020,
            ToYear = 10000
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "ToYear_MustBe4Digits");
    }

    [Fact]
    public void CreateAssessmentYearRangeDto_FromYearZero_FailsValidation()
    {
        var dto = new CreateAssessmentYearRangeDto
        {
            FromYear = 0,
            ToYear = 2025
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void CreateAssessmentYearRangeDto_EqualYears_FailsValidation()
    {
        var dto = new CreateAssessmentYearRangeDto
        {
            FromYear = 2023,
            ToYear = 2023
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "FromYear_MustBeLessThanToYear");
    }

    #endregion
}
