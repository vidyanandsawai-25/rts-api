using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleAction;

public class RetrospectiveRuleActionDto : BaseDtos
{
    public int RuleId { get; set; }
    public string TaxStartMode { get; set; } = string.Empty;
    public int? StartEvidenceTypeId { get; set; }
    public int? OffsetMonths { get; set; }
    public string RetrospectiveLimitType { get; set; } = string.Empty;
    public int? MaximumYears { get; set; }
    public DateTime? CutoffDate { get; set; }
    public string TaxCalculationMode { get; set; } = string.Empty;
    public decimal TaxMultiplier { get; set; }
    public int? SplitStartEvidenceTypeId { get; set; }
    public int? SplitEndEvidenceTypeId { get; set; }
    public decimal? SplitMultiplier { get; set; }
    public decimal? AfterSplitMultiplier { get; set; }
}

public class CreateRetrospectiveRuleActionDto : CreateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveRuleAction_RuleId_Invalid")]
    public int RuleId { get; set; }

    /// <summary>
    /// Get valid choices (with display labels and which extra field each needs) from
    /// GET api/RetrospectiveRuleAction/tax-start-modes.
    /// </summary>
    [Required(ErrorMessage = "RetrospectiveRuleAction_TaxStartMode_Required")]
    [StringLength(50, ErrorMessage = "RetrospectiveRuleAction_TaxStartMode_MaxLen_50")]
    public string TaxStartMode { get; set; } = string.Empty;

    /// <summary>
    /// "Use date" field. Get valid choices from GET api/RetrospectiveRuleAction/use-date-options
    /// (DB-driven: every active evidence type + a synthetic "Cutoff date" option). Leave null and
    /// set TaxStartMode = "FIXED_CUTOFF" when the user picks "Cutoff date".
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

public class UpdateRetrospectiveRuleActionDto : UpdateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveRuleAction_RuleId_Invalid")]
    public int RuleId { get; set; }

    /// <summary>
    /// Get valid choices (with display labels and which extra field each needs) from
    /// GET api/RetrospectiveRuleAction/tax-start-modes.
    /// </summary>
    [Required(ErrorMessage = "RetrospectiveRuleAction_TaxStartMode_Required")]
    [StringLength(50, ErrorMessage = "RetrospectiveRuleAction_TaxStartMode_MaxLen_50")]
    public string TaxStartMode { get; set; } = string.Empty;

    /// <summary>
    /// "Use date" field. Get valid choices from GET api/RetrospectiveRuleAction/use-date-options
    /// (DB-driven: every active evidence type + a synthetic "Cutoff date" option). Leave null and
    /// set TaxStartMode = "FIXED_CUTOFF" when the user picks "Cutoff date".
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

    public decimal TaxMultiplier { get; set; }

    public int? SplitStartEvidenceTypeId { get; set; }
    public int? SplitEndEvidenceTypeId { get; set; }
    public decimal? SplitMultiplier { get; set; }
    public decimal? AfterSplitMultiplier { get; set; }
}
