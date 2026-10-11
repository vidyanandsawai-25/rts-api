using NtisPlatform.Application.DTOs.RTSRuleMaster;
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
