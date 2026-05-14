using NtisPlatform.Application.DTOs.Master.PropertyMapMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IPropertyMapMasterService : ICommonCrudService<PropertyMapMasterEntity, PropertyMapMasterDtos, CreatePropertyMapMasterDto, UpdatePropertyMapMasterDto, PropertyMapQueryParameters, int>
{
}