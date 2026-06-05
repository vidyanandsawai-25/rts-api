using NtisPlatform.Application.DTOs.Master.PropertyPhotoType;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IPropertyPhotoTypeService : ICommonCrudService<PropertyPhotoTypeEntity, PropertyPhotoTypeDto, CreatePropertyPhotoTypeDto, UpdatePropertyPhotoTypeDto, PropertyPhotoTypeQueryParameters, int>
{

}
