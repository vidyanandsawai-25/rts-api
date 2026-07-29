using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property Merge API - Partial controller for property merge operations
/// Handles merging of old property into new property with ward-level data transfer
/// Uses [FromServices] for dependency injection to avoid polluting main constructor
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Merge old property into a new property.
    /// This operation transfers data from the old property to the new property within the same ward,
    /// creates history records, and updates property relationships.
    /// </summary>
    /// <param name="request">Request containing PropertyOldId, PropertyId, WardId, and UpdatedBy</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Response with success status and merge details</returns>
    /// <response code="200">Properties merged successfully</response>
    /// <response code="400">Invalid request - validation error</response>
    [HttpPost("merge")]
    [ProducesResponseType(typeof(ApiResponse<PropertyMergeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MergePropertyAsync([FromBody] PropertyMergeDto request,CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.MergePropertyAsync(request, ct);
            var response = new ApiResponse<PropertyMergeDto> { Success = result.Success, Message = result.Message, Items = null };
            return result.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Error merging properties - PropertyOldId: {PropertyOldId}, PropertyId: {PropertyId}, WardId: {WardId}",
                request?.PropertyOldIds?.FirstOrDefault(),
                request?.PropertyIds?.FirstOrDefault(),
                request?.WardId);

            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyMergeDto>
                {
                    Success = false,
                    Message = "An error occurred while merging properties"
                });
        }
    }

    /// <summary>
    /// Gets detailed merge information for a single property
    /// </summary>
    /// <param name="propertyId">Property ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Property merge details</returns>
    [HttpGet("{propertyId}/merge-details")]
    [ProducesResponseType(typeof(PropertyMergeDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPropertyMergeDetailsById(int propertyId,CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _propertyService.GetPropertyMergeDetailsAsync(propertyId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Error occurred while fetching property merge details for property {PropertyId}",propertyId);

            return StatusCode(StatusCodes.Status500InternalServerError,
                new PropertyMergeDetailResponse
                {
                    Success = false,
                    Message = "An error occurred while processing your request"
                });
        }
    }

    /// <summary>
    /// Demerges a property from specified old properties
    /// </summary>
    /// <param name="dto">Demerge configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Demerge operation response</returns>
    /// <remarks>
    /// 1. Clear PropertyMastOldId from the property
    /// 2. Mark PropertyMapDetail records as CANCELLED and inactive
    /// 3. Clean up any existing CANCELLED records
    /// </remarks>
    [HttpPost("demerge")]
    [ProducesResponseType(typeof(PropertyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PropertyResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DemergeProperty([FromBody] PropertyDemergeDto dto,CancellationToken cancellationToken = default)
    {
        try
        {
            if (dto == null)
            {
                return BadRequest(new PropertyResponse
                {
                    Success = false,
                    Message = "Invalid request data"
                });
            }
            var result = await _propertyService.DemergePropertyAsync(dto, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Error occurred while demerging property {PropertyIds}",dto?.PropertyIds);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new PropertyResponse
                {
                    Success = false,
                    Message = "An error occurred while demerging properties"
                });
        }
    }

    /// <summary>
    /// Merge multiple old properties to multiple new properties (batch one-to-one merge)
    /// </summary>
    /// <param name="request">List of property merge pairs</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Response with success status and merge details</returns>
    [HttpPost("merge-multiple")]
    [ProducesResponseType(typeof(ApiResponse<PropertyMergeMultipleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MergeMultiplePropertyAsync([FromBody] PropertyMergeMultipleDto request,CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.MergeMultiplePropertyAsync(request, ct);
            var response = new ApiResponse<PropertyMergeMultipleDto> { Success = result.Success, Message = result.Message, Items = null };
            return result.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Error merging multiple properties - User: {UserId}, PairCount: {PairCount}",request?.UserId,request?.PropertyIdList?.Count ?? 0);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyMergeMultipleDto>
                {
                    Success = false,
                    Message = "An error occurred while merging properties"
                });
        }
    }

    /// <summary>
    /// Demerge multiple properties (batch demerge)
    /// </summary>
    /// <param name="request">List of property demerge configurations</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Response with success status and demerge details</returns>
    [HttpPost("demerge-multiple")]
    [ProducesResponseType(typeof(ApiResponse<PropertyDemergeMultipleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DemergeMultiplePropertyAsync([FromBody] PropertyDemergeMultipleDto request,CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.DemergeMultiplePropertyAsync(request, ct);
            var response = new ApiResponse<PropertyDemergeMultipleDto> { Success = result.Success, Message = result.Message, Items = null };
            return result.Success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Error demerging multiple properties - User: {UserId}, PropertyCount: {PropertyCount}",request?.UserId,request?.PropertyIdList?.Count ?? 0);

            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyDemergeMultipleDto>
                {
                    Success = false,
                    Message = "An error occurred while demerging properties"  
                });
        }
    }

    /// <summary>
    /// Gets unmerge property details based on property type (NEW or OLD)
    /// </summary>
    /// <param name="request">Unmerge detail request with PropertyType</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Unmerge property details</returns>
    [HttpGet("unmerge-details")]
    [ProducesResponseType(typeof(PagedResults<PropertyUnMergeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedResults<OldPropertyUnMergeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUnMergePropertyDetailsAsync([FromQuery] UnMergePropertydetailDto request,CancellationToken ct)
    {
        try
        {
            var propertyType = request.PropertyType.ToUpperInvariant();
            if (propertyType == "NEW")
            {
                return Ok(await _propertyService.GetUnMergePropertyDetailsAsync(request, ct));
            }
            else if (propertyType == "OLD")
            {
                return Ok(await _propertyService.GetUnMergeOldPropertyDetailsAsync(request, ct));
            }
            else
            {
                return BadRequest(new ApiResponse<PropertyUnMergeResponseDto>  
                {
                    Success = false,
                    Message = "Invalid PropertyType. Must be 'New' or 'Old'"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Error occurred while fetching unmerge property details - PropertyType: {PropertyType}, Request: {@Request}",
                request?.PropertyType,request);

            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertyUnMergeResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while fetching unmerge property details"  // ✅ FIXED: More specific message
                });
        }
    }
}
