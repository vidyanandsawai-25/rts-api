using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.PropertyPhoto;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Controller for Wing Photos operations (EntityType = 'W').
/// </summary>
[ApiController]
[Route("api/wing-photos")]
[Authorize]
public class WingPhotosController : ControllerBase
{
    private readonly IPropertyPhotoApplicationService _service;
    private readonly ILogger<WingPhotosController> _logger;

    public WingPhotosController(
        IPropertyPhotoApplicationService service,
        ILogger<WingPhotosController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// GET - All current photos for a wing.
    /// </summary>
    [HttpGet("wing/{wingId}")]
    [ProducesResponseType(typeof(ApiResponse<List<PropertyPhotoDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPhotosByWing(int wingId, CancellationToken cancellationToken)
    {
        try
        {
            if (wingId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid WingId" });

            var result = await _service.GetPhotosByWingAsync(wingId, cancellationToken);

            return Ok(new ApiResponse<List<PropertyPhotoDto>>
            {
                Success = true,
                Message = "Wing photos retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting photos for WingId={WingId}. CorrelationId: {CorrelationId}",
                wingId, correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving wing photos",
                CorrelationId = correlationId
            });
        }
    }

    /// <summary>
    /// GET - Grouped photo gallery for a wing.
    /// </summary>
    [HttpGet("wing/{wingId}/grouped")]
    [ProducesResponseType(typeof(ApiResponse<PropertyPhotoGalleryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetGroupedPhotosByWing(int wingId, CancellationToken cancellationToken)
    {
        try
        {
            if (wingId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid WingId" });

            var result = await _service.GetGroupedPhotosByWingAsync(wingId, cancellationToken);

            return Ok(new ApiResponse<PropertyPhotoGalleryDto>
            {
                Success = true,
                Message = "Grouped wing photos retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting grouped photos for WingId={WingId}. CorrelationId: {CorrelationId}",
                wingId, correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving grouped wing photos",
                CorrelationId = correlationId
            });
        }
    }

    /// <summary>
    /// GET - All active photo types with status for a wing.
    /// </summary>
    [HttpGet("types-with-status/{wingId}")]
    [ProducesResponseType(typeof(ApiResponse<List<PropertyPhotoTypeWithStatusDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPhotoTypesWithStatus(int wingId, CancellationToken cancellationToken)
    {
        try
        {
            if (wingId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid WingId" });

            var result = await _service.GetPhotoTypesWithStatusForWingAsync(wingId, cancellationToken);

            return Ok(new ApiResponse<List<PropertyPhotoTypeWithStatusDto>>
            {
                Success = true,
                Message = "Wing photo types retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting photo types with status for WingId={WingId}. CorrelationId: {CorrelationId}",
                wingId, correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving photo types for wing",
                CorrelationId = correlationId
            });
        }
    }
}
