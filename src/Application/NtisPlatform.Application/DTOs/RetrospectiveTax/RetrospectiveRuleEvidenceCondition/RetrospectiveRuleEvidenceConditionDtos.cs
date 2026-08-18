using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition;

public class RetrospectiveRuleEvidenceConditionDto : BaseDtos
{
    public int RuleId { get; set; }
    public int EvidenceTypeId { get; set; }
    public string EvidenceState { get; set; } = string.Empty;
}

public class CreateRetrospectiveRuleEvidenceConditionDto : CreateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveRuleEvidenceCondition_RuleId_Invalid")]
    public int RuleId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveRuleEvidenceCondition_EvidenceTypeId_Invalid")]
    public int EvidenceTypeId { get; set; }

    /// <summary>AVAILABLE / UNAVAILABLE</summary>
    [Required(ErrorMessage = "RetrospectiveRuleEvidenceCondition_EvidenceState_Required")]
    [StringLength(20, ErrorMessage = "RetrospectiveRuleEvidenceCondition_EvidenceState_MaxLen_20")]
    public string EvidenceState { get; set; } = string.Empty;
}

public class UpdateRetrospectiveRuleEvidenceConditionDto : UpdateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveRuleEvidenceCondition_RuleId_Invalid")]
    public int RuleId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveRuleEvidenceCondition_EvidenceTypeId_Invalid")]
    public int EvidenceTypeId { get; set; }

    [Required(ErrorMessage = "RetrospectiveRuleEvidenceCondition_EvidenceState_Required")]
    [StringLength(20, ErrorMessage = "RetrospectiveRuleEvidenceCondition_EvidenceState_MaxLen_20")]
    public string EvidenceState { get; set; } = string.Empty;
}
