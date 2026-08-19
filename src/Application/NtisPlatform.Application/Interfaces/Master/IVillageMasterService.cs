using NtisPlatform.Application.DTOs.Master.VillageMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IVillageMasterService : ICommonCrudService<VillageMasterEntity, VillageMasterDtos, CreateVillageMasterDto, UpdateVillageMasterDto, VillageQueryParameters, int>
{
}
