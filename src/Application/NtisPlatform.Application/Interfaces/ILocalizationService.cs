namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Provides runtime translations from an in-memory dictionary.
/// Backed by DB data, loaded on startup and reloadable via API.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Returns translation for given (resource, culture, key).
    /// Uses fallback chain: requested culture -> English -> key.
    /// </summary>
    string GetTranslation(string resource, string language, string key);

    /// <summary>
    /// Returns translations for multiple keys in one call.
    /// Uses fallback chain for each key.
    /// </summary>
    Dictionary<string, string> GetTranslations(string resource, string language, IEnumerable<string> keys);

    /// <summary>
    /// Tries to get translation for exact culture (no fallback).
    /// Returns true if found in cache, false otherwise.
    /// </summary>
    bool TryGetTranslation(string resource, string language, string key, out string? value);

    /// <summary>
    /// Gets translations for exact culture (no fallback).
    /// Returns dictionary with only keys that were found in cache.
    /// Missing keys are NOT included in the result.
    /// </summary>
    Dictionary<string, string> GetTranslationsExact(string resource, string language, IEnumerable<string> keys);

    /// <summary>
    /// Sets a translation directly in the cache (used after DB writes).
    /// </summary>
    void SetTranslation(string resource, string language, string key, string value);

    /// <summary>
    /// Removes cached translations.
    /// - If only resource is given: removes all cultures for that resource
    /// - If resource + culture: removes that bucket (resource||culture)
    /// - If resource + culture + key: removes only that key inside the bucket
    /// - If resource is null/empty: clears everything
    /// </summary>
    void Invalidate(string? resource = null, string? language = null, string? key = null);

    /// <summary>
    /// Reloads translations from DB into memory.
    /// - If resource/culture/key are null: reloads everything
    /// - If resource is provided: reloads only that resource
    /// - If culture is provided: reloads only that culture bucket(s) for the resource
    /// - If excludeGenerated is true: skips per-entity generated keys (IsGenerated=true) to reduce memory footprint
    /// NOTE: In pivot-table model, "key" refresh typically reloads the whole resource bucket from DB.
    /// </summary>
    Task ReloadAsync(string? resource = null, string? language = null, string? key = null, bool excludeGenerated = false, CancellationToken ct = default);

    /// <summary>
    /// Convenience method: Invalidate(...) then ReloadAsync(...).
    /// Use this after DB updates to ensure cache has latest values.
    /// </summary>
    Task RefreshAsync(string? resource = null, string? language = null, string? key = null, CancellationToken ct = default);

    /// <summary>
    /// Returns cache statistics: bucket key -> number of keys in bucket.
    /// Useful for debugging and monitoring.
    /// </summary>
    Dictionary<string, int> GetCacheStats();
}