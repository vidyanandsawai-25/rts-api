using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RTSRuleMaster;

/// <summary>
/// One ordered field condition belonging to an RTS rule. Fee and user-allocation
/// rules both persist their conditions in RTS.RuleCondition.
/// </summary>
public class RTSRuleConditionDto
{
    public int Id { get; set; }
    public int FieldDefinitionId { get; set; }
    public string ComparisonOperator { get; set; } = string.Empty;
    public string ExpectedValue { get; set; } = string.Empty;
    public int ConditionOrder { get; set; }
}

public class RTSRuleMasterDto : BaseDtos
{
    public int ServiceId { get; set; }
    public string? RuleDescription { get; set; }
    public string ConditionMatchType { get; set; } = "ALL";
    public List<RTSRuleConditionDto> Conditions { get; set; } = [];
    public string? RuleActionType { get; set; }
    public decimal? RateAmount { get; set; }
    public int? UserId { get; set; }
    public int Priority { get; set; }
    public string? RuleGroup { get; set; }
    public string? ActionValue { get; set; }
    public bool IsCumulative { get; set; }
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
    public decimal? RateAmount { get; set; }
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
    public decimal? RateAmount { get; set; }
    public int? UserId { get; set; }
    public int Priority { get; set; } = 1;
    public string? RuleGroup { get; set; }
    public string? ActionValue { get; set; }
    public bool IsCumulative { get; set; }
    public bool IsActive { get; set; } = true;
}

public class RTSRuleMasterResponseDto
{
    public int ServiceId { get; set; }
    public int RuleCount { get; set; }
    public List<RTSRuleMasterDto> Rules { get; set; } = [];
}
