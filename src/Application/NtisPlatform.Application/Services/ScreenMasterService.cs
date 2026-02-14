using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for ScreenMaster CRUD operations
/// </summary>
public class ScreenMasterService : BaseCommonCrudService<ScreenMasterEntity, ScreenMasterDto, CreateScreenMasterDto, UpdateScreenMasterDto, ScreenMasterQueryParameters, int>, IScreenMasterService
{
    public ScreenMasterService(
        IRepository<ScreenMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
