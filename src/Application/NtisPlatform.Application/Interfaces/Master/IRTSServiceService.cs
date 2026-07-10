using NtisPlatform.Application.DTOs.Master.RTSServiceMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IRTSServiceService:ICommonCrudService<RTSServiceEntity, RTSServiceDto, CreateRTSServiceDto, UpdateRTSServiceDto, RTSServiceQueryParameters, int>
{
}
