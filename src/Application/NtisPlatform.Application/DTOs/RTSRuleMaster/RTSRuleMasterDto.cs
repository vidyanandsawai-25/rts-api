using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.RTSRuleMaster;

/// <summary>A recursive group or predicate node in an RTS rule expression.</summary>
public class RTSRuleConditionDto
{
    public int? Id { get; set; }
    public int? ParentConditionId { get; set; }
    public string ConditionType { get; set; } = "PREDICATE";
    public string? ConditionMatchType { get; set; }
    public int? FieldDefinitionId { get; set; }
    public string? ComparisonOperator { get; set; }
    public string? ExpectedValue { get; set; }
    public int ConditionOrder { get; set; }
    public List<RTSRuleConditionDto> Children { get; set; } = [];
}

public class RTSRuleMasterDto : BaseDtos
{
    public int ServiceId { get; set; }
    public string? RuleDescription { get; set; }
    public string ConditionMatchType { get; set; } = "ALL";
    public List<RTSRuleConditionDto> Conditions { get; set; } = [];
    public string? RuleActionType { get; set; }
    public string? CalculationType { get; set; }
    public decimal? RateAmount { get; set; }
    public int? QuantityFieldDefinitionId { get; set; }
    public int? UserId { get; set; }
    public RTSRuleUserDetailsDto? UserDetails { get; set; }
    public int Priority { get; set; }
    public string? RuleGroup { get; set; }
    public string? ActionValue { get; set; }
    public bool IsCumulative { get; set; }
}

/// <summary>Safe officer identity returned only for USER_ALLOCATION rule displays.</summary>
public class RTSRuleUserDetailsDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
}

public class CreateRTSRuleMasterRequestDto : CreateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "ServiceId must be greater than 0.")]
    public int ServiceId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one rule is required.")]
    public List<CreateRTSRuleDto> Rules { get; set; } = [];
}

public class CreateRTSRuleDto
{
    public string? RuleDescription { get; set; }
    public string ConditionMatchType { get; set; } = "ALL";
    public List<RTSRuleConditionDto> Conditions { get; set; } = [];
    public string? RuleActionType { get; set; }
    public string? CalculationType { get; set; }
    public decimal? RateAmount { get; set; }
    public int? QuantityFieldDefinitionId { get; set; }
    public int? UserId { get; set; }
    public int Priority { get; set; } = 1;
    public string? RuleGroup { get; set; }
    public string? ActionValue { get; set; }
    public bool IsCumulative { get; set; }
}

public class UpdateRTSRuleMasterRequestDto : UpdateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "ServiceId must be greater than 0.")]
    public int ServiceId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one rule is required for update.")]
    public List<UpdateRTSRuleDto> Rules { get; set; } = [];
}

public class UpdateRTSRuleDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Rule Id must be greater than 0.")]
    public int? Id { get; set; }
    public string? RuleDescription { get; set; }
    public string ConditionMatchType { get; set; } = "ALL";
    public List<RTSRuleConditionDto> Conditions { get; set; } = [];
    public string? RuleActionType { get; set; }
    public string? CalculationType { get; set; }
    public decimal? RateAmount { get; set; }
    public int? QuantityFieldDefinitionId { get; set; }
    public int? UserId { get; set; }
    public int Priority { get; set; } = 1;
    public string? RuleGroup { get; set; }
    public string? ActionValue { get; set; }
    public bool IsCumulative { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Atomically replaces the display/evaluation order for one service rule group.</summary>
public class UpdateRTSRulePrioritiesRequestDto : UpdateBaseDtos
{
    [Range(1, int.MaxValue, ErrorMessage = "ServiceId must be greater than 0.")]
    public int ServiceId { get; set; }

    [Required]
    [RegularExpression("^(FEE|USER_ALLOCATION)$", ErrorMessage = "RuleGroup must be FEE or USER_ALLOCATION.")]
    public string RuleGroup { get; set; } = string.Empty;

    [Required]
    [MinLength(1, ErrorMessage = "At least one ordered rule is required.")]
    public List<RTSRulePriorityDto> Priorities { get; set; } = [];
}

/// <summary>Updates only the active state of a persisted RTS rule.</summary>
public class UpdateRTSRuleMasterStatusRequestDto : UpdateBaseDtos
{
    public bool IsActive { get; set; }
}

public class RTSRulePriorityDto
{
    [Range(1, int.MaxValue)]
    public int RuleId { get; set; }

    [Range(1, int.MaxValue)]
    public int Priority { get; set; }
}

public class RTSRuleMasterResponseDto
{
    public int ServiceId { get; set; }
    public int RuleCount { get; set; }
    public List<RTSRuleMasterDto> Rules { get; set; } = [];
}

public class RTSRuleServiceCardQueryParameters : BaseQueryParameters
{
    [Required]
    [StringLength(100)]
    public string RuleGroup { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int? DepartmentId { get; set; }

    [Range(1, int.MaxValue)]
    public int? ServiceId { get; set; }

    public bool? IsActive { get; set; }
}

public class RTSRuleServiceCardDto
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string RuleGroup { get; set; } = string.Empty;
    public int RuleCount { get; set; }
    public int ActiveRuleCount { get; set; }
}
