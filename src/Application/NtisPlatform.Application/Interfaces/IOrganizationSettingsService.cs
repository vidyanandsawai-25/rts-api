using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service for managing runtime-configurable organization settings (key-value store)
/// </summary>
public interface IOrganizationSettingsService
{
    /// <summary>
    /// Get all settings as dictionary
    /// </summary>
    Task<Dictionary<string, string?>> GetAllSettingsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all settings by category
    /// </summary>
    Task<Dictionary<string, string?>> GetSettingsByCategoryAsync(string category, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get specific setting value (returns string, parse based on DataType)
    /// </summary>
    Task<string?> GetSettingAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get setting value with type conversion
    /// </summary>
    Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Set or update a setting value
    /// </summary>
    Task SetSettingAsync(string key, string? value, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Bulk update settings
    /// </summary>
    Task UpdateSettingsAsync(Dictionary<string, string?> settings, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get setting entity (full details)
    /// </summary>
    Task<OrganizationSetting?> GetSettingEntityAsync(string key, CancellationToken cancellationToken = default);
}
