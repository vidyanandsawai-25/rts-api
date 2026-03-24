namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents an Owner Type master entity.
/// </summary>
public class OwnerTypeEntity : BaseEntity
{
    public int OwnerTypeId { get; set; }
    public string OwnerType { get; set; } = string.Empty;
}