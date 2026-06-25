using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Property search controller — exposes three read-only endpoints:
/// 1. GET /api/PropertySearch/search/grid                      — paginated property grid
/// 2. GET /api/PropertySearch/search/dashboard/main-cards      — 3 main dashboard cards
/// 3. GET /api/PropertySearch/search/dashboard/workflow-cards  — workflow stage counts
/// </summary>
[ApiController]
[Route("api/[controller]/search")]
public class PropertySearchController : ControllerBase
{
    private readonly IPropertySearchService _propertySearchService;
    private readonly ILogger<PropertySearchController> _logger;

    public PropertySearchController(
        IPropertySearchService propertySearchService,
        ILogger<PropertySearchController> logger)
    {
        _propertySearchService = propertySearchService;
        _logger = logger;
    }

    /// <summary>
    /// Returns paginated property search results.
    /// Supports Quick Search, KYC Search, and Values &amp; Dues filter parameters.
    /// </summary>
    [HttpGet("grid")]
    [ProducesResponseType(typeof(ApiResponse<PropertySearchGridResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PropertySearchGridResponseDto>>> GetSearchGrid(
        [FromQuery] PropertySearchQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (queryParameters.PageNumber < 1) queryParameters.PageNumber = 1;
            if (queryParameters.PageSize is < 1 and not -1) queryParameters.PageSize = 10;

            _logger.LogInformation(
                "Property grid search: Page={Page}, Size={Size}",
                queryParameters.PageNumber,
                queryParameters.PageSize);

            var result = await _propertySearchService.SearchPropertiesAsync(queryParameters, cancellationToken);

            return Ok(new ApiResponse<PropertySearchGridResponseDto>
            {
                Success = true,
                Message = "Search results retrieved successfully",
                Items = new PropertySearchGridResponseDto { Results = result }
            });
        }
        catch (NtisPlatform.Application.Exceptions.PropertyValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error in property grid search");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving property grid");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving search results"
            });
        }
    }

    /// <summary>
    /// Returns the 3 main dashboard cards with structure/unit/demand breakdown:
    /// Previously Registered | Assessment Approved (Assessed + Unassessed) | Additional Revenue Generated.
    ///
    /// Supported filter parameters (all optional):
    /// - propertyAssessmentStatusId — filter by PropertyAssessmentStatusMaster.Id
    /// - workflowStageId            — filter by PropertyWorkflowStageMaster.Id
    /// - propertyDescriptionId      — filter by PropertyTypeMaster.Id
    /// - zoneId                     — filter by ZoneMaster.Id
    /// - wardId                     — filter by WardMaster.Id
    /// </summary>
    [HttpGet("dashboard/main-cards")]
    [ProducesResponseType(typeof(ApiResponse<MainCardsResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<MainCardsResponseDto>>> GetMainCards(
        [FromQuery] int? propertyAssessmentStatusId = null,
        [FromQuery] int? workflowStageId = null,
        [FromQuery] int? propertyDescriptionId = null,
        [FromQuery] int? zoneId = null,
        [FromQuery] int? wardId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching main dashboard cards");

            var filter = BuildFilter(propertyAssessmentStatusId, workflowStageId, propertyDescriptionId, zoneId, wardId);
            var result = await _propertySearchService.GetMainCardsAsync(filter, cancellationToken);

            return Ok(new ApiResponse<MainCardsResponseDto>
            {
                Success = true,
                Message = "Main cards retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving main dashboard cards");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving dashboard cards"
            });
        }
    }

    /// <summary>
    /// Returns workflow stage cards showing property/structure/unit counts at each stage.
    ///
    /// Supported filter parameters (all optional):
    /// - propertyAssessmentStatusId — filter by PropertyAssessmentStatusMaster.Id
    /// - workflowStageId            — when set, returns only that stage's card
    /// - propertyDescriptionId      — filter by PropertyTypeMaster.Id
    /// - zoneId                     — filter by ZoneMaster.Id
    /// - wardId                     — filter by WardMaster.Id
    /// </summary>
    [HttpGet("dashboard/workflow-cards")]
    [ProducesResponseType(typeof(ApiResponse<List<WorkflowStageCardDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<WorkflowStageCardDto>>>> GetWorkflowCards(
        [FromQuery] int? propertyAssessmentStatusId = null,
        [FromQuery] int? workflowStageId = null,
        [FromQuery] int? propertyDescriptionId = null,
        [FromQuery] int? zoneId = null,
        [FromQuery] int? wardId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching workflow stage cards");

            var filter = BuildFilter(propertyAssessmentStatusId, workflowStageId, propertyDescriptionId, zoneId, wardId);
            var result = await _propertySearchService.GetWorkflowCardsAsync(filter, cancellationToken);

            return Ok(new ApiResponse<List<WorkflowStageCardDto>>
            {
                Success = true,
                Message = "Workflow cards retrieved successfully",
                Items = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving workflow stage cards");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while retrieving workflow cards"
            });
        }
    }

    private static PropertySearchRequestDto? BuildFilter(
        int? propertyAssessmentStatusId,
        int? workflowStageId,
        int? propertyDescriptionId,
        int? zoneId,
        int? wardId)
    {
        if (!propertyAssessmentStatusId.HasValue && !workflowStageId.HasValue &&
            !propertyDescriptionId.HasValue && !zoneId.HasValue && !wardId.HasValue)
            return null;

        return new PropertySearchRequestDto
        {
            PropertyAssessmentStatusId = propertyAssessmentStatusId,
            WorkflowStageId = workflowStageId,
            PropertyDescriptionId = propertyDescriptionId,
            ZoneId = zoneId,
            WardId = wardId
        };
    }
}
