namespace NtisPlatform.Core.Entities;

/// <summary>
/// Organization entity - stores minimal organization information
/// Single row per deployment (one organization = one database)
/// All other configuration stored in OrganizationSettings as key-value pairs
/// </summary>
public class Organization : BaseEntity
{
    /// <summary>
    /// Organization name displayed in UI
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether organization is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether initial setup/configuration is complete
    /// </summary>
    public bool IsSetupComplete { get; set; } = false;
}
