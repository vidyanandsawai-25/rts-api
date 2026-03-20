using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
namespace NtisPlatform.Application.Interfaces;

public interface IConstructionTypeService : ICommonCrudService<ConstructionTypeEntity, ConstructionTypeDto, CreateConstructionTypeDto, UpdateConstructionTypeDto, ConstructionTypeQueryParameters, int>
{

}
