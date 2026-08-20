using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleMaster;

/// <summary>
/// Request body for the Rule Builder's "Save" button: the rule header, the evidence
/// checkbox panels, the optional date-comparison section, the retrospective tax action and
/// the optional unauthorized-construction penalty — everything on that one screen — saved in a
/// single call. Pass Id = null to create a new rule (RuleStatus is always set to Draft); pass an
/// existing Id to update that rule's header and every section in place. This never changes
/// RuleStatus on an existing rule — publishing (Draft/Review/NeedsClarification -> Active) is a
/// separate action via POST {id}/publish.
/// </summary>
public class SaveRetrospectiveRuleDto
{
    /// <summary>Null/omitted to create a new rule; an existing rule's Id to update it.</summary>
    public int? Id { get; set; }

    [Required(ErrorMessage = "RetrospectiveRuleMaster_RuleCode_Required")]
    [StringLength(50, ErrorMessage = "RetrospectiveRuleMaster_RuleCode_MaxLen_50")]
    public string RuleCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "RetrospectiveRuleMaster_RuleName_Required")]
    [StringLength(200, ErrorMessage = "RetrospectiveRuleMaster_RuleName_MaxLen_200")]
    public string RuleName { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "RetrospectiveRuleMaster_RuleDescription_MaxLen_1000")]
    public string? RuleDescription { get; set; }

    public int PriorityNo { get; set; }

    /// <summary>CONDITION_BASED / EXACT_EVIDENCE_MATCH / PRIORITY_BASED</summary>
    [Required(ErrorMessage = "RetrospectiveRuleMaster_MatchType_Required")]
    [StringLength(30, ErrorMessage = "RetrospectiveRuleMaster_MatchType_MaxLen_30")]
    public string MatchType { get; set; } = "CONDITION_BASED";

    public bool IsFallbackRule { get; set; }

    /// <summary>AUTHORIZED / UNAUTHORIZED / UNDETERMINED</summary>
    [StringLength(30, ErrorMessage = "RetrospectiveRuleMaster_AuthorizationStatus_MaxLen_30")]
    public string? AuthorizationStatus { get; set; }

    public bool LegalCapEnabled { get; set; } = true;
    public int LegalCapYears { get; set; } = 6;
    public int NoticeDays { get; set; } = 15;

    [StringLength(20, ErrorMessage = "RetrospectiveRuleMaster_VersionNo_MaxLen_20")]
    public string? VersionNo { get; set; }

    [StringLength(200, ErrorMessage = "RetrospectiveRuleMaster_ResolutionRef_MaxLen_200")]
    public string? ResolutionRef { get; set; }

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    [StringLength(1000, ErrorMessage = "RetrospectiveRuleMaster_Remarks_MaxLen_1000")]
    public string? Remarks { get; set; }

    /// <summary>EvidenceTypeMaster.Id values checked in the "Available evidence" panel.</summary>
    public List<int> AvailableEvidenceTypeIds { get; set; } = new();

    /// <summary>EvidenceTypeMaster.Id values checked in the "Unavailable evidence" panel.</summary>
    public List<int> UnavailableEvidenceTypeIds { get; set; } = new();

    /// <summary>"Compare evidence dates" section. Omit/null to leave this rule with no date comparison.</summary>
    public SaveRetrospectiveRuleDateConditionDto? DateCondition { get; set; }

    /// <summary>"Retrospective Tax" section (Tax starts from / Retrospective limit / Tax calculation).</summary>
    [Required(ErrorMessage = "RetrospectiveRuleMaster_Action_Required")]
    public SaveRetrospectiveRuleActionDto Action { get; set; } = new();

    /// <summary>"Unauthorized Construction Penalty" section. Omit/null when not applicable for this rule.</summary>
    public SaveRetrospectivePenaltyRuleDto? PenaltyRule { get; set; }

    /// <summary>
    /// Id of the user performing this save. Stamped as CreatedBy on newly-inserted rows (rule
    /// and every section) and as UpdatedBy on rows that already existed.
    /// </summary>
    public int? UpdatedBy { get; set; }
}

public class SaveRetrospectiveRuleDateConditionDto
{
    /// <summary>
    /// Get valid choices (with display labels) from GET api/RetrospectiveRuleDateCondition/comparator-codes.
    /// </summary>
    [Required(ErrorMessage = "RetrospectiveRuleDateCondition_ComparatorCode_Required")]
    [StringLength(50, ErrorMessage = "RetrospectiveRuleDateCondition_ComparatorCode_MaxLen_50")]
    public string ComparatorCode { get; set; } = "NONE";

    public int? LeftEvidenceTypeId { get; set; }
    public int? RightEvidenceTypeId { get; set; }

    [StringLength(30, ErrorMessage = "RetrospectiveRuleDateCondition_CompareOperator_MaxLen_30")]
    public string? CompareOperator { get; set; }

    public DateTime? CompareDate { get; set; }
    public DateTime? CompareDateTo { get; set; }
    public int? CompareYears { get; set; }
}

public class SaveRetrospectiveRuleActionDto
{
    /// <summary>
    /// Get valid choices (with display labels and which extra field each needs) from
    /// GET api/RetrospectiveRuleAction/tax-start-modes.
    /// </summary>
    [Required(ErrorMessage = "RetrospectiveRuleAction_TaxStartMode_Required")]
    [StringLength(50, ErrorMessage = "RetrospectiveRuleAction_TaxStartMode_MaxLen_50")]
    public string TaxStartMode { get; set; } = string.Empty;

    /// <summary>
    /// "Use date" field. Get valid choices from GET api/RetrospectiveRuleAction/use-date-options.
    /// Leave null and set TaxStartMode = "FIXED_CUTOFF" when the user picks "Cutoff date".
    /// </summary>
    public int? StartEvidenceTypeId { get; set; }
    public int? OffsetMonths { get; set; }

    /// <summary>
    /// Get valid choices (with display labels and which extra field each needs) from
    /// GET api/RetrospectiveRuleAction/retrospective-limit-types.
    /// </summary>
    [Required(ErrorMessage = "RetrospectiveRuleAction_RetrospectiveLimitType_Required")]
    [StringLength(50, ErrorMessage = "RetrospectiveRuleAction_RetrospectiveLimitType_MaxLen_50")]
    public string RetrospectiveLimitType { get; set; } = string.Empty;

    public int? MaximumYears { get; set; }
    public DateTime? CutoffDate { get; set; }

    /// <summary>
    /// Get valid choices (with display labels and which extra field each needs) from
    /// GET api/RetrospectiveRuleAction/tax-calculation-modes.
    /// </summary>
    [Required(ErrorMessage = "RetrospectiveRuleAction_TaxCalculationMode_Required")]
    [StringLength(30, ErrorMessage = "RetrospectiveRuleAction_TaxCalculationMode_MaxLen_30")]
    public string TaxCalculationMode { get; set; } = "SINGLE";

    public decimal TaxMultiplier { get; set; } = 1.00m;

    public int? SplitStartEvidenceTypeId { get; set; }
    public int? SplitEndEvidenceTypeId { get; set; }
    public decimal? SplitMultiplier { get; set; }
    public decimal? AfterSplitMultiplier { get; set; }
}

public class SaveRetrospectivePenaltyRuleDto
{
    public bool IsPenaltyApplicable { get; set; }

    /// <summary>
    /// Get valid choices (with display labels and which extra field each needs) from
    /// GET api/RetrospectivePenaltyRule/penalty-modes.
    /// </summary>
    [Required(ErrorMessage = "RetrospectivePenaltyRule_PenaltyMode_Required")]
    [StringLength(50, ErrorMessage = "RetrospectivePenaltyRule_PenaltyMode_MaxLen_50")]
    public string PenaltyMode { get; set; } = "NONE";

    [Range(0, 999, ErrorMessage = "RetrospectivePenaltyRule_PenaltyPercent_Invalid")]
    public decimal? PenaltyPercent { get; set; }

    /// <summary>
    /// Only used when PenaltyMode = DATE_VALIDATION. Get valid choices from
    /// GET api/RetrospectivePenaltyRule/penalty-date-source-types.
    /// </summary>
    [StringLength(30, ErrorMessage = "RetrospectivePenaltyRule_PenaltyDateSourceType_MaxLen_30")]
    public string? PenaltyDateSourceType { get; set; }

    public int? PenaltyDateEvidenceTypeId { get; set; }

    /// <summary>
    /// Only used when PenaltyMode = DATE_VALIDATION. Get valid choices from
    /// GET api/RetrospectivePenaltyRule/penalty-date-conditions.
    /// </summary>
    [StringLength(30, ErrorMessage = "RetrospectivePenaltyRule_PenaltyDateCondition_MaxLen_30")]
    public string? PenaltyDateCondition { get; set; }

    public DateTime? CompareDate { get; set; }
    public DateTime? CompareDateTo { get; set; }

    [StringLength(50, ErrorMessage = "RetrospectivePenaltyRule_ElseAction_MaxLen_50")]
    public string? ElseAction { get; set; }

    public bool RequiresManualReview { get; set; }

    [StringLength(500, ErrorMessage = "RetrospectivePenaltyRule_Remarks_MaxLen_500")]
    public string? Remarks { get; set; }
}
