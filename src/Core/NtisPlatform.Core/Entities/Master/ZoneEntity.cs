namespace NtisPlatform.Core.Entities;
/// <summary>
/// Represents a zone master entities.
/// </summary>
public class ZoneEntity : CommonBaseEntity
{
    public string ZoneNo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? DescriptionEnglish { get; set; }
    public int? SequenceNo { get; set; }
}

