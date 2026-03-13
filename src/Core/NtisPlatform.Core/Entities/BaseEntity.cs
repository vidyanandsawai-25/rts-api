namespace NtisPlatform.Core.Entities;

/// <summary>
/// Base entity with common properties for all entities
/// </summary>
public abstract class BaseEntity
{
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsActive { get; set; } = true;
    //public bool MarkedForDeletion { get; set; } = false;
}
