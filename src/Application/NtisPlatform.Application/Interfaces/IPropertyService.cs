using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IPropertyService
    : ICommonCrudService<PropertyEntity, PropertyDto, CreatePropertyDto, UpdatePropertyDto, PropertyQueryParameters, int>
{
}
