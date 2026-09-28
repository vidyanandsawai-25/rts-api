namespace NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;

/// <summary>
/// The property-info block (division, mouja, survey/plot no, tax zone, contact, address, areas).
/// </summary>
public sealed class PropertyInfoDto
{
    public string? Division { get; init; }
    public string? MoujaName { get; init; }
    public string? SurveyNo { get; init; }
    public string? PlotNo { get; init; }
    public string? TaxZone { get; init; }
    public string? MobileNo { get; init; }
    public string? AlternateMobileNo { get; init; }
    public string? AadharNo { get; init; }
    public string? EmailId { get; init; }
    public string? Address { get; init; }
    public string? Pincode { get; init; }
    public double? PlotAreaFt { get; init; }
    public double? PlotAreaMtr { get; init; }
    public double? CarpetAreaFt { get; init; }
    public double? CarpetAreaMtr { get; init; }
    public double? BuiltUpAreaFt { get; init; }
    public double? BuiltUpAreaMtr { get; init; }
}
