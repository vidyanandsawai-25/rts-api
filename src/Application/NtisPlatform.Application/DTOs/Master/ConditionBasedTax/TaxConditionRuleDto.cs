using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Rules.ConditionEvaluation;

namespace NtisPlatform.Application.DTOs.Master;

/// <summary>One condition item: {fieldId, operator, value, logicalOperator}. Value binds as a
/// boxed object (string, number, bool, or array) via System.Text.Json's default handling.</summary>
public class TaxConditionItemDto
{
    [Required]
    public string FieldId { get; set; } = null!;

    [Required]
    public string Operator { get; set; } = null!;

    public object? Value { get; set; }

    /// <summary>AND | OR — how this condition joins with the PREVIOUS condition in the row's
    /// list (ignored for the first condition). Evaluated strictly left-to-right, no
    /// parentheses/precedence — e.g. "A AND B OR C" means "(A AND B) OR C".</summary>
    public string LogicalOperator { get; set; } = "AND";
}

/// <summary>A single condition rule row (read + upsert item). An empty <see cref="Conditions"/>
/// list is a valid "always matches" catch-all row — not required to be non-empty.</summary>
public class TaxConditionRuleDto
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "ConditionBasedTax_TaxId_Invalid")]
    public int TaxId { get; set; }

    public int SortOrder { get; set; } = 0;

    public List<TaxConditionItemDto> Conditions { get; set; } = new();

    public int? AssessmentYearRangeId { get; set; }
    public string ResultMode { get; set; } = "FIXED";
    public string ResultBase { get; set; } = "NONE";

    /// <summary>
    /// Absolute backstop only. The real per-mode ceilings (PERCENT ≤ 100, FIXED ≤ 999,
    /// PER_UNIT ≤ 99999) are enforced in TaxConditionRuleService.ValidateResultModeAndBase, since a
    /// static attribute cannot see ResultMode — a per-unit RATE is a currency amount and legitimately
    /// exceeds the 999 that suits a flat FIXED charge.
    /// </summary>
    [Range(0, 99999, ErrorMessage = "ConditionBasedTax_ResultValue_OutOfRange")]
    public decimal ResultValue { get; set; }

    /// <summary>Only meaningful when <see cref="ResultBase"/> is "OTHER_TAX" — the tax whose
    /// already-computed amount a PERCENT result is applied to.</summary>
    public int? ReferenceTaxId { get; set; }

    /// <summary>Only meaningful when <see cref="ResultMode"/> is "PER_UNIT" — the numeric field
    /// whose value multiplies <see cref="ResultValue"/> (e.g. "NoOfResidentialToilets" for
    /// "150 per toilet"). Nulled out by the service for every other mode.</summary>
    [StringLength(100, ErrorMessage = "ConditionBasedTax_UnitFieldId_MaxLengthExceeded_100")]
    public string? UnitFieldId { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>When true and this row matches during evaluation, rows below it (by SortOrder) are
    /// never evaluated — reproducing the original first-match-wins behavior from this row onward.
    /// When false (the default), a match here doesn't stop evaluation; every matching row's result
    /// is summed. See TaxConditionRuleEntity.StopFurtherProcessing.</summary>
    public bool StopFurtherProcessing { get; set; } = false;

    /// <summary>PROPERTY_BASED | BUILDING_BASED — descriptive classification only, not wired into
    /// any RV/ALV/condition evaluation logic. See TaxConditionRuleEntity.IsBuildingBased.</summary>
    public string AssessmentBasis { get; set; } = "PROPERTY_BASED";
}

/// <summary>Persist explicit per-row edits (insert or update by Id) — upsert-only, same
/// limitation as SaveMasterMappingRequest: rows omitted from a resend are not deleted.</summary>
public class SaveTaxConditionRuleRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "ConditionBasedTax_TaxId_Invalid")]
    public int TaxId { get; set; }

    public int? UpdatedBy { get; set; }

    /// <summary>Empty list is valid — "zero condition rows" is a legitimate state.</summary>
    public List<TaxConditionRuleDto> Rows { get; set; } = new();
}

/// <summary>Test/evaluate a tax's saved condition rows against one real property.</summary>
public class EvaluateTaxConditionRuleRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "ConditionBasedTax_TaxId_Invalid")]
    public int TaxId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "ConditionBasedTax_PropertyId_Invalid")]
    public int PropertyId { get; set; }

    /// <summary>Optional — defaults to the property's lowest-Id active detail if omitted.</summary>
    public int? PropertyDetailsId { get; set; }

    /// <summary>Optional — defaults to the current finance year.</summary>
    public int? FinanceYear { get; set; }
}

public class TaxConditionRuleEvaluationTraceDto
{
    public int RuleId { get; set; }
    public int SortOrder { get; set; }
    public bool IsMatch { get; set; }
    public bool Skipped { get; set; }
    public string? SkipReason { get; set; }
    public List<string> UnresolvedFields { get; set; } = new();

    /// <summary>Per-condition pass/fail breakdown (fieldId, operator, expected vs. actual
    /// value) for this row, in the row's own AND order — lets the "Test this Rule" UI show
    /// exactly which of a row's conditions passed/failed, not just the row's overall verdict.</summary>
    public List<ConditionItemEvaluationTrace> Conditions { get; set; } = new();
}

/// <summary>One row that matched during evaluation and contributed to <see
/// cref="EvaluateTaxConditionRuleResponseDto.ComputedAmount"/>. Multiple rows can match and
/// execute for a single evaluation — see TaxConditionRuleEntity.StopFurtherProcessing.</summary>
public class TaxConditionRuleMatchResultDto
{
    public int RuleId { get; set; }
    public int SortOrder { get; set; }
    public string ResultMode { get; set; } = "FIXED";
    public string ResultBase { get; set; } = "NONE";

    /// <summary>This row's own contribution — the sum of every matched row's ComputedAmount is
    /// <see cref="EvaluateTaxConditionRuleResponseDto.ComputedAmount"/>.</summary>
    public decimal ComputedAmount { get; set; }

    /// <summary>True if this row's own StopFurtherProcessing was set, meaning evaluation halted
    /// right after it — rows below it (by SortOrder) were never evaluated.</summary>
    public bool StoppedFurtherProcessing { get; set; }

    /// <summary>Populated only when ResultBase is "OTHER_TAX" — the referenced tax's
    /// already-persisted amount that ResultValue% was applied to. 0 here is ambiguous on its own —
    /// see <see cref="ReferenceTaxAmountResolved"/>.</summary>
    public decimal? ReferenceTaxAmountUsed { get; set; }

    /// <summary>
    /// False when ResultBase is "OTHER_TAX" but the referenced tax has no persisted
    /// PropertyTaxCalculationRVResults row for this property — the common case for a
    /// non-VALUE_BASED reference, since only VALUE_BASED taxes are computed by the live billing
    /// pipeline today. Without this, "no result recorded yet" and "the referenced tax genuinely
    /// computed to ₹0" are indistinguishable from ReferenceTaxAmountUsed alone. Null for every
    /// other result base.
    /// </summary>
    public bool? ReferenceTaxAmountResolved { get; set; }

    /// <summary>The multiplier actually read for a "PER_UNIT" row. Null when the field could not be
    /// resolved — see <see cref="UnitCountResolved"/>.</summary>
    public decimal? UnitCountUsed { get; set; }

    /// <summary>
    /// False when a "PER_UNIT" row matched but its unit field was missing from the property's data
    /// (or held a non-numeric value). ComputedAmount is 0 in that case, and callers MUST surface
    /// this rather than presenting a confident ₹0 for a rule that did match — the count simply
    /// isn't recorded for that property. Null for every other result mode.
    /// </summary>
    public bool? UnitCountResolved { get; set; }
}

public class EvaluateTaxConditionRuleResponseDto
{
    public int TaxId { get; set; }
    public int PropertyId { get; set; }
    public int PropertyDetailsId { get; set; }

    /// <summary>True if at least one row matched (and therefore contributed to ComputedAmount).</summary>
    public bool Matched { get; set; }

    /// <summary>Sum of every matched row's own ComputedAmount — see <see cref="MatchedResults"/>
    /// for the per-row breakdown.</summary>
    public decimal ComputedAmount { get; set; }

    public decimal? RateableValueUsed { get; set; }
    public double? AnnualRentalValueUsed { get; set; }

    /// <summary>One entry per row that matched and executed, in evaluation order. Empty when
    /// Matched is false.</summary>
    public List<TaxConditionRuleMatchResultDto> MatchedResults { get; set; } = new();

    public List<TaxConditionRuleEvaluationTraceDto> Trace { get; set; } = new();
}
