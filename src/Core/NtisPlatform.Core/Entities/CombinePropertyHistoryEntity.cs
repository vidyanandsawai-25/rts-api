using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents combine property history in the PTIS system
/// </summary>
public class CombinePropertyHistoryEntity : BaseEntity
{

    [ForeignKey(nameof(SourceProperty))]
    [Column("SourcePropertyId")]
    public int SourcePropertyId { get; set; }

    [ForeignKey(nameof(CombinedProperty))]
    [Column("CombinedPropertyId")]
    public int CombinedPropertyId { get; set; }

    [Required]
    [StringLength(500)]
    public string CombineReason { get; set; } = string.Empty;

    public virtual PropertyEntity? SourceProperty { get; set; }
    public virtual PropertyEntity? CombinedProperty { get; set; }
}
