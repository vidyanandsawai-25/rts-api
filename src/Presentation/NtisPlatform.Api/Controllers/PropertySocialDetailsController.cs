using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.PropertySocialDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PropertySocialDetailsController : ControllerBase
{
    private readonly IPropertySocialDetailsService _service;
    private readonly ILogger<PropertySocialDetailsController> _logger;

    public PropertySocialDetailsController(
        ILogger<PropertySocialDetailsController> logger,
        IPropertySocialDetailsService service)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] PropertySocialDetailsQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("{id}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreatePropertySocialDetailsDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);

    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdatePropertySocialDetailsDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);

    /// <summary>
    /// Gets comprehensive social information for a property including ALL social attributes 
    /// in parent-child hierarchy with existing values and empty attributes.
    /// </summary>
    /// <param name="propertyId">The property ID to get social information for</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Complete social attribute hierarchy with current values</returns>
    /// <response code="200">Returns all social attributes with values</response>
    /// <response code="500">If an error occurs</response>
    /// <remarks>
    /// This endpoint returns:
    /// - ALL active social attributes from SocialAttributeMaster
    /// - Parent-child hierarchy (e.g., HAS_SOLAR ? NO_OF_SOLAR)
    /// - Current values from PropertySocialDetails if they exist
    /// - Empty/null values for attributes not yet saved
    /// 
    /// Sample response structure:
    /// 
    ///     {
    ///       "propertyId": 123,
    ///       "socialAttributes": [
    ///         {
    ///           "id": 5,
    ///           "socialAttributeCode": "HAS_SOLAR",
    ///           "socialAttributeName": "Solar Installed",
    ///           "dataType": "BIT",
    ///           "bitValue": true,
    ///           "propertySocialDetailId": 100,
    ///           "children": [
    ///             {
    ///               "id": 6,
    ///               "socialAttributeCode": "NO_OF_SOLAR",
    ///               "socialAttributeName": "Number Of Solar Units",
    ///               "dataType": "INT",
    ///               "intValue": 10,
    ///               "propertySocialDetailId": 101,
    ///               "isRequiredWhenParentTrue": true,
    ///               "children": []
    ///             }
    ///           ]
    ///         }
    ///       ]
    ///     }
    /// </remarks>
    [HttpGet("property/{propertyId}/social-info")]
    [ProducesResponseType(typeof(ApiResponse<PropertySocialInfoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPropertySocialInfo(int propertyId, CancellationToken ct)
    {
        try
        {
            var result = await _service.GetPropertySocialInfoAsync(propertyId, ct);

            return Ok(new ApiResponse<PropertySocialInfoResponseDto>
            {
                Success = true,
                Message = "Property social information retrieved successfully",
                Items = result
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (NtisPlatform.Application.Exceptions.ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving social information for property {PropertyId}", propertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<PropertySocialInfoResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving property social information"
                });
        }
    }

    /// <summary>
    /// Upsert (Add/Update/Remove) property social information in a single operation.
    /// This endpoint allows you to add new social attributes, update existing ones, and remove unwanted ones.
    /// </summary>
    /// <param name="dto">Contains the property ID, social attributes to add/update, and IDs of attributes to remove</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated list of all active social attributes for the property</returns>
    /// <response code="200">Returns the updated list of property social attributes</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="500">If an error occurs during the operation</response>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PUT /api/PropertySocialDetails/upsert
    ///     {
    ///         "propertyId": 123,
    ///         "updatedBy": 1,
    ///         "socialAttributes": [
    ///             {
    ///                 "id": null,
    ///                 "socialAttributeId": 5,
    ///                 "bitValue": true,
    ///                 "remark": "Solar installed"
    ///             },
    ///             {
    ///                 "id": null,
    ///                 "socialAttributeId": 6,
    ///                 "intValue": 10,
    ///                 "remark": "10 solar units"
    ///             },
    ///             {
    ///                 "id": 10,
    ///                 "socialAttributeId": 3,
    ///                 "decimalValue": 100.5,
    ///                 "remark": "Updated road width"
    ///             }
    ///         ],
    ///         "socialAttributeIdsToRemove": [8, 9]
    ///     }
    ///     
    /// **How it works:**
    /// - **Add**: Set `id` to null or omit it, and provide the social attribute details
    /// - **Update**: Provide existing `id` with updated values
    /// - **Remove**: Add the `socialAttributeId` to `socialAttributeIdsToRemove` array (soft delete - sets IsActive = false)
    /// 
    /// **For parent-child relationships:**
    /// - When adding HAS_SOLAR = true, also add NO_OF_SOLAR with value
    /// - When removing parent, child values are preserved unless explicitly removed
    /// 
    /// All operations are performed in a single transaction for data consistency.
    /// </remarks>
    [HttpPut("upsert")]
    [ProducesResponseType(typeof(ApiResponse<List<PropertySocialDetailsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpsertPropertySocialInfo([FromBody] UpsertPropertySocialInfoDto dto, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _service.UpsertPropertySocialInfoAsync(dto, ct);

            return Ok(new ApiResponse<List<PropertySocialDetailsDto>>
            {
                Success = true,
                Message = "Property social information updated successfully",
                Items = result
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (NtisPlatform.Application.Exceptions.ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting property social information for property {PropertyId}", dto.PropertyId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<List<PropertySocialDetailsDto>>
                {
                    Success = false,
                    Message = "An error occurred while updating property social information"
                });
        }
    }
}
