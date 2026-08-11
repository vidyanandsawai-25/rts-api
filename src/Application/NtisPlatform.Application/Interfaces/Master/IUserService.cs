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

    /// <summary>Requires the user to complete authenticator-app setup (does not enable 2FA itself).</summary>
    Task<UserSecurityStatusDto?> RequireTwoFactorAsync(int id, RequireTwoFactorDto dto, CancellationToken cancellationToken = default);

    /// <summary>Clears the 2FA-required policy flag. Does not disable 2FA if already enabled.</summary>
    Task<UserSecurityStatusDto?> UnrequireTwoFactorAsync(int id, UnrequireTwoFactorDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin-forced 2FA reset for account recovery (e.g. lost device) — clears the user's current
    /// enrollment and recovery codes and invalidates their existing sessions, without needing any
    /// code from the user.
    /// </summary>
    Task<UserSecurityStatusDto?> AdminResetTwoFactorAsync(int id, AdminResetTwoFactorDto dto, CancellationToken cancellationToken = default);
}