using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;
using NtisPlatform.Application.DTOs.Master.PropertyWorkflowStageMaster;
using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Application.Interfaces.AutomationDashboard
{
    /// <summary>
    /// Service interface for Automation Dashboard operations.
    /// Orchestrates calls to dedicated stage repositories.
    /// </summary>
    public interface IAutomationDashboardService
    {
        // Main Dashboard Cards
        Task<MainCardsResponseDto> GetMainCardsAsync();

        // Workflow Stage Cards
        Task<List<WorkflowStageCardDto>> GetWorkflowCardsAsync();

        // Stage-Specific Grids
        Task<GeoSequencingGridResponseDto> GetGeoSequencingGridDataAsync(
            DashboardGridQueryParameters queryParameters,
            CancellationToken cancellationToken = default);

        Task<GeoSequencingWardWiseSummaryResponseDto> GetGeoSequencingWardWiseSummaryAsync(
            WardWiseSummaryQueryParameters queryParameters,
            CancellationToken cancellationToken = default);

        Task<InternalSurveyGridResponseDto> GetInternalSurveyGridDataAsync(
            DashboardGridQueryParameters queryParameters,
            CancellationToken cancellationToken = default);

        Task<InternalSurveyWardWiseSummaryResponseDto> GetInternalSurveyWardWiseSummaryAsync(
            WardWiseSummaryQueryParameters queryParameters,
            CancellationToken cancellationToken = default);

        Task<DataEntryGridResponseDto> GetDataEntryGridDataAsync(
            DashboardGridQueryParameters queryParameters,
            CancellationToken cancellationToken = default);

        Task<DataEntryWardWiseSummaryResponseDto> GetDataEntryWardWiseSummaryAsync(
            WardWiseSummaryQueryParameters queryParameters,
            CancellationToken cancellationToken = default);

        Task<AssessmentGridResponseDto> GetAssessmentGridDataAsync(
            AssessmentGridQueryParameters queryParameters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets property details for a specific zone and workflow stage.
        /// Shows all properties with new vs old details comparison.
        /// Used as sub-grid data when row is clicked on main grid.
        /// </summary>
        Task<SubGridPDDataDto> GetSubGridDataAsync(
            SubGridQueryParameters queryParameters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets property details for a specific ward and workflow stage.
        /// </summary>
        Task<SubGridPDDataDto> GetWardSubGridDataAsync(
            WardSubGridQueryParameters queryParameters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets Assessment stage properties that are still pending signature approval.
        /// </summary>
        Task<PendingAssessmentSubGridPDDataDto> GetPendingAssessmentPropsAsync(
            PendingAssessmentQueryParameters queryParameters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends one Assessment property to Clerk approval.
        /// </summary>
        Task<SendToApproveResponseDto> SendToApproveAsync(
            SendToApproveRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all workflow stages with completion status for the provided property.
        /// </summary>
        Task<List<PropertyWorkflowStageMasterDto>> TrackStageStatusAsync( int propertyId, CancellationToken cancellationToken = default);
    }
}

