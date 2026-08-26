using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.GIS;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class GisKpiMasterService : BaseCommonCrudService<
    GisKpiMasterEntity, 
    GisKpiMasterDto, 
    CreateGisKpiMasterDto, 
    UpdateGisKpiMasterDto, 
    GisKpiMasterQueryParameters, 
    int>, IGisKpiMasterService
{
    public GisKpiMasterService(
        IRepository<GisKpiMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
