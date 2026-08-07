using System.Collections.Generic;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using NtisPlatform.Core.Entities.Rules;

namespace NtisPlatform.Application.Interfaces.Rules
{
    /// <summary>
    /// Flattens a detail-scoped <see cref="PropertyCalculationContext"/> into the flat
    /// <c>string → object</c> dictionary consumed by rule/condition evaluators.
    /// </summary>
    public interface IPropertyFieldFlattenerService
    {
        /// <summary>
        /// Assembles the flat key/value dictionary a rules/condition engine evaluates against.
        /// </summary>
        /// <param name="context">The detail-scoped property calculation context (already
        /// resolved via <see cref="PropertyCalculationContext.CloneForDetail"/>).</param>
        /// <param name="activeFields">Active rules-field DB configuration — final authority on
        /// key-to-value resolution.</param>
        Dictionary<string, object> Flatten(PropertyCalculationContext context, List<RulesFieldEntity> activeFields);
    }
}
