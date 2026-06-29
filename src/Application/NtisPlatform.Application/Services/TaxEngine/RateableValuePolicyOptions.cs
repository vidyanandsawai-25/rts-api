using Microsoft.Extensions.Logging;

namespace NtisPlatform.Application.Services.TaxEngine;

/// <summary>
/// Configuration options for Rateable Value calculation based on policy settings.
/// </summary>
public class RateableValuePolicyOptions
{
    /// <summary>Area type for calculation: CarpetArea or BuiltupArea</summary>
    public string AreaType { get; set; } = RateableValuePolicyConstants.DefaultAreaType;

    /// <summary>Area unit for calculation: SqMeter or SqFeet</summary>
    public string AreaUnit { get; set; } = RateableValuePolicyConstants.DefaultAreaUnit;

    /// <summary>Rate period: Monthly or Yearly</summary>
    public string RatePeriod { get; set; } = RateableValuePolicyConstants.DefaultRatePeriod;

    /// <summary>Education/Employment tax calculation method: "RV" = RateableValue, "ALV" = AnnualRentalValue (default)</summary>
    public string EducationEmploymentTaxCalculationMethod { get; set; } = RateableValuePolicyConstants.DefaultEducationEmploymentTaxCalculationMethod;

    /// <summary>
    /// Maintenance deduction as a percentage of AnnualRentalValue (e.g. 10 means 10%).
    /// Loaded from <see cref="RateableValuePolicyConstants.MaintenanceRateKey"/>; defaults to 10.
    /// </summary>
    public decimal MaintenanceRatePercent { get; set; } = RateableValuePolicyConstants.DefaultMaintenanceRateValue;

    /// <summary>Returns true if rate period is Monthly.</summary>
    public bool IsMonthlyRate =>
        string.Equals(RatePeriod, RateableValuePolicyConstants.Monthly, StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns true if area unit is SqFeet.</summary>
    public bool IsSqFeetUnit =>
        string.Equals(AreaUnit, RateableValuePolicyConstants.SqFeet, StringComparison.OrdinalIgnoreCase);

    /// <summary>Creates policy options with default values.</summary>
    public static RateableValuePolicyOptions Default => new();

    /// <summary>Creates policy options from a policy values dictionary.</summary>
    public static RateableValuePolicyOptions FromPolicies(Dictionary<string, string> policies, ILogger? logger = null)
    {
        var options = new RateableValuePolicyOptions();

        // Log all available policy keys for debugging
        logger?.LogInformation("[Policies] Available policy keys: {Keys}", string.Join(", ", policies.Keys));

        if (policies.TryGetValue(RateableValuePolicyConstants.RateableValueAreaType, out var areaType))
        {
            if (IsValidAreaType(areaType))
                options.AreaType = areaType;
            else
                logger?.LogWarning("Invalid RateableValueAreaType policy value '{Value}'. Using default '{Default}'",
                    areaType, RateableValuePolicyConstants.DefaultAreaType);
        }

        if (policies.TryGetValue(RateableValuePolicyConstants.RateMasterAreaUnit, out var areaUnit))
        {
            if (IsValidAreaUnit(areaUnit))
                options.AreaUnit = areaUnit;
            else
                logger?.LogWarning("Invalid RateMasterAreaUnit policy value '{Value}'. Using default '{Default}'",
                    areaUnit, RateableValuePolicyConstants.DefaultAreaUnit);
        }

        if (policies.TryGetValue(RateableValuePolicyConstants.RateMonthlyOrYearly, out var ratePeriod))
        {
            if (IsValidRatePeriod(ratePeriod))
                options.RatePeriod = ratePeriod;
            else
                logger?.LogWarning("Invalid RateMonthlyOrYearly policy value '{Value}'. Using default '{Default}'",
                    ratePeriod, RateableValuePolicyConstants.DefaultRatePeriod);
        }

        // Try new policy key first
        logger?.LogInformation("[Policy] Attempting to load policy key: '{Key}'", RateableValuePolicyConstants.EducationEmploymentTaxCalculationMethod);
        if (policies.TryGetValue(RateableValuePolicyConstants.EducationEmploymentTaxCalculationMethod, out var taxCalcMethod))
        {
            // Trim whitespace, remove BOM, and normalize
            var rawValue = taxCalcMethod ?? string.Empty;
            var normalizedMethod = rawValue
                .Trim()  // Remove leading/trailing whitespace
                .Replace("﻿", "")  // Remove BOM
                .ToUpperInvariant();  // Uppercase for consistency

            logger?.LogInformation("[Policy] ✓ FOUND policy '{Key}' with raw value: '{RawValue}' (length={Length}, bytes={Bytes})",
                RateableValuePolicyConstants.EducationEmploymentTaxCalculationMethod,
                rawValue,
                rawValue.Length,
                string.Join(",", rawValue.Select(c => (int)c)));
            logger?.LogInformation("[Policy] After normalization: '{NormalizedValue}' (length={Length})",
                normalizedMethod, normalizedMethod.Length);

            if (IsValidTaxCalculationMethod(normalizedMethod))
            {
                options.EducationEmploymentTaxCalculationMethod = normalizedMethod;
                logger?.LogInformation("[Policy] ✓✓ VALIDATION PASSED: Set EducationEmploymentTaxCalculationMethod to: '{Value}'", normalizedMethod);
            }
            else
            {
                logger?.LogWarning("[Policy] ✗✗ VALIDATION FAILED: Invalid EducationEmploymentTaxCalculationMethod value '{Value}'. " +
                    "Expected 'RV' or 'ALV'. Using default '{Default}'. Character codes: {CharCodes}",
                    normalizedMethod, RateableValuePolicyConstants.DefaultEducationEmploymentTaxCalculationMethod,
                    string.Join(",", normalizedMethod.Select(c => (int)c)));
            }
        }
        else
        {
            logger?.LogWarning("[Policy] ✗ EducationEmploymentTaxCalculationMethod not found in policies. " +
                "Using default: '{Default}'. Available keys: {AllKeys}",
                RateableValuePolicyConstants.DefaultEducationEmploymentTaxCalculationMethod,
                string.Join(", ", policies.Keys));
        }

        if (policies.TryGetValue(RateableValuePolicyConstants.MaintenanceRateKey, out var maintenanceStr) &&
            decimal.TryParse(maintenanceStr, out var maintenanceRate) &&
            maintenanceRate >= 0 && maintenanceRate <= 100)
        {
            options.MaintenanceRatePercent = maintenanceRate;
        }
        else if (policies.ContainsKey(RateableValuePolicyConstants.MaintenanceRateKey))
        {
            logger?.LogWarning("Invalid MaintenanceRate policy value '{Value}'. Using default {Default}%",
                policies[RateableValuePolicyConstants.MaintenanceRateKey],
                RateableValuePolicyConstants.DefaultMaintenanceRateValue);
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

    private static bool IsValidTaxCalculationMethod(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        bool isRV = string.Equals(value, RateableValuePolicyConstants.RV, StringComparison.OrdinalIgnoreCase);
        bool isALV = string.Equals(value, RateableValuePolicyConstants.ALV, StringComparison.OrdinalIgnoreCase);
        bool isValid = isRV || isALV;

        return isValid;
    }
}
