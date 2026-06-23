namespace NtisPlatform.Application.DTOs.Rules.RuleExecution
{
    /// <summary>
    /// Result of executing a single matched rule against the input.
    /// </summary>
    public class RuleExecutionResultDto
    {
        /// <summary>Rule code identifier (e.g. RULE-20260528-0001).</summary>
        public string RuleCode { get; set; } = string.Empty;

        /// <summary>Human-readable rule name (e.g. "Basement Rate Adjustment A").</summary>
        public string RuleName { get; set; } = string.Empty;

        /// <summary>Effect type from OnSuccess Context (e.g. "Decrease %", "Multiply", "Override").</summary>
        public string EffectType { get; set; } = string.Empty;

        /// <summary>
        /// Numeric value of the effect magnitude (e.g. 40 for "Decrease % by 40").
        /// Parsed from Context.value (which is stored as a string in JSON).
        /// </summary>
        public decimal EffectValue { get; set; }

        /// <summary>
        /// The base value that was used for computation (the Rate passed in via input).
        /// </summary>
        public decimal BaseRate { get; set; }

        /// <summary>
        /// The final adjusted rate after applying the effect.
        /// e.g. for Decrease % 40 on Rate 1000 → ComputedRate = 600.
        /// </summary>
        public decimal ComputedRate { get; set; }

        /// <summary>
        /// The expression string stored in OnSuccess.Context.Expression — for audit/display only.
        /// e.g. "input.Rate * (1 - 40 / 100)"
        /// </summary>
        public string Expression { get; set; } = string.Empty;

        /// <summary>Full OnSuccess.Context dictionary for transparency and future extensibility.</summary>
        public Dictionary<string, string> Context { get; set; } = new();

        /// <summary>
        /// Indicates whether this rule should stop further rule processing.
        /// When true, no more rules will be evaluated after this one is applied.
        /// </summary>
        public bool StopProcessing { get; set; }

        public int? RuleScopeId { get; set; }
        public string? RuleScopeName { get; set; }
    }
}
