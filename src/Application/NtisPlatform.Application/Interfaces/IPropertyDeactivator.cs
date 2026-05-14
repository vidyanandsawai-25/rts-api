namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service responsible for deactivating combined properties and their related records
/// </summary>
public interface IPropertyDeactivator
{
    /// <summary>
    /// Deactivates combined properties and all their related records
    /// </summary>
    Task DeactivateCombinedPropertiesAsync(
        List<int> propertyIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures all main property related records are active
    /// </summary>
    Task EnsureMainPropertyRecordsActiveAsync(
        int mainPropertyId,
        CancellationToken cancellationToken = default);
}