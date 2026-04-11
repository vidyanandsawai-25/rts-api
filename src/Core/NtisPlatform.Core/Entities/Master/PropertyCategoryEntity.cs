using System.ComponentModel.DataAnnotations.Schema;
namespace NtisPlatform.Core.Entities;

/// <summary>
///  Represents a PropertyCategory entity to manage property category information.
/// </summary>
[Table("PropertyCategoryMaster", Schema = "PTIS")]
public class PropertyCategoryEntity : BaseEntity
{
    /// <summary>
    /// Gets or sets the name of the property category.
    /// </summary>
    [Column(TypeName = "nvarchar(50)")]
    public string PropertyCategoryName { get; set; } = string.Empty;
}