using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface ITypeOfUseService : ICommonCrudService<TypeOfUseEntity, TypeOfUseDto, CreateTypeOfUseDto, UpdateTypeOfUseDto, TypeOfUseQueryParameters, string>
{

}

