using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;
namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Provides category-related operations using the common CRUD service pattern.
/// </summary>
public interface IPropertyCategoryService : ICommonCrudService<PropertyCategoryEntity, PropertyCategoryDto, PropertyCategoryCreateDto, PropertyCategoryUpdateDto, PropertyCategoryQueryParameters, int>
{

}
