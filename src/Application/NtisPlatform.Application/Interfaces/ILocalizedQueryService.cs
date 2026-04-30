namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service for querying entities with localized field values from the multilingual table.
/// </summary>
public interface ILocalizedQueryService
{
    /// <summary>
    /// Gets the localized value for a given key and language.
    /// </summary>
    Task<string?> GetLocalizedValueAsync(string resource, string key, string language, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets localized values for multiple keys.
    /// </summary>
    Task<Dictionary<string, string>> GetLocalizedValuesAsync(string resource, IEnumerable<string> keys, string language, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for keys whose localized values contain the search term.
    /// Returns matching localization keys.
    /// </summary>
    Task<IReadOnlyList<string>> SearchLocalizedKeysAsync(
        string resource,
        string searchTerm,
        string language,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets keys where the localized value matches exactly.
    /// </summary>
    Task<IReadOnlyList<string>> GetKeysByLocalizedValueAsync(
        string resource,
        string value,
        string language,
        bool exactMatch = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batched version: resolves multiple localized values to their keys in a single DB round-trip.
    /// Returns a dictionary where each input value maps to the list of matching keys.
    /// </summary>
    Task<Dictionary<string, IReadOnlyList<string>>> GetKeysByLocalizedValuesBatchAsync(
        string resource,
        IEnumerable<string> values,
        string language,
        bool exactMatch = true,
        CancellationToken cancellationToken = default);
}