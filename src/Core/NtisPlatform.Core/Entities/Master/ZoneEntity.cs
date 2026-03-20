namespace NtisPlatform.Core.Entities;
/// <summary>
/// Represents a zone master entities.
/// </summary>
public class ZoneEntity : BaseEntity
{
    public int ZoneId { get; set; }
    public string ZoneNo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? SequenceNo { get; set; }
}

