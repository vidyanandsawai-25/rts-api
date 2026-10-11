using NtisPlatform.Application.DTOs.RTSFeeRuleMaster;
using NtisPlatform.Application.DTOs.RTSRuleMaster;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces;

public interface IRTSFeeRuleService
{
    Task<IReadOnlyList<RTSFeeRuleDto>> GetAsync(int serviceId, CancellationToken cancellationToken = default);
    Task<RTSFeeRuleDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<RTSFeeRuleDto> CreateAsync(SaveRTSFeeRuleRequestDto request, CancellationToken cancellationToken = default);
    Task<RTSFeeRuleDto> UpdateAsync(int id, SaveRTSFeeRuleRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<RTSFeeRuleServiceCardDto>> GetServiceCardsAsync(RTSFeeRuleServiceCardQueryParameters query, CancellationToken cancellationToken = default);
    Task<RTSFeeEvaluationResponseDto> EvaluateFeeAsync(RTSFeeEvaluationRequestDto request, CancellationToken cancellationToken = default);
}
