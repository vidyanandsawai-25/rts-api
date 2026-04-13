using NtisPlatform.Application.DTOs.Master.PropertyTypeMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces;

public interface IPropertyTypeMasterService : ICommonCrudService<PropertyTypeMasterEntity, PropertyTypeMasterDto, CreatePropertyTypeMasterDto, UpdatePropertyTypeMasterDto, PropertyTypeMasterQueryParameters, int>
{

}
