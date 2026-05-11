using NtisPlatform.Application.DTOs.Master.RoomTypeMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IRoomTypeMasterService
    : ICommonCrudService<RoomTypeMasterEntity, RoomTypeMasterDto, CreateRoomTypeMasterDto, UpdateRoomTypeMasterDto, RoomTypeMasterQueryParameters, int>
{
}
