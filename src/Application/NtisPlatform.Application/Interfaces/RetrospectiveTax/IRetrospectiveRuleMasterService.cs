using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleMaster;
using NtisPlatform.Application.DTOs.Range;
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

    /// <summary>
    /// "Save" button on the Rule Builder screen: upserts the rule header, both evidence
    /// checkbox panels, the date condition, the retrospective tax action and the penalty rule in
    /// one transaction, then returns the full detail. Pass request.Id = null to create a new
    /// (Draft) rule, or an existing rule's Id to update it. Returns null if request.Id is set but
    /// no such rule exists.
    /// </summary>
    Task<RetrospectiveRuleDetailDto?> SaveAsync(SaveRetrospectiveRuleDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Narrower overload the controller's no-transformer Range endpoint dynamically dispatches
    /// to (see CrudControllerExtensions.ExecuteCreateFromRange). Without this, the dynamic call
    /// finds no matching method on the base 3-arg CreateFromRangeAsync and throws at runtime.
    /// </summary>
    Task<RangeResult<RetrospectiveRuleMasterDto>> CreateFromRangeAsync(RangeCreateRequest<CreateRetrospectiveRuleMasterDto> request, CancellationToken cancellationToken = default);
}
