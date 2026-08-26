using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.GIS;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IGisKpiMasterService : ICommonCrudService<
    GisKpiMasterEntity, 
    GisKpiMasterDto, 
    CreateGisKpiMasterDto, 
    UpdateGisKpiMasterDto, 
    GisKpiMasterQueryParameters, 
    int>
{
}
