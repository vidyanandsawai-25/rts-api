using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.GIS;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IGisDepartmentFilterMappingService : ICommonCrudService<
    GisDepartmentFilterMappingEntity, 
    GisDepartmentFilterMappingDto, 
    CreateGisDepartmentFilterMappingDto, 
    UpdateGisDepartmentFilterMappingDto, 
    GisDepartmentFilterMappingQueryParameters, 
    int>
{
}
