using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents old property details in the PTIS system (PropertyDetailsOld table)
/// Stores historical property construction and usage details for reference
/// </summary>
[Table("PropertyDetailsOld", Schema = "PTIS")]
public class PropertyDetailsOldEntity : BaseEntity
{
    [Key]
    [Column("PropertyDetailsOldId")]    public int PropertyId { get; set; }

    [Column(TypeName = "nvarchar(10)")]
    public string? OldFloorId { get; set; }

    [Column(TypeName = "varchar(4)")]
    public string? OldConstructionYear { get; set; }

    [Column(TypeName = "varchar(7)")]
    public string? OldConstructionTypeId { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public string? OldTypeOfUseId { get; set; }

    [Column(TypeName = "float")]
    public double? OldCarpetAreaSqfeet { get; set; }

    [Column(TypeName = "float")]
    public double? OldCarpetAreaSqMeter { get; set; }

    public bool? OldRegistration { get; set; }

    public bool MarkedForDeletion { get; set; } = false;
}
