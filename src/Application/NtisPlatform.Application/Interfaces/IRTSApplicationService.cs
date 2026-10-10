using NtisPlatform.Application.DTOs.RTSApplication;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IRTSApplicationService: ICommonCrudService<RTSApplicationDetailsEntity, RTSApplicationDetailsDto, CreateRTSApplicationDetailsDto, UpdateRTSApplicationDetailsDto, RTSApplicationQueryParameters, int>
{
}
