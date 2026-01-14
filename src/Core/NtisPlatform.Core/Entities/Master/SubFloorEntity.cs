namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents a sub floor entity manage sub floor information.
/// </summary>
public class SubFloorEntity :CommonBaseEntity
{
    public string SubFloorId { get; set; } = string.Empty;
    public string? SubFloorDescription { get; set; }
    public string? SubFloorDescriptionEnglish { get; set; }
    public decimal? SubFloorPercentage { get; set; }

}
