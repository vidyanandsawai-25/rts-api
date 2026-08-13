using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;

/// <summary>
/// DTO for bulk generating child assets (rooms/shops) across multiple floors in a single backend call.
/// The frontend sends floor IDs + units-per-floor + construction/use metadata.
/// The backend creates one AssetMaster row and one SubUnitsDetails row per (child, floor) pair.
/// </summary>
public class BulkGenerateAcrossFloorsDto
{
    /// <summary>
    /// Parent asset ID (e.g., Building ID)
    /// </summary>
    [Required(ErrorMessage = "AMS_BulkGenerateAcrossFloors_ParentAssetId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_BulkGenerateAcrossFloors_ParentAssetId_InvalidRange")]
    public int ParentAssetId { get; set; }

    /// <summary>
    /// Type of sub-unit (e.g., "Flat", "Shop", "Room")
    /// </summary>
    [Required(ErrorMessage = "AMS_BulkGenerateAcrossFloors_Type_Required")]
    [StringLength(50, ErrorMessage = "AMS_BulkGenerateAcrossFloors_Type_MaxLengthExceeded_50")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Floor IDs to generate units on (from CORE.FloorMaster).
    /// One unit per floor per UnitsPerFloor count.
    /// </summary>
    [Required(ErrorMessage = "AMS_BulkGenerateAcrossFloors_FloorIds_Required")]
    [MinLength(1, ErrorMessage = "AMS_BulkGenerateAcrossFloors_FloorIds_Required")]
    public List<int> FloorIds { get; set; } = new();

    /// <summary>
    /// Number of units to generate per floor (e.g., 5 → 5 shops on each selected floor).
    /// </summary>
    [Required(ErrorMessage = "AMS_BulkGenerateAcrossFloors_UnitsPerFloor_Required")]
    [Range(1, 100, ErrorMessage = "AMS_BulkGenerateAcrossFloors_UnitsPerFloor_InvalidRange")]
    public int UnitsPerFloor { get; set; }

    /// <summary>
    /// Construction year (4-digit string, e.g., "2022").
    /// </summary>
    [StringLength(4, ErrorMessage = "AMS_BulkGenerateAcrossFloors_ConstructionYear_MaxLengthExceeded_4")]
    public string? ConstructionYear { get; set; }

    /// <summary>
    /// FK to CORE.ConstructionTypeMaster.
    /// </summary>
    public int ConstructionTypeId { get; set; }

    /// <summary>
    /// FK to AMS.AssetTypeOfUseMaster.
    /// </summary>
    public int TypeOfUseId { get; set; }

    /// <summary>
    /// FK to AMS.AssetSubTypeOfUseMaster (nullable).
    /// </summary>
    public int? SubTypeOfUseId { get; set; }

    /// <summary>
    /// User ID who is creating the assets.
    /// </summary>
    public int? CreatedBy { get; set; }
}

/// <summary>
/// Response DTO for bulk-generate-across-floors operation.
/// </summary>
public class BulkGenerateAcrossFloorsResponseDto
{
    public int TotalGenerated { get; set; }
    public List<GeneratedAssetDto> GeneratedAssets { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
