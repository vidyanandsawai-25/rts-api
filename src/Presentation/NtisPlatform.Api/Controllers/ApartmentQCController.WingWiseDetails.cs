using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers
{
     
    public partial class ApartmentQCController : ControllerBase
    {
        /// <summary>
        /// Returns wing-wise property, area, demand, revenue-impact, collection, and exemption summary
        /// for all active apartment properties matching the ward and property number.
        /// </summary>
        /// <response code="200">Wing-wise details (empty wings list when no matches).</response>
        /// <response code="400">WardId or PropertyNo is missing.</response>
        /// <response code="500">Internal server error.</response>
        [HttpGet("wing-wise-details")]
        [ProducesResponseType(typeof(ApiResponse<WingWiseDetailsResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWingWiseDetails([FromQuery] WingWiseDetailsQueryParameters query, CancellationToken ct)
        {
            if (!query.WardId.HasValue || string.IsNullOrWhiteSpace(query.PropertyNo))
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Both 'WardId' and 'PropertyNo' are required for wing-wise details."
                });

            var result = await _wingWiseDetailsService.GetWingWiseDetailsAsync(query, ct);
            return Ok(new ApiResponse<WingWiseDetailsResponseDto>
            {
                Success = true,
                Message = result.Wings.Count > 0 ? "Record found successfully" : "No records found",
                Items = result
            });
        }
 
    }
}
