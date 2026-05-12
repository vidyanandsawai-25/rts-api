using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IWaterConnectionStatusService
    : ICommonCrudService<WaterConnectionStatusEntity, WaterConnectionStatusDto, CreateWaterConnectionStatusDto, UpdateWaterConnectionStatusDto, WaterConnectionStatusQueryParameters, int>
{
}

