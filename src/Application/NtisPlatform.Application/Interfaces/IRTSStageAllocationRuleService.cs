using NtisPlatform.Application.DTOs.RTSRuleMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IRTSStageAllocationRuleService
{
    Task ValidateAsync(int serviceId, int? userId, string? actionValue, string? conditionMatchType,
        IReadOnlyList<RTSRuleConditionDto> conditions, CancellationToken cancellationToken = default);

    Task<int> ResolveOfficerAsync(int serviceId, int approvalFlowId, int stageId, int defaultUserId,
        IReadOnlyDictionary<int, string> values, CancellationToken cancellationToken = default);

    IReadOnlyDictionary<int, string> NormalizeValues(IEnumerable<RTSFieldValueEntity> values);
}
