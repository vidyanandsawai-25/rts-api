using NtisPlatform.Application.DTOs.RuleEngine;

namespace NtisPlatform.Application.Interfaces.RuleEngine
{
    /// <summary>
    /// Service for executing stored rules (from RuleEngineMaster) against a dynamic
    /// property tax input and returning the effect results for each matched rule.
    /// </summary>
    public interface IRuleExecutionService
    {
        /// <summary>
        /// Loads all enabled rules for <see cref="RuleExecutionInputDto.Category"/>,
        /// evaluates each rule's LambdaExpression against the provided input,
        /// and returns results for every rule whose condition matched.
        /// </summary>
        Task<List<RuleExecutionResultDto>> ExecuteAsync(
            RuleExecutionInputDto input,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns distinct active rule categories from RuleCategoryMaster,
        /// ordered by SortOrder, for use in frontend dropdowns.
        /// </summary>
        Task<List<RuleCategoryDto>> GetCategoriesAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// P0: Invalidates cached rules for a specific category or all categories.
        /// Call this method after creating, updating, or deleting rules to ensure
        /// the rule execution service picks up the latest rules from the database.
        /// </summary>
        /// <param name="category">The category to invalidate (null = clear all cache)</param>
        void InvalidateCache(string? category = null);
    }
}
