using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;

/// <summary>
/// DTO for creating a single child asset (room/shop) under a parent asset with complete details
/// </summary>
public class CreateChildAssetDto
{
    [Required(ErrorMessage = "AMS_ChildAsset_ParentAssetId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_ChildAsset_ParentAssetId_InvalidRange")]
    public int ParentAssetId { get; set; }

    // Basic Information Section
    public int AssetId { get; set; }

    /// <summary>SubUnitsDetails PK — used to link room-wise submissions</summary>
    public int? FloorDetailsId { get; set; }

    // ── SubUnitsDetails fields (persisted to AMS.SubUnitsDetails) ────────────
    /// <summary>FloorMaster FK for the floor this unit is on</summary>
    public int? FloorId { get; set; }

    /// <summary>SubFloorMaster FK (nullable)</summary>
    public int? SubFloorId { get; set; }

    /// <summary>ConstructionTypeMaster FK</summary>
    public int? ConstructionTypeId { get; set; }

    /// <summary>AssetTypeOfUseMaster FK</summary>
    public int? TypeOfUseId { get; set; }

    /// <summary>AssetSubTypeOfUseMaster FK (nullable)</summary>
    public int? SubTypeOfUseId { get; set; }

    /// <summary>Carpet area in sq.metres</summary>
    public decimal? CarpetAreaSqMeter { get; set; }

    /// <summary>Carpet area in sq.feet</summary>
    public decimal? CarpetAreaSqFeet { get; set; }

    /// <summary>Built-up area in sq.metres (= carpet × 1.2)</summary>
    public decimal? BuiltupAreaSqMeter { get; set; }

    /// <summary>Built-up area in sq.feet</summary>
    public decimal? BuiltupAreaSqFeet { get; set; }

    /// <summary>Owning Department ID</summary>
    public int? DepartmentId { get; set; }



    [StringLength(200, ErrorMessage = "AMS_ChildAsset_ComplexName_MaxLengthExceeded_200")]
    public string? ComplexName { get; set; }

    [StringLength(200, ErrorMessage = "AMS_ChildAsset_RenterName_MaxLengthExceeded_200")]
    public string? RenterName { get; set; }

    [StringLength(200, ErrorMessage = "AMS_ChildAsset_PropertyDescription_MaxLengthExceeded_200")]
    public string? PropertyDescription { get; set; }

    [StringLength(100, ErrorMessage = "AMS_ChildAsset_ShopUnitName_MaxLengthExceeded_100")]
    public string? ShopUnitName { get; set; }

    public int? ZoneNo { get; set; }

    [StringLength(50, ErrorMessage = "AMS_ChildAsset_UnitNo_MaxLengthExceeded_50")]
    public string? UnitNo { get; set; }

    public int? WardNo { get; set; }

    [StringLength(50, ErrorMessage = "AMS_ChildAsset_PropertyNo_MaxLengthExceeded_50")]
    public string? PropertyNo { get; set; }

    [StringLength(100, ErrorMessage = "AMS_ChildAsset_PartitionNo_MaxLengthExceeded_100")]
    public string? PartitionNo { get; set; }

    [StringLength(100, ErrorMessage = "AMS_ChildAsset_UpicId_MaxLengthExceeded_100")]
    public string? UpicId { get; set; }

    [StringLength(50, ErrorMessage = "AMS_ChildAsset_AssetWardNo_MaxLengthExceeded_50")]
    public string? AssetWardNo { get; set; }

    [StringLength(15, ErrorMessage = "AMS_ChildAsset_MobileNo_MaxLengthExceeded_15")]
    public string? MobileNo { get; set; }

    [StringLength(100, ErrorMessage = "AMS_ChildAsset_SurveyNo_MaxLengthExceeded_100")]
    public string? SurveyNo { get; set; }

    [StringLength(100, ErrorMessage = "AMS_ChildAsset_EmailId_MaxLengthExceeded_100")]
    [EmailAddress(ErrorMessage = "AMS_ChildAsset_EmailId_Invalid")]
    public string? EmailId { get; set; }

    [StringLength(50, ErrorMessage = "AMS_ChildAsset_GSTNo_MaxLengthExceeded_50")]
    public string? GSTNo { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_ChildAsset_TotalAreaSqFt_InvalidRange")]
    public decimal? TotalAreaSqFt { get; set; }

    [StringLength(50, ErrorMessage = "AMS_ChildAsset_ShopActNo_MaxLengthExceeded_50")]
    public string? ShopActNo { get; set; }

    [StringLength(20, ErrorMessage = "AMS_ChildAsset_AadhaarCardNo_MaxLengthExceeded_20")]
    public string? AadhaarCardNo { get; set; }

    [StringLength(20, ErrorMessage = "AMS_ChildAsset_PanCardNo_MaxLengthExceeded_20")]
    public string? PanCardNo { get; set; }

    public int? CreatedBy { get; set; }

    /// <summary>Year this unit was actually constructed — drives the CV age factor. Distinct from AssessmentYear.</summary>
    public string? ConstructionYear { get; set; }

    public string? AssessmentYear { get; set; }

    // Rent Information Section
    public RentInformationDto? RentInformation { get; set; }

    // Floor QC - Existing Floor Configuration
    public FloorConfigurationDto? FloorConfiguration { get; set; }

    // Room-wise Configuration & Valuation
    public bool IsRoomWiseValuationActive { get; set; }
    public List<RoomDetailDto>? RoomDetails { get; set; }

    // Optional photos/documents uploaded alongside sub-unit creation
    public List<IFormFile>? PhotoFiles { get; set; }

    // JSON metadata string (same order as PhotoFiles)
    // Example: [{"photoTypeId":1,"displayOrder":1,"remarks":"Shop front photo"}]
    public string? PhotoMetadataJson { get; set; }
}

/// <summary>
/// DTO for rent information
/// </summary>
public class RentInformationDto
{
    [StringLength(100, ErrorMessage = "AMS_RentInformation_LeaseRentType_MaxLengthExceeded_100")]
    public string? LeaseRentType { get; set; }

    public DateTime? LeaseStart { get; set; }

    public DateTime? LeaseEnd { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "AMS_RentInformation_Duration_InvalidRange")]
    public int? Duration { get; set; }

    [StringLength(100, ErrorMessage = "AMS_RentInformation_RentFrequency_MaxLengthExceeded_100")]
    public string? RentFrequency { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_RentInformation_RentAmount_InvalidRange")]
    public decimal? RentAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_RentInformation_SecurityDeposit_InvalidRange")]
    public decimal? SecurityDeposit { get; set; }

    [StringLength(50, ErrorMessage = "AMS_RentInformation_DepositType_MaxLengthExceeded_50")]
    public string? DepositType { get; set; }
}

/// <summary>
/// DTO for floor configuration
/// </summary>
public class FloorConfigurationDto
{
    [Range(0, double.MaxValue, ErrorMessage = "AMS_FloorConfiguration_UnitAreaSqFt_InvalidRange")]
    public decimal? UnitAreaSqFt { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_FloorConfiguration_CalculatedCapitalValue_InvalidRange")]
    public decimal? CalculatedCapitalValue { get; set; }
}

public class RoomOffsetDto
{
    public int Id { get; set; }
    public string? Shape { get; set; }
    public double? Length { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
    public double? Base1 { get; set; }
    public double? Base2 { get; set; }
    public double? Radius { get; set; }
    public double? AreaSqM { get; set; }
    public string? Op { get; set; }
}

/// <summary>
/// DTO for individual room details
/// </summary>
public class RoomDetailDto
{
    public double? LengthMtr { get; set; }
    public double? WidthMtr { get; set; }
    public double? HeightMtr { get; set; }
    public double? AreaSqMtr { get; set; }
    public double? Base1Mtr { get; set; }
    public double? Base2Mtr { get; set; }
    public int? NoOfRooms { get; set; }
    public double? TotalAreaSqMtr { get; set; }

    [StringLength(50, ErrorMessage = "AMS_RoomDetail_RoomNo_MaxLengthExceeded_50")]
    public string? RoomNo { get; set; }

    [StringLength(100, ErrorMessage = "AMS_RoomDetail_RoomType_MaxLengthExceeded_100")]
    public string? RoomType { get; set; }

    [StringLength(50, ErrorMessage = "AMS_RoomDetail_Shape_MaxLengthExceeded_50")]
    public string? Shape { get; set; }

    [StringLength(50, ErrorMessage = "AMS_RoomDetail_SubmissionType_MaxLengthExceeded_50")]
    public string? SubmissionType { get; set; }

    public bool OuterYesNo { get; set; }
    public bool MinusYesNo { get; set; }

    public List<RoomOffsetDto>? Offsets { get; set; }
}

/// <summary>
/// Response DTO for creating a child asset
/// </summary>
public class CreateChildAssetResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? AssetId { get; set; }
    public string? AssetNo { get; set; }
    public int? RoomWiseSubmissionDetailsId { get; set; }
    public int? RenterDetailsId { get; set; }

    /// <summary>
    /// This unit's own AMS.SubUnitsDetails row Id — pass to
    /// POST /api/AssetFloorDetails/{id}/calculate-capital-value to calculate and persist
    /// this specific unit's Capital Value as a separate step.
    /// </summary>
    public int? SubUnitsDetailsId { get; set; }

    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Response DTO for getting child asset details by AssetId.
/// Returns only room-wise submission and renter details.
/// Note: Asset master and floor details should be retrieved using their respective GET APIs.
/// </summary>
public class GetChildAssetResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    // Asset Identification (minimal info needed)
    public int AssetId { get; set; }

    // Related Details
    public RenterDetailsDto? RenterDetails { get; set; }
    public List<RoomWiseDetailsDto>? RoomWiseDetails { get; set; }
}

/// <summary>
/// DTO for renter details in response
/// </summary>
public class RenterDetailsDto
{
    public int Id { get; set; }
    public int FloorDetailsId { get; set; }
    public int? RoomWiseSubmissionDetailsId { get; set; }
    public int AssetId { get; set; }

    // Basic Information
    public string? RenterName { get; set; }
    public string? GSTNo { get; set; }
    public decimal? TotalAreaSqFt { get; set; }
    public string? AadhaarCardNo { get; set; }
    public string? PANCardNo { get; set; }
    public string? MobileNo { get; set; }
    public string? EmailId { get; set; }

    // Rent/Lease Information
    public string? LeaseRentType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? Duration { get; set; }
    public string? RentFrequency { get; set; }
    public decimal? RentAmount { get; set; }
    public decimal? SecurityDeposit { get; set; }
    public string? DepositType { get; set; }

    // Legacy/Additional Fields
    public string? AgreementId { get; set; }
    public string? IncrementFrequency { get; set; }
    public string? IncrementType { get; set; }
    public double? IncrementValue { get; set; }
    public string? IncrementMethod { get; set; }
}

/// <summary>
/// DTO for room-wise details in response
/// </summary>
public class RoomWiseDetailsDto
{
    public int Id { get; set; }
    public int? AssetId { get; set; }
    public int? FloorDetailsId { get; set; }
    public string? RoomNo { get; set; }
    public string? RoomType { get; set; }
    public string? Shape { get; set; }
    public double? LengthMtr { get; set; }
    public double? WidthMtr { get; set; }
    public double? HeightMtr { get; set; }
    public double? AreaSqMtr { get; set; }
    public double? TotalAreaSqMtr { get; set; }
    public bool OuterYesNo { get; set; }
    public bool MinusYesNo { get; set; }
}

/// <summary>
/// DTO for retrieving basic subunit details linked to their floor
/// </summary>
public class SubUnitResponseDto
{
    public int Id { get; set; }
    public int ParentAssetId { get; set; }
    public int AssetId { get; set; }
    public string? ComplexName { get; set; }
    public string? ShopUnitName { get; set; }
    public string? UnitNo { get; set; }
    public decimal? TotalAreaSqFt { get; set; }
    public decimal? CalculatedCapitalValue { get; set; }
    public DateTime? CreatedDate { get; set; }
    public int? FloorDetailsId { get; set; }
    /// <summary>Unit type derived from RoomWiseSubmissionDetails.RoomType (Flat, Shop, Office, etc.)</summary>
    public string? UnitType { get; set; }
}

