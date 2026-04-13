using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Provides property type category-related operations using the common CRUD service pattern.
/// </summary>
public interface IPropertyTypeCategoryService : ICommonCrudService<PropertyTypeCategoryEntity, PropertyTypeCategoryDto, CreatePropertyTypeCategoryDto, UpdatePropertyTypeCategoryDto, PropertyTypeCategoryQueryParameters, int>
{
}
