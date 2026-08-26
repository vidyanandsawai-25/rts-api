using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.GIS;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class GisCorporationConfigService : BaseCommonCrudService<
    GisCorporationConfigEntity, 
    GisCorporationConfigDto, 
    CreateGisCorporationConfigDto, 
    UpdateGisCorporationConfigDto, 
    GisCorporationConfigQueryParameters, 
    int>, IGisCorporationConfigService
{
    public GisCorporationConfigService(
        IRepository<GisCorporationConfigEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
