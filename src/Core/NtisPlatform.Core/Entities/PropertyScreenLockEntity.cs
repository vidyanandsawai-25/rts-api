using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities;

public class PropertyScreenLockEntity : BaseEntity
{
    public int PropertyId { get; set; }
    public int LockableScreenId { get; set; }
    public bool IsLocked { get; set; }

    public int? LockedBy { get; set; }
    public DateTime? LockedDate { get; set; }
    public int? UnlockedBy { get; set; }
    public DateTime? UnlockedDate { get; set; }

    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }

    public virtual PropertyEntity? Property { get; set; }
    public virtual ScreenMasterEntity? LockableScreen { get; set; }
}
