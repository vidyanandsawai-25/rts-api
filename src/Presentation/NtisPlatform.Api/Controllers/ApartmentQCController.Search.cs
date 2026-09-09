using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

public partial class ApartmentQCController
{
    /// <summary>
    /// ApartmentQC Search — a ward-scoped PropertyNo/PartitionNo typeahead (mirrors
    /// <c>GET api/Property/propwisesearch/suggestions</c>) where every suggestion is additionally
    /// resolved to a Society, Unit, or IndividualProperty category so the frontend knows which
    /// screen to navigate to once the user picks one: Society lands on the apartment/society-level
    /// screen (with its wing list attached, since WingDetailsMast has no PropertyId of its own to
    /// search by); Unit and IndividualProperty both land on the individual-property screen for that
    /// PropertyId.
    /// </summary>
    /// <response code="200">Returns up to MaxResults matching suggestions.</response>
    /// <response code="400">If WardId is missing.</response>
    [Authorize]
    [HttpGet("search/suggestions")]
    [ProducesResponseType(typeof(ApiResponse<List<ApartmentQcSearchSuggestionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSuggestions([FromQuery] ApartmentQcSearchQueryParameters queryParameters, CancellationToken ct)
    {
        if (queryParameters.WardId <= 0)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "WardId is required."
            });
        }

        var result = await _searchService.GetSuggestionsAsync(
            queryParameters.WardId,
            queryParameters.PropertyNo,
            queryParameters.PartitionNo,
            queryParameters.MaxResults,
            ct);

        return Ok(new ApiResponse<List<ApartmentQcSearchSuggestionDto>>
        {
            Success = true,
            Message = result.Count > 0
                ? $"{result.Count} suggestion(s) found"
                : "No matching suggestions found",
            Items = result
        });
    }
}
