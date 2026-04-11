namespace NtisPlatform.Core.Entities;

/// <summary>
/// Entity representing a configuration category in the system
/// </summary>
public class ConfigCategoryMasterEntity :BaseEntity
{
    /// <summary>
    /// Unique identifier for the configuration category
    /// </summary>    /// <summary>
    /// Unique code for the category
    /// </summary>
    public string CategoryCode { get; set; } = null!;

    /// <summary>
    /// Name of the category
    /// </summary>
    public string CategoryName { get; set; } = null!;

    /// <summary>
    /// Display order for sorting
    /// </summary>
    public int? DisplayOrder { get; set; }

    /// <summary>
    /// Collection of configuration keys in this category
    /// </summary>
    public ICollection<ConfigKeyMasterEntity> ConfigKeys { get; set; } = new List<ConfigKeyMasterEntity>();
}
