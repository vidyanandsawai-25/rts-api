using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface ITypeOfUseCategoryService : ICommonCrudService<TypeOfUseCategoryEntity, TypeOfUseCategoryDto, CreateTypeOfUseCategoryDto, UpdateTypeOfUseCategoryDto, TypeOfUseCategoryQueryParameters, int>
{

}
