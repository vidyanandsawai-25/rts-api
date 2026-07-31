using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;

/// <summary>
/// Simplified DTO for bulk generating child assets (rooms/flats/shops) under a parent asset.
/// Gets basic info from parent asset and links to floor details.
/// </summary>
public class BulkGenerateChildAssetsDto
{
    /// <summary>
    /// Parent asset ID (e.g., Building ID)
    /// </summary>
    [Required(ErrorMessage = "AMS_BulkGenerateChildAssets_ParentAssetId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_BulkGenerateChildAssets_ParentAssetId_InvalidRange")]
    public int ParentAssetId { get; set; }

    /// <summary>
    /// Floor details ID — optional at generate time. Floor can be assigned later when user configures the unit.
    /// </summary>
    public int? FloorDetailsId { get; set; }

    /// <summary>
    /// Type of sub-unit (e.g., "Flat", "Shop", "Room")
    /// </summary>
    [Required(ErrorMessage = "AMS_BulkGenerateChildAssets_Type_Required")]
    [StringLength(50, ErrorMessage = "AMS_BulkGenerateChildAssets_Type_MaxLengthExceeded_50")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Asset number prefix — optional, auto-derived from Type if not supplied.
    /// </summary>
    [StringLength(20, ErrorMessage = "AMS_BulkGenerateChildAssets_Prefix_MaxLengthExceeded_20")]
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Starting number — optional, defaults to 1.
    /// </summary>
    public int StartNumber { get; set; } = 1;

    /// <summary>
    /// Number of assets to generate (e.g., 4)
    /// </summary>
    [Required(ErrorMessage = "AMS_BulkGenerateChildAssets_Count_Required")]
    [Range(1, 500, ErrorMessage = "AMS_BulkGenerateChildAssets_Count_InvalidRange")]
    public int Count { get; set; }

    /// <summary>
    /// Area in square feet — starts at 0, set when rooms are configured.
    /// </summary>
    public decimal AreaSqFt { get; set; } = 0;

    /// <summary>
    /// User ID who is creating the assets
    /// </summary>
    public int? CreatedBy { get; set; }
}

/// <summary>
/// Response DTO for bulk generate operation
/// </summary>
public class BulkGenerateChildAssetsResponseDto
{
    public int TotalGenerated { get; set; }
    public List<GeneratedAssetDto> GeneratedAssets { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// DTO for individual generated asset information
/// </summary>
public class GeneratedAssetDto
{
    public int AssetId { get; set; }
    public string AssetNo { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public int? RoomWiseSubmissionDetailsId { get; set; }
}
