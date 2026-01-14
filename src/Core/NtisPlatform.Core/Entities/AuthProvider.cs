namespace NtisPlatform.Core.Entities;

/// <summary>
/// Authentication provider configuration for tenant
/// </summary>
public class AuthProvider : BaseEntity
{
    public string ProviderType { get; set; } = string.Empty; // Basic, AzureAD, Google, SAML
    public string DisplayName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool IsDefault { get; set; } = false;
    public string? ConfigJson { get; set; } // Provider-specific configuration
    public int Priority { get; set; } = 0;
}
