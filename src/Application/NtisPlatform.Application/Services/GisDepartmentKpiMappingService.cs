using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.GIS;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class GisDepartmentKpiMappingService : BaseCommonCrudService<
    GisDepartmentKpiMappingEntity, 
    GisDepartmentKpiMappingDto, 
    CreateGisDepartmentKpiMappingDto, 
    UpdateGisDepartmentKpiMappingDto, 
    GisDepartmentKpiMappingQueryParameters, 
    int>, IGisDepartmentKpiMappingService
{
    public GisDepartmentKpiMappingService(
        IRepository<GisDepartmentKpiMappingEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
