using NtisPlatform.Application.DTOs.PropertyMergeSingle;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IPropertyMergeSingleService : ICommonCrudService<PropertyMapDetailEntity, PropertyMergeSingleDto, CreatePropertyMergeSingleDto, UpdatePropertyMergeSingleDto, PropertyMergeSingleQueryParameters, int>
{

}
