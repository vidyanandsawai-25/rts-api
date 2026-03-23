using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents a property type in the PTIS system
/// </summary>
[Table("PropertyTypeMaster", Schema = "PTIS")]
public class PropertyTypeEntity : BaseEntity
{
    [Key]
    public int PropertyTypeId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string? PropertyDescription { get; set; }
}
