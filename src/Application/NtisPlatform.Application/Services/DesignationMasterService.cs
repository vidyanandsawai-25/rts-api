using AutoMapper;
using NtisPlatform.Application.DTOs.Master.DesignationMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for DesignationMaster CRUD operations
/// </summary>
public class DesignationMasterService : BaseCommonCrudService<DesignationMasterEntity, DesignationMasterDto, CreateDesignationMasterDto, UpdateDesignationMasterDto, DesignationMasterQueryParameters, int>, IDesignationMasterService
{
    public DesignationMasterService(
        IRepository<DesignationMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
