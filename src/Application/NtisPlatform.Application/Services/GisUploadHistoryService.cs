using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.GIS;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class GisUploadHistoryService : BaseCommonCrudService<
    GisUploadHistoryEntity, 
    GisUploadHistoryDto, 
    CreateGisUploadHistoryDto, 
    UpdateGisUploadHistoryDto, 
    GisUploadHistoryQueryParameters, 
    int>, IGisUploadHistoryService
{
    public GisUploadHistoryService(
        IRepository<GisUploadHistoryEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
