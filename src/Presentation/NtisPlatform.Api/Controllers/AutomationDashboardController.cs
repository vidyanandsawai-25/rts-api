using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.AutomationDashboard;
using NtisPlatform.Application.Interfaces.AutomationDashboard;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;


namespace NtisPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AutomationDashboardController : ControllerBase
    {
        private readonly IAutomationDashboardService _automationDashboardService;

        private readonly ILogger<AutomationDashboardController> _logger;

        public AutomationDashboardController(ILogger<AutomationDashboardController> logger, IAutomationDashboardService automationDashboardService)
        {
            _logger = logger;
            _automationDashboardService = automationDashboardService;
        }


        [HttpGet("MainCards")]
        public async Task<ActionResult<ApiResponse<MainCardsResponseDto>>> GetMainCards()
        {
            try
            {

                var result = await _automationDashboardService.GetMainCardsAsync();

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


        [HttpGet("WorkFlowStages")]
        public async Task<ActionResult<ApiResponse<List<WorkflowStageCardDto>>>> GetWorkflowCards()
        {
            try
            {
                var result = await _automationDashboardService.GetWorkflowCardsAsync();
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

        [HttpGet("TrackStageStatus")]
        public async Task<ActionResult<ApiResponse<List<TrackStageStatusDto>>>> TrackStageStatus( [FromQuery] int propertyId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (propertyId <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "PropertyId parameter is required"
                    });
                }

                var result = await _automationDashboardService.TrackStageStatusAsync(propertyId, cancellationToken);

                return Ok(new ApiResponse<List<TrackStageStatusDto>>
                {
                    Success = true,
                    Message = "Workflow stage status retrieved successfully",
                    Items = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workflow stage status for property {PropertyId}", propertyId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving workflow stage status"
                });
            }
        }


        [HttpGet("GeoSequencingGrid")]
        public async Task<ActionResult<ApiResponse<GeoSequencingGridResponseDto>>> GetGeoSequencingGridData(
            int? workflowStageId = null,
            int? propertyTypeId = null,
            int? propertyTypeCategoryId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = BuildFilter(workflowStageId, propertyTypeId, propertyTypeCategoryId);
                var result = await _automationDashboardService.GetGeoSequencingGridDataAsync(filter, cancellationToken);

                return Ok(new ApiResponse<GeoSequencingGridResponseDto>
                {
                    Success = true,
                    Message = "Geo-Sequencing grid data retrieved successfully",
                    Items = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Geo-Sequencing grid data");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving Geo-Sequencing grid data"
                });
            }
        }

        [HttpGet("GeoSequencingWardWiseSummary")]
        public async Task<ActionResult<ApiResponse<GeoSequencingWardWiseSummaryResponseDto>>> GetGeoSequencingWardWiseSummary(
            int zoneId, int workflowStageId, int? pageNumber, int? pageSize, CancellationToken cancellationToken = default)
        {
            try
            {
                if (zoneId <= 0 || workflowStageId <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "ZoneId and WorkflowStageId parameters are required"
                    });
                }

                var result = await _automationDashboardService.GetGeoSequencingWardWiseSummaryAsync(
                    zoneId,
                    workflowStageId,
                    pageNumber,
                    pageSize,
                    cancellationToken);

                return Ok(new ApiResponse<GeoSequencingWardWiseSummaryResponseDto>
                {
                    Success = true,
                    Message = "Geo-Sequencing ward-wise summary retrieved successfully",
                    Items = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Geo-Sequencing ward-wise summary for zone {ZoneId} and workflow stage {WorkflowStageId}", zoneId, workflowStageId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving Geo-Sequencing ward-wise summary"
                });
            }
        }

        /// <summary>
        /// Returns division-wise grid data for Internal Survey workflow stage.
        /// Shows geo-sequencing properties, survey properties, property type breakdown, and assessment status.
        /// </summary>
        [HttpGet("InternalSurveyGrid")]
        public async Task<ActionResult<ApiResponse<InternalSurveyGridResponseDto>>> GetInternalSurveyGridData(
              int? workflowStageId = null,
              int? propertyTypeId = null,
              int? propertyTypeCategoryId = null,
              CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = BuildFilter(workflowStageId, propertyTypeId, propertyTypeCategoryId);
                var result = await _automationDashboardService.GetInternalSurveyGridDataAsync(filter, cancellationToken);

                return Ok(new ApiResponse<InternalSurveyGridResponseDto>
                {
                    Success = true,
                    Message = "Internal Survey grid data retrieved successfully",
                    Items = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Internal Survey grid data");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving Internal Survey grid data"
                });
            }
        }

        [HttpGet("InternalSurveyWardWiseSummary")]
        public async Task<ActionResult<ApiResponse<InternalSurveyWardWiseSummaryResponseDto>>> GetInternalSurveyWardWiseSummary(
            int zoneId, int workflowStageId, int? pageNumber, int? pageSize, CancellationToken cancellationToken = default)
        {
            try
            {
                if (zoneId <= 0 || workflowStageId <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "ZoneId and WorkflowStageId parameters are required"
                    });
                }

                var result = await _automationDashboardService.GetInternalSurveyWardWiseSummaryAsync(
                    zoneId,
                    workflowStageId,
                    pageNumber,
                    pageSize,
                    cancellationToken);

                return Ok(new ApiResponse<InternalSurveyWardWiseSummaryResponseDto>
                {
                    Success = true,
                    Message = "Internal Survey ward-wise summary retrieved successfully",
                    Items = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Internal Survey ward-wise summary for zone {ZoneId} and workflow stage {WorkflowStageId}", zoneId, workflowStageId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving Internal Survey ward-wise summary"
                });
            }
        }

        [HttpGet("DataEntryGrid")]
        public async Task<ActionResult<ApiResponse<DataEntryGridResponseDto>>> GetDataEntryGridData(
               int? workflowStageId = null,
               int? propertyTypeId = null,
               int? propertyTypeCategoryId = null,
              CancellationToken cancellationToken = default)
        {
            try
            {
                if (!workflowStageId.HasValue)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "WorkflowStageId parameter is required for Data Entry grid data"
                    });
                }

                var filter = BuildFilter(workflowStageId, propertyTypeId, propertyTypeCategoryId);
                var result = await _automationDashboardService.GetDataEntryGridDataAsync(filter, cancellationToken);

                return Ok(new ApiResponse<DataEntryGridResponseDto>
                {
                    Success = true,
                    Message = "Data Entry grid data retrieved successfully",
                    Items = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Data Entry grid data");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving Data Entry grid data"
                });
            }
        }

        [HttpGet("DataEntryWardWiseSummary")]
        public async Task<ActionResult<ApiResponse<DataEntryWardWiseSummaryResponseDto>>> GetDataEntryWardWiseSummary(
            int zoneId, int workflowStageId, int? pageNumber, int? pageSize, CancellationToken cancellationToken = default)
        {
            try
            {
                if (zoneId <= 0 || workflowStageId <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "ZoneId and WorkflowStageId parameters are required"
                    });
                }

                var result = await _automationDashboardService.GetDataEntryWardWiseSummaryAsync(
                    zoneId,
                    workflowStageId,
                    pageNumber,
                    pageSize,
                    cancellationToken);

                return Ok(new ApiResponse<DataEntryWardWiseSummaryResponseDto>
                {
                    Success = true,
                    Message = "Data Entry ward-wise summary retrieved successfully",
                    Items = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Data Entry ward-wise summary for zone {ZoneId} and workflow stage {WorkflowStageId}", zoneId, workflowStageId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving Data Entry ward-wise summary"
                });
            }
        }

        /// <summary>
        /// Returns zone-wise grid data for Assessment workflow stage.
        /// Shows property classification by type (Assessed/Unassessed/Rented) with demand calculations.
        /// </summary>
        [HttpGet("AssessmentGrid")]
        public async Task<ActionResult<ApiResponse<AssessmentGridResponseDto>>> GetAssessmentGridData(
              int? workflowStageId,
              string? type,
              int? propertyTypeId = null,
              int? propertyTypeCategoryId = null,
              CancellationToken cancellationToken = default)
        {
            try
            {
                if (!workflowStageId.HasValue || workflowStageId.Value <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "WorkflowStageId parameter is required for Assessment grid data"
                    });
                }

                if (string.IsNullOrWhiteSpace(type))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Type parameter is required for Assessment grid data"
                    });
                }

                if (!IsValidAssessmentGridType(type))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid Type parameter. Allowed values are Total, Assessed, Unassessed, Rented"
                    });
                }

                var filter = BuildFilter(workflowStageId, propertyTypeId, propertyTypeCategoryId);
                var result = await _automationDashboardService.GetAssessmentGridDataAsync(filter, type.Trim(), cancellationToken);

                return Ok(new ApiResponse<AssessmentGridResponseDto>
                {
                    Success = true,
                    Message = $"Assessment {type} grid data retrieved successfully",
                    Items = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Assessment grid data");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving Assessment grid data"
                });
            }
        }

        [HttpGet("GetSubGridPDData")]
        public async Task<ActionResult<ApiResponse<SubGridPDDataDto>>> GetSubGridData(
           [FromQuery] SubGridQueryParameters queryParameters, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _automationDashboardService.GetSubGridDataAsync(queryParameters, cancellationToken);
                var stageName = string.IsNullOrWhiteSpace(result.WorkflowStageName) ? "Workflow stage" : result.WorkflowStageName;


                return Ok(new ApiResponse<SubGridPDDataDto>
                {
                    Success = true,
                    Message = $"{stageName} property details fetched successfully",
                    Items = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving workflow stage property details for zone {ZoneId} and workflow stage {WorkflowStageId}",
                    queryParameters.ZoneId,
                    queryParameters.WorkflowStageId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving property details"
                });
            }
        }

        [HttpGet("GetPendingAssessmentProps")]
        public async Task<ActionResult<ApiResponse<SubGridPDDataDto>>> GetPendingAssessmentProps(int? pageNumber, int? pageSize, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _automationDashboardService.GetPendingAssessmentPropsAsync(
                    pageNumber,
                    pageSize,
                    cancellationToken);

                return Ok(new ApiResponse<SubGridPDDataDto>
                {
                    Success = true,
                    Message = "Pending Assessment properties fetched successfully",
                    Items = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending Assessment properties");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while retrieving pending Assessment properties"
                });
            }
        }

        [HttpPost("SendToApprove")]
        public async Task<ActionResult<ApiResponse<object>>> SendToApprove([FromBody] SendToApproveRequestDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null || (!HasValidPropertyId(request) && request.UserId <= 0))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "PropertyIds and UserId parameters are required"
                    });
                }

                if (!HasValidPropertyId(request))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "At least one valid PropertyId is required"
                    });
                }

                if (request.UserId <= 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "UserId parameter is required"
                    });
                }

                var result = await _automationDashboardService.SendToApproveAsync(request, cancellationToken);

                return Ok(new ApiResponse<object>
                {
                    Success = result.IsInserted,
                    Message = result.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending properties {PropertyIds} to approval", request == null ? null : GetRequestedPropertyIds(request));
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while sending property to approval"
                });
            }
        }

        private static bool IsValidAssessmentGridType(string type)
            => type.Trim().Equals("Total", StringComparison.OrdinalIgnoreCase)
               || type.Trim().Equals("Assessed", StringComparison.OrdinalIgnoreCase)
               || type.Trim().Equals("Unassessed", StringComparison.OrdinalIgnoreCase)
               || type.Trim().Equals("Rented", StringComparison.OrdinalIgnoreCase);

        private static bool HasValidPropertyId(SendToApproveRequestDto request)
            => GetRequestedPropertyIds(request).Any(id => id > 0);

        private static List<int> GetRequestedPropertyIds(SendToApproveRequestDto request)
            => request.PropertyIds ?? new List<int>();

        private static PropertySearchRequestDto? BuildFilter(
            int? workflowStageId,
            int? propertyTypeId = null,
            int? propertyTypeCategoryId = null)
        {
            var normalizedPropertyTypeId = propertyTypeId is > 0 ? propertyTypeId : null;
            var normalizedPropertyTypeCategoryId = propertyTypeCategoryId is > 0 ? propertyTypeCategoryId : null;

            if (!workflowStageId.HasValue && !normalizedPropertyTypeId.HasValue && !normalizedPropertyTypeCategoryId.HasValue)
                return null;

            return new PropertySearchRequestDto
            {
                WorkflowStageId = workflowStageId,
                PropertyTypeId = normalizedPropertyTypeId,
                PropertyTypeCategoryId = normalizedPropertyTypeCategoryId
            };
        }



    }
}
