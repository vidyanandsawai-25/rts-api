using NtisPlatform.Application.DTOs.Master.RTSDepartmentMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IRTSDepartmentService : ICommonCrudService<RTSDepartmentEntity, RTSDepartmentDto, CreateRTSDepartmentDto, UpdateRTSDepartmentDto, RTSDepartmentQueryParameters, int>
{
}

