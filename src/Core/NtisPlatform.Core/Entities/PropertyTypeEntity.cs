using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents a property type in the PTIS system
/// </summary>
[Table("PropertyTypeMaster", Schema = "PTIS")]
public class PropertyTypeEntity : BaseEntity
{    [Column(TypeName = "nvarchar(100)")]
    public string? PropertyDescription { get; set; }

    [Column(TypeName = "varchar(5)")]
    public string? Type { get; set; }

    [Column(TypeName = "nvarchar(50)")]
    public string? PropertyTypeGroup { get; set; }

    public int? SearchSequence { get; set; }

    public int? PropertyTypeCategoryId { get; set; }
}
