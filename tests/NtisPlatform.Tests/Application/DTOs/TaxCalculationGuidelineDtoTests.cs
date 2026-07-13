using NtisPlatform.Application.DTOs.Master.TaxCalculationGuideline;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs;

public class TaxCalculationGuidelineDtoTests
{
    [Fact]
    public void CreateDto_WithValidData_PassesValidation()
    {
        var dto = new CreateTaxCalculationGuidelineDto
        {
            GuidelineCode = "GUIDE_001",
            GuidelineName = "Default Guideline",
            DatePriority1 = "RETROSPECTIVE",
            DatePriority2 = "ELECTRIC_BILL",
            DatePriority3 = "CC",
            DatePriority4 = "OC",
            IgnoreCCToOCIfWithinType = "MONTHS",
            ElectricBillDateRule = "NO_TAX",
            NoDateRule = "DEFAULT_RETROSPECTIVE",
            FloorCertificatePriority = "PROPERTY_OVERRIDES_FLOOR",
            ProrationMethod = "FULL_YEAR",
            TaxPersistenceMode = "PROPERTY_AGGREGATED"
        };

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void CreateDto_WithInvalidDatePriority_FailsValidation()
    {
        var dto = new CreateTaxCalculationGuidelineDto
        {
            GuidelineCode = "GUIDE_001",
            GuidelineName = "Default Guideline",
            DatePriority1 = "INVALID"
        };

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateTaxCalculationGuidelineDto.DatePriority1)));
    }

    [Fact]
    public void CreateDto_WithInvalidFinancialYearStartMonth_FailsValidation()
    {
        var dto = new CreateTaxCalculationGuidelineDto
        {
            GuidelineCode = "GUIDE_001",
            GuidelineName = "Default Guideline",
            FinancialYearStartMonth = 13
        };

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateTaxCalculationGuidelineDto.FinancialYearStartMonth)));
    }
}
