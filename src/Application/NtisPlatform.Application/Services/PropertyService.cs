using AutoMapper;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Global Property Service - Used across all features
/// Provides property search, lookup, and master data functionality
/// </summary>
public class PropertyService
    : BaseCommonCrudService<PropertyEntity, PropertyDto, CreatePropertyDto, UpdatePropertyDto, PropertyQueryParameters, int>,
      IPropertyService
{
    public PropertyService(
        IRepository<PropertyEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}