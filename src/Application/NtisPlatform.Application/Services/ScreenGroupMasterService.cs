using AutoMapper;
using NtisPlatform.Application.DTOs.Master.ScreenGroupMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for ScreenGroupMaster CRUD operations
/// </summary>
public class ScreenGroupMasterService : BaseCommonCrudService<ScreenGroupMasterEntity, ScreenGroupMasterDto, CreateScreenGroupMasterDto, UpdateScreenGroupMasterDto, ScreenGroupMasterQueryParameters, int>, IScreenGroupMasterService
{
    public ScreenGroupMasterService(
        IRepository<ScreenGroupMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
