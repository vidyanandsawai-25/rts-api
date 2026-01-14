namespace NtisPlatform.Application.DTOs.Auth;

/// <summary>
/// Organization configuration for login page customization
/// </summary>
public class OrganizationConfigResponse
{
    public string OrganizationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public int? LogoWidth { get; set; }
    public int? LogoHeight { get; set; }
    public string? LocalizedName { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public string? PortalTitle { get; set; }
    public List<AuthProviderConfig> EnabledAuthProviders { get; set; } = new();
    public Dictionary<string, string> ThemeConfig { get; set; } = new();
    public bool RequiresTwoFactor { get; set; }
}

/// <summary>
/// Authentication provider configuration
/// </summary>
public class AuthProviderConfig
{
    public AuthProviderType Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
    public bool IsDefault { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
