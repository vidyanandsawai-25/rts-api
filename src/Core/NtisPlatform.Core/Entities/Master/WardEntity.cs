
namespace NtisPlatform.Core.Entities;
/// <summary>
/// Represents a Ward master entities.
/// </summary>
public class WardEntity : BaseEntity
{
    public string WardNo { get; set; } = string.Empty;
    public string ZoneNo { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DescriptionEnglish { get; set; } 
    public int? SequenceNo { get; set; } 
}

