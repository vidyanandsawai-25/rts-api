using System.Collections.Generic;
using NtisPlatform.Application.DTOs.Rules.ConditionEvaluation;

namespace NtisPlatform.Application.Interfaces.Rules
{
    /// <summary>
    /// Evaluates a flat condition list (Dynamic Tax Register's CONDITION_BASED rows — see
    /// <c>TaxConditionRuleEntity.ConditionsJson</c>) against a resolved field-value dictionary.
    /// Each condition (after the first) joins the running result via its own AND/OR, folded
    /// strictly left-to-right — no parentheses or operator precedence.
    ///
    /// <para>
    /// Deliberately independent of the MS RulesEngine/System.Linq.Dynamic.Core pipeline used by
    /// <see cref="IRuleExecutionService"/> — this flat, non-nested shape doesn't need
    /// expression-string compilation, so this is a stateless, dependency-free comparison switch
    /// instead.
    /// </para>
    /// </summary>
    public interface IConditionRuleEvaluator
    {
        /// <summary>
        /// Evaluates every condition in <paramref name="conditionsJson"/> left-to-right (an empty
        /// or null/blank list is vacuously true, i.e. an "always matches" catch-all row) against
        /// <paramref name="fieldValues"/>. Never throws — malformed JSON or an unresolved field
        /// simply yields a non-match for that condition, with the reason captured in the
        /// returned trace.
        /// </summary>
        ConditionEvaluationResult Evaluate(string? conditionsJson, IReadOnlyDictionary<string, object> fieldValues);

        /// <summary>
        /// Resolves a single field to a number, using the SAME key convention and numeric coercion
        /// as <see cref="Evaluate"/> — so a field usable in a condition is usable as an arithmetic
        /// input, and the two can never drift apart.
        ///
        /// <para>
        /// Returns false (with <paramref name="value"/> = 0) when the field is absent from
        /// <paramref name="fieldValues"/> or its value is not numeric. Callers must treat that as
        /// "unresolved" and report it rather than silently computing with zero. Never throws.
        /// </para>
        /// </summary>
        bool TryResolveNumericField(string? fieldId, IReadOnlyDictionary<string, object> fieldValues, out decimal value);
    }
}
