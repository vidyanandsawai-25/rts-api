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
        public int ServiceId { get; set; }
        public List<CreateRTSRuleDto> Rules { get; set; } = new();
    }

    public class CreateRTSRuleDto
    {
        public string? RuleDescription { get; set; }
        public int? ConditionFieldDefinitionId { get; set; }
        public string? ConditionField { get; set; }
        public string? ComparisonOperator { get; set; }
        public string? ExpectedValue { get; set; }
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
        public int ServiceId { get; set; }
        public List<UpdateRTSRuleDto> Rules { get; set; } = new();
    }
    public class UpdateRTSRuleDto
    {
        public int? Id { get; set; }
        public string? RuleDescription { get; set; }
        public int? ConditionFieldDefinitionId { get; set; }
        public string? ConditionField { get; set; }
        public string? ComparisonOperator { get; set; }
        public string? ExpectedValue { get; set; }
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
        public List<RTSRuleMasterDto> Rules { get; set; } = new();
    }


