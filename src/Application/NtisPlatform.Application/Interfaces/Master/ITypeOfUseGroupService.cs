
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;
namespace NtisPlatform.Application.Interfaces;

public interface ITypeOfUseGroupService : ICommonCrudService<TypeOfUseGroupEntity, TypeOfUseGroupDto, CreateTypeOfUseGroupDto, UpdateTypeOfUseGroupDto, TypeOfUseGroupQueryParameters, string>
{
}

