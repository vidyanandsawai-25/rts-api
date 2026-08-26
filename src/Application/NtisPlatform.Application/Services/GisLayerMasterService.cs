using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.GIS;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class GisLayerMasterService : BaseCommonCrudService<
    GisLayerMasterEntity, 
    GisLayerMasterDto, 
    CreateGisLayerMasterDto, 
    UpdateGisLayerMasterDto, 
    GisLayerMasterQueryParameters, 
    int>, IGisLayerMasterService
{
    public GisLayerMasterService(
        IRepository<GisLayerMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
