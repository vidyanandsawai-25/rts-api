using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.wardallocation;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Manages employee allocations to departments, modules, zones and wards.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WardAllocationController : ControllerBase
{
    private readonly IWardAllocationService _service;
    private readonly ILogger<WardAllocationController> _logger;

    public WardAllocationController(
        IWardAllocationService service,
        ILogger<WardAllocationController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Returns paginated ward allocation records.
    /// </summary>
    [HttpGet]
    public Task<IActionResult> GetAll(
        [FromQuery] WardAllocationQueryParameters query,
        CancellationToken ct)
        => this.ExecuteGetAllPaged(
            _service,
            query,
            _logger,
            ct);

    /// <summary>
    /// Returns a ward allocation by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public Task<IActionResult> GetById(
        int id,
        CancellationToken ct)
        => this.ExecuteGetById(
            _service,
            id,
            _logger,
            ct);

    /// <summary>
    /// Allocates one or more zones and wards to a user.
    /// </summary>
    /// <remarks>
    /// Example:
    ///
    ///     POST /api/WardAllocation
    ///     {
    ///         "userId": 7,
    ///         "departmentId": 1,
    ///         "moduleId": 1,
    ///         "isActive": true,
    ///         "createdBy": 1,
    ///         "allocations": [
    ///             {
    ///                 "zoneId": 3,
    ///                 "wardIds": [4, 5, 6]
    ///             }
    ///         ]
    ///     }
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> CreateFlexible(
        [FromBody] CreateFlexibleWardAllocationDto createDto,
        CancellationToken ct)
    {
        try
        {
            var result = await _service.CreateFlexibleAsync(
                createDto,
                ct);

            var zoneCount = createDto.Allocations.Count;
            var wardCount = result.Count;

            var summary = zoneCount == 1
                ? $"{wardCount} ward allocation(s) created in 1 zone"
                : $"{wardCount} ward allocation(s) created across " +
                  $"{zoneCount} zones";

            return Ok(new
            {
                success = true,
                message = summary,
                totalAllocations = wardCount,
                zonesAffected = zoneCount,
                items = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                ex,
                "Validation failed while creating ward allocations " +
                "for UserId {UserId}",
                createDto.UserId);

            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Duplicate ward allocation detected for UserId {UserId}",
                createDto.UserId);

            return Conflict(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create ward allocations for UserId {UserId}",
                createDto.UserId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    success = false,
                    message =
                        "An error occurred while creating ward allocations."
                });
        }
    }

    /// <summary>
    /// Replaces all ward allocations for the supplied user,
    /// department and module.
    /// </summary>
    /// <remarks>
    /// Existing active allocations are deactivated and new active
    /// allocation records are created.
    /// </remarks>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateFlexibleWardAllocationDto updateDto,
        CancellationToken ct)
    {
        try
        {
            var existingAllocation = await _service.GetByIdAsync(
                id,
                ct);

            if (existingAllocation == null)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        $"Ward allocation with ID {id} was not found."
                });
            }

            var result = await _service.ReplaceAllocationsAsync(
                updateDto.UserId,
                updateDto.ModuleId,
                updateDto,
                ct);

            var zoneCount = updateDto.Allocations.Count;
            var wardCount = result.Count;

            var summary = zoneCount == 1
                ? $"Replaced with {wardCount} ward allocation(s) " +
                  "in 1 zone"
                : $"Replaced with {wardCount} ward allocation(s) " +
                  $"across {zoneCount} zones";

            return Ok(new
            {
                success = true,
                message = summary,
                totalAllocations = wardCount,
                zonesAffected = zoneCount,
                existingAllocationId = id,
                items = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                ex,
                "Validation failed while updating ward allocation " +
                "{AllocationId}",
                id);

            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Ward allocation update conflict for AllocationId " +
                "{AllocationId}",
                id);

            return Conflict(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update ward allocation {AllocationId}",
                id);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    success = false,
                    message =
                        "An error occurred while updating ward allocations."
                });
        }
    }

    /// <summary>
    /// Deletes a ward allocation.
    /// </summary>
    [HttpDelete("{id:int}")]
    public Task<IActionResult> Delete(
        int id,
        CancellationToken ct)
        => this.ExecuteDelete(
            _service,
            id,
            _logger,
            ct);

    /// <summary>
    /// Returns modules allocated to the specified user.
    /// </summary>
    [HttpGet("modules/{userId:int}")]
    public async Task<IActionResult> GetModulesByUserId(
        int userId,
        CancellationToken ct)
    {
        try
        {
            var result = await _service.GetModulesByUserIdAsync(
                userId,
                ct);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve modules for UserId {UserId}",
                userId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    success = false,
                    message =
                        "An error occurred while retrieving modules."
                });
        }
    }

    
    

    /// <summary>
    /// Returns active wards belonging to the specified zone.
    /// </summary>
    [HttpGet("wards/{zoneId:int}")]
    public async Task<IActionResult> GetWardsByZoneId(
        int zoneId,
        CancellationToken ct)
    {
        try
        {
            var result = await _service.GetWardsByZoneIdAsync(
                zoneId,
                ct);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve wards for ZoneId {ZoneId}",
                zoneId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    success = false,
                    message =
                        "An error occurred while retrieving wards."
                });
        }
    }

    /// <summary>
    /// Returns allocated zones with their wards for the user.
    /// </summary>
    [HttpGet("allocated-zones-wards/{userId:int}")]
    public async Task<IActionResult>
        GetAllocatedZonesAndWardsByUserId(
            int userId,
            CancellationToken ct)
    {
        try
        {
            var isDeallocated =
                await _service.IsUserDeallocatedAsync(
                    userId,
                    ct);

            if (isDeallocated)
            {
                return Ok(new
                {
                    success = false,
                    userId,
                    message = "User is deallocated",
                    items = Array.Empty<UserAllocatedZoneWardDto>()
                });
            }

            var result =
                await _service
                    .GetAllocatedZonesAndWardsByUserIdAsync(
                        userId,
                        ct);

            return Ok(new
            {
                success = true,
                userId,
                items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve allocated zones and wards " +
                "for UserId {UserId}",
                userId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    success = false,
                    message =
                        "An error occurred while retrieving allocations."
                });
        }
    }

    /// <summary>
    /// Returns distinct zones allocated to the user,
    /// grouped by module.
    /// </summary>
    [HttpGet("allocated-zones/{userId:int}")]
    public async Task<IActionResult> GetAllocatedZonesByUserId(
        int userId,
        CancellationToken ct)
    {
        try
        {
            var isDeallocated =
                await _service.IsUserDeallocatedAsync(
                    userId,
                    ct);

            if (isDeallocated)
            {
                return Ok(new
                {
                    success = false,
                    userId,
                    message = "User is deallocated",
                    items = Array.Empty<AllocatedZoneByUserDto>()
                });
            }

            // Correct service method for allocated zones.
            var result =
                await _service.GetAllocatedZonesByUserIdAsync(
                    userId,
                    ct);

            return Ok(new
            {
                success = true,
                userId,
                items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve allocated zones for UserId " +
                "{UserId}",
                userId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    success = false,
                    message =
                        "An error occurred while retrieving allocated zones."
                });
        }
    }

    /// <summary>
    /// Returns distinct wards allocated to the user,
    /// grouped by module and zone.
    /// </summary>
    [HttpGet("allocated-wards/{userId:int}")]
    public async Task<IActionResult> GetAllocatedWardsByUserId(
        int userId,
        CancellationToken ct)
    {
        try
        {
            var isDeallocated =
                await _service.IsUserDeallocatedAsync(
                    userId,
                    ct);

            if (isDeallocated)
            {
                return Ok(new
                {
                    success = false,
                    userId,
                    message = "User is deallocated",
                    items = Array.Empty<AllocatedWardByUserDto>()
                });
            }

            // Correct service method for allocated wards.
            var result =
                await _service.GetAllocatedWardsByUserIdAsync(
                    userId,
                    ct);

            return Ok(new
            {
                success = true,
                userId,
                items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve allocated wards for UserId " +
                "{UserId}",
                userId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    success = false,
                    message =
                        "An error occurred while retrieving allocated wards."
                });
        }
    }
}