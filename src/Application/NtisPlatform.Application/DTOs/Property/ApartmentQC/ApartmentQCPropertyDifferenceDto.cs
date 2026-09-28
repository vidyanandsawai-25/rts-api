namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

/// <summary>
/// Calculated differences between New Survey and Old Survey property details.
/// </summary>
public class ApartmentQCPropertyDifferenceDto
{
    public decimal CarpetAreaSqMeterDiff { get; set; }
    public decimal CarpetAreaSqFeetDiff { get; set; }
    public decimal BuiltupAreaSqMeterDiff { get; set; }
    public decimal BuiltupAreaSqFeetDiff { get; set; }

    public decimal? RateableValueDiff { get; set; }
    public decimal? CapitalValueDiff { get; set; }
    public decimal? TotalTaxDiff { get; set; }
    public decimal? RetroTaxDiff { get; set; }
}
