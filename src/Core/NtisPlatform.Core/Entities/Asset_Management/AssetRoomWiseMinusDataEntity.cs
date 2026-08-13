using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

public class AssetRoomWiseMinusDataEntity : BaseEntity, IHardDeletable
{
    public int? RoomWiseSubmissionId { get; set; }
    public double? LengthMtr { get; set; }
    public double? LengthFt { get; set; }
    public double? WidthMtr { get; set; }
    public double? WidthFt { get; set; }
    public double? AreaSqMtr { get; set; }
    public double? AreaSqFeet { get; set; }
    public double? HeightMtr { get; set; }
    public double? HeightFt { get; set; }
    public string? Shape { get; set; }
    public bool MarkedForDeletion { get; set; } = false;
    public DateTime? MarkedForDeletionDate { get; set; }

    public AssetRoomWiseSubmissionDetailsEntity? RoomWiseSubmissionDetails { get; set; }
}
