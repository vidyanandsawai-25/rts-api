namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents a rule exclusion relationship: when AppliedRule matches and executes,
/// SkipRule should be excluded from execution for that evaluation cycle.
/// </summary>
public class RuleExclusionEntity : BaseEntity
{
    /// <summary>
    /// The rule that, when matched/applied, triggers exclusion of another rule.
    /// </summary>
    public int AppliedRuleId { get; set; }

    /// <summary>
    /// The rule that should be skipped/excluded when AppliedRule is matched.
    /// </summary>
    public int SkipRuleId { get; set; }

    /// <summary>
    /// Optional description explaining why this exclusion exists.
    /// Example: "Heritage property exemption overrides standard depreciation"
    /// </summary>
    public string? Reason { get; set; }

    // Navigation properties
    /// <summary>The rule that triggers the exclusion</summary>
    public virtual RuleEngineEntity AppliedRule { get; set; } = null!;

    /// <summary>The rule that gets skipped</summary>
    public virtual RuleEngineEntity SkipRule { get; set; } = null!;
}
