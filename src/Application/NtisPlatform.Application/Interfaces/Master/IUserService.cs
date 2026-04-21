using NtisPlatform.Application.DTOs.Master.UserMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Master;

public interface IUserService : ICommonCrudService<UserEntity, UserDto, CreateUserDto, UpdateUserDto, UserQueryParameter, int>
{
    // Override DeleteAsync to require audit trail for allocation deactivation
    Task<bool> DeleteAsync(int id, DeleteUserDto dto, CancellationToken cancellationToken = default);

    Task<UserSecurityStatusDto?> DeactivateUserAsync(int id, DeactivateUserDto dto, CancellationToken cancellationToken = default);

    Task<UserSecurityStatusDto?> ActivateUserAsync(int id, ActivateUserDto dto, CancellationToken cancellationToken = default);

    Task<UserSecurityStatusDto?> ResetPasswordAsync(int id, ResetPasswordDto dto, CancellationToken cancellationToken = default);
}