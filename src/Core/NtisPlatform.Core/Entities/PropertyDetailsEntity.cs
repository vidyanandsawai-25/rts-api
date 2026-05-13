using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents property details in the PTIS system
/// </summary>
[Table("PropertyDetails", Schema = "PTIS")]
public class PropertyDetailsEntity : BaseEntity
{
    public int PropertyId { get; set; }

    public int FloorId { get; set; }

    public int? SubFloorId { get; set; }

    [Column(TypeName = "varchar(4)")]
    public string? ConstructionYear { get; set; }

    [Column(TypeName = "nvarchar(4)")]
    public string? AssessmentYear { get; set; }

    public int ConstructionTypeId { get; set; }

    public int TypeOfUseId { get; set; }

    [Column(TypeName = "float")]
    public double? CarpetAreaSqMeter { get; set; }

    [Column(TypeName = "float")]
    public double? CarpetAreaSqFeet { get; set; }

    [Column(TypeName = "float")]
    public double? BuiltupAreaSqMeter { get; set; }

    [Column(TypeName = "float")]
    public double? BuiltupAreaSqFeet { get; set; }

    public int? NoOfRooms { get; set; }

    public bool? IsRenter { get; set; }

    public int? SubTypeOfUseId { get; set; }

    public bool? IsTaxable { get; set; }

    /// <summary>
    /// Indicates whether the entity is marked for deletion
    /// </summary>

    public bool MarkedForDeletion { get; set; } = false;

    /// <summary>
    /// Date when marked for deletion
    /// </summary>
    public DateTime? MarkedForDeletionDate { get; set; }


    public virtual ICollection<RenterMastEntity> Renters { get; set; } = new List<RenterMastEntity>();


    [ForeignKey(nameof(PropertyId))]
    public virtual PropertyEntity? Property { get; set; }

    [ForeignKey(nameof(FloorId))]             
    public virtual FloorEntity? Floor { get; set; }

    [ForeignKey(nameof(SubFloorId))]         
    public virtual SubFloorEntity? SubFloor { get; set; }

    [ForeignKey(nameof(ConstructionTypeId))]
    public virtual ConstructionTypeEntity? ConstructionType { get; set; }

    [ForeignKey(nameof(TypeOfUseId))]
    public virtual TypeOfUseEntity? TypeOfUse { get; set; }

    [ForeignKey(nameof(SubTypeOfUseId))]
    public virtual SubTypeOfUseEntity? SubTypeOfUse { get; set; }

    
    public virtual ICollection<RenterDetailEntity> RenterDetails { get; set; } = new List<RenterDetailEntity>();
 
    public virtual ICollection<RoomWiseSubmissionDetailsEntity> RoomWiseSubmissionDetails { get; set; } = new List<RoomWiseSubmissionDetailsEntity>();
}
