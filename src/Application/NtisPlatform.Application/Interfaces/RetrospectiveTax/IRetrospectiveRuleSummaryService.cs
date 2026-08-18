using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleSummary;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Application.Interfaces.RetrospectiveTax;

public interface IRetrospectiveRuleSummaryService : ICommonCrudService<RetrospectiveRuleSummaryEntity, RetrospectiveRuleSummaryDto, CreateRetrospectiveRuleSummaryDto, UpdateRetrospectiveRuleSummaryDto, RetrospectiveRuleSummaryQueryParameters, int>
{
    /// <summary>
    /// Everything the "Rule Summary" screen needs for one rule in a single call: the rule's code
    /// (for the badge) plus its active When/Tax/Penalty summary lines. Returns null when the rule
    /// doesn't exist or has no active summary row yet.
    /// </summary>
    Task<RetrospectiveRuleSummaryViewDto?> GetForRuleAsync(int ruleId, CancellationToken cancellationToken = default);
}
