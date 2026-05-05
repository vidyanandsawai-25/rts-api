using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces;

public interface ILocalization
{
    /// <summary>
    /// Creates or updates a localization entry.
    /// Key format: {resource}_{entityId}_{propertyName}
    /// </summary>
    Task<string> SaveAsync(LocalizationEntry entry);

    /// <summary>
    /// Creates or updates multiple localization entries in a single DB transaction.
    /// Returns dictionary of PropertyName → generated Key.
    /// </summary>
    Task<Dictionary<string, string>> SaveBatchAsync(IEnumerable<LocalizationEntry> entries);
    
    /// <summary>
    /// Gets localized values for multiple keys.
    /// </summary>
    Task<Dictionary<string, string>> GetAsync(string resource, IEnumerable<string> keys, string language);

    /// <summary>
    /// Deactivates localization entries for the specified keys.
    /// </summary>

    Task DeactivateByKeysAsync(string resource, IEnumerable<string> keys); // NEW - Soft delete
}
