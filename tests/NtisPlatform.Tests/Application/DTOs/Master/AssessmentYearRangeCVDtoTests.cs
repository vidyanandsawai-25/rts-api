using NtisPlatform.Application.DTOs;

using System.ComponentModel.DataAnnotations;
using Xunit;
using System.Collections.Generic;

namespace NtisPlatform.Tests.Application.DTOs.Master;

public class AssessmentYearRangeCVDtoTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        if (model is IValidatableObject validatable)
            results.AddRange(validatable.Validate(ctx));
        return results;
    }

    [Fact]
    public void CreateAssessmentYearRangeCVDto_ValidData_PassesValidation()
    {
        var dto = new CreateAssessmentYearRangeCVDto { FromYear = 2020, ToYear = 2025 };
        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(1699, 2025)] // Below min
    [InlineData(1700, 1699)] // ToYear below min
    [InlineData(10000, 2025)] // Above max
    [InlineData(2020, 10000)] // ToYear above max
    public void CreateAssessmentYearRangeCVDto_InvalidYearRange_FailsValidation(int from, int to)
    {
        var dto = new CreateAssessmentYearRangeCVDto { FromYear = from, ToYear = to };
        var results = Validate(dto);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void CreateAssessmentYearRangeCVDto_EqualYears_FailsValidation()
    {
        var dto = new CreateAssessmentYearRangeCVDto { FromYear = 2023, ToYear = 2023 };
        var results = Validate(dto);
        Assert.Contains(results, r => r.ErrorMessage == "FromYear_MustBeLessThanToYear");
    }

    [Fact]
    public void CreateAssessmentYearRangeCVDto_FromYearGreaterThanToYear_FailsValidation()
    {
        var dto = new CreateAssessmentYearRangeCVDto { FromYear = 2025, ToYear = 2020 };
        var results = Validate(dto);
        Assert.Contains(results, r => r.ErrorMessage == "FromYear_MustBeLessThanToYear");
    }

    [Fact]
    public void CreateAssessmentYearRangeCVDto_RequiredFields_FailValidation()
    {
        var dto = new CreateAssessmentYearRangeCVDto();
        var results = Validate(dto);
        Assert.Contains(results, r => r.ErrorMessage == "FromYear_MustBe4Digits");
        Assert.Contains(results, r => r.ErrorMessage == "ToYear_MustBe4Digits");
    }

    [Fact]
    public void UpdateAssessmentYearRangeCVDto_ValidData_PassesValidation()
    {
        var dto = new UpdateAssessmentYearRangeCVDto { FromYear = 2020, ToYear = 2025 };
        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateAssessmentYearRangeCVDto_EqualYears_FailsValidation()
    {
        var dto = new UpdateAssessmentYearRangeCVDto { FromYear = 2023, ToYear = 2023 };
        var results = Validate(dto);
        Assert.Contains(results, r => r.ErrorMessage == "FromYear_MustBeLessThanToYear");
    }

    [Fact]
    public void UpdateAssessmentYearRangeCVDto_FromYearGreaterThanToYear_FailsValidation()
    {
        var dto = new UpdateAssessmentYearRangeCVDto { FromYear = 2025, ToYear = 2020 };
        var results = Validate(dto);
        Assert.Contains(results, r => r.ErrorMessage == "FromYear_MustBeLessThanToYear");
    }

    [Fact]
    public void UpdateAssessmentYearRangeCVDto_RequiredFields_FailValidation()
    {
        var dto = new UpdateAssessmentYearRangeCVDto();
        var results = Validate(dto);
        Assert.Contains(results, r => r.ErrorMessage == "FromYear_MustBe4Digits");
        Assert.Contains(results, r => r.ErrorMessage == "ToYear_MustBe4Digits");
    }
}
