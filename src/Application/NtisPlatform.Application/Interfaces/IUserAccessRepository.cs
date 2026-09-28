using NtisPlatform.Application.DTOs.Auth;

namespace NtisPlatform.Application.Interfaces;

public interface IUserAccessRepository
{
    Task<List<string>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> IsAdminAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<LoginPermissionDto>> GetUserPermissionsAsync(int userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<bool> CanAllocateWardsAsync(int userId, bool isAdmin, CancellationToken cancellationToken = default);
}
