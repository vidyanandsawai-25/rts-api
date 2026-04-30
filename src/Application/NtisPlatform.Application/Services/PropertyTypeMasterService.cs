using AutoMapper;
using Microsoft.AspNetCore.Http;
using NtisPlatform.Application.DTOs.Master.PropertyTypeMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class PropertyTypeMasterService : BaseCommonCrudService<PropertyTypeMasterEntity, PropertyTypeMasterDto, CreatePropertyTypeMasterDto, UpdatePropertyTypeMasterDto, PropertyTypeMasterQueryParameters, int>, IPropertyTypeMasterService
{
    public PropertyTypeMasterService(
        IRepository<PropertyTypeMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
