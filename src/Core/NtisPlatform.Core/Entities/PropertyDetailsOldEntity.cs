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
    /// <summary>
    /// Foreign Key to PropertyMastOld.Id
    /// </summary>
    public int PropertyMastOldId { get; set; }

    /// <summary>
    /// Foreign Key to FloorMaster.Id
    /// </summary>
    public int OldFloorId { get; set; }

    /// <summary>
    /// Foreign Key to SubFloorMaster.Id
    /// </summary>
    public int? OldSubFloorId { get; set; }

    /// <summary>
    /// Construction Year as string (e.g., "2020")
    /// </summary>
    [Column(TypeName = "varchar(4)")]
    public string? OldConstructionYear { get; set; }

    /// <summary>
    /// Assessment Year as string (e.g., "2020")
    /// </summary>
    [Column(TypeName = "nvarchar(4)")]
    public string? OldAssessmentYear { get; set; }

    /// <summary>
    /// Foreign Key to ConstructionTypeMaster.Id
    /// </summary>
    public int OldConstructionTypeId { get; set; }

    /// <summary>
    /// Foreign Key to TypeOfUseMaster.Id
    /// </summary>
    public int OldTypeOfUseId { get; set; }

    /// <summary>
    /// Foreign Key to SubTypeOfUseMaster.Id
    /// </summary>
    public int? OldSubTypeOfUseId { get; set; }

    [Column(TypeName = "float")]
    public double? OldCarpetAreaSqMeter { get; set; }

    [Column(TypeName = "float")]
    public double? OldCarpetAreaSqFeet { get; set; }

    [Column(TypeName = "float")]
    public double? OldBuiltupAreaSqMeter { get; set; }

    [Column(TypeName = "float")]
    public double? OldBuiltupAreaSqFeet { get; set; }

    public bool MarkedForDeletion { get; set; } = false;

    public DateTime? MarkedForDeletionDate { get; set; }
}
