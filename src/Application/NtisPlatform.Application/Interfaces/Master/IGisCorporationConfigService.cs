using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.GIS;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IGisCorporationConfigService : ICommonCrudService<
    GisCorporationConfigEntity, 
    GisCorporationConfigDto, 
    CreateGisCorporationConfigDto, 
    UpdateGisCorporationConfigDto, 
    GisCorporationConfigQueryParameters, 
    int>
{
}
