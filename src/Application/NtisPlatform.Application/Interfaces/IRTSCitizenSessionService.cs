using NtisPlatform.Application.DTOs.RTSCitizenSession;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IRTSCitizenSessionService:ICommonCrudService<RTSCitizenSessionEntity, RTSCitizenSessionDto, CreateRTSCitizenSessionDto, UpdateRTSCitizenSessionDto, RTSCitizenSessionQueryParameters, int>
{
}
