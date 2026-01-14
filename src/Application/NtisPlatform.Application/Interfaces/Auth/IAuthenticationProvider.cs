using NtisPlatform.Application.DTOs.Auth;

namespace NtisPlatform.Application.Interfaces.Auth;

/// <summary>
/// Authentication result containing user info and status
/// </summary>
public class AuthResult
{
    public AuthResultStatus Status { get; set; }
    public UserInfo? User { get; set; }
    public string? ErrorMessage { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();

    public bool IsSuccess => Status == AuthResultStatus.Success;

    public static AuthResult Success(UserInfo user) => new()
    {
        Status = AuthResultStatus.Success,
        User = user
    };

    public static AuthResult Failure(AuthResultStatus status, string errorMessage) => new()
    {
        Status = status,
        ErrorMessage = errorMessage
    };

    public static AuthResult TwoFactorRequired(UserInfo user) => new()
    {
        Status = AuthResultStatus.TwoFactorRequired,
        User = user,
        RequiresTwoFactor = true
    };
}

/// <summary>
/// Interface for authentication providers (Basic, Azure AD, Google, SAML, etc.)
/// </summary>
public interface IAuthenticationProvider
{
    /// <summary>
    /// Provider type identifier
    /// </summary>
    AuthProviderType ProviderType { get; }

    /// <summary>
    /// Authenticate user with credentials
    /// </summary>
    Task<AuthResult> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate two-factor authentication code
    /// </summary>
    Task<bool> ValidateTwoFactorAsync(int userId, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if provider is enabled for the organization
    /// </summary>
    Task<bool> IsEnabledAsync(string organizationId, CancellationToken cancellationToken = default);
}
