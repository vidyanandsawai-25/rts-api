using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Rule Master registry (PTIS.DynamicTaxRuleMaster).
/// Admin/developer-defined rules surfaced to end users by <see cref="DisplayName"/> only —
/// the technical <see cref="RuleType"/> (calculation mode) and <see cref="AttachedReference"/>
/// stay internal. Drives the Rule Name dropdown on the Dynamic Tax Register.
/// </summary>
[Table("DynamicTaxRuleMaster", Schema = "PTIS")]
public class DynamicTaxRuleEntity : BaseEntity
{
    /// <summary>User-facing name shown in the Rule Name dropdown (e.g. "PROPERTY_TYPE_CHARGE Rule").</summary>
    [Required]
    [Column(TypeName = "nvarchar(200)")]
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// Internal calculation mode this rule maps to — a
    /// <see cref="TaxCalculationModeMasterEntity.ModeCode"/> value. Validated against that table
    /// on save (see DynamicTaxRuleService), so it can only hold a mode that actually exists.
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(20)")]
    public string RuleType { get; set; } = null!;

    /// <summary>
    /// Optional pointer to the backing data the rule reads: a master-table key for
    /// MASTER_BASED, or (legacy) a RuleEngine RuleCode for CONDITION_BASED — superseded by
    /// PTIS.TaxConditionRule; no longer populated for new CONDITION_BASED rule slots.
    /// </summary>
    [Column(TypeName = "nvarchar(200)")]
    public string? AttachedReference { get; set; }

    /// <summary>Display order in the Rule Name dropdown / Rule Master grid.</summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>Optional description shown on the rule card.</summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    // ── Navigation ──────────────────────────────────────────────────────────────

    /// <summary>Taxes that reference this rule definition.</summary>
    public ICollection<TaxMasterEntity> Taxes { get; set; } = new List<TaxMasterEntity>();
}
