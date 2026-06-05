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
    public const string EducationEmploymentTaxOnRV = "EducationEmploymentTaxOnRV";

    // Area Type Values
    public const string CarpetArea = "CarpetArea";
    public const string BuiltupArea = "BuiltupArea";

    // Area Unit Values
    public const string SqMeter = "SqMeter";
    public const string SqFeet = "SqFeet";

    // Rate Period Values
    public const string Monthly = "Monthly";
    public const string Yearly = "Yearly";

    // Boolean Policy Values
    public const string PolicyValueTrue = "1";
    public const string PolicyValueFalse = "0";

    // Default Values
    public const string DefaultAreaType = CarpetArea;
    public const string DefaultAreaUnit = SqMeter;
    public const string DefaultRatePeriod = Yearly;
    public const string DefaultEducationEmploymentTaxOnRV = PolicyValueFalse;
}
