using NtisPlatform.Application.DTOs.RTSRuleMaster;

namespace NtisPlatform.Application.Interfaces;

public interface IRtsFeeEvaluationRuleService
{
    Task ValidateRuleAsync(
        int serviceId,
        string? ruleActionType,
        string? conditionMatchType,
        string? calculationType,
        decimal? rateAmount,
        int? quantityFieldDefinitionId,
        IReadOnlyCollection<RTSRuleConditionDto> conditions,
        CancellationToken cancellationToken = default);

    Task<RTSFeeEvaluationResponseDto> EvaluateFeeAsync(RTSFeeEvaluationRequestDto request, CancellationToken cancellationToken = default);
}
