using NtisPlatform.Application.DTOs.Asset_Management.CVCalculation;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for CVCalculationResultDto / CVCalculationInputDto - plain data carriers for the
/// Capital Value calculation, with no DataAnnotations (validated by the calculation service itself).
/// </summary>
public class CVCalculationDtoTests
{
    [Fact]
    public void CVCalculationResultDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new CVCalculationResultDto
        {
            CapitalValue = 100000m,
            BaseValue = 50000m,
            Rate = 1000m,
            CarpetAreaSqMeter = 50m,
            NatureFactor = 1.1m,
            UseFactor = 1.2m,
            AgeFactor = 0.9m,
            FloorFactor = 1.0m,
            AgeOfAsset = 5,
            CalculationDetails = "Base * Rate * Factors"
        };

        Assert.Equal(100000m, dto.CapitalValue);
        Assert.Equal(50000m, dto.BaseValue);
        Assert.Equal(1000m, dto.Rate);
        Assert.Equal(50m, dto.CarpetAreaSqMeter);
        Assert.Equal(1.1m, dto.NatureFactor);
        Assert.Equal(1.2m, dto.UseFactor);
        Assert.Equal(0.9m, dto.AgeFactor);
        Assert.Equal(1.0m, dto.FloorFactor);
        Assert.Equal(5, dto.AgeOfAsset);
        Assert.Equal("Base * Rate * Factors", dto.CalculationDetails);
    }

    [Fact]
    public void CVCalculationResultDto_Defaults_NumericFieldsAreZero_DetailsIsNull()
    {
        var dto = new CVCalculationResultDto();

        Assert.Equal(0m, dto.CapitalValue);
        Assert.Equal(0m, dto.BaseValue);
        Assert.Equal(0, dto.AgeOfAsset);
        Assert.Null(dto.CalculationDetails);
    }

    [Fact]
    public void CVCalculationInputDto_PropertiesGetAndSetCorrectly()
    {
        var dto = new CVCalculationInputDto
        {
            AssetId = 1,
            SubZoneId = 2,
            TypeOfUseId = 3,
            SubTypeOfUseId = 4,
            ConstructionTypeId = 5,
            FloorId = 6,
            CarpetAreaSqMeter = 75.5m,
            ConstructionYear = 2010,
            AssessmentYear = 2026,
            HasLift = true
        };

        Assert.Equal(1, dto.AssetId);
        Assert.Equal(2, dto.SubZoneId);
        Assert.Equal(3, dto.TypeOfUseId);
        Assert.Equal(4, dto.SubTypeOfUseId);
        Assert.Equal(5, dto.ConstructionTypeId);
        Assert.Equal(6, dto.FloorId);
        Assert.Equal(75.5m, dto.CarpetAreaSqMeter);
        Assert.Equal(2010, dto.ConstructionYear);
        Assert.Equal(2026, dto.AssessmentYear);
        Assert.True(dto.HasLift);
    }

    [Fact]
    public void CVCalculationInputDto_Defaults_OptionalFieldsAreNull_HasLiftIsFalse()
    {
        var dto = new CVCalculationInputDto();

        Assert.Null(dto.SubZoneId);
        Assert.Null(dto.TypeOfUseId);
        Assert.Null(dto.SubTypeOfUseId);
        Assert.Null(dto.ConstructionTypeId);
        Assert.Null(dto.FloorId);
        Assert.Null(dto.CarpetAreaSqMeter);
        Assert.Null(dto.ConstructionYear);
        Assert.Null(dto.AssessmentYear);
        Assert.False(dto.HasLift);
    }
}
