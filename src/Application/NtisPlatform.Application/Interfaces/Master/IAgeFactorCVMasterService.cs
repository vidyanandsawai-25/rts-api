using NtisPlatform.Application.DTOs.Master.AgeFactorCVMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IAgeFactorCVMasterService : ICommonCrudService<AgeFactorCVMasterEntity, AgeFactorCVMasterDto, CreateAgeFactorCVMasterDto, UpdateAgeFactorCVMasterDto, AgeFactorCVMasterQueryParameters, int>
{
}
