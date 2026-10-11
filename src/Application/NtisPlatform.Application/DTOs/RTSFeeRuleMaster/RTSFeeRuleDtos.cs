using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.RTSFeeRuleMaster;

public sealed class RTSFeeRuleConditionDto
{
    public int Id { get; set; }
    [Range(1, int.MaxValue)] public int FieldDefinitionId { get; set; }
    [Required] public string ComparisonOperator { get; set; } = string.Empty;
    [Required] public string ExpectedValue { get; set; } = string.Empty;
    public int ConditionOrder { get; set; }
}

public sealed class RTSFeeRuleDto
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public string RuleDescription { get; set; } = string.Empty;
    public string RuleActionType { get; set; } = string.Empty;
    public string ConditionMatchType { get; set; } = "ALL";
    public string CalculationType { get; set; } = string.Empty;
    public decimal RateAmount { get; set; }
    public int? QuantityFieldDefinitionId { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public List<RTSFeeRuleConditionDto> Conditions { get; set; } = [];
}

public sealed class SaveRTSFeeRuleRequestDto
{
    [Range(1, int.MaxValue)] public int ServiceId { get; set; }
    [Required, StringLength(200)] public string RuleDescription { get; set; } = string.Empty;
    [Required] public string RuleActionType { get; set; } = string.Empty;
    [Required] public string ConditionMatchType { get; set; } = "ALL";
    [Required] public string CalculationType { get; set; } = string.Empty;
    [Range(0, double.MaxValue)] public decimal RateAmount { get; set; }
    public int? QuantityFieldDefinitionId { get; set; }
    [Range(1, int.MaxValue)] public int Priority { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public int? UpdatedBy { get; set; }
    [MinLength(1)] public List<RTSFeeRuleConditionDto> Conditions { get; set; } = [];
}

public sealed class RTSFeeRuleServiceCardDto
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int RuleCount { get; set; }
    public int ActiveRuleCount { get; set; }
}

public sealed class RTSFeeRuleServiceCardQueryParameters : BaseQueryParameters
{
    [Range(1, int.MaxValue)] public int? DepartmentId { get; set; }
    [Range(1, int.MaxValue)] public int? ServiceId { get; set; }
    public bool? IsActive { get; set; }
}
