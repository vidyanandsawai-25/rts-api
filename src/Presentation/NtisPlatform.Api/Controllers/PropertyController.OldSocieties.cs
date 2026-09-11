using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.OldSociety;

namespace NtisPlatform.Api.Controllers;

public partial class PropertyController
{
    /// <summary>
    /// Gets a list of distinct old societies with wing and flat counts.
    /// </summary>
    [HttpGet("old-societies")]
    [ProducesResponseType(typeof(OldSocietyResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OldSocietyResponseDto>> GetOldSocieties(
        [FromQuery] SearchOldSocietyDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _propertyService.GetOldSocietiesAsync(dto, cancellationToken);
        return Ok(result ?? new OldSocietyResponseDto());
    }
}
