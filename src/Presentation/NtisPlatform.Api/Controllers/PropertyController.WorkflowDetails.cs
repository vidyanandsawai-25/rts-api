using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Property.PropertyWorkflowDetails;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property Workflow Details Tab API — thin HTTP adapter.
/// Business logic (CurrentStatus toggle) lives in <c>PropertyWorkflowDetailsService</c>.
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Retrieves all workflow detail records for a specific property.
    /// </summary>
    /// <response code="200">Returns the list of workflow details for the property</response>
    /// <response code="404">No workflow details found for the property</response>
    [HttpGet("{propertyId}/workflow-details")]
    [ProducesResponseType(typeof(ApiResponse<List<PropertyWorkflowDetailsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkflowDetailsByPropertyId(int propertyId, CancellationToken ct)
    {
        var result = await _propertyWorkflowDetailsService.GetByPropertyIdAsync(propertyId, ct);

        if (result == null || result.Count == 0)
        {
            _logger.LogWarning("No workflow details found for Property ID {PropertyId}", propertyId);
            return NotFound(new ApiResponse<List<PropertyWorkflowDetailsDto>>
            {
                Success = false,
                Message = $"No workflow details found for property with ID {propertyId}"
            });
        }

        return Ok(new ApiResponse<List<PropertyWorkflowDetailsDto>>
        {
            Success = true,
            Message = "Records fetched successfully",
            Items = result
        });
    }

    /// <summary>
    /// Retrieves a single workflow detail record by its ID.
    /// </summary>
    /// <response code="200">Returns the workflow detail record</response>
    /// <response code="404">Workflow detail not found</response>
    [HttpGet("workflow-details/{id}")]
    [ProducesResponseType(typeof(ApiResponse<PropertyWorkflowDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkflowDetailsById(int id, CancellationToken ct)
    {
        var result = await _propertyWorkflowDetailsService.GetByIdAsync(id, ct);

        if (result == null)
        {
            _logger.LogWarning("Workflow detail with ID {Id} not found", id);
            return NotFound(new ApiResponse<PropertyWorkflowDetailsDto>
            {
                Success = false,
                Message = $"Workflow detail with ID {id} not found"
            });
        }

        return Ok(new ApiResponse<PropertyWorkflowDetailsDto>
        {
            Success = true,
            Message = "Record fetched successfully",
            Items = result
        });
    }

    /// <summary>
    /// Creates a new workflow detail for a property.
    /// Sets CurrentStatus=true on the new record and CurrentStatus=false on all previous records for the same property.
    /// </summary>
    /// <response code="201">Workflow detail created successfully</response>
    /// <response code="400">Invalid data</response>
    [HttpPost("{propertyId}/workflow-details")]
    [ProducesResponseType(typeof(ApiResponse<PropertyWorkflowDetailsDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWorkflowDetails(int propertyId, [FromBody] CreatePropertyWorkflowDetailsDto dto, CancellationToken ct)
    {
        dto.PropertyId = propertyId;

        var result = await _propertyWorkflowDetailsService.CreateAsync(dto, ct);

        return CreatedAtAction(nameof(GetWorkflowDetailsById), new { id = result.Id }, new ApiResponse<PropertyWorkflowDetailsDto>
        {
            Success = true,
            Message = "Record created successfully",
            Items = result
        });
    }

    /// <summary>
    /// Updates an existing workflow detail record.
    /// </summary>
    /// <response code="200">Workflow detail updated successfully</response>
    /// <response code="404">Workflow detail not found</response>
    /// <response code="400">Invalid data</response>
    [HttpPut("workflow-details/{id}")]
    [ProducesResponseType(typeof(ApiResponse<PropertyWorkflowDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateWorkflowDetails(int id, [FromBody] UpdatePropertyWorkflowDetailsDto dto, CancellationToken ct)
    {
        var result = await _propertyWorkflowDetailsService.UpdateAsync(id, dto, ct);

        if (result == null)
        {
            _logger.LogWarning("Workflow detail with ID {Id} not found for update", id);
            return NotFound(new ApiResponse<PropertyWorkflowDetailsDto>
            {
                Success = false,
                Message = $"Workflow detail with ID {id} not found"
            });
        }

        return Ok(new ApiResponse<PropertyWorkflowDetailsDto>
        {
            Success = true,
            Message = "Record updated successfully",
            Items = result
        });
    }

    /// <summary>
    /// Retrieves the current workflow detail record (CurrentStatus=true) for a property by its PropertyNo.
    /// </summary>
    /// <response code="200">Returns the current workflow detail for the property</response>
    /// <response code="400">PropertyNo is required</response>
    /// <response code="404">No current workflow detail found for the given PropertyNo</response>
    [HttpGet("workflow-details/current")]
    [ProducesResponseType(typeof(ApiResponse<PropertyWorkflowDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentWorkflowDetailByPropertyNo([FromQuery] string propertyNo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(propertyNo))
            return BadRequest(new ApiResponse<PropertyWorkflowDetailsDto>
            {
                Success = false,
                Message = "PropertyNo is required"
            });

        var result = await _propertyWorkflowDetailsService.GetCurrentByPropertyNoAsync(propertyNo, ct);

        if (result is null)
        {
            _logger.LogWarning("No current workflow detail found for PropertyNo {PropertyNo}", propertyNo);
            return NotFound(new ApiResponse<PropertyWorkflowDetailsDto>
            {
                Success = false,
                Message = $"No current workflow detail found for property number '{propertyNo}'"
            });
        }

        return Ok(new ApiResponse<PropertyWorkflowDetailsDto>
        {
            Success = true,
            Message = "Record fetched successfully",
            Items = result
        });
    }

    /// <summary>
    /// Soft-deletes a workflow detail record by setting IsActive=false.
    /// </summary>
    /// <response code="200">Workflow detail deleted successfully</response>
    /// <response code="404">Workflow detail not found</response>
    [HttpDelete("workflow-details/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWorkflowDetails(int id, CancellationToken ct)
    {
        var deleted = await _propertyWorkflowDetailsService.DeleteAsync(id, ct);

        if (!deleted)
        {
            _logger.LogWarning("Workflow detail with ID {Id} not found for deletion", id);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"Workflow detail with ID {id} not found"
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Record deleted successfully"
        });
    }
}
