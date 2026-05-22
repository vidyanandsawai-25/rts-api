using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Master;

public class AssetTypeEntity : BaseEntity, IHardDeletable
{
    public int CategoryId { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string? TypeNameLocal { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string CodeFormat { get; set; } = string.Empty;
    public int LastSequence { get; set; }
    public byte[]? RowVersion { get; set; }

    // IHardDeletable implementation
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
