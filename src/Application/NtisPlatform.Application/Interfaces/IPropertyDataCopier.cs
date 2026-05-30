namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service responsible for copying property data from combined properties to main property
/// </summary>
public interface IPropertyDataCopier
{
    /// <summary>
    /// Copies all property data (details, toilet counts, room data) from combined properties to main property
    /// </summary>
    /// <param name="mainPropertyId">The main property ID to copy data into</param>
    /// <param name="combinePropertyIds">List of property IDs to combine</param>
    /// <param name="createdBy">User ID who initiated the operation</param>
    /// <param name="mergeOwnerNames">If true, merges distinct owner names from all properties into a comma-separated string</param>
    /// <param name="propertyTypeId">If provided, updates the PropertyTypeId on the main property</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CopyPropertyDataAsync(
        int mainPropertyId,
        List<int> combinePropertyIds,
        int? createdBy,
        bool mergeOwnerNames = false,
        int? propertyTypeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates toilet counts in the main property by adding counts from combined properties
    /// </summary>
    Task UpdateMainPropertyToiletCountsAsync(
        int mainPropertyId,
        List<int> combinePropertyIds,
        CancellationToken cancellationToken = default);
}