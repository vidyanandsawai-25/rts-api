using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Master;

public class AssetDesignationEntity : BaseEntity, IHardDeletable
{
    public int OwningDepartmentId { get; set; }
    public string DesignationCode { get; set; } = default!;
    public string DesignationName { get; set; } = default!;
    public string? DesignationLocal { get; set; }
    public string? DesignationDescription { get; set; }

    // IHardDeletable implementation
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
