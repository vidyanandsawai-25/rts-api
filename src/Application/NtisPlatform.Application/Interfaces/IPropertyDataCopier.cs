namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service responsible for copying property data from combined properties to main property
/// </summary>
public interface IPropertyDataCopier
{
    /// <summary>
    /// Copies all property data (details, toilet counts, room data) from combined properties to main property
    /// </summary>
    Task CopyPropertyDataAsync(
        int mainPropertyId,
        List<int> combinePropertyIds,
        int? createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates toilet counts in the main property by adding counts from combined properties
    /// </summary>
    Task UpdateMainPropertyToiletCountsAsync(
        int mainPropertyId,
        List<int> combinePropertyIds,
        CancellationToken cancellationToken = default);
}