using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IWaterConnectionSizeService
    : ICommonCrudService<WaterConnectionSizeEntity, WaterConnectionSizeDto, CreateWaterConnectionSizeDto, UpdateWaterConnectionSizeDto, WaterConnectionSizeQueryParameters, int>
{
}
