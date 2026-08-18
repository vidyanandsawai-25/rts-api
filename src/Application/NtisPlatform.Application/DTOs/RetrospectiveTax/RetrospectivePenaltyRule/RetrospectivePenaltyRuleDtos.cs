using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectivePenaltyRule;

public class RetrospectivePenaltyRuleDto : BaseDtos
{
    public int RuleId { get; set; }
    public bool IsPenaltyApplicable { get; set; }
    public string PenaltyMode { get; set; } = string.Empty;
    public decimal? PenaltyPercent { get; set; }
    public string? PenaltyDateSourceType { get; set; }
    public int? PenaltyDateEvidenceTypeId { get; set; }
    public string? PenaltyDateCondition { get; set; }
    public DateTime? CompareDate { get; set; }
    public DateTime? CompareDateTo { get; set; }
    public string? ElseAction { get; set; }
    public bool RequiresManualReview { get; set; }
    public string? Remarks { get; set; }
}

public class CreateRetrospectivePenaltyRuleDto : CreateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "RetrospectivePenaltyRule_RuleId_Invalid")]
    public int RuleId { get; set; }

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

public class UpdateRetrospectivePenaltyRuleDto : UpdateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "RetrospectivePenaltyRule_RuleId_Invalid")]
    public int RuleId { get; set; }

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
