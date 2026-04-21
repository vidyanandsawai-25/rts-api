using NtisPlatform.Application.DTOs.Master.GenderMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IGenderMasterService : ICommonCrudService<GenderMasterEntity, GenderMasterDtos, CreateGenderMasterDto, UpdateGenderMasterDto, GenderQueryParameters, int>
{
}
