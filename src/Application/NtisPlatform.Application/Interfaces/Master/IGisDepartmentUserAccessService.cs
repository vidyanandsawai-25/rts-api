using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.GIS;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IGisDepartmentUserAccessService : ICommonCrudService<
    GisDepartmentUserAccessEntity, 
    GisDepartmentUserAccessDto, 
    CreateGisDepartmentUserAccessDto, 
    UpdateGisDepartmentUserAccessDto, 
    GisDepartmentUserAccessQueryParameters, 
    int>
{
}
