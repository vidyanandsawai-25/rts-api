using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents property type category master data in the PTIS system
/// </summary>
[Table("PropertyTypeCategoryMaster", Schema = "PTIS")]
public class PropertyTypeCategoryEntity : BaseEntity
{
    /// <summary>
    /// Gets or sets the property type category name.
    /// </summary>
    [Column(TypeName = "nvarchar(100)")]
    public string? PropertyTypeCategory { get; set; }
}
