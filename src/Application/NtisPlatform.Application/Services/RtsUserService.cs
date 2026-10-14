using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.RTSUser;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Reads profile-only RTS users. User department allocations are used exclusively
/// for eligibility filtering and are not materialized or returned.
/// </summary>
public sealed class RtsUserService : IRtsUserService
{
    private readonly IRepository<UserEntity, int> _userRepository;
    private readonly IRepository<UserDepartmentAllocationEntity, int> _departmentAllocationRepository;

    public RtsUserService(
        IRepository<UserEntity, int> userRepository,
        IRepository<UserDepartmentAllocationEntity, int> departmentAllocationRepository)
    {
        _userRepository = userRepository;
        _departmentAllocationRepository = departmentAllocationRepository;
    }

    public async Task<PagedResult<RtsUserDto>> GetAllAsync(
        RtsUserQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        IQueryable<UserEntity> query = _userRepository.GetQueryable().AsNoTracking();

        if (queryParameters.DepartmentId.HasValue)
        {
            var eligibleUserIds = _departmentAllocationRepository
                .GetQueryable()
                .AsNoTracking()
                .Where(allocation =>
                    allocation.IsActive && allocation.DepartmentId == queryParameters.DepartmentId.Value)
                .Select(allocation => allocation.UserId);
            query = query.Where(user => eligibleUserIds.Contains(user.Id));
        }

        query = query.ApplyFilters(queryParameters);

        var searchTerm = queryParameters.SearchTerm?.Trim();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var hasUserId = int.TryParse(searchTerm, out var userId);
            query = query.Where(user =>
                (hasUserId && user.Id == userId) ||
                user.UserName.Contains(searchTerm) ||
                (user.FirstName != null && user.FirstName.Contains(searchTerm)) ||
                (user.MiddleName != null && user.MiddleName.Contains(searchTerm)) ||
                (user.LastName != null && user.LastName.Contains(searchTerm)) ||
                (user.MobileNo != null && user.MobileNo.Contains(searchTerm)) ||
                (user.UserCode != null && user.UserCode.Contains(searchTerm)));
        }

        query = query.ApplySort(queryParameters);
        var totalCount = await query.CountAsync(cancellationToken);
        var isUnpaged = queryParameters.PageSize == -1;
        var pagedQuery = isUnpaged
            ? query
            : query.Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize);

        var items = await pagedQuery
            .Select(user => new RtsUserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                UserCode = user.UserCode,
                Address = user.Address,
                MobileNo = user.MobileNo,
                AlternateMobileNo = user.AlternateMobileNo,
                Email = user.Email,
                Remark = user.Remark,
                EmployeeTypeID = user.EmployeeTypeID
            })
            .ToListAsync(cancellationToken);

        var pageSize = isUnpaged ? Math.Max(totalCount, 1) : queryParameters.PageSize;
        var pageNumber = isUnpaged ? 1 : queryParameters.PageNumber;
        return new PagedResult<RtsUserDto>(items, totalCount, pageNumber, pageSize);
    }
}
