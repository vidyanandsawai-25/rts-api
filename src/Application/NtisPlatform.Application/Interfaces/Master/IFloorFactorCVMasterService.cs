using NtisPlatform.Application.DTOs.Master.FloorFactorCVMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IFloorFactorCVMasterService : ICommonCrudService<FloorFactorCVMasterEntity, FloorFactorCVMasterDto, CreateFloorFactorCVMasterDto, UpdateFloorFactorCVMasterDto, FloorFactorCVMasterQueryParameters, int>
{ 
}
