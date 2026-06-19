namespace NtisPlatform.Application.DTOs.Rules.RuleExecution
{
    /// <summary>
    /// Full dry-run trace returned by the dry-run endpoint.
    /// Includes both matched and unmatched rule evaluations, the resolved
    /// input snapshot, and a summary of the overall execution.
    /// </summary>
    public class RuleDryRunResultDto
    {
        /// <summary>Category that was evaluated.</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// The exact input dictionary that was passed to the rules engine,
        /// serialised to string for inspection.
        /// Keys are the same names used in rule expressions (e.g. "Floor", "Rate").
        /// </summary>
        public Dictionary<string, string> ResolvedInput { get; set; } = new();

        /// <summary>Total number of DB rule entities loaded for this category.</summary>
        public int TotalRulesLoaded { get; set; }

        /// <summary>Total number of individual sub-rules evaluated across all workflows.</summary>
        public int TotalSubRulesEvaluated { get; set; }

        /// <summary>Number of sub-rules whose condition matched (IsSuccess = true).</summary>
        public int MatchedCount { get; set; }

        /// <summary>Whether execution was halted early by a StopProcessing flag.</summary>
        public bool StoppedEarly { get; set; }

        /// <summary>
        /// Per-workflow evaluation results. One entry per RuleEngineEntity (DB row).
        /// Each entry contains individual sub-rule results (matched and unmatched).
        /// </summary>
        public List<RuleDryRunWorkflowResult> Workflows { get; set; } = new();
    }

    /// <summary>
    /// Dry-run trace for a single workflow (one RuleEngineEntity row from DB).
    /// </summary>
    public class RuleDryRunWorkflowResult
    {
        /// <summary>WorkflowName = RuleCode of the parent DB entity (or overridden by JSON "RuleName").</summary>
        public string WorkflowName { get; set; } = string.Empty;

        /// <summary>DB entity Id (0 if testing with raw RuleJson).</summary>
        public int RuleEntityId { get; set; }

        /// <summary>Entity-level Priority from DB.</summary>
        public int Priority { get; set; }

        /// <summary>Whether the entity-level StopProcessing flag is set.</summary>
        public bool EntityStopOnMatch { get; set; }

        /// <summary>
        /// Individual sub-rule evaluation results, in original JSON array order.
        /// </summary>
        public List<RuleDryRunSubRuleResult> SubRules { get; set; } = new();
    }

    /// <summary>
    /// Dry-run trace for a single sub-rule evaluation.
    /// </summary>
    public class RuleDryRunSubRuleResult
    {
        /// <summary>0-based position of this sub-rule in the "rules" JSON array.</summary>
        public int ArrayIndex { get; set; }

        /// <summary>Rule code / identifier (from "RuleCode" or "ruleName" in JSON).</summary>
        public string RuleCode { get; set; } = string.Empty;

        /// <summary>Human-readable label (from "errorMessage" / "description" in JSON).</summary>
        public string RuleName { get; set; } = string.Empty;

        /// <summary>The normalised expression that was evaluated (SQL AND/OR replaced with C# &&/||).</summary>
        public string Expression { get; set; } = string.Empty;

        /// <summary>Whether the rule's condition evaluated to true against the input.</summary>
        public bool IsMatch { get; set; }

        /// <summary>
        /// Reason the rule did not match, or "Matched" if it did.
        /// Populated from MS Rules Engine's ExceptionMessage or a descriptive label.
        /// </summary>
        public string MatchStatus { get; set; } = string.Empty;

        /// <summary>
        /// If matched: the effect that would be applied (effectType + value from Actions.OnSuccess.Context).
        /// Null if the rule did not match or has no effect configured.
        /// </summary>
        public RuleDryRunEffect? Effect { get; set; }

        /// <summary>Whether this sub-rule carries a per-sub-rule StopProcessing flag.</summary>
        public bool StopProcessing { get; set; }

        /// <summary>Whether this sub-rule was skipped (disabled or unsafe expression).</summary>
        public bool WasSkipped { get; set; }

        /// <summary>Reason for skipping, if applicable (e.g. "disabled", "unsafe expression").</summary>
        public string? SkipReason { get; set; }

        /// <summary>
        /// The base rate resolved from the input dictionary using the effect's ParameterCode.
        /// Null if the rule did not match or no base rate could be resolved.
        /// Example: if input has Rate=1000 and ParameterCode="input.Rate", this is 1000.
        /// </summary>
        public decimal? BaseRate { get; set; }

        /// <summary>
        /// The value computed after applying the effect to the BaseRate.
        /// Null if the rule did not match or no effect is configured.
        /// Example: BaseRate=1000, effectType="Decrease %", effectValue=40 → ComputedValue=600.
        /// </summary>
        public decimal? ComputedValue { get; set; }
    }

    /// <summary>
    /// Describes the effect that would be applied by a matched rule, for display in dry-run output.
    /// </summary>
    public class RuleDryRunEffect
    {
        /// <summary>Effect type (e.g. "Decrease %", "Multiply", "Override").</summary>
        public string EffectType { get; set; } = string.Empty;

        /// <summary>Numeric effect magnitude (e.g. 40 for 40% decrease).</summary>
        public decimal EffectValue { get; set; }

        /// <summary>The ParameterCode from Context (the input key used as base rate).</summary>
        public string ParameterCode { get; set; } = string.Empty;

        /// <summary>Full OnSuccess.Context key-value pairs for transparency.</summary>
        public Dictionary<string, string> Context { get; set; } = new();
    }
}
