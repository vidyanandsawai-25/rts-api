using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.GIS;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IGisDepartmentKpiMappingService : ICommonCrudService<
    GisDepartmentKpiMappingEntity, 
    GisDepartmentKpiMappingDto, 
    CreateGisDepartmentKpiMappingDto, 
    UpdateGisDepartmentKpiMappingDto, 
    GisDepartmentKpiMappingQueryParameters, 
    int>
{
}
