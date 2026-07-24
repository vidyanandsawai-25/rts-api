using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs;

namespace NtisPlatform.Application.DTOs.Master.AssetRoomType;

public class AssetRoomTypeMasterDto : BaseDtos
{
    public int? AssetCategoryId { get; set; }

    public int AssetTypeId { get; set; }

    public string? RoomTypeCode { get; set; }

    public string RoomTypeName { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>[AMS].[AssetCategoryMaster].CategoryName for <see cref="AssetCategoryId"/>, resolved via join.</summary>
    public string? AssetCategoryName { get; set; }

    /// <summary>[AMS].[AssetTypeMaster].TypeName for <see cref="AssetTypeId"/>, resolved via join.</summary>
    public string? AssetTypeName { get; set; }
}

public class CreateAssetRoomTypeDto : CreateBaseDtos
{
    public int? AssetCategoryId { get; set; }

    [Required(ErrorMessage = "AssetRoomType_AssetTypeId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AssetRoomType_AssetTypeId_Invalid")]
    public int? AssetTypeId { get; set; }

    [StringLength(20, ErrorMessage = "AssetRoomType_RoomTypeCode_MaxLengthExceeded_20")]
    public string? RoomTypeCode { get; set; }

    [Required(ErrorMessage = "AssetRoomType_RoomTypeName_Required")]
    [StringLength(100, ErrorMessage = "AssetRoomType_RoomTypeName_MaxLengthExceeded_100")]
    public string RoomTypeName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "AssetRoomType_Description_MaxLengthExceeded_500")]
    public string? Description { get; set; }
}

public class UpdateAssetRoomTypeDto : UpdateBaseDtos
{
    public int? AssetCategoryId { get; set; }

    [Required(ErrorMessage = "AssetRoomType_AssetTypeId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AssetRoomType_AssetTypeId_Invalid")]
    public int? AssetTypeId { get; set; }

    [StringLength(20, ErrorMessage = "AssetRoomType_RoomTypeCode_MaxLengthExceeded_20")]
    public string? RoomTypeCode { get; set; }

    [Required(ErrorMessage = "AssetRoomType_RoomTypeName_Required")]
    [StringLength(100, ErrorMessage = "AssetRoomType_RoomTypeName_MaxLengthExceeded_100")]
    public string RoomTypeName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "AssetRoomType_Description_MaxLengthExceeded_500")]
    public string? Description { get; set; }
}
