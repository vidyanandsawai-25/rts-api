using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management.AssetRoomWiseSubmissionDetails;

/// <summary>
/// DTO for AssetRoomWiseSubmissionDetailsEntity - Room-wise details for child assets.
/// </summary>
public class AssetRoomWiseSubmissionDetailsDto : BaseDtos
{
    public int? ParentAssetId { get; set; }
    public int? AssetId { get; set; }
    public int? FloorDetailsId { get; set; }
    public double? LengthMtr { get; set; }
    public double? WidthMtr { get; set; }
    public double? AreaSqMtr { get; set; }
    public double? HeightMtr { get; set; }
    public double? TotalAreaSqMtr { get; set; }
    public string? Shape { get; set; }
    public string? RoomNo { get; set; }
    public bool OuterYesNo { get; set; }
    public string? RoomType { get; set; }
    public bool MinusYesNo { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }

    // Navigation property names
    public string? ParentAssetName { get; set; }
    public string? AssetName { get; set; }
    public string? FloorName { get; set; }
}

public class CreateAssetRoomWiseSubmissionDetailsDto : CreateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_ParentAssetId_InvalidRange")]
    public int? ParentAssetId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_AssetId_InvalidRange")]
    public int? AssetId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_FloorDetailsId_InvalidRange")]
    public int? FloorDetailsId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_LengthMtr_InvalidRange")]
    public double? LengthMtr { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_WidthMtr_InvalidRange")]
    public double? WidthMtr { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_AreaSqMtr_InvalidRange")]
    public double? AreaSqMtr { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_HeightMtr_InvalidRange")]
    public double? HeightMtr { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_TotalAreaSqMtr_InvalidRange")]
    public double? TotalAreaSqMtr { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_Shape_MaxLengthExceeded_50")]
    public string? Shape { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_RoomNo_MaxLengthExceeded_50")]
    public string? RoomNo { get; set; }

    public bool OuterYesNo { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_RoomType_MaxLengthExceeded_50")]
    public string? RoomType { get; set; }

    public bool MinusYesNo { get; set; }
}

public class UpdateAssetRoomWiseSubmissionDetailsDto : UpdateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_ParentAssetId_InvalidRange")]
    public int? ParentAssetId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_AssetId_InvalidRange")]
    public int? AssetId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_FloorDetailsId_InvalidRange")]
    public int? FloorDetailsId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_LengthMtr_InvalidRange")]
    public double? LengthMtr { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_WidthMtr_InvalidRange")]
    public double? WidthMtr { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_AreaSqMtr_InvalidRange")]
    public double? AreaSqMtr { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_HeightMtr_InvalidRange")]
    public double? HeightMtr { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_TotalAreaSqMtr_InvalidRange")]
    public double? TotalAreaSqMtr { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_Shape_MaxLengthExceeded_50")]
    public string? Shape { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_RoomNo_MaxLengthExceeded_50")]
    public string? RoomNo { get; set; }

    public bool OuterYesNo { get; set; }

    [StringLength(50, ErrorMessage = "AMS_AssetRoomWiseSubmissionDetails_RoomType_MaxLengthExceeded_50")]
    public string? RoomType { get; set; }

    public bool MinusYesNo { get; set; }
}
