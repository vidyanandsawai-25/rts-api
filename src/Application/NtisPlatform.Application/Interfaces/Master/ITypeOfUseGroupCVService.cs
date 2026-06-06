using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface ITypeOfUseGroupCVService : ICommonCrudService<TypeOfUseGroupCVEntity, TypeOfUseGroupCVDto, CreateTypeOfUseGroupCVDto, UpdateTypeOfUseGroupCVDto, TypeOfUseGroupCVQueryParameters, int>
{
}
