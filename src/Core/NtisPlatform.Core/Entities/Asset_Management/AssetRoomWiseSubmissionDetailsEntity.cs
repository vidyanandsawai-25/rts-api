using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

/// <summary>
/// Asset Room-wise Submission Details entity for the Asset Management System.
/// Represents room-wise details for child assets (rooms/shops) under a parent asset.
/// </summary>
public class AssetRoomWiseSubmissionDetailsEntity : BaseEntity, IHardDeletable
{
    public int? AssetId { get; set; }
    public int? SubUnitsDetailsId { get; set; }
    public double? LengthMtr { get; set; }
    public double? WidthMtr { get; set; }
    public double? LengthFt { get; set; }
    public double? WidthFt { get; set; }
    public double? AreaSqMtr { get; set; }
    public double? AreaSqFeet { get; set; }
    public double? HeightMtr { get; set; }
    public double? HeightFt { get; set; }
    public double? TotalAreaSqMtr { get; set; }
    public double? TotalAreaSqFeet { get; set; }
    public string? Shape { get; set; }
    public string? RoomNo { get; set; }
    // Maps to schema column IsOuter.
    public bool OuterYesNo { get; set; } = false;
    public string? RoomType { get; set; }
    // Maps to schema column IsMinus.
    public bool MinusYesNo { get; set; } = false;
    public bool MarkedForDeletion { get; set; } = false;
    public DateTime? MarkedForDeletionDate { get; set; }

    // Navigation properties
    public AssetMasterEntity? Asset { get; set; }
    public SubUnitsDetailsEntity? SubUnitsDetails { get; set; }
    public ICollection<AssetRoomWiseMinusDataEntity>? RoomMinusData { get; set; }
}
