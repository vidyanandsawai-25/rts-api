using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Master.UserMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

[Route("api/user-department-allocations")]
[ApiController]
[Authorize]
public class UserDepartmentAllocationController : ControllerBase
{
    private readonly IUserDepartmentAllocationService _service;
    private readonly ILogger<UserDepartmentAllocationController> _logger;

    public UserDepartmentAllocationController(
        IUserDepartmentAllocationService service,
        ILogger<UserDepartmentAllocationController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the list of active departments allocated to the logged-in user.
    /// </summary>
    [HttpGet("my-allocations")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserDepartmentDetailsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMyAllocations(CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            var result = await _service.GetMyAllocatedDepartmentsAsync(userId, cancellationToken);
            
            return Ok(new ApiResponse<IEnumerable<UserDepartmentDetailsDto>>
            {
                Success = true,
                Message = "User department allocations retrieved successfully",
                Items = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt to my-allocations");
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting my-allocations. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving your department allocations",
                CorrelationId = correlationId
            });
        }
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id) || id <= 0)
        {
            throw new UnauthorizedAccessException("Valid user identification is required.");
        }
        return id;
    }
}
