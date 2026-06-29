namespace NtisPlatform.Application.Services.TaxEngine;

/// <summary>
/// Constants for Rateable Value policy configuration
/// </summary>
public static class RateableValuePolicyConstants
{
    // Policy Codes
    public const string RateableValueAreaType = "RateableValueAreaType";
    public const string RateMasterAreaUnit = "RateMasterAreaUnit";
    public const string RateMonthlyOrYearly = "RateMonthlyOrYearly";
    public const string EducationEmploymentTaxCalculationMethod = "EducationEmploymentTaxCalculationMethod";

    // Area Type Values
    public const string CarpetArea = "CarpetArea";
    public const string BuiltupArea = "BuiltupArea";

    // Area Unit Values
    public const string SqMeter = "SqMeter";
    public const string SqFeet = "SqFeet";

    // Rate Period Values
    public const string Monthly = "Monthly";
    public const string Yearly = "Yearly";

    // Education/Employment Tax Calculation Method Values
    public const string RV = "RV";      // RateableValue
    public const string ALV = "ALV";    // AnnualRentalValue

    // Boolean Policy Values
    public const string PolicyValueTrue = "1";
    public const string PolicyValueFalse = "0";

    // Maintenance Rate
    /// <summary>Policy key for the statutory maintenance deduction percentage (e.g. "10" = 10%).</summary>
    public const string MaintenanceRateKey = "RV_MaintenanceRate";
    /// <summary>Default maintenance rate percentage when the policy key is absent.</summary>
    public const string DefaultMaintenanceRate = "10";
    /// <summary>Parsed numeric default used when the policy string cannot be resolved.</summary>
    public const decimal DefaultMaintenanceRateValue = 10m;

    // Default Values
    public const string DefaultAreaType = CarpetArea;
    public const string DefaultAreaUnit = SqMeter;
    public const string DefaultRatePeriod = Yearly;
    public const string DefaultEducationEmploymentTaxCalculationMethod = ALV;
}
