using NtisPlatform.Core.Entities.Master;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;
/// <summary>
/// Represents a Ward master entities.
/// </summary>
public class WardEntity : BaseEntity
{
    public string WardNo { get; set; } = string.Empty;
    public int ZoneId { get; set; }
    public string? Description { get; set; }
    public int? SequenceNo { get; set; }
    public virtual ZoneEntity? Zone { get; set; }
    public ICollection<BlockMasterEntity> BlockMaster { get; set; } = new List<BlockMasterEntity>();
    public ICollection<RateSectionDetailsEntity> RateSectionDetails { get; set; } = new List<RateSectionDetailsEntity>();
    public ICollection<PropertyEntity> Property { get; set; } = new List<PropertyEntity>();
}

