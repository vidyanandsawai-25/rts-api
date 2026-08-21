using NtisPlatform.Core.Entities;
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

    Task<(int PropertyCount, int StructureCount, int UnitCount, decimal Demand)> ReadPreviouslyRegisteredBreakdownAsync(
        CancellationToken cancellationToken = default);

    Task<(int PropertyCount, int StructureCount, int UnitCount, decimal Demand)> ReadPropertyBreakdownByAssessmentStatusAsync(
        int statusId,
        PropertySearchRequestDto? searchRequest = null,
        bool includeDemand = false,
        CancellationToken cancellationToken = default);

    Task<(int PropertyCount, int StructureCount, int UnitCount, decimal Demand)> ReadAcdApprovedPropertyBreakdownAsync(
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default);

    Task<List<PropertyWorkflowStageMasterEntity>> ReadWorkflowStagesAsync(
        int? workflowStageId = null,
        CancellationToken cancellationToken = default);

    Task<Dictionary<int, (int PropertyCount, int StructureCount, int UnitCount)>> ReadWorkflowStageCountsAsync(
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

    Task<List<int>> ReadCompletedWorkflowStageIdsAsync(
        int propertyId,
        CancellationToken cancellationToken = default);

    Task<Dictionary<int, (int? UserId, string? OfficerName)>> ReadWorkflowStageOfficerDetailsAsync(
        int propertyId,
        IEnumerable<int> stageIds,
        CancellationToken cancellationToken = default);
}
