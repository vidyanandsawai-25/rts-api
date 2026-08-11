using NtisPlatform.Application.Services;
using Xunit;

namespace NtisPlatform.Tests.Application.Services.Asset_Management;

/// <summary>
/// Covers <see cref="AssetCapitalValueService.CapitalValueCalculationEngine"/> — the pure,
/// stateless math used by both the floor-detail CV path and the open-plot CV path
/// (AssetCapitalValueService.BuildingCV.cs / .OpenPlot.cs). No repositories, no mocking needed.
/// </summary>
public class CapitalValueCalculationEngineTests
{
    [Fact]
    public void Calculate_WithAllDefaultFactors_ReturnsBaseValueAsCapitalValue()
    {
        var result = AssetCapitalValueService.CapitalValueCalculationEngine.Calculate(
            baseRate: 1000m, carpetAreaSqMeter: 50m);

        Assert.Equal(50000m, result.BaseValue);
        Assert.Equal(50000m, result.CapitalValue);
        Assert.Contains("CV =", result.Formula);
    }

    [Fact]
    public void Calculate_WithAllFactorsSupplied_MultipliesEveryFactor()
    {
        var result = AssetCapitalValueService.CapitalValueCalculationEngine.Calculate(
            baseRate: 1000m,
            carpetAreaSqMeter: 50m,
            natureFactor: 1.1m,
            useFactor: 0.9m,
            ageFactor: 0.8m,
            floorFactor: 1.2m);

        // BaseValue = rate * area = 50000; CV = BaseValue * 1.1 * 0.9 * 0.8 * 1.2
        Assert.Equal(50000m, result.BaseValue);
        Assert.Equal(50000m * 1.1m * 0.9m * 0.8m * 1.2m, result.CapitalValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Calculate_WithNonPositiveCarpetArea_ReturnsZeroWithInvalidMessage(decimal area)
    {
        var result = AssetCapitalValueService.CapitalValueCalculationEngine.Calculate(
            baseRate: 1000m, carpetAreaSqMeter: area);

        Assert.Equal(0m, result.BaseValue);
        Assert.Equal(0m, result.CapitalValue);
        Assert.Equal("Invalid carpet area", result.Formula);
    }

    [Fact]
    public void Calculate_WithZeroBaseRate_ReturnsZeroCapitalValue()
    {
        var result = AssetCapitalValueService.CapitalValueCalculationEngine.Calculate(
            baseRate: 0m, carpetAreaSqMeter: 100m);

        Assert.Equal(0m, result.BaseValue);
        Assert.Equal(0m, result.CapitalValue);
    }

    [Fact]
    public void Calculate_WithZeroFactor_ZeroesOutCapitalValueButNotBaseValue()
    {
        var result = AssetCapitalValueService.CapitalValueCalculationEngine.Calculate(
            baseRate: 1000m, carpetAreaSqMeter: 50m, natureFactor: 0m);

        Assert.Equal(50000m, result.BaseValue);
        Assert.Equal(0m, result.CapitalValue);
    }

    [Fact]
    public void Calculate_FormulaString_ContainsBaseRateAndArea()
    {
        var result = AssetCapitalValueService.CapitalValueCalculationEngine.Calculate(
            baseRate: 250.5m, carpetAreaSqMeter: 12.3456m);

        Assert.Contains("250.50", result.Formula);
        Assert.Contains("12.3456", result.Formula);
    }
}
