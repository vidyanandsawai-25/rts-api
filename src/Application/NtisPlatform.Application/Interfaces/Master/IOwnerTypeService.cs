using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IOwnerTypeService : ICommonCrudService<OwnerTypeEntity, OwnerTypeDto, CreateOwnerTypeDto, UpdateOwnerTypeDto, OwnerTypeQueryParameters, int>
{
}