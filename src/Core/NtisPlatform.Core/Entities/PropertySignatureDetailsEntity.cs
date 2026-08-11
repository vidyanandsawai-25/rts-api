using NtisPlatform.Core.Entities.Master;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Records one property approval by a signing authority user.
/// One row = one authority has approved one property.
///
/// Sequential rule: A property can only be approved by Authority N
/// if it already has an active row for Authority (N-1).
///
/// Unique constraint (enforced in DB): (PropertyId, SignAuthorityId) WHERE IsActive = 1
/// </summary>
[Table("PropertySignatureDetails", Schema = "PTIS")]
public class PropertySignatureDetailsEntity : BaseEntity
{
    /// <summary>FK → UserMasters — the user who performed the approval</summary>
    public int UserId { get; set; }

    /// <summary>FK → PropertyMast — which property was approved</summary>
    public int PropertyId { get; set; }

    /// <summary>FK → SignAuthorityMaster — in which signing capacity (Clerk/TI/AC/ADC)</summary>
    public int SignAuthorityId { get; set; }

    /// <summary>Optional approval note or comment from the signing user</summary>
    public string? NoticeNo { get; set; }
  
    // Navigation Properties

    [ForeignKey(nameof(UserId))]
    public virtual UserEntity User { get; set; } = null!;

    [ForeignKey(nameof(PropertyId))]
    public virtual PropertyEntity Property { get; set; } = null!;

    [ForeignKey(nameof(SignAuthorityId))]
    public virtual SignAuthorityMasterEntity SignAuthority { get; set; } = null!;
}
