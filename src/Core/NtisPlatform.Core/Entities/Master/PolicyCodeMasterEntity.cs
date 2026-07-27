using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents a policy code / workflow stage in the PTIS system (PTIS.PolicyCodeMaster table).
/// PolicyType groups codes into NORMAL (NETTAX, AS_PER_OLD, MIN_RV, RETENTION),
/// DATE_BASED (PARTIAL_OC, OC, PARTIAL_CC, CC, PARTIAL_ELECTRIC_BILL, ELECTRIC_BILL),
/// STAGE_BASED (SECTION_129_OLD_1, SECTION_129_OLD_2, SECTION_129_20/40/60/80/100),
/// and DECISION (HEARING, APPEAL_COMMITTEE, REMISSION). Column types/lengths and the
/// PolicyType CHECK constraint are configured via Fluent API in ApplicationDbContext, which is
/// authoritative -- DataAnnotations are omitted here to avoid two sources of truth.
/// </summary>
[Table("PolicyCodeMaster", Schema = "PTIS")]
public class PolicyCodeMasterEntity : BaseEntity
{
    public string PolicyCode { get; set; } = string.Empty;

    public string PolicyName { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>NORMAL / DATE_BASED / STAGE_BASED / DECISION</summary>
    public string PolicyType { get; set; } = string.Empty;

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
