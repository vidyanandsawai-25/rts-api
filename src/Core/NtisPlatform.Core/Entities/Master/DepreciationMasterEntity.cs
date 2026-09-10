using NtisPlatform.Core.Entities.Master;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents a depreciation master entity used to manage depreciation information.
/// </summary>
public class DepreciationMasterEntity : BaseEntity
{
    [Required]
    public int ConstructionTypeId { get; set; }
    public int MinYear { get; set; }
    public int MaxYear { get; set; }
    public decimal Rate { get; set; }
    public virtual ConstructionTypeEntity? ConstructionType { get; set; }
}
