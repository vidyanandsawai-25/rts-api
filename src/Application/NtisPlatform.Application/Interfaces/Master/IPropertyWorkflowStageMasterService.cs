using NtisPlatform.Application.DTOs.Master.PropertyWorkflowStageMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces.Master;

/// <summary>
/// Service interface for PropertyWorkflowStageMaster CRUD operations
/// </summary>
public interface IPropertyWorkflowStageMasterService : ICommonCrudService<PropertyWorkflowStageMasterEntity, PropertyWorkflowStageMasterDto, CreatePropertyWorkflowStageMasterDto, UpdatePropertyWorkflowStageMasterDto, PropertyWorkflowStageMasterQueryParameters, int>
{
}
