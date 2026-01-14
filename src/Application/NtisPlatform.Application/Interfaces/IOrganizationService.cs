using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service for managing organization information
/// </summary>
public interface IOrganizationService
{
    /// <summary>
    /// Get organization information (cached)
    /// </summary>
    Task<Organization?> GetOrganizationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Update organization information
    /// </summary>
    Task<Organization> UpdateOrganizationAsync(Organization organization, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initialize organization (seed initial data)
    /// </summary>
    Task<Organization> InitializeOrganizationAsync(Organization organization, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if initial setup is required
    /// </summary>
    Task<bool> IsSetupRequiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Complete initial setup with organization and admin user
    /// </summary>
    Task<Organization> CompleteInitialSetupAsync(Organization organization, User adminUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update organization settings (key-value pairs)
    /// </summary>
    Task<int> UpdateOrganizationSettingsAsync(Dictionary<string, string> settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get organization settings by keys
    /// </summary>
    Task<Dictionary<string, string>> GetOrganizationSettingsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all organization settings for a specific category
    /// </summary>
    Task<Dictionary<string, string>> GetOrganizationSettingsByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a specific organization setting by key
    /// </summary>
    Task<bool> DeleteOrganizationSettingAsync(string key, CancellationToken cancellationToken = default);
}
