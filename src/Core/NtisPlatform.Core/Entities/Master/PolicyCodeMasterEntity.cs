using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents a policy code / workflow stage in the PTIS system (PTIS.PolicyCodeMaster table).
/// </summary>
[Table("PolicyCodeMaster", Schema = "PTIS")]
public class PolicyCodeMasterEntity : BaseEntity
{
    [Required]
    [Column(TypeName = "nvarchar(20)")]
    public string PolicyCode { get; set; } = null!;

    [Required]
    [Column(TypeName = "nvarchar(200)")]
    public string PolicyName { get; set; } = null!;

    [Column(TypeName = "nvarchar(500)")]
    public string? Description { get; set; }

    [Column(TypeName = "nvarchar(50)")]
    public string? PolicyType { get; set; }

    /// <summary>FK → <see cref="PolicyCodeMasterEntity"/> for the next stage in the workflow.</summary>
    public int? NextPolicyCodeId { get; set; }

    public bool IsFinalStage { get; set; } = false;

    public bool IsExclusive { get; set; } = false;

    public bool RequiresStageTracking { get; set; } = false;

    public int DisplayOrder { get; set; } = 0;

    // ── Navigation ──────────────────────────────────────────────────────────────

    /// <summary>Next stage in the workflow (self-referencing FK).</summary>
    public PolicyCodeMasterEntity? NextPolicyCode { get; set; }
}
