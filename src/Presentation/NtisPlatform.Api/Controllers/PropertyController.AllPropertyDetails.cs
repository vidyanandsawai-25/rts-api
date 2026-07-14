using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

public partial class PropertyController
{
    /// <summary>
    /// Updates all property details (PropertyMast, SocietyDetailsMast, PropertyMastDetails,
    /// PropertyDetails, RoomWiseSubmissionDetails) within a single transaction.
    /// </summary>
    /// <param name="propertyId">The ID of the property to update.</param>
    /// <param name="dto">The data transfer object containing all the details to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Property details updated successfully</response>
    /// <response code="404">Property not found</response>
    /// <response code="400">Invalid data — validation error</response>
    [HttpPut("{propertyId}/all-details")]
    [ProducesResponseType(typeof(UpdateAllPropertyDetailsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UpdateAllPropertyDetailsResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAllPropertyDetails(int propertyId, [FromBody] UpdateAllPropertyDetailsDto dto, CancellationToken ct)
    {
        var result = await _propertyService.UpdatePropertyAsync(propertyId, dto, ct);

        if (!result.Success && result.Message == NtisPlatform.Core.Constants.PropertyConstants.ErrorMessages.NotFound)
        {
            _logger.LogWarning("Property with ID {PropertyId} not found for all details update", propertyId);
            return NotFound(result);
        }

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
