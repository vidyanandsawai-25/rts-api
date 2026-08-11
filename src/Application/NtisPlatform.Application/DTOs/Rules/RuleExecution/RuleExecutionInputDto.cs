namespace NtisPlatform.Application.DTOs.Rules.RuleExecution
{
    /// <summary>
    /// Input payload for executing rules against a tax calculation context.
    /// The <see cref="Category"/> maps to RuleEngineEntity.RuleCategory (e.g. "ARV", "ALV").
    /// The <see cref="Input"/> is a flat or nested property bag that is matched against
    /// the LambdaExpression stored in each rule's ruleJson (e.g. input.Floor, input.TypeOfUseGroup, input.Rate).
    /// </summary>
    public class RuleExecutionInputDto
    {
        /// <summary>Rule category to filter rules (e.g. "ARV", "ALV", "UAV").</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Optional PropertyRuleEvaluationMasterId to filter rules by evaluation parameter (e.g. Rate, Rent, Maintenance).</summary>
        public int? PropertyRuleEvaluationMasterId { get; set; }

        /// <summary>
        /// Flat or nested property bag passed as the lambda input.
        /// Example: { "Floor": 65, "TypeOfUseGroup": 1, "Rate": 1000.0 }
        /// Values can be int, double, decimal, string, or nested Dictionary&lt;string,object&gt;.
        /// </summary>
        public Dictionary<string, object> Input { get; set; } = new();
    }
}
