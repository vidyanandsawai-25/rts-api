using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

public partial class PropertyController
{
    /// <summary>
    /// Splits an existing property into one or more new sub-properties with sequential alphabetic suffixes (A, B, ... Z, AA, AB, ...).
    /// Each new property is cloned from the parent and assigned a unique property number.
    /// </summary>
    /// <param name="dto">The split request containing the base property number, ward, and number of splits to generate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of <see cref="PropertySpiltResponseDto"/> containing the generated property numbers.</returns>
    /// <response code="200">Split properties created successfully.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpPost("split-property")]
    [ProducesResponseType(typeof(PropertySplitResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SplitProperty([FromBody] PropertySplitCreateDto dto, CancellationToken cancellationToken = default)
        => Ok(await _propertyService.SplitProperty(dto, cancellationToken));
}
