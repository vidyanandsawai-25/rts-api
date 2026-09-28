using NtisPlatform.Application.DTOs.RTS;

namespace NtisPlatform.Application.Interfaces;

public interface IRTSServiceOfficerAllocationService
{
    Task<List<RTSServiceOfficerAllocationDto>> GetOfficersByServiceIdAsync(int serviceId, CancellationToken ct = default);
    Task<List<RTSServiceOfficerAllocationDto>> GetAllAllocationsAsync(int? serviceId = null, int? zoneId = null, CancellationToken ct = default);
    Task<RTSServiceOfficerAllocationDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<RTSServiceOfficerAllocationDto> CreateAllocationAsync(CreateRTSServiceOfficerAllocationDto dto, int? userId = null, CancellationToken ct = default);
    Task<RTSServiceOfficerAllocationDto?> UpdateAllocationAsync(int id, UpdateRTSServiceOfficerAllocationDto dto, int? userId = null, CancellationToken ct = default);
    Task<bool> DeleteAllocationAsync(int id, int? userId = null, CancellationToken ct = default);
}
