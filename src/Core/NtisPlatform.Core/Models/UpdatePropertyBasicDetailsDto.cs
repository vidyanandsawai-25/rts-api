using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Models;

/// <summary>
/// DTO for updating Property Basic Details Tab
/// Used for the PUT /{propertyId}/basic-details API endpoint
/// </summary>
public class UpdatePropertyBasicDetailsDto
{
    [Range(1, int.MaxValue, ErrorMessage = "WardId must be greater than 0.")]
    public int WardId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "TaxZoneId must be greater than 0.")]
    public int TaxZoneId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "CategoryId must be greater than 0.")]
    public int? CategoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "PropertyTypeId must be greater than 0.")]
    public int? PropertyTypeId { get; set; }

    [StringLength(10, ErrorMessage = "PartitionNo cannot exceed 10 characters.")]
    public string? PartitionNo { get; set; }

    [StringLength(50, ErrorMessage = "FlatOrShopNo cannot exceed 50 characters.")]
    public string? FlatOrShopNo { get; set; }

    [StringLength(20, ErrorMessage = "PlotNo cannot exceed 20 characters.")]
    public string? PlotNo { get; set; }

    [StringLength(30, ErrorMessage = "SurveyNo cannot exceed 30 characters.")]
    public string? SurveyNo { get; set; }

    [StringLength(30, ErrorMessage = "UPICId cannot exceed 30 characters.")]
    public string? UPICId { get; set; }

    [StringLength(20, ErrorMessage = "SubZoneNo cannot exceed 20 characters.")]
    public string? SubZoneNo { get; set; }

    [StringLength(50, ErrorMessage = "WingNo cannot exceed 50 characters.")]
    public string? WingNo { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "NoOfResidentialToilets cannot be negative.")]
    public int? NoOfResidentialToilets { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "NoOfCommercialToilets cannot be negative.")]
    public int? NoOfCommercialToilets { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "PlotArea cannot be negative.")]
    public double? PlotArea { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "PlotAreaFtLength cannot be negative.")]
    public double? PlotAreaFtLength { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "PlotAreaFtWidth cannot be negative.")]
    public double? PlotAreaFtWidth { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "PlotAreaMtrLength cannot be negative.")]
    public double? PlotAreaMtrLength { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "PlotAreaMtrWidth cannot be negative.")]
    public double? PlotAreaMtrWidth { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "WingId must be greater than 0.")]
    public int? WingId { get; set; }

    [StringLength(100, ErrorMessage = "WingName cannot exceed 100 characters.")]
    public string? WingName { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "MoujaId must be greater than 0.")]
    public int? MoujaId { get; set; }
}
