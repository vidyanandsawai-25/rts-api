using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Core.Interfaces.IAutomationDashboard;

/// <summary>
/// Repository interface for common Automation Dashboard operations.
/// Stage-specific grid summaries are handled by dedicated repositories.
/// </summary>
public interface IAutomationDashboardRepository
{
    /// <summary>
    /// Gets the 3 main dashboard cards: Previously Registered, Assessment Approved, Additional Revenue Generated.
    /// Supports optional filters: property type, type of use, zone, ward, category.
    /// </summary>
    Task<MainCardsResponseDto> GetMainCardsAsync(
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets workflow stage cards showing structure/unit counts at each stage.
    /// Supports the same optional filters as GetMainCardsAsync.
    /// </summary>
    Task<List<WorkflowStageCardDto>> GetWorkflowCardsAsync(
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

    /// <summary>
    /// Gets all workflow stages with completion status for the provided property.
    /// </summary>
    Task<List<TrackStageStatusDto>> TrackStageStatusAsync(
        int propertyId,
        CancellationToken cancellationToken = default);
}
