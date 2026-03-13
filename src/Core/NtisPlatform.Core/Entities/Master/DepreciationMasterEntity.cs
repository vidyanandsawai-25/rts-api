using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents a depreciation master entity used to manage depreciation information.
/// </summary>
public class DepreciationMasterEntity : BaseEntity
{
    [Required]
    public int ID { get; set; }

    [Required]
    [StringLength(7)]
    public string ConstructionId { get; set; }=string.Empty;

    public int MinYear { get; set; }
    public int MaxYear { get; set; }
    public decimal Rate { get; set; }
    public int Year { get; set; }   
}
