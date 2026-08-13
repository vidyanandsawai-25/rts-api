using System;
using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Attributes;

namespace NtisPlatform.Application.DTOs.Asset_Management.AssetRoomWiseMinusData;

/// <summary>
/// DTO for reading AssetRoomWiseMinusData records.
/// </summary>
public class AssetRoomWiseMinusDataDto : BaseDtos
{
    public int? RoomWiseSubmissionId { get; set; }
    public double? LengthMtr { get; set; }
    public double? WidthMtr { get; set; }
    public double? AreaSqMtr { get; set; }
    public double? HeightMtr { get; set; }
    public string? Shape { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}

/// <summary>
/// DTO for creating an AssetRoomWiseMinusData record.
/// </summary>
public class CreateAssetRoomWiseMinusDataDto : CreateBaseDtos
{
    public int? RoomWiseSubmissionId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetRoomWiseMinusData_LengthMtr_InvalidRange")]
    public double? LengthMtr { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetRoomWiseMinusData_WidthMtr_InvalidRange")]
    public double? WidthMtr { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetRoomWiseMinusData_AreaSqMtr_InvalidRange")]
    public double? AreaSqMtr { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetRoomWiseMinusData_HeightMtr_InvalidRange")]
    public double? HeightMtr { get; set; }

    [StringLength(25, ErrorMessage = "AMS_AssetRoomWiseMinusData_Shape_MaxLengthExceeded_25")]
    public string? Shape { get; set; }
}

/// <summary>
/// DTO for updating an AssetRoomWiseMinusData record.
/// </summary>
public class UpdateAssetRoomWiseMinusDataDto : UpdateBaseDtos
{
    public int Id { get; set; }
    public int? RoomWiseSubmissionId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetRoomWiseMinusData_LengthMtr_InvalidRange")]
    public double? LengthMtr { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetRoomWiseMinusData_WidthMtr_InvalidRange")]
    public double? WidthMtr { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetRoomWiseMinusData_AreaSqMtr_InvalidRange")]
    public double? AreaSqMtr { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "AMS_AssetRoomWiseMinusData_HeightMtr_InvalidRange")]
    public double? HeightMtr { get; set; }

    [StringLength(25, ErrorMessage = "AMS_AssetRoomWiseMinusData_Shape_MaxLengthExceeded_25")]
    public string? Shape { get; set; }
}

/// <summary>
/// Query parameters for filtering AssetRoomWiseMinusData records.
/// </summary>
public class AssetRoomWiseMinusDataQueryParameters : BaseQueryParameters
{
    [Filterable]
    public int? RoomWiseSubmissionId { get; set; }
}
