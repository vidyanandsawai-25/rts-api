namespace NtisPlatform.Core.Entities;

/// <summary>
/// Entity representing a configuration value in the system
/// </summary>
public class ConfigValueMasterEntity : BaseEntity
{
    /// <summary>
    /// Unique identifier for the configuration value
    /// </summary>    /// <summary>
    /// Foreign key to the configuration key
    /// </summary>
    public int ConfigKeyId { get; set; }

    /// <summary>
    /// Navigation property to the configuration key
    /// </summary>
    public ConfigKeyMasterEntity? ConfigKey { get; set; }

    /// <summary>
    /// Foreign key to the department (optional)
    /// </summary>
    public int? DepartmentId { get; set; }

    /// <summary>
    /// Navigation property to the department
    /// </summary>
    public DepartmentMasterEntity? Department { get; set; }

    /// <summary>
    /// Foreign key to the module (optional)
    /// </summary>
    public int? ModuleId { get; set; }

    /// <summary>
    /// Navigation property to the module
    /// </summary>
    public ModuleMasterEntity? Module { get; set; }

    /// <summary>
    /// Value of the configuration
    /// </summary>
    public string? Value { get; set; }
}
