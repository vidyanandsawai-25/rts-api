using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Master;

public class OwningDepartmentEntity : BaseEntity, IHardDeletable
{
    public string OwningDepartmentName { get; set; } = default!;
    public string? Description { get; set; }

    // IHardDeletable implementation
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
