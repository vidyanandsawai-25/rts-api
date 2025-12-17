namespace NtisPlatform.Core.Entities;

/// <summary>
/// Base entity with common properties for all entities
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    //public DateTime CreatedAt { get; set; }
    //public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    //public bool IsDeleted { get; set; }
	public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
