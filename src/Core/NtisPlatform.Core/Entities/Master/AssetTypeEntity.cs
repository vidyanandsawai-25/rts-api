using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Master;

public class AssetTypeEntity : BaseEntity, IHardDeletable
{
    public int AssetCategoryId { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string? TypeNameLocal { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string CodeFormat { get; set; } = string.Empty;
    public int LastSequence { get; set; }
    public byte[]? RowVersion { get; set; }
    public bool IsSubUnit { get; set; }

    // Registration Flow Controls
    public bool AllowUnitRegistration { get; set; }
    public bool AllowRoomRegistration { get; set; }

    // IHardDeletable implementation
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }

    public string? AssetWardNo { get; set; }
}
