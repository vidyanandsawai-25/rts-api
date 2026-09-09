using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.PropertyPhoto;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Controller for Society Photos operations (EntityType = 'S').
/// </summary>
[ApiController]
[Route("api/society-photos")]
[Authorize]
public class SocietyPhotosController : ControllerBase
{
    private readonly IPropertyPhotoApplicationService _service;
    private readonly ILogger<SocietyPhotosController> _logger;

    public SocietyPhotosController(
        IPropertyPhotoApplicationService service,
        ILogger<SocietyPhotosController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// GET - All current photos for a society.
    /// </summary>
    [HttpGet("society/{societyId}")]
    [ProducesResponseType(typeof(ApiResponse<List<PropertyPhotoDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPhotosBySociety(int societyId, CancellationToken cancellationToken)
    {
        try
        {
            if (societyId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid SocietyId" });

            var result = await _service.GetPhotosBySocietyAsync(societyId, cancellationToken);

            return Ok(new ApiResponse<List<PropertyPhotoDto>>
            {
                Success = true,
                Message = "Society photos retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting photos for SocietyId={SocietyId}. CorrelationId: {CorrelationId}",
                societyId, correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving society photos",
                CorrelationId = correlationId
            });
        }
    }

    /// <summary>
    /// GET - Grouped photo gallery for a society.
    /// </summary>
    [HttpGet("society/{societyId}/grouped")]
    [ProducesResponseType(typeof(ApiResponse<PropertyPhotoGalleryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetGroupedPhotosBySociety(int societyId, CancellationToken cancellationToken)
    {
        try
        {
            if (societyId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid SocietyId" });

            var result = await _service.GetGroupedPhotosBySocietyAsync(societyId, cancellationToken);

            return Ok(new ApiResponse<PropertyPhotoGalleryDto>
            {
                Success = true,
                Message = "Grouped society photos retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting grouped photos for SocietyId={SocietyId}. CorrelationId: {CorrelationId}",
                societyId, correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving grouped society photos",
                CorrelationId = correlationId
            });
        }
    }

    /// <summary>
    /// GET - All active photo types with status for a society.
    /// </summary>
    [HttpGet("types-with-status/{societyId}")]
    [ProducesResponseType(typeof(ApiResponse<List<PropertyPhotoTypeWithStatusDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPhotoTypesWithStatus(int societyId, CancellationToken cancellationToken)
    {
        try
        {
            if (societyId <= 0)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid SocietyId" });

            var result = await _service.GetPhotoTypesWithStatusForSocietyAsync(societyId, cancellationToken);

            return Ok(new ApiResponse<List<PropertyPhotoTypeWithStatusDto>>
            {
                Success = true,
                Message = "Society photo types retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogError(ex, "Error getting photo types with status for SocietyId={SocietyId}. CorrelationId: {CorrelationId}",
                societyId, correlationId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving photo types for society",
                CorrelationId = correlationId
            });
        }
    }
}
