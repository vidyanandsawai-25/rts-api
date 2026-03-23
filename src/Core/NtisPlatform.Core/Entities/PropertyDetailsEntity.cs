using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents property details in the PTIS system
/// </summary>
[Table("PropertyDetails", Schema = "PTIS")]
public class PropertyDetailsEntity : BaseEntity
{
    [Key]
    public int PropertyDetailsId { get; set; }

    public int PropertyId { get; set; }

    [Column(TypeName = "float")]
    public double? CarpetAreaSqMeter { get; set; }

    [Column(TypeName = "float")]
    public double? BuiltupAreaSqMeter { get; set; }

    [Column(TypeName = "float")]
    public double? CarpetAreaSqFeet { get; set; }

    [Column(TypeName = "float")]
    public double? BuiltupAreaSqFeet { get; set; }
}
