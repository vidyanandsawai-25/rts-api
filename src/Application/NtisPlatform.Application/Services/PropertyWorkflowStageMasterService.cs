using AutoMapper;
using NtisPlatform.Application.DTOs.Master.PropertyWorkflowStageMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service for PropertyWorkflowStageMaster CRUD operations
/// </summary>
public class PropertyWorkflowStageMasterService : BaseCommonCrudService<PropertyWorkflowStageMasterEntity, PropertyWorkflowStageMasterDto, CreatePropertyWorkflowStageMasterDto, UpdatePropertyWorkflowStageMasterDto, PropertyWorkflowStageMasterQueryParameters, int>, IPropertyWorkflowStageMasterService
{
    public PropertyWorkflowStageMasterService(
        IRepository<PropertyWorkflowStageMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
