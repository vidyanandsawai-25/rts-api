using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Partial class for bulk property creation endpoint.
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Creates multiple properties in bulk within a single transaction.
    /// </summary>
    /// <param name="items">Array of property creation details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Bulk result with success/failure counts and created property details</returns>
    [HttpPost("Bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BulkCreate([FromBody] CreateBulkPropertyDto[]? items, CancellationToken ct)
    {
        try
        {
            if (items == null || items.Length == 0)
            {
                return BadRequest(new CreateBulkPropertyResponseDto
                {
                    Message = "Please enter property details."
                });
            }

            var res = await _propertyService.BulkCreateAsync(items, ct);
            return Ok(res);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating bulk properties");
            return BadRequest(new CreateBulkPropertyResponseDto { Message = ex.Message });
        }
    }
}
