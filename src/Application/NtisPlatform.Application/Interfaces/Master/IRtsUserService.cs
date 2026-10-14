using NtisPlatform.Application.DTOs.RTSUser;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces.Master;

/// <summary>Read-only RTS user lookup without allocation payloads.</summary>
public interface IRtsUserService
{
    Task<PagedResult<RtsUserDto>> GetAllAsync(
        RtsUserQueryParameters queryParameters,
        CancellationToken cancellationToken = default);
}
