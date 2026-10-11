namespace NtisPlatform.Core.Entities;

/// <summary>One ordered condition for a fee or user-allocation rule.</summary>
public class RTSRuleConditionEntity : BaseEntity
{
    public int RuleMasterId { get; set; }
    public int ConditionOrder { get; set; }
    public int FieldDefinitionId { get; set; }
    public string ComparisonOperator { get; set; } = string.Empty;
    public string ExpectedValue { get; set; } = string.Empty;
    public virtual RTSRuleMasterEntity? Rule { get; set; }
}
