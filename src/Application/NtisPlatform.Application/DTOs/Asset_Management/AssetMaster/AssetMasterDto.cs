using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using NtisPlatform.Application.DTOs.Asset_Management.SubUnitsDetails;
using NtisPlatform.Application.DTOs.Asset_Management.AssetRoomWiseSubmissionDetails;
using NtisPlatform.Application.DTOs.Asset_Management.AssetLeaseRentDetails;
using NtisPlatform.Application.DTOs.Asset_Management.AssetDetails;
using NtisPlatform.Application.DTOs.Asset_Management.AssetFieldValue;
using System.Text.Json.Serialization;
using NtisPlatform.Application.DTOs.Asset_Management.AssetDocument;

namespace NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;

public class AssetMasterDto : BaseDtos
{
    // Identification / Category (AMS.AssetMaster)
    public string? AssetNo { get; set; }
    public string? AssetName { get; set; }
    public string? AssetRegionalName { get; set; }
    public int? AssetCategoryId { get; set; }
    public int? AssetTypeId { get; set; }
    public int? ParentAssetId { get; set; }
    public int? DepartmentId { get; set; }

    // Hierarchy
    public int? HierarchyLevel { get; set; }
    public string? HierarchyPath { get; set; }

    // Legal / Acquisition
    public string? OwnershipType { get; set; }
    public string? OccupancyStatus { get; set; }

    // FK into AMS.AssetConditionMaster — needed so edit forms can preselect the right dropdown option.
    public int? AssetConditionId { get; set; }

    // Computed aggregates (not columns; derived from the asset's own children/documents).
    public int TotalUnits { get; set; }
    public int TotalSubUnits { get; set; }
    public int TotalFloors { get; set; }
    public int? AssetDocumentId { get; set; }

    // Field Values for dynamic fields
    public List<AssetFieldValueDto>? FieldValues { get; set; }

    // Associated Photos
    public List<AssetPhotoDto> Photos { get; set; } = new();

    // Associated Documents (AMS.AssetDocument)
    public List<AssetDocumentDto> Documents { get; set; } = new();

    public AssetDetailsDto Details { get; set; } = new();
    [JsonIgnore]
    public AssetMasterNamesDto Names { get; set; } = new();

    // Flat name-resolution properties (resolved from master-table JOINs, not directly on any single table)
    public string? AssetCategoryName { get; set; }
    public string? AssetTypeName { get; set; }
    public string? DepartmentName { get; set; }
    public string? WardName { get; set; }
    public string? WardNo { get; set; }
    public string? ZoneName { get; set; }
    public string? ZoneNo { get; set; }
    public string? MoujaName { get; set; }
    public string? SubZoneName { get; set; }
    public string? SubZoneNo { get; set; }
    public string? AssetCondition { get; set; }

    // Flat mirror of Details.Address — Details is nested and some consumers (e.g. the lease/rent
    // registration drawers) read the flat shape only.
    public string? Address { get; set; }

    // Computed values (not columns; derived from child records)
    public decimal? CapitalValue { get; set; }
    public int? AssetLife { get; set; }
}



public class CreateAssetMasterDto : CreateBaseDtos
{
    // Jurisdiction / Ownership Context
    [Required(ErrorMessage = "AMS_AssetMaster_OrganizationId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetMaster_OrganizationId_InvalidRange")]
    public int OrganizationId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetMaster_DepartmentId_InvalidRange")]
    public int? DepartmentId { get; set; }

    // Identification / Category
    [Required(ErrorMessage = "AMS_AssetMaster_AssetName_Required")]
    [StringLength(200, ErrorMessage = "AMS_AssetMaster_AssetName_MaxLengthExceeded_200")]
    public string AssetName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "AMS_AssetMaster_AssetRegionalName_MaxLengthExceeded_200")]
    public string? AssetRegionalName { get; set; }

    [Required(ErrorMessage = "AMS_AssetMaster_AssetCategoryId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetMaster_AssetCategoryId_InvalidRange")]
    public int AssetCategoryId { get; set; }

    [Required(ErrorMessage = "AMS_AssetMaster_AssetTypeId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetMaster_AssetTypeId_InvalidRange")]
    public int AssetTypeId { get; set; }

    public int? ParentAssetId { get; set; }

    // Hierarchy
    [Range(0, int.MaxValue, ErrorMessage = "AMS_AssetMaster_HierarchyLevel_InvalidRange")]
    public int HierarchyLevel { get; set; }

    [StringLength(500, ErrorMessage = "AMS_AssetMaster_HierarchyPath_MaxLengthExceeded_500")]
    public string? HierarchyPath { get; set; }

    // Location (AMS.AssetDetails)
    [StringLength(500, ErrorMessage = "AMS_AssetMaster_Address_MaxLengthExceeded_500")]
    public string? Address { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetMaster_WardId_InvalidRange")]
    public int? WardId { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetMaster_AssetWardNo_MaxLengthExceeded_50")]
    public string? AssetWardNo { get; set; }

    [StringLength(100, ErrorMessage = "AMS_AssetMaster_PropertyNo_MaxLengthExceeded_100")]
    public string? PropertyNo { get; set; }

    [StringLength(100, ErrorMessage = "AMS_AssetMaster_PartitionNo_MaxLengthExceeded_100")]
    public string? PartitionNo { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetMaster_PlotNo_MaxLengthExceeded_50")]
    public string? PlotNo { get; set; }

    [StringLength(100, ErrorMessage = "AMS_AssetMaster_UpicId_MaxLengthExceeded_100")]
    public string? UpicId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetMaster_ZoneId_InvalidRange")]
    public int? ZoneId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetMaster_SubZoneId_InvalidRange")]
    public int? SubZoneId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetMaster_MoujaId_InvalidRange")]
    public int? MoujaId { get; set; }

    [Range(-90, 90, ErrorMessage = "AMS_AssetMaster_Latitude_InvalidRange")]
    public decimal? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "AMS_AssetMaster_Longitude_InvalidRange")]
    public decimal? Longitude { get; set; }

    [StringLength(30, ErrorMessage = "AMS_AssetMaster_CSN_MaxLengthExceeded_30")]
    public string? CSN { get; set; }

    // Area Details
    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetMaster_LandAreaSqMeter_InvalidRange")]
    public decimal? LandAreaSqMeter { get; set; }

    // Legal / Acquisition
    [StringLength(50, ErrorMessage = "AMS_AssetMaster_OwnershipType_MaxLengthExceeded_50")]
    public string? OwnershipType { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetMaster_OccupancyStatus_MaxLengthExceeded_50")]
    public string? OccupancyStatus { get; set; }

    // FK into AMS.AssetConditionMaster — the id the "Asset Condition" dropdown must send.
    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetMaster_AssetConditionId_InvalidRange")]
    public int? AssetConditionId { get; set; }

    // Auxiliary Details (AMS.AssetDetails)
    [StringLength(200, ErrorMessage = "AMS_AssetMaster_InChargeName_MaxLengthExceeded_200")]
    public string? InChargeName { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetMaster_InChargeDesignationId_InvalidRange")]
    public int? InChargeDesignationId { get; set; }

    [StringLength(20, ErrorMessage = "AMS_AssetMaster_InChargeMobile_MaxLengthExceeded_20")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "AMS_AssetMaster_InChargeMobile_Invalid")]
    public string? InChargeMobile { get; set; }

    [StringLength(100, ErrorMessage = "AMS_AssetMaster_InChargeEmail_MaxLengthExceeded_100")]
    [EmailAddress(ErrorMessage = "AMS_AssetMaster_InChargeEmail_Invalid")]
    public string? InChargeEmail { get; set; }

    [StringLength(150, ErrorMessage = "AMS_AssetMaster_InChargeRegionalName_MaxLengthExceeded_150")]
    public string? InChargeRegionalName { get; set; }

    /// <summary>The Basic Info form's "Landmark" field is bound to this and written to AssetDetails.NearestLandmark.</summary>
    [StringLength(255, ErrorMessage = "AMS_AssetMaster_Locality_MaxLengthExceeded_255")]
    public string? Locality { get; set; }

    [StringLength(10, ErrorMessage = "AMS_AssetMaster_PinCode_MaxLengthExceeded_10")]
    public string? PinCode { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetMaster_LandRate_InvalidRange")]
    public decimal? LandRate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetMaster_TotalLength_InvalidRange")]
    public decimal? TotalLength { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetMaster_AverageWidth_InvalidRange")]
    public decimal? AverageWidth { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetMaster_LengthFt_InvalidRange")]
    public decimal? LengthFt { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetMaster_WidthFt_InvalidRange")]
    public decimal? WidthFt { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetMaster_LandAreaSqFeet_InvalidRange")]
    public decimal? LandAreaSqFeet { get; set; }

    // JSON string for dynamic field values (supports both single object and array format)
    public string? FieldValuesJson { get; set; }

    // Optional photos/documents uploaded alongside asset creation.
    public List<IFormFile>? PhotoFiles { get; set; }

    // JSON metadata string (same order as PhotoFiles)
    // Example:
    // [{"photoTypeId":1,"displayOrder":1,"remarks":"Front view"},{"photoTypeId":2,"displayOrder":2,"remarks":"Electricity bill"}]
    public string? PhotoMetadataJson { get; set; }
}

public class UpdateAssetMasterDto : UpdateBaseDtos
{
    // Jurisdiction / Ownership Context
    [Required(ErrorMessage = "AMS_AssetMaster_OrganizationId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetMaster_OrganizationId_InvalidRange")]
    public int OrganizationId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetMaster_DepartmentId_InvalidRange")]
    public int? DepartmentId { get; set; }

    // Identification / Category
    [StringLength(50, ErrorMessage = "AMS_AssetMaster_AssetNo_MaxLengthExceeded_50")]
    public string? AssetNo { get; set; }

    [Required(ErrorMessage = "AMS_AssetMaster_AssetName_Required")]
    [StringLength(200, ErrorMessage = "AMS_AssetMaster_AssetName_MaxLengthExceeded_200")]
    public string AssetName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "AMS_AssetMaster_AssetRegionalName_MaxLengthExceeded_200")]
    public string? AssetRegionalName { get; set; }

    [Required(ErrorMessage = "AMS_AssetMaster_AssetCategoryId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetMaster_AssetCategoryId_InvalidRange")]
    public int AssetCategoryId { get; set; }

    [Required(ErrorMessage = "AMS_AssetMaster_AssetTypeId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetMaster_AssetTypeId_InvalidRange")]
    public int AssetTypeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetMaster_ParentAssetId_InvalidRange")]
    public int? ParentAssetId { get; set; }

    // Hierarchy
    [Range(0, int.MaxValue, ErrorMessage = "AMS_AssetMaster_HierarchyLevel_InvalidRange")]
    public int HierarchyLevel { get; set; }

    [StringLength(500, ErrorMessage = "AMS_AssetMaster_HierarchyPath_MaxLengthExceeded_500")]
    public string? HierarchyPath { get; set; }

    // Location (AMS.AssetDetails)
    [StringLength(500, ErrorMessage = "AMS_AssetMaster_Address_MaxLengthExceeded_500")]
    public string? Address { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetMaster_WardId_InvalidRange")]
    public int? WardId { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetMaster_AssetWardNo_MaxLengthExceeded_50")]
    public string? AssetWardNo { get; set; }

    [StringLength(100, ErrorMessage = "AMS_AssetMaster_PropertyNo_MaxLengthExceeded_100")]
    public string? PropertyNo { get; set; }

    [StringLength(100, ErrorMessage = "AMS_AssetMaster_PartitionNo_MaxLengthExceeded_100")]
    public string? PartitionNo { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetMaster_PlotNo_MaxLengthExceeded_50")]
    public string? PlotNo { get; set; }

    [StringLength(100, ErrorMessage = "AMS_AssetMaster_UpicId_MaxLengthExceeded_100")]
    public string? UpicId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetMaster_ZoneId_InvalidRange")]
    public int? ZoneId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetMaster_SubZoneId_InvalidRange")]
    public int? SubZoneId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetMaster_MoujaId_InvalidRange")]
    public int? MoujaId { get; set; }

    [Range(-90, 90, ErrorMessage = "AMS_AssetMaster_Latitude_InvalidRange")]
    public decimal? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "AMS_AssetMaster_Longitude_InvalidRange")]
    public decimal? Longitude { get; set; }

    [StringLength(30, ErrorMessage = "AMS_AssetMaster_CSN_MaxLengthExceeded_30")]
    public string? CSN { get; set; }

    // Area Details
    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetMaster_LandAreaSqMeter_InvalidRange")]
    public decimal? LandAreaSqMeter { get; set; }

    // Legal / Acquisition
    [StringLength(50, ErrorMessage = "AMS_AssetMaster_OwnershipType_MaxLengthExceeded_50")]
    public string? OwnershipType { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetMaster_OccupancyStatus_MaxLengthExceeded_50")]
    public string? OccupancyStatus { get; set; }

    // FK into AMS.AssetConditionMaster — the id the "Asset Condition" dropdown must send.
    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetMaster_AssetConditionId_InvalidRange")]
    public int? AssetConditionId { get; set; }

    // Auxiliary Details (AMS.AssetDetails)
    [StringLength(200, ErrorMessage = "AMS_AssetMaster_InChargeName_MaxLengthExceeded_200")]
    public string? InChargeName { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetMaster_InChargeDesignationId_InvalidRange")]
    public int? InChargeDesignationId { get; set; }

    [StringLength(20, ErrorMessage = "AMS_AssetMaster_InChargeMobile_MaxLengthExceeded_20")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "AMS_AssetMaster_InChargeMobile_Invalid")]
    public string? InChargeMobile { get; set; }

    [StringLength(100, ErrorMessage = "AMS_AssetMaster_InChargeEmail_MaxLengthExceeded_100")]
    [EmailAddress(ErrorMessage = "AMS_AssetMaster_InChargeEmail_Invalid")]
    public string? InChargeEmail { get; set; }

    [StringLength(150, ErrorMessage = "AMS_AssetMaster_InChargeRegionalName_MaxLengthExceeded_150")]
    public string? InChargeRegionalName { get; set; }

    /// <summary>The Basic Info form's "Landmark" field is bound to this and written to AssetDetails.NearestLandmark.</summary>
    [StringLength(255, ErrorMessage = "AMS_AssetMaster_Locality_MaxLengthExceeded_255")]
    public string? Locality { get; set; }

    [StringLength(10, ErrorMessage = "AMS_AssetMaster_PinCode_MaxLengthExceeded_10")]
    public string? PinCode { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetMaster_LandRate_InvalidRange")]
    public decimal? LandRate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetMaster_TotalLength_InvalidRange")]
    public decimal? TotalLength { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetMaster_AverageWidth_InvalidRange")]
    public decimal? AverageWidth { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetMaster_LengthFt_InvalidRange")]
    public decimal? LengthFt { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetMaster_WidthFt_InvalidRange")]
    public decimal? WidthFt { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetMaster_LandAreaSqFeet_InvalidRange")]
    public decimal? LandAreaSqFeet { get; set; }

    // Field Values for dynamic fields
    public List<UpdateAssetFieldValueDto> FieldValues { get; set; } = new();

    // Support photo upload during update
    public List<IFormFile>? PhotoFiles { get; set; }

    // JSON metadata string (same order as PhotoFiles)
    public string? PhotoMetadataJson { get; set; }
}

/// <summary>
/// Response DTO for grouped sub-assets with parent asset details
/// </summary>
public class SubAssetGroupedResponseDto
{
    /// <summary>
    /// Parent asset details
    /// </summary>
    public ParentAssetDetailDto? ParentAsset { get; set; }

    /// <summary>
    /// Total count of sub-assets
    /// </summary>
    public int TotalSubAssets { get; set; }

    /// <summary>
    /// List of sub-assets with their related data
    /// </summary>
    public List<SubAssetDetailDto> SubAssets { get; set; } = new();
}

/// <summary>
/// DTO for parent asset details
/// </summary>
public class ParentAssetDetailDto
{
    // Base entity fields
    public int Id { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    // Identification / Category
    public string AssetNo { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public int? AssetCategoryId { get; set; }
    public int? AssetTypeId { get; set; }
    public int? ParentAssetId { get; set; }
    public int? DepartmentId { get; set; }

    // Hierarchy
    public int? HierarchyLevel { get; set; }
    public string? HierarchyPath { get; set; }

    // Legal / Acquisition
    public string? OwnershipType { get; set; }
    public string? OccupancyStatus { get; set; }
    // Field Values for dynamic fields
    public List<AssetFieldValueDto> FieldValues { get; set; } = new();

    public AssetDetailsDto Details { get; set; } = new();
    [JsonIgnore]
    public AssetMasterNamesDto Names { get; set; } = new();
}

/// <summary>
/// DTO for sub-asset details with related data
/// </summary>
public class SubAssetDetailDto
{
    // Base entity fields
    public int Id { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    // Identification / Category
    public string AssetNo { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public int? AssetCategoryId { get; set; }
    public int? AssetTypeId { get; set; }
    public int? ParentAssetId { get; set; }
    public int? DepartmentId { get; set; }

    // Hierarchy
    public int? HierarchyLevel { get; set; }
    public string? HierarchyPath { get; set; }

    // Legal / Acquisition
    public string? OwnershipType { get; set; }
    public string? OccupancyStatus { get; set; }
    // Field Values for dynamic fields
    public List<AssetFieldValueDto> FieldValues { get; set; } = new();

    public AssetDetailsDto Details { get; set; } = new();
    [JsonIgnore]
    public AssetMasterNamesDto Names { get; set; } = new();

    // Resolved from this sub-unit's matched floor detail (AMS.SubUnitsDetails), not from the
    // sub-asset's own row.
    public string? TypeOfUseName { get; set; }
    public string? SubTypeOfUseName { get; set; }

    // Related data collections
    public List<SubUnitsDetailsDto> FloorDetails { get; set; } = new();
    public List<AssetRoomWiseSubmissionDetailsDto> RoomWiseSubmissions { get; set; } = new();
    public List<AssetLeaseRentDetailsDto> RenterDetails { get; set; } = new();
}


/// <summary>
/// Display names resolved by joining an asset's FK ids against their master tables. Not backed by
/// columns on AssetMaster or AssetDetails themselves.
/// </summary>
public class AssetMasterNamesDto
{
    public string? OrganizationName { get; set; }
    public string? DepartmentName { get; set; }
    public string? AssetCategoryName { get; set; }
    public string? AssetTypeName { get; set; }
    public string? ParentAssetName { get; set; }
    public string? ZoneName { get; set; }
    public string? WardName { get; set; }
    public string? MoujaName { get; set; }
    // Zone / Ward short codes (the "No" values) kept alongside the descriptive names above.
    public string? ZoneNo { get; set; }
    public string? WardNo { get; set; }
    public string? SubZoneNo { get; set; }
    public string? AssetCondition { get; set; }
}


