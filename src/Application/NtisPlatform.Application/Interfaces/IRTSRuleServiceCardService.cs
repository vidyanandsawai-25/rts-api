using NtisPlatform.Application.DTOs.RTSRuleMaster;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces;

public interface IRTSRuleServiceCardService
{
    Task<PagedResult<RTSRuleServiceCardDto>> GetCardsAsync(RTSRuleServiceCardQueryParameters query, CancellationToken ct = default);
}
