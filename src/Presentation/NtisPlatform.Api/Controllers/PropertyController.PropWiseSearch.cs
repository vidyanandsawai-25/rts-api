using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property-Wise Search API — thin HTTP adapter.
/// Business logic lives in <c>PropertySearchService</c>;
/// exception-to-HTTP mapping is handled by <c>PropertyApiExceptionFilter</c>.
/// </summary>
public partial class PropertyController
{
    /// <summary>
    /// Finds the property (or properties) under a ward identified by PropertyNo and, optionally,
    /// PartitionNo. Equivalent to search-by-category with SearchCategory=BuildingWise, exposed
    /// under simpler, purpose-specific parameter names.
    /// </summary>
    /// <response code="200">Returns the paged list of properties matching WardId/PropertyNo/PartitionNo</response>
    /// <response code="400">If WardId or PropertyNo is missing</response>
    [HttpGet("propwisesearch")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PropertySuggestionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PropWiseSearch([FromQuery] PropertyWiseSearchQueryParameters queryParameters, CancellationToken ct)
    {
        var result = await _propertySearchService.SearchByCategoryAsync(new PropertySearchByCategoryQueryParameters
        {
            SearchCategory = PropertySearchCategory.BuildingWise,
            WardId = queryParameters.WardId,
            PropertyNo = queryParameters.PropertyNo,
            PartitionNo = queryParameters.PartitionNo,
            PageNumber = queryParameters.PageNumber,
            PageSize = queryParameters.PageSize
        }, ct);

        var items = result.Items.Select(x => new PropertySuggestionDto
        {
            PropertyId = x.PropertyId,
            ZoneId = x.ZoneId ?? 0,
            ZoneNo = x.ZoneNo,
            WardId = x.WardId,
            WardNo = x.WardNo,
            PropertyNo = x.PropertyNo,
            PartitionNo = x.PartitionNo,
            UpicId = x.UPICId,
            DisplayLabel = string.IsNullOrEmpty(x.PartitionNo)
                ? (x.PropertyNo ?? string.Empty)
                : $"{x.PropertyNo}-{x.PartitionNo}"
        }).ToList();

        return Ok(new ApiResponse<PagedResult<PropertySuggestionDto>>
        {
            Success = true,
            Message = result.TotalCount > 0
                ? $"{result.TotalCount} record(s) found"
                : "No records found matching the search criteria",
            Items = new PagedResult<PropertySuggestionDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            }
        });
    }

    /// <summary>
    /// Returns PropertyNo/PartitionNo typeahead suggestions for a ward, matching the given terms
    /// anywhere in the value (SQL LIKE '%term%' semantics) — e.g. PropertyNo="1" matches "123", "812", "10".
    /// Intended for a progressive UI: pick WardId, then type a partial PropertyNo (and optionally
    /// PartitionNo) to narrow the suggestion list. Backed by a short-lived per-ward cache, so only
    /// the first request for a given ward hits the database — subsequent keystrokes are served
    /// from memory until the cache entry expires.
    /// </summary>
    /// <response code="200">Returns up to MaxResults matching suggestions</response>
    /// <response code="400">If WardId is missing</response>
    [HttpGet("propwisesearch/suggestions")]
    [ProducesResponseType(typeof(ApiResponse<List<PropertySuggestionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PropWiseSearchSuggestions([FromQuery] PropertySuggestionQueryParameters queryParameters, CancellationToken ct)
    {
        var result = await _propertySearchService.GetPropertySuggestionsAsync(
            queryParameters.WardId,
            queryParameters.PropertyNo,
            queryParameters.PartitionNo,
            queryParameters.MaxResults,
            ct);

        return Ok(new ApiResponse<List<PropertySuggestionDto>>
        {
            Success = true,
            Message = result.Count > 0
                ? $"{result.Count} suggestion(s) found"
                : "No matching suggestions found",
            Items = result
        });
    }
}
