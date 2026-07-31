using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management.AssetDetails;

/// <summary>
/// DTO for AssetDetailsEntity - Auxiliary location + KYC details for an asset (1:1 with AssetMaster).
/// </summary>
public class AssetDetailsDto : BaseDtos
{
    public int AssetId { get; set; }
    public int OrganizationId { get; set; }
    public int? ZoneId { get; set; }
    public int? WardId { get; set; }
    public int? MoujaId { get; set; }
    public int? SubZoneId { get; set; }
    public string? AssetWardNo { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string? UpicId { get; set; }
    public string? PlotNo { get; set; }
    public string? CSN { get; set; }
    public decimal? LandRate { get; set; }
    public decimal? LengthFt { get; set; }
    public decimal? LengthMtr { get; set; }
    public decimal? WidthFt { get; set; }
    public decimal? WidthMtr { get; set; }
    public decimal? LandAreaSqFeet { get; set; }
    public decimal? LandAreaSqMeter { get; set; }
    public string? Address { get; set; }
    public string? NearestLandmark { get; set; }
    public string? PinCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? BoundaryGeoJson { get; set; }
    public string? InChargeName { get; set; }
    public int? InChargeDesignationId { get; set; }
    public string? InChargeDesignationName { get; set; }
    public string? InChargeMobile { get; set; }
    public string? InChargeEmail { get; set; }
    public string? InChargeRegionalName { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }

    // Navigation property names
    public string? ZoneName { get; set; }
    public string? WardName { get; set; }
    public string? MoujaName { get; set; }
    public string? SubZoneName { get; set; }
}

public class CreateAssetDetailsDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AMS_AssetDetails_AssetId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetDetails_AssetId_InvalidRange")]
    public int AssetId { get; set; }

    [Required(ErrorMessage = "AMS_AssetDetails_OrganizationId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetDetails_OrganizationId_InvalidRange")]
    public int OrganizationId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetDetails_ZoneId_InvalidRange")]
    public int? ZoneId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetDetails_WardId_InvalidRange")]
    public int? WardId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetDetails_MoujaId_InvalidRange")]
    public int? MoujaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetDetails_SubZoneId_InvalidRange")]
    public int? SubZoneId { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetDetails_AssetWardNo_MaxLengthExceeded_50")]
    public string? AssetWardNo { get; set; }

    [StringLength(100, ErrorMessage = "AMS_AssetDetails_PropertyNo_MaxLengthExceeded_100")]
    public string? PropertyNo { get; set; }

    [StringLength(100, ErrorMessage = "AMS_AssetDetails_PartitionNo_MaxLengthExceeded_100")]
    public string? PartitionNo { get; set; }

    [StringLength(100, ErrorMessage = "AMS_AssetDetails_UpicId_MaxLengthExceeded_100")]
    public string? UpicId { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetDetails_PlotNo_MaxLengthExceeded_50")]
    public string? PlotNo { get; set; }

    [StringLength(30, ErrorMessage = "AMS_AssetDetails_CSN_MaxLengthExceeded_30")]
    public string? CSN { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetDetails_LandRate_InvalidRange")]
    public decimal? LandRate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetDetails_LengthFt_InvalidRange")]
    public decimal? LengthFt { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetDetails_LengthMtr_InvalidRange")]
    public decimal? LengthMtr { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetDetails_WidthFt_InvalidRange")]
    public decimal? WidthFt { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetDetails_WidthMtr_InvalidRange")]
    public decimal? WidthMtr { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetDetails_LandAreaSqFeet_InvalidRange")]
    public decimal? LandAreaSqFeet { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetDetails_LandAreaSqMeter_InvalidRange")]
    public decimal? LandAreaSqMeter { get; set; }

    [StringLength(500, ErrorMessage = "AMS_AssetDetails_Address_MaxLengthExceeded_500")]
    public string? Address { get; set; }

    [StringLength(200, ErrorMessage = "AMS_AssetDetails_NearestLandmark_MaxLengthExceeded_200")]
    public string? NearestLandmark { get; set; }

    [StringLength(10, ErrorMessage = "AMS_AssetDetails_PinCode_MaxLengthExceeded_10")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "AMS_AssetDetails_PinCode_Invalid")]
    public string? PinCode { get; set; }

    [Range(-90, 90, ErrorMessage = "AMS_AssetDetails_Latitude_InvalidRange")]
    public decimal? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "AMS_AssetDetails_Longitude_InvalidRange")]
    public decimal? Longitude { get; set; }

    public string? BoundaryGeoJson { get; set; }

    [StringLength(200, ErrorMessage = "AMS_AssetDetails_InChargeName_MaxLengthExceeded_200")]
    public string? InChargeName { get; set; }

    [StringLength(150, ErrorMessage = "AMS_AssetDetails_InChargeRegionalName_MaxLengthExceeded_150")]
    public string? InChargeRegionalName { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetDetails_InChargeDesignationId_InvalidRange")]
    public int? InChargeDesignationId { get; set; }

    [StringLength(20, ErrorMessage = "AMS_AssetDetails_InChargeMobile_MaxLengthExceeded_20")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "AMS_AssetDetails_InChargeMobile_Invalid")]
    public string? InChargeMobile { get; set; }

    [StringLength(100, ErrorMessage = "AMS_AssetDetails_InChargeEmail_MaxLengthExceeded_100")]
    [EmailAddress(ErrorMessage = "AMS_AssetDetails_InChargeEmail_Invalid")]
    public string? InChargeEmail { get; set; }
}

public class UpdateAssetDetailsDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AMS_AssetDetails_OrganizationId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetDetails_OrganizationId_InvalidRange")]
    public int OrganizationId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetDetails_ZoneId_InvalidRange")]
    public int? ZoneId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetDetails_WardId_InvalidRange")]
    public int? WardId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetDetails_MoujaId_InvalidRange")]
    public int? MoujaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetDetails_SubZoneId_InvalidRange")]
    public int? SubZoneId { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetDetails_AssetWardNo_MaxLengthExceeded_50")]
    public string? AssetWardNo { get; set; }

    [StringLength(100, ErrorMessage = "AMS_AssetDetails_PropertyNo_MaxLengthExceeded_100")]
    public string? PropertyNo { get; set; }

    [StringLength(100, ErrorMessage = "AMS_AssetDetails_PartitionNo_MaxLengthExceeded_100")]
    public string? PartitionNo { get; set; }

    [StringLength(100, ErrorMessage = "AMS_AssetDetails_UpicId_MaxLengthExceeded_100")]
    public string? UpicId { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetDetails_PlotNo_MaxLengthExceeded_50")]
    public string? PlotNo { get; set; }

    [StringLength(30, ErrorMessage = "AMS_AssetDetails_CSN_MaxLengthExceeded_30")]
    public string? CSN { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetDetails_LandRate_InvalidRange")]
    public decimal? LandRate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetDetails_LengthFt_InvalidRange")]
    public decimal? LengthFt { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetDetails_LengthMtr_InvalidRange")]
    public decimal? LengthMtr { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetDetails_WidthFt_InvalidRange")]
    public decimal? WidthFt { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetDetails_WidthMtr_InvalidRange")]
    public decimal? WidthMtr { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetDetails_LandAreaSqFeet_InvalidRange")]
    public decimal? LandAreaSqFeet { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetDetails_LandAreaSqMeter_InvalidRange")]
    public decimal? LandAreaSqMeter { get; set; }

    [StringLength(500, ErrorMessage = "AMS_AssetDetails_Address_MaxLengthExceeded_500")]
    public string? Address { get; set; }

    [StringLength(200, ErrorMessage = "AMS_AssetDetails_NearestLandmark_MaxLengthExceeded_200")]
    public string? NearestLandmark { get; set; }

    [StringLength(10, ErrorMessage = "AMS_AssetDetails_PinCode_MaxLengthExceeded_10")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "AMS_AssetDetails_PinCode_Invalid")]
    public string? PinCode { get; set; }

    [Range(-90, 90, ErrorMessage = "AMS_AssetDetails_Latitude_InvalidRange")]
    public decimal? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "AMS_AssetDetails_Longitude_InvalidRange")]
    public decimal? Longitude { get; set; }

    public string? BoundaryGeoJson { get; set; }

    [StringLength(200, ErrorMessage = "AMS_AssetDetails_InChargeName_MaxLengthExceeded_200")]
    public string? InChargeName { get; set; }

    [StringLength(150, ErrorMessage = "AMS_AssetDetails_InChargeRegionalName_MaxLengthExceeded_150")]
    public string? InChargeRegionalName { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetDetails_InChargeDesignationId_InvalidRange")]
    public int? InChargeDesignationId { get; set; }

    [StringLength(20, ErrorMessage = "AMS_AssetDetails_InChargeMobile_MaxLengthExceeded_20")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "AMS_AssetDetails_InChargeMobile_Invalid")]
    public string? InChargeMobile { get; set; }

    [StringLength(100, ErrorMessage = "AMS_AssetDetails_InChargeEmail_MaxLengthExceeded_100")]
    [EmailAddress(ErrorMessage = "AMS_AssetDetails_InChargeEmail_Invalid")]
    public string? InChargeEmail { get; set; }
}
