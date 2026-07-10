using AutoMapper;
using NtisPlatform.Application.DTOs.Master.RTSServiceMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RTSServiceService:BaseCommonCrudService<RTSServiceEntity, RTSServiceDto, CreateRTSServiceDto, UpdateRTSServiceDto, RTSServiceQueryParameters, int>, IRTSServiceService
{
    public RTSServiceService(IRepository<RTSServiceEntity ,int> repository,IUnitOfWork unitOfWork,IMapper mapper):base(repository,unitOfWork,mapper)
    {
    }
}

