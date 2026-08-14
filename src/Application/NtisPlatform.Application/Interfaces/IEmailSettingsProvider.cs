using NtisPlatform.Application.DTOs.Email;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Provider for retrieving email settings from configuration tables
/// </summary>
public interface IEmailSettingsProvider
{
    /// <summary>
    /// Retrieves email SMTP settings from the database configuration tables
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Email settings DTO</returns>
    Task<EmailSettingsDto> GetEmailSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the cached email settings so the next call re-reads the configuration tables.
    /// Call after any write to the "EmailSettings" ConfigKeyMaster/ConfigValueMaster rows.
    /// </summary>
    Task RefreshCacheAsync(CancellationToken cancellationToken = default);
}
