using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.UserScreenAccess;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Service implementation for user screen access operations
/// Translates complex SQL joins to EF Core LINQ without affecting clean architecture
/// </summary>
public class UserScreenAccessService : IUserScreenAccessService
{
    private readonly IRepository<DepartmentMasterEntity, int> _departmentRepository;
    private readonly IRepository<ModuleMasterEntity, int> _moduleRepository;
    private readonly IRepository<ScreenMasterEntity, int> _screenRepository;
    private readonly IRepository<RoleWiseScreenAccessMasterEntity, int> _roleScreenAccessRepository;
    private readonly IRepository<UserEntity, int> _userRepository;
    private readonly IRepository<UserRoleAllocationEntity, int> _userRoleAllocationRepository;
    private readonly IRepository<ScreenGroupMasterEntity, int> _screenGroupRepository;
    private readonly ILogger<UserScreenAccessService> _logger;

    public UserScreenAccessService(
        IRepository<DepartmentMasterEntity, int> departmentRepository,
        IRepository<ModuleMasterEntity, int> moduleRepository,
        IRepository<ScreenMasterEntity, int> screenRepository,
        IRepository<RoleWiseScreenAccessMasterEntity, int> roleScreenAccessRepository,
        IRepository<UserEntity, int> userRepository,
        IRepository<UserRoleAllocationEntity, int> userRoleAllocationRepository,
        IRepository<ScreenGroupMasterEntity, int> screenGroupRepository,
        ILogger<UserScreenAccessService> logger)
    {
        _departmentRepository = departmentRepository;
        _moduleRepository = moduleRepository;
        _screenRepository = screenRepository;
        _roleScreenAccessRepository = roleScreenAccessRepository;
        _userRepository = userRepository;
        _userRoleAllocationRepository = userRoleAllocationRepository;
        _screenGroupRepository = screenGroupRepository;
        _logger = logger;
    }

    public async Task<PagedResult<UserScreenAccessDto>> GetUserScreenAccessAsync(
        UserScreenAccessQueryParameters queryParams, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build base query with all joins and projections
            var query = BuildUserScreenAccessBaseQuery();

            // Apply filters
            if (queryParams.UserId.HasValue)
            {
                query = query.Where(x => x.UserId == queryParams.UserId.Value);
            }

            if (queryParams.UserRoleId.HasValue)
            {
                query = query.Where(x => x.UserRoleId == queryParams.UserRoleId.Value);
            }

            if (queryParams.DepartmentId.HasValue)
            {
                query = query.Where(x => x.DepartmentId == queryParams.DepartmentId.Value);
            }

            if (queryParams.ModuleId.HasValue)
            {
                query = query.Where(x => x.ModuleId == queryParams.ModuleId.Value);
            }

            // Apply search if provided
            if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
            {
                var searchTerm = queryParams.SearchTerm.ToLower();
                query = query.Where(x => 
                    (x.ScreenName != null && x.ScreenName.ToLower().Contains(searchTerm)) ||
                    (x.ScreenCode != null && x.ScreenCode.ToLower().Contains(searchTerm)) ||
                    (x.ModuleName != null && x.ModuleName.ToLower().Contains(searchTerm)) ||
                    (x.DepartmentName != null && x.DepartmentName.ToLower().Contains(searchTerm)));
            }

            // Apply sorting
            var isDescending = queryParams.SortOrder?.ToLower() == "desc";
            query = queryParams.SortBy?.ToLower() switch
            {
                "departmentname" => isDescending 
                    ? query.OrderByDescending(x => x.DepartmentName) 
                    : query.OrderBy(x => x.DepartmentName),
                "modulename" => isDescending 
                    ? query.OrderByDescending(x => x.ModuleName) 
                    : query.OrderBy(x => x.ModuleName),
                "screenname" => isDescending 
                    ? query.OrderByDescending(x => x.ScreenName) 
                    : query.OrderBy(x => x.ScreenName),
                _ => query.OrderBy(x => x.DepartmentId)
                          .ThenBy(x => x.ModuleId)
                          .ThenBy(x => x.ScreenName)
            };

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var items = await query
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Retrieved {Count} user screen access records (page {Page} of {TotalPages})",
                items.Count,
                queryParams.PageNumber,
                (int)Math.Ceiling(totalCount / (double)queryParams.PageSize));

            return new PagedResult<UserScreenAccessDto>(
                items, 
                totalCount, 
                queryParams.PageNumber, 
                queryParams.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user screen access data");
            throw;
        }
    }

    public async Task<IEnumerable<UserScreenAccessDto>> GetUserScreensByUserIdAsync(
        int userId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build base query and filter for specific user with permissions
            var rawQuery = BuildUserScreenAccessBaseQuery()
                .Where(x => x.UserId == userId                             
                    && (x.CanView || x.HaveFullAccess)
                    && !x.HaveNoAccess);

            // De-duplicate screens when user has multiple roles granting access to same screen
            // Merge permissions using logical OR - if any role grants a permission, user has it
            var query = from screen in rawQuery
                        group screen by new 
                        { 
                            screen.DepartmentId, 
                            screen.ModuleId, 
                            screen.ScreenCode,
                            screen.ScreenName,
                            screen.ScreenNameLocal,
                            screen.ScreenIcon,
                            screen.RoutePath,
                            screen.IsMenu,
                            screen.ScreenGroupName,
                            screen.DepartmentName,
                            screen.ModuleName
                        } into g
                        orderby g.Key.DepartmentId, g.Key.ModuleId, g.Key.ScreenName
                        select new UserScreenAccessDto
                        {
                            DepartmentId = g.Key.DepartmentId,
                            DepartmentName = g.Key.DepartmentName,
                            ModuleId = g.Key.ModuleId,
                            ModuleName = g.Key.ModuleName,
                            UserId = userId,
                            UserRoleId = g.Max(x => x.UserRoleId), // Use highest role ID (arbitrary - screens are same)
                            ScreenCode = g.Key.ScreenCode,
                            ScreenName = g.Key.ScreenName,
                            ScreenNameLocal = g.Key.ScreenNameLocal,
                            ScreenIcon = g.Key.ScreenIcon,
                            RoutePath = g.Key.RoutePath,
                            IsMenu = g.Key.IsMenu,
                            // Merge permissions with OR logic - if ANY role grants it, user has it
                            // Use Any() instead of Max() for bool types (SQL Server doesn't support MAX on bit columns)
                            CanView = g.Any(x => x.CanView),
                            CanEdit = g.Any(x => x.CanEdit),
                            CanDelete = g.Any(x => x.CanDelete),
                            HaveFullAccess = g.Any(x => x.HaveFullAccess),
                            HaveNoAccess = false, // Always false - we filtered out HaveNoAccess=true records
                            ScreenGroupName = g.Key.ScreenGroupName
                        };

            var result = await query.ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Retrieved {Count} unique accessible screens for user {UserId}", 
                result.Count, 
                userId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving screens for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Builds the base IQueryable for user screen access with all joins and projections.
    /// Ensures consistent department scoping and join logic across all queries.
    /// </summary>
    /// <returns>Base query with all joins - caller applies filters/ordering/pagination</returns>
    private IQueryable<UserScreenAccessDto> BuildUserScreenAccessBaseQuery()
    {
        // AsNoTracking() improves performance for read-only queries
        var departments = _departmentRepository.GetQueryable().AsNoTracking();
        var modules = _moduleRepository.GetQueryable().AsNoTracking();
        var screens = _screenRepository.GetQueryable().AsNoTracking().Where(s => s.IsActive); // Only filter screens
        var roleAccess = _roleScreenAccessRepository.GetQueryable().AsNoTracking();
        var users = _userRepository.GetQueryable().AsNoTracking();
        var userRoleAllocations = _userRoleAllocationRepository.GetQueryable().AsNoTracking();
        var screenGroups = _screenGroupRepository.GetQueryable().AsNoTracking();

        // Join hierarchy with department scoping:
        // Department → Module → Screen → ScreenGroup
        //                       ↓
        //           RoleWiseScreenAccess (by ScreenId)
        //                       ↓
        //           UserRoleAllocation (by UserRoleId AND DepartmentId)
        //                       ↓
        //                     User
        var query = from dm in departments
                    join mm in modules 
                        on dm.Id equals mm.DepartmentId
                    join sm in screens 
                        on mm.Id equals sm.ModuleId
                    join sg in screenGroups
                        on sm.ScreenGroupId equals sg.Id
                    join rwsa in roleAccess
                        on sm.Id equals rwsa.ScreenId
                    join ura in userRoleAllocations
                        on new { rwsa.UserRoleId, DepartmentId = dm.Id } equals new { ura.UserRoleId, ura.DepartmentId }
                    join um in users 
                        on ura.UserId equals um.Id
                    select new UserScreenAccessDto
                    {
                        DepartmentId = dm.Id,
                        DepartmentName = dm.DepartmentName,
                        ModuleId = mm.Id,
                        ModuleName = mm.ModuleName,
                        UserId = um.Id,
                        UserRoleId = ura.UserRoleId,
                        ScreenCode = sm.ScreenCode,
                        ScreenName = sm.ScreenName,
                        ScreenNameLocal = sm.ScreenNameLocal,
                        ScreenIcon = sm.ScreenIcon,
                        RoutePath = sm.RoutePath,
                        IsMenu = sm.IsMenu,
                        CanView = rwsa.CanView,
                        CanEdit = rwsa.CanEdit,
                        CanDelete = rwsa.CanDelete,
                        HaveFullAccess = rwsa.HaveFullAccess,
                        HaveNoAccess = rwsa.HaveNoAccess,
                        ScreenGroupName = sg.ScreenGroupName
                    };

        return query;
    }
}
