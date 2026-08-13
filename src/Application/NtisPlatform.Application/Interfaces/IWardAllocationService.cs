using NtisPlatform.Application.DTOs.wardallocation;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

public interface IWardAllocationService : ICommonCrudService<
    GlobalSurveyWardAllocationEntity,
    WardAllocationDto,
    CreateWardAllocationDto,
    UpdateWardAllocationDto,
    WardAllocationQueryParameters,
    int>
{
 
    Task<List<WardAllocationModuleDto>> GetModulesByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<List<WardAllocationWardDto>> GetWardsByZoneIdAsync(
        int zoneId,
        CancellationToken cancellationToken = default);

    Task<List<WardAllocationDto>> GetByUserModuleZoneAsync(
        int userId,
        int moduleId,
        int zoneId,
        CancellationToken cancellationToken = default);

    Task<List<WardAllocationDto>> CreateFlexibleAsync(
        CreateFlexibleWardAllocationDto createDto,
        CancellationToken cancellationToken = default);

    Task<List<WardAllocationDto>> ReplaceAllocationsAsync(
        int userId,
        int moduleId,
        UpdateFlexibleWardAllocationDto updateDto,
        CancellationToken cancellationToken = default);

    Task<List<UserAllocatedZoneWardDto>> GetAllocatedZonesAndWardsByUserIdAsync(
    int userId,
    CancellationToken cancellationToken = default);

    Task<List<AllocatedZoneByUserDto>> GetAllocatedZonesByUserIdAsync(
     int userId,
     CancellationToken cancellationToken = default);

    Task<List<AllocatedWardByUserDto>> GetAllocatedWardsByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<bool> IsUserDeallocatedAsync(int userId, CancellationToken cancellationToken = default);
}