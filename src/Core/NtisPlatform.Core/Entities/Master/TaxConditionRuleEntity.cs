using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Condition-based tax rule row (PTIS.TaxConditionRule).
/// One priority-ordered, flat condition set per row for a CONDITION_BASED (or HYBRID) tax,
/// giving the result to apply when it matches. Rows for a tax are evaluated in
/// <see cref="SortOrder"/> order; EVERY matching row contributes its result to the total
/// (they're summed) unless <see cref="StopFurtherProcessing"/> halts evaluation early.
/// </summary>
[Table("TaxConditionRule", Schema = "PTIS")]
public class TaxConditionRuleEntity : BaseEntity
{
    /// <summary>FK → PTIS.TaxMaster.</summary>
    [Required]
    public int TaxId { get; set; }

    /// <summary>Optional FK → PTIS.DynamicTaxRuleMaster (the rule slot this row belongs to).</summary>
    public int? RuleDefinitionId { get; set; }

    /// <summary>Evaluation order — ascending. Every row in order is evaluated and every match
    /// contributes to the total unless a matching row upstream has <see cref="StopFurtherProcessing"/>
    /// set.</summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// When true AND this row matches, evaluation halts immediately after this row — rows below it
    /// (by <see cref="SortOrder"/>) are never evaluated, reproducing the original first-match-wins
    /// behavior from this row onward. When false (the default for new rows), a match here doesn't
    /// stop anything — every subsequent row is still evaluated and, if it matches, its own result is
    /// added to this row's. Has no effect on a row that does NOT match — a non-matching row never
    /// halts evaluation regardless of this flag.
    /// </summary>
    public bool StopFurtherProcessing { get; set; } = false;

    /// <summary>Descriptive classification only — not wired into any RV/ALV/condition evaluation
    /// logic. False (the default) means Property Based; true means Building Based. Exposed on the
    /// wire as <c>AssessmentBasis</c> ("PROPERTY_BASED" | "BUILDING_BASED") — see
    /// TaxConditionRuleService.ValidateAndParseAssessmentBasis.</summary>
    public bool IsBuildingBased { get; set; } = false;

    /// <summary>Condition list, JSON array of {fieldId, operator, value, logicalOperator}.
    /// Each item's logicalOperator (AND|OR) joins it with the PREVIOUS item, folded strictly
    /// left-to-right — no parentheses/precedence. An empty array ("[]") is a valid
    /// "always matches" catch-all row.</summary>
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string ConditionsJson { get; set; } = null!;

    /// <summary>Optional FK → PTIS.AssessmentYearRange. Null means the row applies regardless
    /// of assessment year — unlike TaxMasterMappingEntity, a condition row has no natural key
    /// to disambiguate by year.</summary>
    public int? AssessmentYearRangeId { get; set; }

    /// <summary>FIXED | PERCENT | PER_UNIT.</summary>
    [Required]
    [Column(TypeName = "nvarchar(10)")]
    public string ResultMode { get; set; } = "FIXED";

    /// <summary>NONE | RV | ALV | OTHER_TAX (base a PERCENT result is applied to).</summary>
    [Required]
    [Column(TypeName = "nvarchar(10)")]
    public string ResultBase { get; set; } = "NONE";

    /// <summary>Fixed amount or percentage, per <see cref="ResultMode"/>.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal ResultValue { get; set; }

    /// <summary>FK → PTIS.TaxMaster. Only meaningful when <see cref="ResultBase"/> is
    /// "OTHER_TAX" — the tax whose already-computed amount a PERCENT result is applied to.</summary>
    public int? ReferenceTaxId { get; set; }

    /// <summary>
    /// A RulesField field id (matched the same way conditions match: trimmed and space-stripped,
    /// case-insensitive). Only meaningful when <see cref="ResultMode"/> is "PER_UNIT", where the
    /// result is <see cref="ResultValue"/> × this field's numeric value — e.g. 150 per toilet.
    ///
    /// <para>
    /// SCOPE CAVEAT for anyone wiring PER_UNIT into real billing later: counts like
    /// PropertyAssessment.NoOfResidentialToilets are recorded once PER PROPERTY, whereas the
    /// evaluator runs against a SINGLE property detail. Multiplying inside a per-detail billing
    /// loop would therefore charge a 3-floor property 3 × rate × count.
    /// </para>
    /// </summary>
    [Column(TypeName = "nvarchar(100)")]
    public string? UnitFieldId { get; set; }

    // ── Navigation ──────────────────────────────────────────────────────────────
    public TaxMasterEntity? Tax { get; set; }
    public TaxMasterEntity? ReferenceTax { get; set; }
    public DynamicTaxRuleEntity? RuleDefinition { get; set; }
}
