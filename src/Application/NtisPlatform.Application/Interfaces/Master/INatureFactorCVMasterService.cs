using NtisPlatform.Application.DTOs.Master.NatureFactorCVMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface INatureFactorCVMasterService : ICommonCrudService<NatureFactorCVMasterEntity, NatureFactorCVMasterDto, CreateNatureFactorCVMasterDto, UpdateNatureFactorCVMasterDto, NatureFactorCVMasterQueryParameters, int>
{
}
