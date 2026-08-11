using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Core.Models.AutomationDashboard;
using NtisPlatform.Application.Interfaces.AutomationDashboard;
using NtisPlatform.Core.Models;


namespace NtisPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AutomationDashboardController : ControllerBase
    {
        private readonly IAutomationDashboardService _automationDashboardService;
        private readonly ILogger<AutomationDashboardController> _logger;

        public AutomationDashboardController(
            IAutomationDashboardService automationDashboardService,
            ILogger<AutomationDashboardController> logger)
        {
            _automationDashboardService = automationDashboardService;
            _logger = logger;
        }


        [HttpGet("MainCards")]
        public async Task<ActionResult<AutomationDashboardItemsResponse<IReadOnlyList<MainCardsResponseDto>>>> GetMainCards()
        {
            try
            {
                var result = await _automationDashboardService.GetMainCardsAsync();
                return OkItem(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving main dashboard cards");
                throw;
            }
        }


        [HttpGet("WorkFlowStages")]
        public async Task<ActionResult<AutomationDashboardItemsResponse<IReadOnlyList<WorkflowStageCardDto>>>> GetWorkflowCards()
        {
            try
            {
                var result = await _automationDashboardService.GetWorkflowCardsAsync();
                return OkItems(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workflow stage cards");
                throw;
            }
        }

        [HttpGet("TrackStageStatus")]
        public async Task<ActionResult<AutomationDashboardItemsResponse<IReadOnlyList<TrackStageStatusDto>>>> TrackStageStatus([FromQuery] int propertyId,CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _automationDashboardService.TrackStageStatusAsync(propertyId, cancellationToken);
                return OkItems(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workflow stage status for property {PropertyId}", propertyId);
                throw;
            }
        }

        [HttpGet("GeoSequencingGrid")]
        public async Task<ActionResult<AutomationDashboardItemsResponse<IReadOnlyList<GeoSequencingGridResponseDto>>>> GetGeoSequencingGridData(
            [FromQuery] DashboardGridQueryParameters queryParameters,CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _automationDashboardService.GetGeoSequencingGridDataAsync(queryParameters, cancellationToken);
                return OkItem(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Geo-Sequencing grid data");
                throw;
            }
        }

        [HttpGet("GeoSequencingWardWiseSummary")]
        public async Task<ActionResult<AutomationDashboardItemsResponse<IReadOnlyList<GeoSequencingWardWiseSummaryResponseDto>>>> GetGeoSequencingWardWiseSummary(
            [FromQuery] WardWiseSummaryQueryParameters queryParameters,CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _automationDashboardService.GetGeoSequencingWardWiseSummaryAsync(queryParameters, cancellationToken);

                return OkItem(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Geo-Sequencing ward-wise summary for zone {ZoneId} and workflow stage {WorkflowStageId}", queryParameters.ZoneId, queryParameters.WorkflowStageId);
                throw;
            }
        }

        /// <summary>
        /// Returns division-wise grid data for Internal Survey workflow stage.
        /// Shows geo-sequencing properties, survey properties, property type breakdown, and assessment status.
        /// </summary>
        [HttpGet("InternalSurveyGrid")]
        public async Task<ActionResult<AutomationDashboardItemsResponse<IReadOnlyList<InternalSurveyGridResponseDto>>>> GetInternalSurveyGridData(
            [FromQuery] DashboardGridQueryParameters queryParameters,CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _automationDashboardService.GetInternalSurveyGridDataAsync(queryParameters, cancellationToken);
                return OkItem(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Internal Survey grid data");
                throw;
            }
        }

        [HttpGet("InternalSurveyWardWiseSummary")]
        public async Task<ActionResult<AutomationDashboardItemsResponse<IReadOnlyList<InternalSurveyWardWiseSummaryResponseDto>>>> GetInternalSurveyWardWiseSummary(
            [FromQuery] WardWiseSummaryQueryParameters queryParameters,CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _automationDashboardService.GetInternalSurveyWardWiseSummaryAsync(queryParameters, cancellationToken);

                return OkItem(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Internal Survey ward-wise summary for zone {ZoneId} and workflow stage {WorkflowStageId}", queryParameters.ZoneId, queryParameters.WorkflowStageId);
                throw;
            }
        }

        [HttpGet("DataEntryGrid")]
        public async Task<ActionResult<AutomationDashboardItemsResponse<IReadOnlyList<DataEntryGridResponseDto>>>> GetDataEntryGridData(
            [FromQuery] DashboardGridQueryParameters queryParameters,CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _automationDashboardService.GetDataEntryGridDataAsync(queryParameters, cancellationToken);
                return OkItem(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Data Entry grid data");
                throw;
            }
        }

        [HttpGet("DataEntryWardWiseSummary")]
        public async Task<ActionResult<AutomationDashboardItemsResponse<IReadOnlyList<DataEntryWardWiseSummaryResponseDto>>>> GetDataEntryWardWiseSummary(
            [FromQuery] WardWiseSummaryQueryParameters queryParameters,CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _automationDashboardService.GetDataEntryWardWiseSummaryAsync(queryParameters, cancellationToken);

                return OkItem(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Data Entry ward-wise summary for zone {ZoneId} and workflow stage {WorkflowStageId}", queryParameters.ZoneId, queryParameters.WorkflowStageId);
                throw;
            }
        }

        /// <summary>
        /// Returns zone-wise grid data for Assessment workflow stage.
        /// Shows property classification by type (Assessed/Unassessed/Rented) with demand calculations.
        /// </summary>
        [HttpGet("AssessmentGrid")]
        public async Task<ActionResult<AutomationDashboardItemsResponse<IReadOnlyList<AssessmentGridResponseDto>>>> GetAssessmentGridData(
            [FromQuery] AssessmentGridQueryParameters queryParameters,CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _automationDashboardService.GetAssessmentGridDataAsync(queryParameters, cancellationToken);
                return OkItem(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Assessment grid data");
                throw;
            }
        }

        [HttpGet("GetSubGridPDData")]
        public async Task<ActionResult<AutomationDashboardItemsResponse<IReadOnlyList<SubGridPDDataDto>>>> GetSubGridData([FromQuery] SubGridQueryParameters queryParameters,CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _automationDashboardService.GetSubGridDataAsync(queryParameters, cancellationToken);
                return OkItem(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Argument error retrieving workflow stage property details for zone {ZoneId} and workflow stage {WorkflowStageId}", queryParameters.ZoneId, queryParameters.WorkflowStageId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workflow stage property details for zone {ZoneId} and workflow stage {WorkflowStageId}", queryParameters.ZoneId, queryParameters.WorkflowStageId);
                throw;
            }
        }

        [HttpGet("GetWardSubGridPDData")]
        public async Task<ActionResult<AutomationDashboardItemsResponse<IReadOnlyList<SubGridPDDataDto>>>> GetWardSubGridData([FromQuery] WardSubGridQueryParameters queryParameters,CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _automationDashboardService.GetWardSubGridDataAsync(queryParameters, cancellationToken);
                return OkItem(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Argument error retrieving workflow stage property details for ward {WardId} and workflow stage {WorkflowStageId}", queryParameters.WardId, queryParameters.WorkflowStageId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workflow stage property details for ward {WardId} and workflow stage {WorkflowStageId}", queryParameters.WardId, queryParameters.WorkflowStageId);
                throw;
            }
        }

        [HttpGet("GetPendingAssessmentProps")]
        public async Task<ActionResult<AutomationDashboardItemsResponse<IReadOnlyList<PendingAssessmentSubGridPDDataDto>>>> GetPendingAssessmentProps(
            [FromQuery] PendingAssessmentQueryParameters queryParameters,CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _automationDashboardService.GetPendingAssessmentPropsAsync(queryParameters, cancellationToken);
                return OkItem(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending Assessment properties");
                throw;
            }
        }

        [HttpPost("SendToApprove")]
        public async Task<ActionResult<AutomationDashboardItemsResponse<IReadOnlyList<object>>>> SendToApprove([FromBody] SendToApproveRequestDto request,CancellationToken cancellationToken = default)
        {
            try
            {
                await _automationDashboardService.SendToApproveAsync(request, cancellationToken);
                return OkItems(Array.Empty<object>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending properties {PropertyIds} to approval", request == null ? null : GetRequestedPropertyIds(request));
                throw;
            }
        }

 
        private static List<int> GetRequestedPropertyIds(SendToApproveRequestDto request)
            => request.PropertyIds ?? new List<int>();

        private ActionResult<AutomationDashboardItemsResponse<IReadOnlyList<T>>> OkItem<T>(T item)
            => OkItems(new[] { item });

        private ActionResult<AutomationDashboardItemsResponse<IReadOnlyList<T>>> OkItems<T>(IEnumerable<T> items)
            => Ok(new AutomationDashboardItemsResponse<IReadOnlyList<T>>
            {
                Items = items.ToList()
            });
    }

    public sealed class AutomationDashboardItemsResponse<T>
    {
        public T? Items { get; set; }
    }
}

