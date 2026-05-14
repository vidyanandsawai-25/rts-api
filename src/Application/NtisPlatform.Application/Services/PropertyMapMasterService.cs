using AutoMapper;
using NtisPlatform.Application.DTOs.Master.PropertyMapMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class PropertyMapMasterService : BaseCommonCrudService<PropertyMapMasterEntity, PropertyMapMasterDtos, CreatePropertyMapMasterDto, UpdatePropertyMapMasterDto, PropertyMapQueryParameters, int>, IPropertyMapMasterService
{
    public PropertyMapMasterService(
        IRepository<PropertyMapMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}