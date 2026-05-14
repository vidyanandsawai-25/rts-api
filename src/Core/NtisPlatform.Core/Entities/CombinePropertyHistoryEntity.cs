using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents combine property history in the PTIS system
/// </summary>
public class CombinePropertyHistoryEntity : BaseEntity
{

    [ForeignKey(nameof(MainProperty))]
    public int MainPropertyId { get; set; }


    [ForeignKey(nameof(TargetProperty))]
    public int TargetPropertyId { get; set; }

    public string? Remark { get; set; }

    public virtual PropertyEntity? MainProperty { get; set; }
    public virtual PropertyEntity? TargetProperty { get; set; }
}
