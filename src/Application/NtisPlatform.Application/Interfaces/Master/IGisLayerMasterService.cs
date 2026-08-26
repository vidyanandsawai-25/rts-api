using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.GIS;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IGisLayerMasterService : ICommonCrudService<
    GisLayerMasterEntity, 
    GisLayerMasterDto, 
    CreateGisLayerMasterDto, 
    UpdateGisLayerMasterDto, 
    GisLayerMasterQueryParameters, 
    int>
{
}
