using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Core.Interfaces.IAutomationDashboard;

/// <summary>
/// Repository interface for Data Entry and Quality Analyst stage operations.
/// Handles division-wise grid data for Data Entry and Quality Analyst workflow stage.
/// </summary>
public interface IDataEntryStageRepository
{
    /// <summary>
    /// Reads all raw data required for the main Data Entry grid in one database round trip.
    /// </summary>
    Task<DataEntryGridSnapshotProjection> GetDataEntryGridSnapshotAsync(
        int dataEntryStageId,
        int? zoneId,
        CancellationToken cancellationToken = default,
        int? propertyTypeId = null,
        int? propertyTypeCategoryId = null);

    Task<DataEntryWardWiseSummaryResponseDto> GetDataEntryWardWiseSummaryAsync(
        int zoneId,
        int workflowStageId,
        int? pageNumber,
        int? pageSize,
        CancellationToken cancellationToken = default);
}
