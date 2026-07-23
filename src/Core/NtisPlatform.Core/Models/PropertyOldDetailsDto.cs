namespace NtisPlatform.Core.Models;

/// <summary>
/// DTO for Property Old Details Tab - includes joined data from PropertyMastOld and PropertyDetailsOld tables
/// Used for the GET /{propertyId}/old-details API endpoint
/// </summary>
public class PropertyOldDetailsDto
{
    public int PropertyId { get; set; }

    // From PropertyMastOld
    public string? OldWardNo { get; set; }
    public string? OldPropertyNo { get; set; }
    public string? OldPartitionNo { get; set; }
    public string? OldEgovNo { get; set; }
    public string? OldPlotArea { get; set; }
    public string? OldPlotNo { get; set; }
    public double? OldRV { get; set; }
    public double? OldALV { get; set; }
    public double? OldTotalTax { get; set; }
    public string? OldZoneNo { get; set; }
    public double? OldGeneralTax { get; set; }
    public string? OldCSN { get; set; }
    public double? OldConstructionArea { get; set; }

    // From PropertyDetailsOld (Aggregated if multiple records exist)
    public string? OldConstructionYear { get; set; }
    public double? OldCarpetAreaSqFeet { get; set; }
    public double? OldCarpetAreaSqMeter { get; set; }
    public string? OldConstructionTypeId { get; set; }
    public string? OldTypeOfUseId { get; set; }
}
