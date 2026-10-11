namespace NtisPlatform.Core.Entities;

public class RTSRuleMasterEntity : BaseEntity
{
    public int ServiceId { get; set; }
    public string RuleDescription { get; set; } = null!;
    public string RuleActionType { get; set; } = null!;
    public decimal? RateAmount { get; set; }
    public int? UserId { get; set; }
    public int Priority { get; set; } 
    public string? RuleGroup { get; set; }
    public string? ActionValue { get; set; }
    public bool IsCumulative { get; set; }
    public string? ConditionMatchType { get; set; }
    public string? CalculationType { get; set; }
    public int? QuantityFieldDefinitionId { get; set; }
    public virtual List<RTSRuleConditionEntity> Conditions { get; set; } = [];

}
