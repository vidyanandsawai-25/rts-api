namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Dynamic security settings service (values from ConfigKeyMaster/ConfigValueMaster).
/// </summary>
public interface ISecuritySettingsService
{
    /// <summary>
    /// Get a required security setting by key (example: "MaxFailedAttempts").
    /// Throws <see cref="InvalidOperationException"/> if the key is missing or empty to prevent silent security failures.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the security setting is missing or empty.</exception>
    Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get an optional security setting by key with an explicit default value.
    /// Returns the specified default value if the key is missing or empty.
    /// </summary>
    Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all settings for the SECURITY_AUTH category.
    /// </summary>
    Task<IReadOnlyDictionary<string, string?>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears cached settings so they reload from DB on next request.
    /// </summary>
    Task RefreshCacheAsync(CancellationToken cancellationToken = default);
}
