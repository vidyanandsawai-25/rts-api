using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IWaterConnectionService
    : ICommonCrudService<WaterConnectionMasterEntity, WaterConnectionDto, CreateWaterConnectionDto, UpdateWaterConnectionDto, WaterConnectionQueryParameters, int>
{
    Task<WaterConnectionDto?> GetByIdWithFinanceYearAsync(int id, int? financeYearId, CancellationToken cancellationToken = default);
}
