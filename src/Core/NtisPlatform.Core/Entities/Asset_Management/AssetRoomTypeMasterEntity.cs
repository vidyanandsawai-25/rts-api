using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Asset room type master mapped to AMS.AssetRoomTypeMaster.
/// </summary>
[Table("AssetRoomTypeMaster", Schema = "AMS")]
public class AssetRoomTypeMasterEntity : BaseEntity, IHardDeletable
{
    public int? AssetCategoryId { get; set; }

    public int AssetTypeId { get; set; }

    public string? RoomTypeCode { get; set; }

    public string RoomTypeName { get; set; } = string.Empty;

    public string? Description { get; set; }

    // IHardDeletable properties
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
