using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Application.Interfaces.RetrospectiveTax;

public interface IRetrospectiveRuleEvidenceConditionService : ICommonCrudService<RetrospectiveRuleEvidenceConditionEntity, RetrospectiveRuleEvidenceConditionDto, CreateRetrospectiveRuleEvidenceConditionDto, UpdateRetrospectiveRuleEvidenceConditionDto, RetrospectiveRuleEvidenceConditionQueryParameters, int>
{
    /// <summary>
    /// Returns every active evidence type with its current Available/Unavailable selection for
    /// this rule (or null if unselected in both panels) — everything the "Available evidence" /
    /// "Unavailable evidence" checkbox screen needs in one call.
    /// </summary>
    Task<List<RetrospectiveRuleEvidenceConditionStateDto>> GetEvidenceStateForRuleAsync(
        int ruleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces both checkbox panels for this rule in one transaction: upserts an AVAILABLE/
    /// UNAVAILABLE condition row for every id in the two lists, and deactivates any existing
    /// condition row for this rule whose evidence type is in neither list.
    /// </summary>
    Task<List<RetrospectiveRuleEvidenceConditionStateDto>> SetEvidenceStateForRuleAsync(
        int ruleId, SetRetrospectiveRuleEvidenceConditionStateDto request, CancellationToken cancellationToken = default);
}
