using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class PropertyTypeCategoryService : BaseCommonCrudService<PropertyTypeCategoryEntity, PropertyTypeCategoryDto, CreatePropertyTypeCategoryDto, UpdatePropertyTypeCategoryDto, PropertyTypeCategoryQueryParameters, int>, IPropertyTypeCategoryService
{
    public PropertyTypeCategoryService(
        IRepository<PropertyTypeCategoryEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
