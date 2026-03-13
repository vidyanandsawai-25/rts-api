using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents a floor entity manage floor information.
/// </summary>
public class FloorEntity :BaseEntity
{
    [Required]
    [StringLength(5)]
    public string? FloorID { get; set; }
    
    [StringLength(100)]
    public string? Description { get; set; }
    public int? SequenceNo { get; set; }

    [StringLength(100)]
    public string? DescriptionEnglish { get; set; }
    public int? MaxFloorNo { get; set; }


}
