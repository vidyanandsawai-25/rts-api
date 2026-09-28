using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.PropertyAmenity;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PropertyAmenityController : ControllerBase
    {
        private readonly IPropertyAmenityService _propertyAmenityService;
        private readonly ILogger<PropertyAmenityController> _logger;

        public PropertyAmenityController(
            IPropertyAmenityService propertyAmenityService,
            ILogger<PropertyAmenityController> logger)
        {
            _propertyAmenityService = propertyAmenityService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the maximum amenity partition number and calculates the next available one.
        /// Supports optional filtering by WingDetailsId (from WingDetailsMast) or SocietyDetailId for multi-society properties.
        /// </summary>
        /// <param name="request">Query parameters including UserId, WardId, BasePropertyNo, optional SocietyDetailId, and optional WingDetailsId</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Response containing last and next amenity partition information</returns>
        /// <response code="200">Returns the max amenity partition details</response>
        /// <response code="400">Invalid request parameters or business validation failure</response>
        /// <response code="500">Unexpected server error</response>
        [HttpGet("MaxPropertyAmenity")]
        [ProducesResponseType(typeof(ApiResponse<GetMaxPropertyAmenityResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMaxPropertyAmenity([FromQuery] MaxPropertyAmenityQueryParameters request, CancellationToken ct)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _propertyAmenityService.GetMaxPropertyAmenityAsync(request, ct);
                if (!result.Success)
                {
                    return BadRequest(result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching max property amenity number.");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<GetMaxPropertyAmenityResponseDto>
                {
                    Success = false,
                    Message = "An unexpected error occurred while fetching max property amenity number."
                });
            }
        }

        /// <summary>
        /// Retrieves all amenities based on ward and property prefix.
        /// </summary>
        /// <param name="request">Query parameters including WardId and PropertyNo</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>List of matching amenities</returns>
        [HttpGet("Amenities")]
        [ProducesResponseType(typeof(PagedResult<AmenityPropertyDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllAmenities([FromQuery] AmenityQueryParameters request, CancellationToken ct)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _propertyAmenityService.GetAllAmenitiesAsync(request, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching all amenities.");
                return StatusCode(StatusCodes.Status500InternalServerError, new PagedResult<AmenityPropertyDto>());
            }
        }

        /// <summary>
        /// Soft deletes an amenity. This invokes the overridden DeleteAsync service 
        /// which ensures both the main Property and PropertyDetails are soft-deleted.
        /// </summary>
        /// <param name="propertyId">The ID of the amenity property to soft delete.</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Boolean indicating success</returns>
        [HttpDelete("{propertyId}/Amenities")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SoftDeleteAmenity([FromRoute] int propertyId, CancellationToken ct)
        {
            try
            {
                if (propertyId <= 0)
                    return BadRequest(new ApiResponse<bool> { Success = false, Message = "Invalid PropertyId." });

                // Invoke the generic overridden service method
                var success = await _propertyAmenityService.DeleteAsync(propertyId, ct);

                if (!success)
                {
                    return BadRequest(new ApiResponse<bool> { Success = false, Message = "Failed to delete amenity or it was not found." });
                }

                return Ok(new ApiResponse<bool> { Success = true, Message = "Amenity soft-deleted successfully.", Items = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while soft deleting amenity property {PropertyId}.", propertyId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An unexpected error occurred while soft deleting amenity."
                });
            }
        }

        /// <summary>
        /// Updates a selected amenity according to propertyId.
        /// Updates both PropertyMast and PropertyDetails table data simultaneously.
        /// </summary>
        /// <param name="propertyId">The ID of the amenity property to update.</param>
        /// <param name="dto">The updated amenity details.</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The updated amenity</returns>
        [HttpPatch("{propertyId}/Amenities")]
        [ProducesResponseType(typeof(ApiResponse<AmenityPropertyDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateAmenity([FromRoute] int propertyId, [FromBody] UpdateAmenityDto dto, CancellationToken ct)
        {
            try
            {
                if (propertyId <= 0)
                    return BadRequest(new ApiResponse<AmenityPropertyDto> { Success = false, Message = "Invalid PropertyId." });

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _propertyAmenityService.UpdateAmenityAsync(propertyId, dto, ct);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating amenity property {PropertyId}.", propertyId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<AmenityPropertyDto>
                {
                    Success = false,
                    Message = "An unexpected error occurred while updating amenity."
                });
            }
        }
    } 
}
