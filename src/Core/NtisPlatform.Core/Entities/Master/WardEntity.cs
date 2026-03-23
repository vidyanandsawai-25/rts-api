


namespace NtisPlatform.Core.Entities;
/// <summary>
/// Represents a Ward master entities.
/// </summary>
public class WardEntity : BaseEntity
{
    public int WardId { get; set; }
    public string WardNo { get; set; } = string.Empty;
    public int ZoneId { get; set; }
    public string? Description { get; set; }
    public int? SequenceNo { get; set; }
}

