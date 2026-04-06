using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces;

public interface IOwnerTypeService : ICommonCrudService<OwnerTypeMasterEntity, OwnerTypeDto, CreateOwnerTypeDto, UpdateOwnerTypeDto, OwnerTypeQueryParameters, int>
{
}