using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces;

public interface IOldWardMasterService : ICommonCrudService<OldWardMasterEntity, OldWardMasterDto, CreateOldWardMasterDto, UpdateOldWardMasterDto, OldWardMasterQueryParameters, int>
{
}
