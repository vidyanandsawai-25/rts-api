namespace NtisPlatform.Core.Entities;

/// <summary>
///  Represents a PropertyCategory entity to manage property category information.
/// </summary>
public class PropertyCategoryEntity : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the property category.
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Gets or sets the name of the property category.
    /// </summary>
    public string PropertyCategoryName { get; set; } = string.Empty;
}