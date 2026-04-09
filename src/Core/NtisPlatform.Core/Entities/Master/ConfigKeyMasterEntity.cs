namespace NtisPlatform.Core.Entities;

/// <summary>
/// Entity representing a configuration key in the system
/// </summary>
public class ConfigKeyMasterEntity : BaseEntity
{
    /// <summary>
    /// Unique identifier for the configuration key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the category this config key belongs to
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Navigation property to the category
    /// </summary>
    public ConfigCategoryMasterEntity? Category { get; set; }

    /// <summary>
    /// Unique code for the configuration
    /// </summary>
    public string ConfigCode { get; set; } = null!;

    /// <summary>
    /// Name of the configuration
    /// </summary>
    public string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the configuration
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Data type of the configuration value
    /// </summary>
    public string? DataType { get; set; }

    /// <summary>
    /// Control type for UI rendering
    /// </summary>
    public string? ControlType { get; set; }

    /// <summary>
    /// Default value for the configuration
    /// </summary>
    public string? DefaultValue { get; set; }
 
}
