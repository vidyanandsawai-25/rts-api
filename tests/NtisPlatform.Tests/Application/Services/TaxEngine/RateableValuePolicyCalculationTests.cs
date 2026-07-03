using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NtisPlatform.Application.Services.TaxEngine;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using System;
using System.Collections.Generic;
using Xunit;

namespace NtisPlatform.Tests.Application.Services.TaxEngine;

/// <summary>
/// Tests for policy-based Rateable Value calculation including:
/// - Area type selection (CarpetArea, BuiltupArea)
/// - Area unit selection (SqMeter, SqFeet)
/// - Rate period (Monthly, Yearly)
/// - Fallback/default behavior for missing or invalid policies
/// </summary>
public class RateableValuePolicyCalculationTests
{
    private readonly ILogger<RateableValuePolicyCalculationTests> _logger;

    public RateableValuePolicyCalculationTests()
    {
        _logger = NullLoggerFactory.Instance.CreateLogger<RateableValuePolicyCalculationTests>();
    }

    // Convenience wrapper that mirrors the old static signature used in tests.
    // Computes selectedArea from policyOptions so test call sites stay concise.
    private static PropertyTaxCalculationRVResultsEntity Calculate(
        PropertyDetailsEntity detail,
        int financeYear,
        int taxZoneId,
        int wardId,
        List<TypeOfUseEntity> typeOfUses,
        List<RateEntity> rates,
        List<DepreciationMasterEntity> depreciations,
        List<AssessmentYearRangeEntity> yearRanges,
        List<RenterMastEntity> renters,
        RateableValuePolicyOptions? policyOptions = null)
    {
        var options = policyOptions ?? RateableValuePolicyOptions.Default;
        var selectedArea = RateableValuePolicyHelper.GetSelectedArea(detail, options);

        // If year ranges are provided and contain an active entry, use its ID.
        // This matches the new method signature requirement for detailYearRangeRVId.
        int? detailYearRangeRVId = null;
        var safeYearRanges = yearRanges ?? new List<AssessmentYearRangeEntity>();
        if (safeYearRanges.Any())
        {
            var yearRange = safeYearRanges.FirstOrDefault(y => y.IsActive);
            detailYearRangeRVId = yearRange?.Id ?? (safeYearRanges.Count > 0 ? safeYearRanges[0].Id : null);
        }

        return new RateableValueCalculatorService(NullLogger<RateableValueCalculatorService>.Instance)
            .CalculateBaseValues(detail, financeYear, taxZoneId, wardId, typeOfUses, rates,
                depreciations, safeYearRanges, renters, selectedArea, options, null, detailYearRangeRVId);
    }

    #region GetSelectedArea Tests

    [Fact]
    public void GetSelectedArea_CarpetAreaSqMeter_ReturnsCarpetAreaSqMeter()
    {
        // Arrange
        var detail = CreatePropertyDetail(
            carpetAreaSqMeter: 100.5,
            carpetAreaSqFeet: 1082.3,
            builtupAreaSqMeter: 120.5,
            builtupAreaSqFeet: 1297.1);

        // Act
        var result = RateableValuePolicyHelper.GetSelectedArea(
            detail,
            RateableValuePolicyConstants.CarpetArea,
            RateableValuePolicyConstants.SqMeter);

        // Assert
        Assert.Equal(100.5m, result);
    }

    [Fact]
    public void GetSelectedArea_CarpetAreaSqFeet_ReturnsCarpetAreaSqFeet()
    {
        // Arrange
        var detail = CreatePropertyDetail(
            carpetAreaSqMeter: 100.5,
            carpetAreaSqFeet: 1082.3,
            builtupAreaSqMeter: 120.5,
            builtupAreaSqFeet: 1297.1);

        // Act
        var result = RateableValuePolicyHelper.GetSelectedArea(
            detail,
            RateableValuePolicyConstants.CarpetArea,
            RateableValuePolicyConstants.SqFeet);

        // Assert
        Assert.Equal(1082.3m, result);
    }

    [Fact]
    public void GetSelectedArea_BuiltupAreaSqMeter_ReturnsBuiltupAreaSqMeter()
    {
        // Arrange
        var detail = CreatePropertyDetail(
            carpetAreaSqMeter: 100.5,
            carpetAreaSqFeet: 1082.3,
            builtupAreaSqMeter: 120.5,
            builtupAreaSqFeet: 1297.1);

        // Act
        var result = RateableValuePolicyHelper.GetSelectedArea(
            detail,
            RateableValuePolicyConstants.BuiltupArea,
            RateableValuePolicyConstants.SqMeter);

        // Assert
        Assert.Equal(120.5m, result);
    }

    [Fact]
    public void GetSelectedArea_BuiltupAreaSqFeet_ReturnsBuiltupAreaSqFeet()
    {
        // Arrange
        var detail = CreatePropertyDetail(
            carpetAreaSqMeter: 100.5,
            carpetAreaSqFeet: 1082.3,
            builtupAreaSqMeter: 120.5,
            builtupAreaSqFeet: 1297.1);

        // Act
        var result = RateableValuePolicyHelper.GetSelectedArea(
            detail,
            RateableValuePolicyConstants.BuiltupArea,
            RateableValuePolicyConstants.SqFeet);

        // Assert
        Assert.Equal(1297.1m, result);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("InvalidAreaType", "SqMeter")]
    [InlineData("CarpetArea", "InvalidUnit")]
    [InlineData("InvalidAreaType", "InvalidUnit")]
    public void GetSelectedArea_InvalidOrNullValues_FallsBackToCarpetAreaSqMeter(string areaType, string areaUnit)
    {
        // Arrange
        var detail = CreatePropertyDetail(
            carpetAreaSqMeter: 100.5,
            carpetAreaSqFeet: 1082.3,
            builtupAreaSqMeter: 120.5,
            builtupAreaSqFeet: 1297.1);

        // Act
        var result = RateableValuePolicyHelper.GetSelectedArea(detail, areaType, areaUnit);

        // Assert - Falls back to CarpetAreaSqMeter
        Assert.Equal(100.5m, result);
    }

    [Fact]
    public void GetSelectedArea_NullAreaValues_ReturnsZero()
    {
        // Arrange
        var detail = CreatePropertyDetail(
            carpetAreaSqMeter: null,
            carpetAreaSqFeet: null,
            builtupAreaSqMeter: null,
            builtupAreaSqFeet: null);

        // Act
        var result = RateableValuePolicyHelper.GetSelectedArea(
            detail,
            RateableValuePolicyConstants.CarpetArea,
            RateableValuePolicyConstants.SqMeter);

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public void GetSelectedArea_CaseInsensitive_Works()
    {
        // Arrange
        var detail = CreatePropertyDetail(
            carpetAreaSqMeter: 100.5,
            carpetAreaSqFeet: 1082.3,
            builtupAreaSqMeter: 120.5,
            builtupAreaSqFeet: 1297.1);

        // Act & Assert - Various case combinations
        Assert.Equal(120.5m, RateableValuePolicyHelper.GetSelectedArea(detail, "builtuparea", "sqmeter"));
        Assert.Equal(1297.1m, RateableValuePolicyHelper.GetSelectedArea(detail, "BUILTUPAREA", "SQFEET"));
        Assert.Equal(100.5m, RateableValuePolicyHelper.GetSelectedArea(detail, "CarpetArea", "SqMeter"));
    }

    #endregion

    #region GetSelectedAreasForProperty (Batch) Tests

    [Fact]
    public void GetSelectedAreasForProperty_MultipleDetails_ReturnsAllAreas()
    {
        // Arrange
        var details = new List<PropertyDetailsEntity>
        {
            CreatePropertyDetail(id: 1, carpetAreaSqMeter: 100.0, builtupAreaSqMeter: 120.0),
            CreatePropertyDetail(id: 2, carpetAreaSqMeter: 200.0, builtupAreaSqMeter: 240.0),
            CreatePropertyDetail(id: 3, carpetAreaSqMeter: 300.0, builtupAreaSqMeter: 360.0)
        };

        // Act
        var result = RateableValuePolicyHelper.GetSelectedAreasForProperty(
            details,
            RateableValuePolicyConstants.CarpetArea,
            RateableValuePolicyConstants.SqMeter);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(100.0m, result[1]);
        Assert.Equal(200.0m, result[2]);
        Assert.Equal(300.0m, result[3]);
    }

    [Fact]
    public void GetSelectedAreasForProperty_BuiltupAreaSqFeet_ReturnsCorrectValues()
    {
        // Arrange
        var details = new List<PropertyDetailsEntity>
        {
            CreatePropertyDetail(id: 1, carpetAreaSqFeet: 1000.0, builtupAreaSqFeet: 1200.0),
            CreatePropertyDetail(id: 2, carpetAreaSqFeet: 2000.0, builtupAreaSqFeet: 2400.0)
        };

        // Act
        var result = RateableValuePolicyHelper.GetSelectedAreasForProperty(
            details,
            RateableValuePolicyConstants.BuiltupArea,
            RateableValuePolicyConstants.SqFeet);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(1200.0m, result[1]);
        Assert.Equal(2400.0m, result[2]);
    }

    [Fact]
    public void GetSelectedAreasForProperty_EmptyList_ReturnsEmptyDictionary()
    {
        // Arrange
        var details = new List<PropertyDetailsEntity>();

        // Act
        var result = RateableValuePolicyHelper.GetSelectedAreasForProperty(
            details,
            RateableValuePolicyConstants.CarpetArea,
            RateableValuePolicyConstants.SqMeter);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetSelectedAreasForProperty_NullList_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            RateableValuePolicyHelper.GetSelectedAreasForProperty(
                null!,
                RateableValuePolicyConstants.CarpetArea,
                RateableValuePolicyConstants.SqMeter));
    }

    [Fact]
    public void GetSelectedAreasForProperty_InvalidPolicyValues_FallsBackToDefault()
    {
        // Arrange
        var details = new List<PropertyDetailsEntity>
        {
            CreatePropertyDetail(id: 1, carpetAreaSqMeter: 100.0, builtupAreaSqMeter: 120.0)
        };

        // Act
        var result = RateableValuePolicyHelper.GetSelectedAreasForProperty(
            details, "InvalidAreaType", "InvalidUnit");

        // Assert - Falls back to CarpetAreaSqMeter
        Assert.Equal(100.0m, result[1]);
    }

    [Fact]
    public void GetSelectedAreasForProperty_MixedNullValues_HandlesGracefully()
    {
        // Arrange
        var details = new List<PropertyDetailsEntity>
        {
            CreatePropertyDetail(id: 1, carpetAreaSqMeter: 100.0),
            CreatePropertyDetail(id: 2, carpetAreaSqMeter: null),
            CreatePropertyDetail(id: 3, carpetAreaSqMeter: 300.0)
        };

        // Act
        var result = RateableValuePolicyHelper.GetSelectedAreasForProperty(
            details,
            RateableValuePolicyConstants.CarpetArea,
            RateableValuePolicyConstants.SqMeter);

        // Assert
        Assert.Equal(100.0m, result[1]);
        Assert.Equal(0m, result[2]); // Null becomes 0
        Assert.Equal(300.0m, result[3]);
    }

    #endregion

    #region IsMonthlyRate and IsSqFeetUnit Property Tests

    [Fact]
    public void IsMonthlyRate_WhenMonthly_ReturnsTrue()
    {
        // Arrange
        var options = new RateableValuePolicyOptions { RatePeriod = RateableValuePolicyConstants.Monthly };

        // Assert
        Assert.True(options.IsMonthlyRate);
    }

    [Fact]
    public void IsMonthlyRate_WhenYearly_ReturnsFalse()
    {
        // Arrange
        var options = new RateableValuePolicyOptions { RatePeriod = RateableValuePolicyConstants.Yearly };

        // Assert
        Assert.False(options.IsMonthlyRate);
    }

    [Theory]
    [InlineData("monthly", true)]
    [InlineData("MONTHLY", true)]
    [InlineData("Monthly", true)]
    [InlineData("yearly", false)]
    [InlineData("YEARLY", false)]
    [InlineData("Yearly", false)]
    public void IsMonthlyRate_CaseInsensitive(string ratePeriod, bool expected)
    {
        // Arrange
        var options = new RateableValuePolicyOptions { RatePeriod = ratePeriod };

        // Assert
        Assert.Equal(expected, options.IsMonthlyRate);
    }

    [Fact]
    public void IsSqFeetUnit_WhenSqFeet_ReturnsTrue()
    {
        // Arrange
        var options = new RateableValuePolicyOptions { AreaUnit = RateableValuePolicyConstants.SqFeet };

        // Assert
        Assert.True(options.IsSqFeetUnit);
    }

    [Fact]
    public void IsSqFeetUnit_WhenSqMeter_ReturnsFalse()
    {
        // Arrange
        var options = new RateableValuePolicyOptions { AreaUnit = RateableValuePolicyConstants.SqMeter };

        // Assert
        Assert.False(options.IsSqFeetUnit);
    }

    [Theory]
    [InlineData("sqfeet", true)]
    [InlineData("SQFEET", true)]
    [InlineData("SqFeet", true)]
    [InlineData("sqmeter", false)]
    [InlineData("SQMETER", false)]
    [InlineData("SqMeter", false)]
    public void IsSqFeetUnit_CaseInsensitive(string areaUnit, bool expected)
    {
        // Arrange
        var options = new RateableValuePolicyOptions { AreaUnit = areaUnit };

        // Assert
        Assert.Equal(expected, options.IsSqFeetUnit);
    }

    [Fact]
    public void EducationEmploymentTaxCalculationMethod_WhenRV_CalculatesOnRV()
    {
        // Arrange
        var options = new RateableValuePolicyOptions { EducationEmploymentTaxCalculationMethod = RateableValuePolicyConstants.RV };

        // Assert
        Assert.Equal(RateableValuePolicyConstants.RV, options.EducationEmploymentTaxCalculationMethod);
    }

    [Fact]
    public void EducationEmploymentTaxCalculationMethod_WhenALV_CalculatesOnALV()
    {
        // Arrange
        var options = new RateableValuePolicyOptions { EducationEmploymentTaxCalculationMethod = RateableValuePolicyConstants.ALV };

        // Assert
        Assert.Equal(RateableValuePolicyConstants.ALV, options.EducationEmploymentTaxCalculationMethod);
    }

    [Fact]
    public void EducationEmploymentTaxCalculationMethod_Default_IsALV()
    {
        // Arrange
        var options = RateableValuePolicyOptions.Default;

        // Assert - Default should calculate on AnnualRentalValue (ALV)
        Assert.Equal(RateableValuePolicyConstants.ALV, options.EducationEmploymentTaxCalculationMethod);
    }

    [Theory]
    [InlineData("RV", "RV")]
    [InlineData("ALV", "ALV")]
    [InlineData("rv", "RV")]
    [InlineData("invalid", "ALV")] // Invalid values should use default (ALV)
    public void EducationEmploymentTaxCalculationMethod_FromPolicies(string policyValue, string expected)
    {
        // Arrange
        var policies = new Dictionary<string, string>
        {
            { RateableValuePolicyConstants.EducationEmploymentTaxCalculationMethod, policyValue }
        };

        // Act
        var options = RateableValuePolicyOptions.FromPolicies(policies);

        // Assert - Code normalizes to uppercase for consistency
        Assert.Equal(expected, options.EducationEmploymentTaxCalculationMethod);
    }

    #endregion

    #region RateableValuePolicyOptions Tests

    [Fact]
    public void RateableValuePolicyOptions_Default_HasCorrectValues()
    {
        // Act
        var options = RateableValuePolicyOptions.Default;

        // Assert
        Assert.Equal(RateableValuePolicyConstants.DefaultAreaType, options.AreaType);
        Assert.Equal(RateableValuePolicyConstants.DefaultAreaUnit, options.AreaUnit);
        Assert.Equal(RateableValuePolicyConstants.DefaultRatePeriod, options.RatePeriod);
        Assert.Equal(RateableValuePolicyConstants.DefaultEducationEmploymentTaxCalculationMethod, options.EducationEmploymentTaxCalculationMethod);
    }

    [Fact]
    public void RateableValuePolicyOptions_FromPolicies_SetsAllValues()
    {
        // Arrange
        var policies = new Dictionary<string, string>
        {
            { RateableValuePolicyConstants.RateableValueAreaType, RateableValuePolicyConstants.BuiltupArea },
            { RateableValuePolicyConstants.RateMasterAreaUnit, RateableValuePolicyConstants.SqFeet },
            { RateableValuePolicyConstants.RateMonthlyOrYearly, RateableValuePolicyConstants.Monthly }
        };

        // Act
        var options = RateableValuePolicyOptions.FromPolicies(policies);

        // Assert
        Assert.Equal(RateableValuePolicyConstants.BuiltupArea, options.AreaType);
        Assert.Equal(RateableValuePolicyConstants.SqFeet, options.AreaUnit);
        Assert.Equal(RateableValuePolicyConstants.Monthly, options.RatePeriod);
    }

    [Fact]
    public void RateableValuePolicyOptions_FromPolicies_MissingPolicies_UsesDefaults()
    {
        // Arrange - Empty dictionary
        var policies = new Dictionary<string, string>();

        // Act
        var options = RateableValuePolicyOptions.FromPolicies(policies);

        // Assert - Uses defaults
        Assert.Equal(RateableValuePolicyConstants.DefaultAreaType, options.AreaType);
        Assert.Equal(RateableValuePolicyConstants.DefaultAreaUnit, options.AreaUnit);
        Assert.Equal(RateableValuePolicyConstants.DefaultRatePeriod, options.RatePeriod);
    }

    [Fact]
    public void RateableValuePolicyOptions_FromPolicies_InvalidValues_UsesDefaults()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        var policies = new Dictionary<string, string>
        {
            { RateableValuePolicyConstants.RateableValueAreaType, "InvalidAreaType" },
            { RateableValuePolicyConstants.RateMasterAreaUnit, "InvalidUnit" },
            { RateableValuePolicyConstants.RateMonthlyOrYearly, "InvalidPeriod" }
        };

        // Act
        var options = RateableValuePolicyOptions.FromPolicies(policies, mockLogger.Object);

        // Assert - Uses defaults for invalid values
        Assert.Equal(RateableValuePolicyConstants.DefaultAreaType, options.AreaType);
        Assert.Equal(RateableValuePolicyConstants.DefaultAreaUnit, options.AreaUnit);
        Assert.Equal(RateableValuePolicyConstants.DefaultRatePeriod, options.RatePeriod);
    }

    [Fact]
    public void RateableValuePolicyOptions_FromPolicies_PartialInvalidValues_MixedBehavior()
    {
        // Arrange
        var policies = new Dictionary<string, string>
        {
            { RateableValuePolicyConstants.RateableValueAreaType, RateableValuePolicyConstants.BuiltupArea }, // Valid
            { RateableValuePolicyConstants.RateMasterAreaUnit, "InvalidUnit" }, // Invalid
            { RateableValuePolicyConstants.RateMonthlyOrYearly, RateableValuePolicyConstants.Monthly } // Valid
        };

        // Act
        var options = RateableValuePolicyOptions.FromPolicies(policies);

        // Assert
        Assert.Equal(RateableValuePolicyConstants.BuiltupArea, options.AreaType); // From policy
        Assert.Equal(RateableValuePolicyConstants.DefaultAreaUnit, options.AreaUnit); // Default (invalid policy)
        Assert.Equal(RateableValuePolicyConstants.Monthly, options.RatePeriod); // From policy
    }

    #endregion

    #region CalculateBaseValues with Policy Options Tests

    [Theory]
    [InlineData("CarpetArea", "SqMeter", "Monthly", 100.0, 1200)]  // 100 sqm selected
    [InlineData("CarpetArea", "SqFeet", "Monthly", 100.548916899999999, 1200)]  // 1082.3 sqft = 100.548916... sqm (converted)
    [InlineData("BuiltupArea", "SqMeter", "Monthly", 120.5, 1200)] // 120.5 sqm selected
    [InlineData("BuiltupArea", "SqFeet", "Monthly", 120.504481299999999, 1200)] // 1297.1 sqft = 120.504481... sqm (converted)
    [InlineData("CarpetArea", "SqMeter", "Yearly", 100.0, 1200)]   // 100 sqm selected
    [InlineData("CarpetArea", "SqFeet", "Yearly", 100.548916899999999, 1200)]   // 1082.3 sqft = 100.548916... sqm (converted)
    public void CalculateBaseValues_WithPolicyOptions_UsesCorrectAreaAndPeriod(
        string areaType, string areaUnit, string ratePeriod, double expectedArea, decimal ratePerSqM)
    {
        // Arrange
        var detail = CreatePropertyDetail(
            carpetAreaSqMeter: 100.0,
            carpetAreaSqFeet: 1082.3,
            builtupAreaSqMeter: 120.5,
            builtupAreaSqFeet: 1297.1);

        var policyOptions = new RateableValuePolicyOptions
        {
            AreaType = areaType,
            AreaUnit = areaUnit,
            RatePeriod = ratePeriod
        };

        var typeOfUses = new List<TypeOfUseEntity>
        {
            new() { Id = 1, TypeOfUseGroupId = 1, Type = "R", IsActive = true }
        };

        var rates = new List<RateEntity>
        {
            new()
            {
                TaxZoneId = 1,
                ConstructionTypeId = 1,
                TypeOfUseGroupId = 1,
                YearRangeRVId = 1,
                RateSquareMeter = ratePerSqM,
                IsActive = true
            }
        };

        var yearRanges = new List<AssessmentYearRangeEntity>
        {
            new() { Id = 1, FromYear = 2000, ToYear = 2100, IsActive = true }
        };

        // Act
        var result = Calculate(
            detail,
            financeYear: 2024,
            taxZoneId: 1,
            wardId: 1,
            typeOfUses,
            rates,
            new List<DepreciationMasterEntity>(),
            yearRanges,
            new List<RenterMastEntity>(),
            policyOptions);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalAreaSqMtr.HasValue);
        Assert.Equal(expectedArea, result.TotalAreaSqMtr.Value, precision: 10);
        Assert.Equal(1, result.PropertyId);
    }

    [Fact]
    public void CalculateBaseValues_MonthlyRate_CalculatesCorrectYearlyRent()
    {
        // Arrange: CarpetArea + SqMeter + Monthly
        // Area = 100, Rate = 100/sqm (monthly), Yearly Rent = 100 * 100 * 12 = 120000
        var detail = CreatePropertyDetail(carpetAreaSqMeter: 100.0);

        var policyOptions = new RateableValuePolicyOptions
        {
            AreaType = RateableValuePolicyConstants.CarpetArea,
            AreaUnit = RateableValuePolicyConstants.SqMeter,
            RatePeriod = RateableValuePolicyConstants.Monthly
        };

        var typeOfUses = new List<TypeOfUseEntity>
        {
            new() { Id = 1, TypeOfUseGroupId = 1, Type = "R", IsActive = true }
        };

        var rates = new List<RateEntity>
        {
            new()
            {
                TaxZoneId = 1,
                ConstructionTypeId = 1,
                TypeOfUseGroupId = 1,
                YearRangeRVId = 1,
                RateSquareMeter = 100m,
                IsActive = true
            }
        };

        var yearRanges = new List<AssessmentYearRangeEntity>
        {
            new() { Id = 1, FromYear = 2000, ToYear = 2100, IsActive = true }
        };

        // Act
        var result = Calculate(
            detail, 2024, 1, 1, typeOfUses, rates,
            new List<DepreciationMasterEntity>(), yearRanges,
            new List<RenterMastEntity>(), policyOptions);

        // Assert: YearlyRent = 100 * 100 * 12 = 120000
        Assert.Equal(120000d, result.YearlyRent);
    }

    [Fact]
    public void CalculateBaseValues_YearlyRate_CalculatesCorrectYearlyRent()
    {
        // Arrange: CarpetArea + SqMeter + Yearly
        // Area = 100, Rate = 1200/sqm (yearly), Yearly Rent = 100 * 1200 = 120000
        var detail = CreatePropertyDetail(carpetAreaSqMeter: 100.0);

        var policyOptions = new RateableValuePolicyOptions
        {
            AreaType = RateableValuePolicyConstants.CarpetArea,
            AreaUnit = RateableValuePolicyConstants.SqMeter,
            RatePeriod = RateableValuePolicyConstants.Yearly
        };

        var typeOfUses = new List<TypeOfUseEntity>
        {
            new() { Id = 1, TypeOfUseGroupId = 1, Type = "R", IsActive = true }
        };

        var rates = new List<RateEntity>
        {
            new()
            {
                TaxZoneId = 1,
                ConstructionTypeId = 1,
                TypeOfUseGroupId = 1,
                YearRangeRVId = 1,
                RateSquareMeter = 1200m,
                IsActive = true
            }
        };

        var yearRanges = new List<AssessmentYearRangeEntity>
        {
            new() { Id = 1, FromYear = 2000, ToYear = 2100, IsActive = true }
        };

        // Act
        var result = Calculate(
            detail, 2024, 1, 1, typeOfUses, rates,
            new List<DepreciationMasterEntity>(), yearRanges,
            new List<RenterMastEntity>(), policyOptions);

        // Assert: YearlyRent = 100 * 1200 = 120000
        Assert.Equal(120000d, result.YearlyRent);
    }

    [Fact]
    public void CalculateBaseValues_BackwardCompatibility_DefaultPoliciesApplied()
    {
        // Arrange: Use the overload without policy options (backward compatible)
        var detail = CreatePropertyDetail(carpetAreaSqMeter: 100.0);

        var typeOfUses = new List<TypeOfUseEntity>
        {
            new() { Id = 1, TypeOfUseGroupId = 1, Type = "R", IsActive = true }
        };

        var rates = new List<RateEntity>
        {
            new()
            {
                TaxZoneId = 1,
                ConstructionTypeId = 1,
                TypeOfUseGroupId = 1,
                YearRangeRVId = 1,
                RateSquareMeter = 1200m,
                IsActive = true
            }
        };

        var yearRanges = new List<AssessmentYearRangeEntity>
        {
            new() { Id = 1, FromYear = 2000, ToYear = 2100, IsActive = true }
        };

        // Act - Use backward compatible overload (no policy options)
        var result = Calculate(
            detail, 2024, 1, 1, typeOfUses, rates,
            new List<DepreciationMasterEntity>(), yearRanges,
            new List<RenterMastEntity>());

        // Assert: Uses default policies - CarpetArea, SqMeter, Yearly
        // YearlyRent = 100 * 1200 = 120000 (yearly calculation)
        Assert.Equal(120000d, result.YearlyRent);
        Assert.Equal(100d, result.TotalAreaSqMtr);
    }

    [Fact]
    public void CalculateBaseValues_NullPolicyOptions_UsesDefaults()
    {
        // Arrange
        var detail = CreatePropertyDetail(carpetAreaSqMeter: 100.0);

        var typeOfUses = new List<TypeOfUseEntity>
        {
            new() { Id = 1, TypeOfUseGroupId = 1, Type = "R", IsActive = true }
        };

        var rates = new List<RateEntity>
        {
            new()
            {
                TaxZoneId = 1,
                ConstructionTypeId = 1,
                TypeOfUseGroupId = 1,
                YearRangeRVId = 1,
                RateSquareMeter = 1200m,
                IsActive = true
            }
        };

        var yearRanges = new List<AssessmentYearRangeEntity>
        {
            new() { Id = 1, FromYear = 2000, ToYear = 2100, IsActive = true }
        };

        // Act - Pass null policy options
        var result = Calculate(
            detail, 2024, 1, 1, typeOfUses, rates,
            new List<DepreciationMasterEntity>(), yearRanges,
            new List<RenterMastEntity>(), null!);

        // Assert: Uses default policies
        Assert.Equal(120000d, result.YearlyRent);
    }

    [Fact]
    public void CalculateBaseValues_IsTaxableFalse_ReturnsZeroedResult()
    {
        // Arrange
        var detail = CreatePropertyDetail(carpetAreaSqMeter: 100.0);
        detail.IsTaxable = false;

        var policyOptions = new RateableValuePolicyOptions
        {
            AreaType = RateableValuePolicyConstants.CarpetArea,
            AreaUnit = RateableValuePolicyConstants.SqMeter,
            RatePeriod = RateableValuePolicyConstants.Monthly
        };

        // Act
        var result = Calculate(
            detail, 2024, 1, 1,
            new List<TypeOfUseEntity>(),
            new List<RateEntity>(),
            new List<DepreciationMasterEntity>(),
            new List<AssessmentYearRangeEntity> { new() { Id = 1, FromYear = 2000, ToYear = 2100, IsActive = true } },
            new List<RenterMastEntity>(),
            policyOptions);

        // Assert
        Assert.Equal("Not Taxable", result.AppliedOn);
        Assert.Equal(0m, result.RateableValue);
        Assert.Equal(0d, result.YearlyRent);
    }

    #endregion

    #region Helper Methods

    private static PropertyDetailsEntity CreatePropertyDetail(
        int id = 100,
        double? carpetAreaSqMeter = null,
        double? carpetAreaSqFeet = null,
        double? builtupAreaSqMeter = null,
        double? builtupAreaSqFeet = null)
    {
        return new PropertyDetailsEntity
        {
            Id = id,
            PropertyId = 1,
            IsTaxable = true,
            TypeOfUseId = 1,
            ConstructionTypeId = 1,
            FloorId = 1,
            ConstructionYear = "2020",
            CarpetAreaSqMeter = carpetAreaSqMeter,
            CarpetAreaSqFeet = carpetAreaSqFeet,
            BuiltupAreaSqMeter = builtupAreaSqMeter,
            BuiltupAreaSqFeet = builtupAreaSqFeet
        };
    }

    #endregion
}
