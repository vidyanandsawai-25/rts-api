using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.GIS;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class GisDepartmentFilterMappingService : BaseCommonCrudService<
    GisDepartmentFilterMappingEntity, 
    GisDepartmentFilterMappingDto, 
    CreateGisDepartmentFilterMappingDto, 
    UpdateGisDepartmentFilterMappingDto, 
    GisDepartmentFilterMappingQueryParameters, 
    int>, IGisDepartmentFilterMappingService
{
    public GisDepartmentFilterMappingService(
        IRepository<GisDepartmentFilterMappingEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
