using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IWaterRateMasterService
    : ICommonCrudService<WaterRateMasterEntity, WaterRateMasterDto, CreateWaterRateMasterDto, UpdateWaterRateMasterDto, WaterRateMasterQueryParameters, int>
{
}
