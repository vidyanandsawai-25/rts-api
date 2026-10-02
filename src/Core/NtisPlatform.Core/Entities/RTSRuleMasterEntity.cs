namespace NtisPlatform.Core.Entities;

public class RTSRuleMasterEntity : BaseEntity
{
    public int ServiceId { get; set; }
    public string RuleDescription { get; set; } = null!;
    public string ConditionField { get; set; } = null!;
    public string ComparisonOperator { get; set; } = null!;
    public string ExpectedValue { get; set; } = null!;
    public string RuleActionType { get; set; } = null!;
    public decimal? RateAmount { get; set; }
    public int? UserId { get; set; }
    public int Priority { get; set; } = 1;
    public string? RuleGroup { get; set; }
    public string? ActionValue { get; set; }
    public bool IsCumulative { get; set; }
    public int? ConditionFieldDefinitionId { get; set; }

}
