using NtisPlatform.Application.DTOs.RTSRuleMaster;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IRTSRuleMasterService: ICommonCrudService<RTSRuleMasterEntity, RTSRuleMasterDto, CreateRTSRuleMasterRequestDto, UpdateRTSRuleMasterRequestDto, RTSRuleMasterQueryParameters, int>
{
    Task<RTSRuleMasterResponseDto> CreateRulesAsync(
        CreateRTSRuleMasterRequestDto createDto,
        CancellationToken cancellationToken = default);

    Task<RTSRuleMasterResponseDto> UpdateRulesAsync(int serviceId, UpdateRTSRuleMasterRequestDto updateDto, CancellationToken cancellationToken = default);

    Task<RTSFeeEvaluationResponseDto> EvaluateFeeAsync(
        RTSFeeEvaluationRequestDto request,
        CancellationToken cancellationToken = default);

}

public interface IRTSRuleServiceCardService
{
    Task<PagedResult<RTSRuleServiceCardDto>> GetCardsAsync(
        RTSRuleServiceCardQueryParameters query,
        CancellationToken ct = default);
}

public interface IRTSStageAllocationRuleService
{
    Task ValidateAsync(
        int serviceId,
        int? userId,
        string? actionValue,
        string? conditionMatchType,
        IReadOnlyList<RTSRuleConditionDto> conditions,
        CancellationToken cancellationToken = default);

    Task<int> ResolveOfficerAsync(
        int serviceId,
        int approvalFlowId,
        int stageId,
        int defaultUserId,
        IReadOnlyDictionary<int, string> values,
        CancellationToken cancellationToken = default);

    IReadOnlyDictionary<int, string> NormalizeValues(IEnumerable<RTSFieldValueEntity> values);
}
