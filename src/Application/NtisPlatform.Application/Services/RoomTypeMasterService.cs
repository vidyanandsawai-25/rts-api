using AutoMapper;
using NtisPlatform.Application.DTOs.Master.RoomTypeMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RoomTypeMasterService : BaseCommonCrudService<RoomTypeMasterEntity, RoomTypeMasterDto, CreateRoomTypeMasterDto, UpdateRoomTypeMasterDto, RoomTypeMasterQueryParameters, int>,
      IRoomTypeMasterService
{
    public RoomTypeMasterService(
        IRepository<RoomTypeMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
