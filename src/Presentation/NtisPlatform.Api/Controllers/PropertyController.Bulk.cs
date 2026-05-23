using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

public partial class PropertyController
{
    [HttpPost("Bulk")]
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
