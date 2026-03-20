namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents a sub floor entity manage sub floor information.
/// </summary>
public class SubFloorEntity :BaseEntity
{
    public int SubFloorId { get; set; } 
    public string? SubFloorCode { get; set; }
    public string? Description { get; set; }
    public decimal? SubFloorPercentage { get; set; }

}
