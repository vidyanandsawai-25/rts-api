namespace NtisPlatform.Core.Entities;

/// <summary>
/// Feature flag for enabling/disabling modules per tenant
/// </summary>
public class FeatureFlag : BaseEntity
{
    public string ModuleName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = false;
    public string? Description { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
