using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// HYBRID strategy configuration (PTIS.TaxHybridConfig), one row per HYBRID tax.
/// Combines the master mapping (PTIS.TaxMasterMapping) with a condition rule fallback;
/// this entity only persists the evaluation strategy — the mapping and rule live in their
/// own tables. (Calculation-engine integration is a follow-up.)
/// </summary>
[Table("TaxHybridConfig", Schema = "PTIS")]
public class TaxHybridConfigEntity : BaseEntity
{
    /// <summary>FK → PTIS.TaxMaster (unique — one config per tax).</summary>
    [Required]
    public int TaxId { get; set; }

    /// <summary>MASTER_THEN_CONDITION | CONDITION_THEN_MASTER.</summary>
    [Required]
    [Column(TypeName = "nvarchar(30)")]
    public string EvaluationPriority { get; set; } = "MASTER_THEN_CONDITION";

    /// <summary>DEFAULT_ZERO | CONDITION_RULE.</summary>
    [Required]
    [Column(TypeName = "nvarchar(20)")]
    public string FallbackStrategy { get; set; } = "DEFAULT_ZERO";

    /// <summary>NONE | RV | ALV.</summary>
    [Required]
    [Column(TypeName = "nvarchar(10)")]
    public string ResultBase { get; set; } = "NONE";

    // ── Navigation ──────────────────────────────────────────────────────────────
    public TaxMasterEntity? Tax { get; set; }
}
