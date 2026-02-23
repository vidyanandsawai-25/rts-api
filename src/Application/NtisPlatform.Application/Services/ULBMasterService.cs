using AutoMapper;
using NtisPlatform.Application.DTOs.Master.ULBMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for ULB Master CRUD operations
/// </summary>
public class ULBMasterService : BaseCommonCrudService<ULBMasterEntity, ULBMasterDto, CreateULBMasterDto, UpdateULBMasterDto, ULBMasterQueryParameters, int>, IULBMasterService
{
    public ULBMasterService(
        IRepository<ULBMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
