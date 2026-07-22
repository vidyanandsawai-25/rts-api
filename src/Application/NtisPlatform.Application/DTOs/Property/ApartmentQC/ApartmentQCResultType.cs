namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

/// <summary>
/// Controls which tax-calculation result fields are returned by the
/// per-PropertyDetails Apartment QC endpoint.
/// </summary>
public enum ApartmentQCResultType
{
    /// <summary>Return both RV and CV calculation fields.</summary>
    Dual = 0,

    /// <summary>Return only RateableValue calculation fields (RVCalculationResults).</summary>
    Rateable = 1,

    /// <summary>Return only CapitalValue calculation fields (PropertyTaxCalculationCVResults).</summary>
    Capital = 2
}
