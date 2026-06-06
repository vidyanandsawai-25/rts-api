using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces;

public interface IFloorGroupService : ICommonCrudService<FloorGroupMasterEntity, FloorGroupDto, CreateFloorGroupDto, UpdateFloorGroupDto, FloorGroupQueryParameters, int>
{
}
