using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Models;

/// <summary>
/// DTO for updating Property Old Details Tab
/// Used for the PUT /{propertyId}/old-details API endpoint
/// </summary>
public class UpdatePropertyOldDetailsDto
{
    [StringLength(10, ErrorMessage = "OldWardNo cannot exceed 10 characters.")]
    public string? OldWardNo { get; set; }

    [StringLength(10, ErrorMessage = "OldPropertyNo cannot exceed 10 characters.")]
    public string? OldPropertyNo { get; set; }

    [StringLength(10, ErrorMessage = "OldPartitionNo cannot exceed 10 characters.")]
    public string? OldPartitionNo { get; set; }

    [StringLength(10, ErrorMessage = "OldEgovNo cannot exceed 10 characters.")]
    public string? OldEgovNo { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "OldPlotArea cannot be negative.")]
    public double? OldPlotArea { get; set; }

    [StringLength(20, ErrorMessage = "OldPlotNo cannot exceed 20 characters.")]
    public string? OldPlotNo { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "OldRV cannot be negative.")]
    public double? OldRV { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "OldALV cannot be negative.")]
    public double? OldALV { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "OldTotalTax cannot be negative.")]
    public double? OldTotalTax { get; set; }

    [StringLength(20, ErrorMessage = "OldZoneNo cannot exceed 20 characters.")]
    public string? OldZoneNo { get; set; }

    [StringLength(4, ErrorMessage = "OldConstructionYear cannot exceed 4 characters.")]
    [RegularExpression(@"^$|^\d{4}$", ErrorMessage = "OldConstructionYear must be a 4-digit year.")]
    public string? OldConstructionYear { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "OldCarpetAreaSqFeet cannot be negative.")]
    public double? OldCarpetAreaSqFeet { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "OldCarpetAreaSqMeter cannot be negative.")]
    public double? OldCarpetAreaSqMeter { get; set; }

    public int? OldConstructionTypeId { get; set; }

    public int? OldTypeOfUseId { get; set; }
}
