using NtisPlatform.Application.DTOs.LockUnlock;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces;

public interface ILockUnlockService
{
    Task<List<LockableScreenDto>> GetLockableScreensAsync(CancellationToken ct);

    Task<PagedResult<PropertyLockRowDto>> GetPropertyLocksAsync(
        FilterPropertyLocksRequestDto request, CancellationToken ct);

    Task<BulkLockResultDto> BulkApplyAsync(
        BulkLockRequestDto request, int actingUserId, CancellationToken ct);
}
