using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IWaterConnectionDetailsService
    : ICommonCrudService<WaterConnectionDetailsEntity, WaterConnectionDetailsDto, CreateWaterConnectionDetailsDto, UpdateWaterConnectionDetailsDto, WaterConnectionDetailsQueryParameters, int>
{
    Task<WaterConnectionDetailsDto?> GenerateBillAsync(int waterConnectionId, int financeYearId, CancellationToken cancellationToken = default);
}
