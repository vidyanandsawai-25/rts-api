namespace NtisPlatform.Core.Entities;

/// <summary>
/// A node in a fee or user-allocation rule expression. Nodes are stored in one
/// table: a GROUP combines child nodes and a PREDICATE compares one field value.
/// </summary>
public class RTSRuleConditionEntity : BaseEntity
{
    public int RuleMasterId { get; set; }
    public int? ParentConditionId { get; set; }
    public int ConditionOrder { get; set; }
    public string ConditionType { get; set; } = "PREDICATE";
    public string? ConditionMatchType { get; set; }
    public int? FieldDefinitionId { get; set; }
    public string? ComparisonOperator { get; set; }
    public string? ExpectedValue { get; set; }
    public virtual RTSRuleMasterEntity? Rule { get; set; }
    public virtual RTSRuleConditionEntity? ParentCondition { get; set; }
    public virtual List<RTSRuleConditionEntity> ChildConditions { get; set; } = [];
}
