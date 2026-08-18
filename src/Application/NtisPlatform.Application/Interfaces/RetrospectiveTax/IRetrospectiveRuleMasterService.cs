using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleMaster;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Application.Interfaces.RetrospectiveTax;

public interface IRetrospectiveRuleMasterService : ICommonCrudService<RetrospectiveRuleMasterEntity, RetrospectiveRuleMasterDto, CreateRetrospectiveRuleMasterDto, UpdateRetrospectiveRuleMasterDto, RetrospectiveRuleMasterQueryParameters, int>
{
    /// <summary>
    /// "Publish Rule" action: moves RuleStatus from Draft/Review/NeedsClarification to Active and
    /// writes a PUBLISH row to RetrospectiveRuleAuditLog. Returns null if the rule doesn't exist;
    /// throws a ValidationException (400) if the rule is already Active.
    /// </summary>
    Task<RetrospectiveRuleMasterDto?> PublishAsync(int id, PublishRetrospectiveRuleDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// "View" action for the Rule Library grid: everything about one rule (header + evidence
    /// conditions + date condition + action + penalty rule + summary) in a single call. Returns
    /// null if the rule doesn't exist.
    /// </summary>
    Task<RetrospectiveRuleDetailDto?> GetDetailAsync(int id, CancellationToken cancellationToken = default);
}
