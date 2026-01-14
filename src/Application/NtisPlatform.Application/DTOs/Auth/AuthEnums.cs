namespace NtisPlatform.Application.DTOs.Auth;

/// <summary>
/// Client application types for authentication
/// </summary>
public enum ClientType
{
    /// <summary>
    /// Web browser application (uses httpOnly cookies)
    /// </summary>
    Web = 1,

    /// <summary>
    /// Mobile application (iOS/Android, uses JWT tokens)
    /// </summary>
    Mobile = 2,

    /// <summary>
    /// Desktop application (Windows/Mac/Linux, uses refresh tokens)
    /// </summary>
    Desktop = 3,

    /// <summary>
    /// Service-to-service authentication (API keys or client credentials)
    /// </summary>
    Service = 4
}

/// <summary>
/// Authentication provider types
/// </summary>
public enum AuthProviderType
{
    /// <summary>
    /// Username and password authentication
    /// </summary>
    Basic = 1,

    /// <summary>
    /// Azure Active Directory / Microsoft Entra ID
    /// </summary>
    AzureAD = 2,

    /// <summary>
    /// Google OAuth 2.0
    /// </summary>
    Google = 3,

    /// <summary>
    /// SAML 2.0 authentication
    /// </summary>
    SAML = 4,

    /// <summary>
    /// OAuth 2.0 generic provider
    /// </summary>
    OAuth = 5
}

/// <summary>
/// Authentication result status
/// </summary>
public enum AuthResultStatus
{
    Success = 1,
    InvalidCredentials = 2,
    AccountLocked = 3,
    AccountDisabled = 4,
    TwoFactorRequired = 5,
    PasswordExpired = 6,
    TenantNotFound = 7,
    ProviderNotEnabled = 8,
    RateLimitExceeded = 9
}
