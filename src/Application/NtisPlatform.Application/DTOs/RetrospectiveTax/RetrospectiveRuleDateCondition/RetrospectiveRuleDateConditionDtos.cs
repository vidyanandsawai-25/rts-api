using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleDateCondition;

public class RetrospectiveRuleDateConditionDto : BaseDtos
{
    public int RuleId { get; set; }
    public string ComparatorCode { get; set; } = string.Empty;
    public int? LeftEvidenceTypeId { get; set; }
    public int? RightEvidenceTypeId { get; set; }
    public string? CompareOperator { get; set; }
    public DateTime? CompareDate { get; set; }
    public DateTime? CompareDateTo { get; set; }
    public int? CompareYears { get; set; }
}

public class CreateRetrospectiveRuleDateConditionDto : CreateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveRuleDateCondition_RuleId_Invalid")]
    public int RuleId { get; set; }

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

public class UpdateRetrospectiveRuleDateConditionDto : UpdateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "RetrospectiveRuleDateCondition_RuleId_Invalid")]
    public int RuleId { get; set; }

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
