using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Filters;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.PropertySurveySearch;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Models;
using NtisPlatform.Application.Models;
using System.Threading;
using System.Threading.Tasks;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Handles survey-related property endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[TypeFilter(typeof(PropertyApiExceptionFilter))]
public class PropertySurveyController : ControllerBase
{
    private readonly IPropertySurveyService _propertySurveyService;

    public PropertySurveyController(IPropertySurveyService propertySurveyService)
    {
        _propertySurveyService = propertySurveyService;
    }

    /// <summary>
    /// Searches newly created properties for a module (paginated).
    /// </summary>
    [HttpGet("created-by-user")]
    [ProducesResponseType(typeof(ApiResponse<UserPropertyPageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchNewlyCreatedProperties(
        [FromQuery] CreatedByUserPropertySearchRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _propertySurveyService
            .SearchNewlyCreatedPropertiesAsync(request, cancellationToken);

        return Ok(new ApiResponse<UserPropertyPageDto>
        {
            Success = true,
            Message = result.PageItemCount == 0 ? "No properties found." : "Properties fetched successfully.",
            Items = result
        });
    }
}
