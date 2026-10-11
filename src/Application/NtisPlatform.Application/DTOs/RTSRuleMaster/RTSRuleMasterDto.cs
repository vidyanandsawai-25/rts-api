using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RTSRuleMaster;

public class RTSRuleMasterDto : BaseDtos
{
    public int ServiceId { get; set; }
    public string? RuleDescription { get; set; }
    public int? ConditionFieldDefinitionId { get; set; }
    public string? ConditionField { get; set; }
    public string? ComparisonOperator { get; set; }
    public string? ExpectedValue { get; set; }
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
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ServiceId must be greater than 0.")]
    public int ServiceId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one rule is required.")]
    public List<CreateRTSRuleDto> Rules { get; set; } = new();
}

public class CreateRTSRuleDto
{
    [Required(ErrorMessage = "RuleDescription is required.")]
    public string? RuleDescription { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "ConditionFieldDefinitionId must be greater than 0.")]
    public int? ConditionFieldDefinitionId { get; set; }

    [Required(ErrorMessage = "ConditionField is required.")]
    public string? ConditionField { get; set; }

    [Required(ErrorMessage = "ComparisonOperator is required.")]
    public string? ComparisonOperator { get; set; }

    [Required(ErrorMessage = "ExpectedValue is required.")]
    public string? ExpectedValue { get; set; }

    [Required(ErrorMessage = "RuleActionType is required.")]
    public string? RuleActionType { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Priority must be greater than 0.")]
    public int Priority { get; set; } = 1;

    public decimal? RateAmount { get; set; }
    public int? UserId { get; set; }
    public string? RuleGroup { get; set; }
    public string? ActionValue { get; set; }
    public bool IsCumulative { get; set; }
}

public class UpdateRTSRuleMasterRequestDto : UpdateBaseDtos
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ServiceId must be greater than 0.")]
    public int ServiceId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one rule is required for update.")]
    public List<UpdateRTSRuleDto> Rules { get; set; } = new();
}

public class UpdateRTSRuleDto
{
    [Required(ErrorMessage = "Rule Id is required for update.")]
    [Range(1, int.MaxValue, ErrorMessage = "Rule Id must be greater than 0.")]
    public int? Id { get; set; }

    [Required(ErrorMessage = "RuleDescription is required.")]
    public string? RuleDescription { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "ConditionFieldDefinitionId must be greater than 0.")]
    public int? ConditionFieldDefinitionId { get; set; }

    [Required(ErrorMessage = "ConditionField is required.")]
    public string? ConditionField { get; set; }

    [Required(ErrorMessage = "ComparisonOperator is required.")]
    public string? ComparisonOperator { get; set; }

    [Required(ErrorMessage = "ExpectedValue is required.")]
    public string? ExpectedValue { get; set; }

    [Required(ErrorMessage = "RuleActionType is required.")]
    public string? RuleActionType { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Priority must be greater than 0.")]
    public int Priority { get; set; } = 1;

    public decimal? RateAmount { get; set; }
    public int? UserId { get; set; }
    public string? RuleGroup { get; set; }
    public string? ActionValue { get; set; }
    public bool IsCumulative { get; set; }
    public bool IsActive { get; set; } = true;
}

public class RTSRuleMasterResponseDto
{
    public int ServiceId { get; set; }
    public int RuleCount { get; set; }
    public List<RTSRuleMasterDto> Rules { get; set; } = new();
}