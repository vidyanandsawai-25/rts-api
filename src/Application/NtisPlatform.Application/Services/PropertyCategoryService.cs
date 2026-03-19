using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class PropertyCategoryService : BaseCommonCrudService<PropertyCategoryEntity, PropertyCategoryDto, PropertyCategoryCreateDto, PropertyCategoryUpdateDto, PropertyCategoryQueryParameters, int>, IPropertyCategoryService
{
    public PropertyCategoryService(
        IRepository<PropertyCategoryEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}
