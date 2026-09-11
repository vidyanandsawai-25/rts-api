using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

public class UserAccessRepository : IUserAccessRepository
{
    private readonly ApplicationDbContext _context;

    public UserAccessRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<string>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await (
            from ura in _context.Set<UserRoleAllocationEntity>().AsNoTracking()
            join urm in _context.Set<UserRoleMasterEntity>().AsNoTracking()
                on ura.UserRoleId equals urm.Id
            where ura.UserId == userId
                  && ura.IsActive
                  && urm.IsActive
            select urm.UserRoleName
        )
        .Distinct()
        .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsAdminAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await (
            from ura in _context.Set<UserRoleAllocationEntity>().AsNoTracking()
            join urm in _context.Set<UserRoleMasterEntity>().AsNoTracking()
                on ura.UserRoleId equals urm.Id
            where ura.UserId == userId
                  && ura.IsActive
                  && urm.IsActive
                  && urm.UserRoleName == "Admin"
            select ura.Id
        ).AnyAsync(cancellationToken);
    }

    public async Task<List<LoginPermissionDto>> GetUserPermissionsAsync(
        int userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (isAdmin)
        {
            return await _context.Set<ModuleMasterEntity>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DepartmentId)
                .ThenBy(x => x.Id)
                .Select(x => new LoginPermissionDto
                {
                    DepartmentId = x.DepartmentId,
                    ModuleId = x.Id,
                    ModuleName = x.ModuleName ?? string.Empty
                })
                .ToListAsync(cancellationToken);
        }

        return await (
            from uma in _context.Set<UserModuleAllocationEntity>().AsNoTracking()
            join module in _context.Set<ModuleMasterEntity>().AsNoTracking()
                on uma.ModuleId equals module.Id
            where uma.UserId == userId
                  && uma.IsActive
                  && module.IsActive
            orderby uma.DepartmentId, uma.ModuleId
            select new LoginPermissionDto
            {
                DepartmentId = uma.DepartmentId,
                ModuleId = uma.ModuleId,
                ModuleName = module.ModuleName ?? string.Empty
            }
        )
        .Distinct()
        .ToListAsync(cancellationToken);
    }

    public async Task<bool> CanAllocateWardsAsync(
        int userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (isAdmin)
            return true;

        return await _context.Set<UserModuleAllocationEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId && x.IsActive, cancellationToken);
    }
}
