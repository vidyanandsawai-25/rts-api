using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.GIS;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IGisFilterMasterService : ICommonCrudService<
    GisFilterMasterEntity, 
    GisFilterMasterDto, 
    CreateGisFilterMasterDto, 
    UpdateGisFilterMasterDto, 
    GisFilterMasterQueryParameters, 
    int>
{
}
