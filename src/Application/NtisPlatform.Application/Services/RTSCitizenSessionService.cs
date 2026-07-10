using AutoMapper;
using NtisPlatform.Application.DTOs.RTSCitizenSession;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RTSCitizenSessionService:BaseCommonCrudService<RTSCitizenSessionEntity, RTSCitizenSessionDto, CreateRTSCitizenSessionDto, UpdateRTSCitizenSessionDto, RTSCitizenSessionQueryParameters, int>, IRTSCitizenSessionService
{
    public RTSCitizenSessionService(IRepository<RTSCitizenSessionEntity, int> repository,IMapper mapper,IUnitOfWork unitOfWork):base(repository, unitOfWork, mapper)
    {
    }
}
