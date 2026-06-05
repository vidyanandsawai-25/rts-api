using AutoMapper;
using NtisPlatform.Application.DTOs.Master.PropertyPhotoType;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Master;

public class PropertyPhotoTypeService : BaseCommonCrudService<PropertyPhotoTypeEntity, PropertyPhotoTypeDto, CreatePropertyPhotoTypeDto, UpdatePropertyPhotoTypeDto, PropertyPhotoTypeQueryParameters, int>, IPropertyPhotoTypeService
{
    public PropertyPhotoTypeService(
        IRepository<PropertyPhotoTypeEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
