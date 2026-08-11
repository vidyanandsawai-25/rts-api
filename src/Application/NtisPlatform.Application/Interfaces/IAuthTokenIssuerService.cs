using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Mints a fully authenticated session (access token + persisted refresh token) for a user who
/// has already completed authentication. Shared by <see cref="IAuthService"/> (password-only
/// login) and <see cref="IMfaChallengeService"/> (post-MFA login) so token issuance and refresh
/// token persistence exist in exactly one place.
/// </summary>
public interface IAuthTokenIssuerService
{
    /// <summary>
    /// Issues an access token (carrying the given amr claim and the user's current security
    /// stamp) and a new persisted refresh token.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <param name="authenticationMethod">"pwd" or "mfa" — embedded as the access token's amr claim.</param>
    Task<LoginResponseDto> IssueAsync(UserEntity user, string authenticationMethod, CancellationToken cancellationToken = default);
}
