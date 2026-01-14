namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents a floor entity manage floor information.
/// </summary>
public class FloorEntity :CommonBaseEntity
{
    public string? FloorID { get; set; }
    public string? Description { get; set; }
    public int? SequenceNo { get; set; }
    public string? DescriptionEnglish { get; set; }
    public int? MaxFloorNo { get; set; }


}
