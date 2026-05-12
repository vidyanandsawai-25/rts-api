using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.UserScreenAccess;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// User Access partial controller - Provides screen access information based on user roles.
/// Used for dynamic menu generation, permission checks, and role-based UI rendering.
/// </summary>
/// <remarks>
/// This partial controller exposes screen access mappings derived from the relationship between
/// users, roles, screens, modules, and departments. It serves as a critical component
/// for authorization and UI customization across the application.
/// 
/// Query joins: DepartmentMaster → ModuleMaster → ScreenMaster → RoleWiseScreenAccessMaster → UserMaster
/// </remarks>
[Authorize]
public partial class UserController
{
    // Service field - initialized via main controller constructor
    private readonly IUserScreenAccessService _userScreenAccessService;

    // ── User Screen Access Endpoints ─────────────────────────────────────────

    /// <summary>
    /// Get user screen access with filtering and pagination.
    /// Supports filtering by userId, userRoleId, departmentId, moduleId, and search terms.
    /// </summary>
    [HttpGet("access")]
    [ProducesResponseType(typeof(PagedResult<UserScreenAccessDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUserScreenAccess([FromQuery] UserScreenAccessQueryParameters query, CancellationToken ct)
    {
        try
        {
            var result = await _userScreenAccessService.GetUserScreenAccessAsync(query, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetUserScreenAccess failed");
            return BadRequest(new { error = "Failed to retrieve user screen access", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all screens accessible to a specific user.
    /// Filters based on role permissions (CanView or HaveFullAccess).
    /// Typically used for menu generation and permission validation.
    /// </summary>
    [HttpGet("{userId:int}/screens")]
    [ProducesResponseType(typeof(IEnumerable<UserScreenAccessDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserScreens(int userId, CancellationToken ct)
    {
        try
        {
            var screens = await _userScreenAccessService.GetUserScreensByUserIdAsync(userId, ct);
            
            if (!screens.Any())
            {
                _logger.LogWarning("No accessible screens found for user {UserId}", userId);
                return NotFound(new { error = $"No accessible screens found for user {userId}" });
            }

            return Ok(screens);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetUserScreens failed for user {UserId}", userId);
            return BadRequest(new { error = "Failed to retrieve user screens", details = ex.Message });
        }
    }

    /// <summary>
    /// Get screens accessible to users with a specific role.
    /// Useful for role administration and permission preview.
    /// </summary>
    [HttpGet("role/{userRoleId:int}/screens")]
    [ProducesResponseType(typeof(IEnumerable<UserScreenAccessDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetScreensByRole(int userRoleId, CancellationToken ct)
    {
        try
        {
            var query = new UserScreenAccessQueryParameters
            {
                UserRoleId = userRoleId,
                PageSize = 1000
            };

            var result = await _userScreenAccessService.GetUserScreenAccessAsync(query, ct);
            return Ok(result.Items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetScreensByRole failed for role {UserRoleId}", userRoleId);
            return BadRequest(new { error = "Failed to retrieve role screens", details = ex.Message });
        }
    }
}
