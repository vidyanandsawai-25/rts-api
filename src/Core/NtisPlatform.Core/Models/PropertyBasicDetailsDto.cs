namespace NtisPlatform.Core.Models;

/// <summary>
/// DTO for Property Basic Details Tab - includes joined data from multiple tables
/// Used for the GET /{propertyId}/basic-details API endpoint
/// </summary>
public class PropertyBasicDetailsDto
{
    public int PropertyId { get; set; }
    public int WardId { get; set; }
    public string? WardNo { get; set; }
    public int? ZoneId { get; set; }
    public string? Division { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string? FlatOrShopNo { get; set; }
    public string? PlotNo { get; set; }
    public string? SurveyNo { get; set; }
    public int TaxZoneId { get; set; }
    public string? TaxZoneNo { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? PropertyTypeId { get; set; }
    public string? PropertyDescription { get; set; }
    
    // From PropertyMast
    public string? UPICId { get; set; }
    public string? SubZoneNo { get; set; }
    public int? MoujaId { get; set; }
    
    // From MoujaMaster
    public string? MoujaName { get; set; }
    
    // From PropertyMastDetails (Assessment)
    public string? WingNo { get; set; }
    public int? NoOfResidentialToilets { get; set; }
    public int? NoOfCommercialToilets { get; set; }
    
    // From PropertyDetails (Aggregated)
    public double TotalCarpetAreaSqMeter { get; set; }
    public double TotalBuiltupAreaSqMeter { get; set; }
    public double? TotalCarpetAreaSqFeet { get; set; }
    public double? TotalBuiltupAreaSqFeet { get; set; }
    
    // From PlotDetails
    public double? PlotArea { get; set; }
    public double? PlotAreaFtLength { get; set; }
    public double? PlotAreaFtWidth { get; set; }
    public double? PlotAreaMtrLength { get; set; }
    public double? PlotAreaMtrWidth { get; set; }
    public double? PlotAreaSqFeet { get; set; }
    public double? PlotAreaSqMeter { get; set; }
    
    // From SocietyDetailsMast
    public int? WingId { get; set; }
    public string? WingName { get; set; }

    // From RateSectionMaster
    public string? RateSectionDescription { get; set; }

    // From PropertyMastDetails (Assessment)
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }

    // Earliest active construction year among all property details/floors
    public string? ConstructionYear { get; set; }

    // From PropertyDetailsOld (Summed from mapped old properties)
    public double? OldCarpetAreaSqFeet { get; set; }
    public double? OldCarpetAreaSqMeter { get; set; }
    public double? OldBuiltupAreaSqFeet { get; set; }
    public double? OldBuiltupAreaSqMeter { get; set; }
}
