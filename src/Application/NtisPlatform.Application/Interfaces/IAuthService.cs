using NtisPlatform.Application.DTOs.Auth;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Authentication service interface
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticate user with username and password
    /// </summary>
    /// <param name="request">Login request with username and password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Login response with JWT token if successful</returns>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh an access token using a refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New access and refresh tokens</returns>
    Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate an access token
    /// </summary>
    /// <param name="request">Session validation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with user information</returns>
    Task<ValidateSessionResponseDto> ValidateSessionAsync(ValidateSessionRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logout and revoke refresh token
    /// </summary>
    /// <remarks>
    /// <b>Note:</b> Only revokes the refresh token. The access token remains valid
    /// until expiry. This is intentional for stateless JWT architecture.
    /// For immediate invalidation needs, consider implementing a token blocklist.
    /// </remarks>
    /// <param name="request">Logout request with refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Logout result</returns>
    Task<LogoutResponseDto> LogoutAsync(LogoutRequestDto request, CancellationToken cancellationToken = default);
}
