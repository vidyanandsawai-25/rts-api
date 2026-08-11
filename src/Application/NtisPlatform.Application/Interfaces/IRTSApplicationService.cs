using NtisPlatform.Application.DTOs.RTSApplication;
using NtisPlatform.Application.DTOs.RTSFieldValue;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IRTSApplicationService: ICommonCrudService<RTSApplicationDetailsEntity, RTSApplicationDetailsDto, CreateRTSApplicationDetailsDto, UpdateRTSFieldValueDto, RTSApplicationQueryParameters, int>
{
    Task<PagedResult<RTSApplicationDashboardResponseDto>> GetAllDashboardApplicationAsync(RTSApplicationQueryParameters queryParameters,CancellationToken cancellationToken = default);
}
