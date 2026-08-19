using AutoMapper;
using NtisPlatform.Application.DTOs.Master.VillageMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Master;

public class VillageMasterService : BaseCommonCrudService<VillageMasterEntity, VillageMasterDtos, CreateVillageMasterDto, UpdateVillageMasterDto, VillageQueryParameters, int>, IVillageMasterService
{
    public VillageMasterService(
        IRepository<VillageMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
