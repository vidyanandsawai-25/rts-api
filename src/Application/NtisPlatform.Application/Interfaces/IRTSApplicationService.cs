using NtisPlatform.Application.DTOs.RTSApplication;
using NtisPlatform.Application.DTOs.RTSFieldValue;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IRTSApplicationService: ICommonCrudService<RTSApplicationDetailsEntity, RTSApplicationDetailsDto, CreateRTSApplicationDetailsDto, UpdateRTSFieldValueDto, RTSApplicationQueryParameters, int>
{
}
