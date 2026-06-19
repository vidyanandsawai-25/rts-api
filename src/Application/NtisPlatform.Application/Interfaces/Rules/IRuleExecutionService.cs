using NtisPlatform.Application.DTOs.Rules.RuleCategory;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;

namespace NtisPlatform.Application.Interfaces.Rules
{
    /// <summary>
    /// Evaluates stored rule policies (from <c>RuleEngineMaster</c>) against a dynamic
    /// property tax input and returns the effect result for every matched rule.
    /// </summary>
    public interface IRuleExecutionService
    {
        /// <summary>
        /// Loads all enabled rules for <see cref="RuleExecutionInputDto.Category"/>,
        /// evaluates each rule's condition against the provided input dictionary,
        /// and returns a result for every rule whose condition matched.
        ///
        /// <para>
        /// Rules are evaluated in ascending Priority order. When a rule's
        /// <c>StopProcessing</c> flag is set, execution halts immediately after
        /// that rule's effect is recorded.
        /// </para>
        /// </summary>
        Task<List<RuleExecutionResultDto>> ExecuteAsync(
            RuleExecutionInputDto input,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns all active rule categories from <c>RuleCategoryMaster</c>,
        /// ordered by <c>SortOrder</c>, for use in frontend dropdowns.
        /// </summary>
        Task<List<RuleCategoryDto>> GetCategoriesAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a full dry-run evaluation of rules for the given category and input,
        /// returning a detailed trace of every sub-rule — both matched and unmatched —
        /// without persisting any results.
        ///
        /// <para>
        /// If <see cref="RuleDryRunInputDto.RuleJson"/> is provided, that JSON is used
        /// directly instead of loading rules from the database, allowing rule authors
        /// to validate a new rule JSON before saving it.
        /// </para>
        /// </summary>
        Task<RuleDryRunResultDto> DryRunAsync(
            RuleDryRunInputDto input,
            CancellationToken cancellationToken = default);
    }
}
