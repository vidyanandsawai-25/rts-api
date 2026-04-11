using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents a floor entity manage floor information.
/// </summary>
public class FloorEntity :BaseEntity
{
    public string FloorCode { get; set; }
    public string Description { get; set; }
    public int? SequenceNo { get; set; }
    public int? MaxFloorNo { get; set; }

}
