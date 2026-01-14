using NtisPlatform.Application.DTOs.Auth;

namespace NtisPlatform.Application.Interfaces.Auth;

/// <summary>
/// Service for managing authentication operations
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticate user and generate tokens
    /// </summary>
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logout user and revoke refresh token
    /// </summary>
    Task LogoutAsync(string refreshToken, string? ipAddress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate session token
    /// </summary>
    Task<bool> ValidateSessionAsync(string accessToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get organization configuration for login page
    /// </summary>
    Task<OrganizationConfigResponse?> GetOrganizationConfigAsync(CancellationToken cancellationToken = default);
}
