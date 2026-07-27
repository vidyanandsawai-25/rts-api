using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Master.UserMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class UserDepartmentAllocationService : IUserDepartmentAllocationService
{
    private readonly IRepository<UserDepartmentAllocationEntity, int> _deptAllocationRepository;
    private readonly ILogger<UserDepartmentAllocationService> _logger;

    public UserDepartmentAllocationService(
        IRepository<UserDepartmentAllocationEntity, int> deptAllocationRepository,
        ILogger<UserDepartmentAllocationService> logger)
    {
        _deptAllocationRepository = deptAllocationRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<UserDepartmentDetailsDto>> GetMyAllocatedDepartmentsAsync(
        int userId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving allocated departments for user {UserId}", userId);

            // Fetch active department allocations along with the Department details
            var activeDepts = await _deptAllocationRepository.GetQueryable()
                .AsNoTracking()
                .Include(da => da.Department)
                .Where(da => da.UserId == userId && da.IsActive && da.Department != null && da.Department.IsActive)
                .Select(da => da.Department)
                .ToListAsync(cancellationToken);

            if (!activeDepts.Any())
            {
                return Enumerable.Empty<UserDepartmentDetailsDto>();
            }

            var result = activeDepts.Select(dept => new UserDepartmentDetailsDto
            {
                DepartmentId = dept!.Id,
                DepartmentCode = dept.DepartmentCode,
                DepartmentName = dept.DepartmentName,
                DepartmentNameLocal = dept.DepartmentNameLocal
            }).ToList();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allocated departments for user {UserId}", userId);
            throw;
        }
    }
}
