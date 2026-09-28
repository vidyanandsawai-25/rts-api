using NtisPlatform.Application.Services;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class ApartmentQcTopSectionPerformanceCalculatorTests
{
    private readonly ApartmentQcTopSectionPerformanceCalculator _calculator = new();

    [Fact]
    public void ComputeAdditionalRevenue_ChangePercent_IsRelativeToOldCurrentTax()
    {
        // Old tax 100, new total tax 150 -> a genuine 50% increase over the old value,
        // not 33.33% (which (150-100)/150*100 would incorrectly give).
        var result = _calculator.ComputeAdditionalRevenue(currentTax: 150m, retroTax: null, oldCurrentTax: 100m, pendingCurrent: null);

        Assert.Equal(50m, result.DifferenceAmount);
        Assert.Equal(50d, result.ChangePercent);
    }

    [Fact]
    public void ComputeAdditionalRevenue_OldCurrentTaxZero_ChangePercentIsNull()
    {
        // No prior tax to compare against -- percent change is undefined, not a division by
        // the (irrelevant) new total.
        var result = _calculator.ComputeAdditionalRevenue(currentTax: 150m, retroTax: null, oldCurrentTax: 0m, pendingCurrent: null);

        Assert.Equal(150m, result.DifferenceAmount);
        Assert.Null(result.ChangePercent);
    }

    [Fact]
    public void ComputeAdditionalRevenue_TaxDecreased_ChangePercentIsNegative()
    {
        var result = _calculator.ComputeAdditionalRevenue(currentTax: 80m, retroTax: null, oldCurrentTax: 100m, pendingCurrent: null);

        Assert.Equal(-20m, result.DifferenceAmount);
        Assert.Equal(-20d, result.ChangePercent);
    }
}
