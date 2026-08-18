using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Filters;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.PropertySurveySearch;
using NtisPlatform.Application.DTOs.PropertyVisitTracker;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Models;
using NtisPlatform.Application.Models;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
    private readonly IPropertyVisitTrackerService _propertyVisitTrackerService;
    private readonly ILogger<PropertySurveyController> _logger;

    public PropertySurveyController(
        IPropertySurveyService propertySurveyService,
        IPropertyVisitTrackerService propertyVisitTrackerService,
        ILogger<PropertySurveyController> logger)
    {
        _propertySurveyService = propertySurveyService;
        _propertyVisitTrackerService = propertyVisitTrackerService;
        _logger = logger;
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

    /// <summary>
    /// Inserts a property visit into PTIS.PropertyWorkflowDetails.
    /// </summary>
    [HttpPost("property-visit-tracker")]
    [ProducesResponseType(
        typeof(CreatePropertyVisitTrackerResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePropertyVisitAsync(
        [FromBody] CreatePropertyVisitTrackerDto request,
        CancellationToken cancellationToken)
    {
        var loggedInUserId = GetLoggedInUserId();

        if (loggedInUserId <= 0)
        {
            return Unauthorized(new
            {
                status = false,
                message = "Logged-in user information is invalid."
            });
        }

        var response =
            await _propertyVisitTrackerService.CreateVisitAsync(
                request,
                loggedInUserId,
                cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Gets property visit tracker records with filters and pagination.
    /// </summary>
    [HttpGet("property-visit-tracker")]
    [ProducesResponseType(
        typeof(PropertyVisitTrackerResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPropertyVisitsAsync(
        [FromQuery] PropertyVisitTrackerQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var loggedInUserId = GetLoggedInUserId();
        var loggedInRole = GetLoggedInUserRole();

        if (loggedInUserId <= 0)
        {
            return Unauthorized(new
            {
                status = false,
                message = "Logged-in user information is invalid."
            });
        }

        var response =
            await _propertyVisitTrackerService.GetVisitsAsync(
                queryParameters,
                loggedInUserId,
                loggedInRole,
                cancellationToken);

        return Ok(response);
    }

    private int GetLoggedInUserId()
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("UserId");

        return int.TryParse(userIdValue, out var userId)
            ? userId
            : 0;
    }

    private string? GetLoggedInUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ??
               User.FindFirstValue("role");
    }

    /// <summary>
    /// Creates a property survey visit with remark and location details.
    /// </summary>
    [HttpPost("survey-visit")]
    [ProducesResponseType(
        typeof(ApiResponse<CreatePropertySurveyVisitResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateSurveyVisit(
        [FromBody] CreatePropertySurveyVisitDto request,
        CancellationToken cancellationToken)
    {
        var loggedInUserId = GetLoggedInUserId();

        if (loggedInUserId <= 0)
        {
            return Unauthorized(new
            {
                status = false,
                message = "Logged-in user information is invalid."
            });
        }

        var result = await _propertyVisitTrackerService
            .CreateSurveyVisitAsync(
                request,
                loggedInUserId,
                cancellationToken);

        return Ok(new ApiResponse<CreatePropertySurveyVisitResponseDto>
        {
            Success = true,
            Message = result.Message,
            Items = result
        });
    }

    /// <summary>
    /// Verifies property after photo validation.
    /// </summary>
    [HttpPost("survey-visit/verify")]
    [ProducesResponseType(
    typeof(ApiResponse<VerifyPropertySurveyVisitResponseDto>),
    StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyPropertySurveyVisit(
        [FromBody] VerifyPropertySurveyVisitDto request,
        CancellationToken cancellationToken)
    {
        var loggedInUserId = GetLoggedInUserId();

        if (loggedInUserId <= 0)
        {
            return Unauthorized(new
            {
                status = false,
                message = "Logged-in user information is invalid."
            });
        }

        var result =
            await _propertyVisitTrackerService
            .VerifyPropertySurveyVisitAsync(
                request,
                loggedInUserId,
                cancellationToken);

        return Ok(new ApiResponse<VerifyPropertySurveyVisitResponseDto>
        {
            Success = true,
            Message = result.Message,
            Items = result
        });
    }

    /// <summary>
    /// Unverifies a property survey visit.
    /// </summary>
    [HttpPost("survey-visit/unverify")]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnverifySurveyVisit(
        [FromBody] UnverifyPropertySurveyVisitDto request,
        CancellationToken cancellationToken)
    {
        var loggedInUserId = GetLoggedInUserId();

        if (loggedInUserId <= 0)
        {
            return Unauthorized(new
            {
                status = false,
                message = "Logged-in user information is invalid."
            });
        }

        var result =
            await _propertyVisitTrackerService
            .UnverifyPropertySurveyVisitAsync(
                request,
                loggedInUserId,
                cancellationToken);

        return Ok(new ApiResponse<bool>
        {
            Success = result,
            Message = "Property unverified successfully.",
            Items = result
        });
    }
}
