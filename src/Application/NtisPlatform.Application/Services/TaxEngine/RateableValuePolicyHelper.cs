using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NtisPlatform.Application.Services.TaxEngine;

/// <summary>
/// Configuration options for Rateable Value calculation based on policy settings
/// </summary>
public class RateableValuePolicyOptions
{
    /// <summary>
    /// Area type for calculation: CarpetArea or BuiltupArea
    /// </summary>
    public string AreaType { get; set; } = RateableValuePolicyConstants.DefaultAreaType;

    /// <summary>
    /// Area unit for calculation: SqMeter or SqFeet
    /// </summary>
    public string AreaUnit { get; set; } = RateableValuePolicyConstants.DefaultAreaUnit;

    /// <summary>
    /// Rate period: Monthly or Yearly
    /// </summary>
    public string RatePeriod { get; set; } = RateableValuePolicyConstants.DefaultRatePeriod;

    /// <summary>
    /// Education/Employment tax calculation base: "1" = RateableValue, "0" = AnnualRentalValue
    /// </summary>
    public string EducationEmploymentTaxOnRV { get; set; } = RateableValuePolicyConstants.DefaultEducationEmploymentTaxOnRV;

    /// <summary>
    /// Returns true if rate period is Monthly (computed once, reused for all calculations)
    /// </summary>
    public bool IsMonthlyRate => string.Equals(RatePeriod, RateableValuePolicyConstants.Monthly, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns true if area unit is SqFeet (computed once, reused for all calculations)
    /// </summary>
    public bool IsSqFeetUnit => string.Equals(AreaUnit, RateableValuePolicyConstants.SqFeet, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns true if Education/Employment tax should be calculated on RateableValue instead of AnnualRentalValue
    /// </summary>
    public bool IsEducationEmploymentTaxOnRV => string.Equals(EducationEmploymentTaxOnRV, RateableValuePolicyConstants.PolicyValueTrue, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Creates policy options with default values
    /// </summary>
    public static RateableValuePolicyOptions Default => new();

    /// <summary>
    /// Creates policy options from policy values dictionary
    /// </summary>
    public static RateableValuePolicyOptions FromPolicies(Dictionary<string, string> policies, ILogger? logger = null)
    {
        var options = new RateableValuePolicyOptions();

        if (policies.TryGetValue(RateableValuePolicyConstants.RateableValueAreaType, out var areaType))
        {
            if (IsValidAreaType(areaType))
            {
                options.AreaType = areaType;
            }
            else
            {
                logger?.LogWarning("Invalid RateableValueAreaType policy value '{Value}'. Using default '{Default}'", areaType, RateableValuePolicyConstants.DefaultAreaType);
            }
        }

        if (policies.TryGetValue(RateableValuePolicyConstants.RateMasterAreaUnit, out var areaUnit))
        {
            if (IsValidAreaUnit(areaUnit))
            {
                options.AreaUnit = areaUnit;
            }
            else
            {
                logger?.LogWarning("Invalid RateMasterAreaUnit policy value '{Value}'. Using default '{Default}'", areaUnit, RateableValuePolicyConstants.DefaultAreaUnit);
            }
        }

        if (policies.TryGetValue(RateableValuePolicyConstants.RateMonthlyOrYearly, out var ratePeriod))
        {
            if (IsValidRatePeriod(ratePeriod))
            {
                options.RatePeriod = ratePeriod;
            }
            else
            {
                logger?.LogWarning("Invalid RateMonthlyOrYearly policy value '{Value}'. Using default '{Default}'", ratePeriod, RateableValuePolicyConstants.DefaultRatePeriod);
            }
        }

        if (policies.TryGetValue(RateableValuePolicyConstants.EducationEmploymentTaxOnRV, out var eduEmpTaxOnRV))
        {
            if (IsValidBooleanPolicy(eduEmpTaxOnRV))
            {
                options.EducationEmploymentTaxOnRV = eduEmpTaxOnRV;
            }
            else
            {
                logger?.LogWarning("Invalid EducationEmploymentTaxOnRV policy value '{Value}'. Using default '{Default}'", eduEmpTaxOnRV, RateableValuePolicyConstants.DefaultEducationEmploymentTaxOnRV);
            }
        }

        return options;
    }

    private static bool IsValidAreaType(string value) =>
        string.Equals(value, RateableValuePolicyConstants.CarpetArea, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, RateableValuePolicyConstants.BuiltupArea, StringComparison.OrdinalIgnoreCase);

    private static bool IsValidAreaUnit(string value) =>
        string.Equals(value, RateableValuePolicyConstants.SqMeter, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, RateableValuePolicyConstants.SqFeet, StringComparison.OrdinalIgnoreCase);

    private static bool IsValidRatePeriod(string value) =>
        string.Equals(value, RateableValuePolicyConstants.Monthly, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, RateableValuePolicyConstants.Yearly, StringComparison.OrdinalIgnoreCase);

    private static bool IsValidBooleanPolicy(string value) =>
        string.Equals(value, RateableValuePolicyConstants.PolicyValueTrue, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, RateableValuePolicyConstants.PolicyValueFalse, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Helper class for policy-based area selection in Rateable Value calculations.
/// Provides reusable methods for selecting appropriate area values based on policy configuration.
/// </summary>
public static class RateableValuePolicyHelper
{
    /// <summary>
    /// Gets selected areas for all property details at once based on policy configuration.
    /// This is more efficient than calling GetSelectedArea for each detail individually.
    /// </summary>
    /// <param name="details">List of property details entities</param>
    /// <param name="areaType">Area type: CarpetArea or BuiltupArea</param>
    /// <param name="areaUnit">Area unit: SqMeter or SqFeet</param>
    /// <returns>Dictionary mapping property detail ID to selected area value</returns>
    public static Dictionary<int, decimal> GetSelectedAreasForProperty(
        List<PropertyDetailsEntity> details,
        string areaType,
        string areaUnit)
    {
        if (details == null) throw new ArgumentNullException(nameof(details));

        // Normalize input values for comparison once
        var normalizedAreaType = areaType?.Trim() ?? RateableValuePolicyConstants.DefaultAreaType;
        var normalizedAreaUnit = areaUnit?.Trim() ?? RateableValuePolicyConstants.DefaultAreaUnit;

        // Determine which area selector to use based on policy
        Func<PropertyDetailsEntity, decimal> areaSelector = GetAreaSelector(normalizedAreaType, normalizedAreaUnit);

        // Process all details in a single pass
        return details.ToDictionary(
            detail => detail.Id,
            detail => areaSelector(detail));
    }

    /// <summary>
    /// Gets selected areas for all property details using policy options.
    /// </summary>
    /// <param name="details">List of property details entities</param>
    /// <param name="policyOptions">Policy options containing area type and unit</param>
    /// <returns>Dictionary mapping property detail ID to selected area value</returns>
    public static Dictionary<int, decimal> GetSelectedAreasForProperty(
        List<PropertyDetailsEntity> details,
        RateableValuePolicyOptions policyOptions)
    {
        var options = policyOptions ?? RateableValuePolicyOptions.Default;
        return GetSelectedAreasForProperty(details, options.AreaType, options.AreaUnit);
    }

    /// <summary>
    /// Gets the selected area for a single property detail based on policy configuration.
    /// For processing multiple details, use GetSelectedAreasForProperty for better performance.
    /// </summary>
    /// <param name="detail">Property details entity</param>
    /// <param name="areaType">Area type: CarpetArea or BuiltupArea</param>
    /// <param name="areaUnit">Area unit: SqMeter or SqFeet</param>
    /// <returns>The selected area value</returns>
    public static decimal GetSelectedArea(PropertyDetailsEntity detail, string areaType, string areaUnit)
    {
        if (detail == null) throw new ArgumentNullException(nameof(detail));

        // Normalize input values for comparison
        var normalizedAreaType = areaType?.Trim() ?? RateableValuePolicyConstants.DefaultAreaType;
        var normalizedAreaUnit = areaUnit?.Trim() ?? RateableValuePolicyConstants.DefaultAreaUnit;

        return GetAreaSelector(normalizedAreaType, normalizedAreaUnit)(detail);
    }

    /// <summary>
    /// Gets the selected area for a single property detail using policy options.
    /// </summary>
    /// <param name="detail">Property details entity</param>
    /// <param name="policyOptions">Policy options containing area type and unit</param>
    /// <returns>The selected area value</returns>
    public static decimal GetSelectedArea(PropertyDetailsEntity detail, RateableValuePolicyOptions policyOptions)
    {
        var options = policyOptions ?? RateableValuePolicyOptions.Default;
        return GetSelectedArea(detail, options.AreaType, options.AreaUnit);
    }

    /// <summary>
    /// Returns the appropriate area selector function based on area type and unit.
    /// </summary>
    private static Func<PropertyDetailsEntity, decimal> GetAreaSelector(string areaType, string areaUnit)
    {
        // CarpetArea + SqMeter (default)
        if (string.Equals(areaType, RateableValuePolicyConstants.CarpetArea, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(areaUnit, RateableValuePolicyConstants.SqMeter, StringComparison.OrdinalIgnoreCase))
        {
            return detail => Convert.ToDecimal(detail.CarpetAreaSqMeter ?? 0d);
        }

        // CarpetArea + SqFeet
        if (string.Equals(areaType, RateableValuePolicyConstants.CarpetArea, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(areaUnit, RateableValuePolicyConstants.SqFeet, StringComparison.OrdinalIgnoreCase))
        {
            return detail => Convert.ToDecimal(detail.CarpetAreaSqFeet ?? 0d);
        }

        // BuiltupArea + SqMeter
        if (string.Equals(areaType, RateableValuePolicyConstants.BuiltupArea, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(areaUnit, RateableValuePolicyConstants.SqMeter, StringComparison.OrdinalIgnoreCase))
        {
            return detail => Convert.ToDecimal(detail.BuiltupAreaSqMeter ?? 0d);
        }

        // BuiltupArea + SqFeet
        if (string.Equals(areaType, RateableValuePolicyConstants.BuiltupArea, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(areaUnit, RateableValuePolicyConstants.SqFeet, StringComparison.OrdinalIgnoreCase))
        {
            return detail => Convert.ToDecimal(detail.BuiltupAreaSqFeet ?? 0d);
        }

        // Fallback to default: CarpetAreaSqMeter
        return detail => Convert.ToDecimal(detail.CarpetAreaSqMeter ?? 0d);
    }
}
