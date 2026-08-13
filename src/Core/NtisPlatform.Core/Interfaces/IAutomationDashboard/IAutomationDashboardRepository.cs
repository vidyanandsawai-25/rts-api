using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Core.Interfaces.IAutomationDashboard;

/// <summary>
/// Repository interface for common Automation Dashboard operations.
/// Stage-specific grid summaries are handled by dedicated repositories.
/// </summary>
public interface IAutomationDashboardRepository
{
    Task<Dictionary<string, int>> ReadAssessmentStatusIdsAsync(CancellationToken cancellationToken = default);

    Task<DashboardCardBreakdownProjection> ReadPreviouslyRegisteredBreakdownAsync(
        CancellationToken cancellationToken = default);

    Task<DashboardCardBreakdownProjection> ReadPropertyBreakdownByAssessmentStatusAsync(
        int statusId,
        PropertySearchRequestDto? searchRequest = null,
        bool includeDemand = false,
        CancellationToken cancellationToken = default);

    Task<DashboardCardBreakdownProjection> ReadAcdApprovedPropertyBreakdownAsync(
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default);

    Task<List<WorkflowStageProjection>> ReadWorkflowStagesAsync(
        int? workflowStageId = null,
        CancellationToken cancellationToken = default);

    Task<Dictionary<int, WorkflowStageCountProjection>> ReadWorkflowStageCountsAsync(
        IEnumerable<int> stageIds,
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets common property details sub-grid data for a zone and workflow stage.
    /// </summary>
    Task<SubGridDataProjection> GetSubGridDataAsync(
        SubGridQueryParameters query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets common property details sub-grid data for a ward and workflow stage.
    /// </summary>
    Task<SubGridDataProjection> GetSubGridDataAsync(
        WardSubGridQueryParameters query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets Assessment stage properties that have not yet entered the signature approval table.
    /// </summary>
    Task<SubGridDataProjection> GetPendingAssessmentPropsAsync(
        PendingAssessmentQueryParameters query,
        CancellationToken cancellationToken = default);

    Task<List<WorkflowStageCompletionProjection>> ReadWorkflowStageCompletionsAsync(
        int propertyId,
        CancellationToken cancellationToken = default);
}
