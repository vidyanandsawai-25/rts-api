using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Master;

public class AssetApplicationTypeEntity : BaseEntity, IHardDeletable
{
    public string ApplicationTypeCode { get; set; } = default!;
    public string ApplicationTypeName { get; set; } = default!;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }

    // IHardDeletable implementation
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
