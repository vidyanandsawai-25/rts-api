using NtisPlatform.Application.DTOs.Auth;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// ULB (Urban Local Body) configuration service interface
/// </summary>
public interface IUlbConfigService
{
    /// <summary>
    /// Get ULB configuration for the organization
    /// Fetches details like logo, name, contact information
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ULB configuration details or null if not found</returns>
    Task<UlbConfigDto?> GetUlbConfigAsync(CancellationToken cancellationToken = default);
}
