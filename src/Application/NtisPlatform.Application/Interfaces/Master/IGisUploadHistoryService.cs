using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.GIS;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IGisUploadHistoryService : ICommonCrudService<
    GisUploadHistoryEntity, 
    GisUploadHistoryDto, 
    CreateGisUploadHistoryDto, 
    UpdateGisUploadHistoryDto, 
    GisUploadHistoryQueryParameters, 
    int>
{
}
