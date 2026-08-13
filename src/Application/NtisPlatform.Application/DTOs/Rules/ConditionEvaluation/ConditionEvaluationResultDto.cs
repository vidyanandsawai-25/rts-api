using System.Collections.Generic;

namespace NtisPlatform.Application.DTOs.Rules.ConditionEvaluation
{
    /// <summary>Result of evaluating one flat condition list (each item joined to the PREVIOUS
    /// one by its own AND/OR, evaluated strictly left-to-right — no parentheses/precedence)
    /// against a field-value dictionary — see <see cref="Interfaces.Rules.IConditionRuleEvaluator"/>.</summary>
    public class ConditionEvaluationResult
    {
        public bool IsMatch { get; set; }
        public List<ConditionItemEvaluationTrace> ConditionResults { get; set; } = new();
    }

    public class ConditionItemEvaluationTrace
    {
        public string FieldId { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        /// <summary>AND | OR — how this condition joins with the PREVIOUS one in the row
        /// (meaningless for the first condition, always reported as "AND").</summary>
        public string LogicalOperator { get; set; } = "AND";
        public object? ExpectedValue { get; set; }
        public object? ActualValue { get; set; }
        public bool FieldResolved { get; set; }
        public bool IsMatch { get; set; }
    }
}
