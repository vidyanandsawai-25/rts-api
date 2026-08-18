using NtisPlatform.Application.DTOs.PropertyChangeCategory;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IPropertyChangeCategoryService : ICommonCrudService<PropertyMapDetailEntity, PropertyChangeCategoryDto, CreatePropertyChangeCategoryDto, UpdatePropertyChangeCategoryDto, PropertyChangeCategoryQueryParameters, int>
{

}
