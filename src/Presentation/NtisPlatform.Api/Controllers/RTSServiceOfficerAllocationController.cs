using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.RTS;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

[ApiController]
[Route("api/rts-service-officers")]
public class RTSServiceOfficerAllocationController : ControllerBase
{
    private readonly IRTSServiceOfficerAllocationService _service;
    private readonly ILogger<RTSServiceOfficerAllocationController> _logger;

    public RTSServiceOfficerAllocationController(
        IRTSServiceOfficerAllocationService service,
        ILogger<RTSServiceOfficerAllocationController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Public endpoint for citizens: get zone-wise allocated RTS officers for a specific service.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("by-service/{serviceId}")]
    [ProducesResponseType(typeof(ApiResponse<List<RTSServiceOfficerAllocationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByServiceId(int serviceId, CancellationToken ct)
    {
        var officers = await _service.GetOfficersByServiceIdAsync(serviceId, ct);
        return Ok(new ApiResponse<List<RTSServiceOfficerAllocationDto>>
        {
            Success = true,
            Message = "Service officers retrieved successfully",
            Items = officers
        });
    }

    /// <summary>
    /// Get all allocations with optional filters.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<RTSServiceOfficerAllocationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int? serviceId, [FromQuery] int? zoneId, CancellationToken ct)
    {
        var officers = await _service.GetAllAllocationsAsync(serviceId, zoneId, ct);
        return Ok(new ApiResponse<List<RTSServiceOfficerAllocationDto>>
        {
            Success = true,
            Message = "Service officer allocations retrieved successfully",
            Items = officers
        });
    }

    /// <summary>
    /// Get allocation by ID.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<RTSServiceOfficerAllocationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var officer = await _service.GetByIdAsync(id, ct);
        if (officer == null)
            return NotFound(new ApiResponse<RTSServiceOfficerAllocationDto> { Success = false, Message = "Allocation not found" });

        return Ok(new ApiResponse<RTSServiceOfficerAllocationDto>
        {
            Success = true,
            Message = "Allocation retrieved successfully",
            Items = officer
        });
    }

    /// <summary>
    /// Create a new zone officer allocation for a service.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RTSServiceOfficerAllocationDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateRTSServiceOfficerAllocationDto dto, CancellationToken ct)
    {
        var created = await _service.CreateAllocationAsync(dto, null, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, new ApiResponse<RTSServiceOfficerAllocationDto>
        {
            Success = true,
            Message = "Service officer allocation created successfully",
            Items = created
        });
    }

    /// <summary>
    /// Update an existing zone officer allocation.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<RTSServiceOfficerAllocationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRTSServiceOfficerAllocationDto dto, CancellationToken ct)
    {
        var updated = await _service.UpdateAllocationAsync(id, dto, null, ct);
        if (updated == null)
            return NotFound(new ApiResponse<RTSServiceOfficerAllocationDto> { Success = false, Message = "Allocation not found" });

        return Ok(new ApiResponse<RTSServiceOfficerAllocationDto>
        {
            Success = true,
            Message = "Service officer allocation updated successfully",
            Items = updated
        });
    }

    /// <summary>
    /// Soft-delete an officer allocation.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var success = await _service.DeleteAllocationAsync(id, null, ct);
        return Ok(new ApiResponse<bool>
        {
            Success = success,
            Message = success ? "Allocation deleted successfully" : "Allocation not found or already deleted",
            Items = success
        });
    }
}
