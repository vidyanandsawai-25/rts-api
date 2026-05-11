using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property Generation API - Partial controller for property structure generation endpoints
/// Handles the `generate-propertystructure` API endpoint for vertical property generation
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Handles the `generate-buildingstructure` API endpoint for vertical property generation
    /// Creates a cross join of floors and units, ordered by UnitNo then FloorNo.
    /// </summary>
    /// <param name="dto">The generation parameters including floor range, units per floor, and wing info</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of generated property structures with RowNo, FloorNo, UnitNo, FlatNo, and PartitionNo</returns>
    /// <response code="200">Returns the list of generated property structures</response>
    /// <response code="400">Invalid input parameters</response>
    [HttpGet("generate-buildingstructure")]
    [ProducesResponseType(typeof(ApiResponse<List<BuildingGenerateStructureDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetGeneratebuildingStructure([FromQuery] BuildingGenerateDetailsDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _propertyService.GetGenerateBuildingStructureAsync(dto, ct);

            if (result == null || result.Count == 0)
            {
                _logger.LogWarning("No building structures generated for the given parameters: {@Dto}", dto);
                return Ok(new ApiResponse<List<BuildingGenerateStructureDto>>
                {
                    Success = true,
                    Message = "No building structures generated",
                    Items = result ?? []
                });
            }

            return Ok(new ApiResponse<List<BuildingGenerateStructureDto>>
            {
                Success = true,
                Message = $"{result.Count} building structures generated successfully",
                Items = result
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error generating building structure: {Message}", ex.Message);
            return BadRequest(new ApiResponse<List<BuildingGenerateStructureDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating building structure.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ApiResponse<List<BuildingGenerateStructureDto>>
                {
                    Success = false,
                    Message = "An unexpected error occurred while generating building structure."
                });
        }
    }
}
