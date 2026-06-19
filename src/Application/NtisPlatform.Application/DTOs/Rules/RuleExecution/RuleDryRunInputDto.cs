namespace NtisPlatform.Application.DTOs.Rules.RuleExecution
{
    /// <summary>
    /// Input payload for the dry-run rule execution endpoint.
    /// Identical to <see cref="RuleExecutionInputDto"/> but adds an optional
    /// <see cref="RuleJson"/> field so callers can test a rule JSON directly
    /// without saving it to the database first.
    /// </summary>
    public class RuleDryRunInputDto
    {
        /// <summary>
        /// Rule category to load rules from DB (e.g. "ARV", "ALV").
        /// Required unless <see cref="RuleJson"/> is provided.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Flat property bag passed as the lambda input to the rules engine.
        /// Example: { "FloorId": 65, "TypeOfUseGroupId": 3, "Rate": 1000.0 }
        /// </summary>
        public Dictionary<string, object> Input { get; set; } = new();

        /// <summary>
        /// Optional explicit base rate to use for all effect calculations.
        /// When provided, overrides the rate resolved from the <see cref="Input"/> dictionary.
        ///
        /// <para>
        /// Use this when you want to say "calculate against a rate of 1000"
        /// without needing to know which input key the rule's ParameterCode maps to.
        /// </para>
        ///
        /// <para>
        /// Example: set <c>BaseValue = 1000</c> and the dry-run response will show
        /// <c>baseRate = 1000</c> and <c>computedValue = 600</c> for a 40% decrease rule.
        /// </para>
        /// </summary>
        public decimal? BaseValue { get; set; }

        /// <summary>
        /// Optional raw RuleJson to test directly — bypasses the database entirely.
        /// Use this to validate a new rule JSON before saving it.
        /// Must be a valid RuleJson object (same format as RuleEngineMaster.RuleJson).
        /// If omitted, rules are loaded from the DB for the given <see cref="Category"/>.
        /// </summary>
        public string? RuleJson { get; set; }
    }
}
