using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces;

public interface IOwnerTitleService : ICommonCrudService<OwnerTitleMasterEntity, OwnerTitleDto, CreateOwnerTitleDto, UpdateOwnerTitleDto, OwnerTitleQueryParameters, int>
{
}
