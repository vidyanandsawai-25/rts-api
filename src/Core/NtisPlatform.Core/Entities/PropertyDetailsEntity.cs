using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents property details in the PTIS system
/// </summary>
[Table("PropertyDetails", Schema = "PTIS")]
public class PropertyDetailsEntity : BaseEntity
{
    [Key]    public int PropertyId { get; set; }

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

    public bool? RenterYesNO { get; set; }

    [Column(TypeName = "float")]
    public double? RentMonthly { get; set; }

    [Column(TypeName = "float")]
    public double? RentYearly { get; set; }

    [Column(TypeName = "float")]
    public double? NonCalculateRentMonthly { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? RenterNameEnglish { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? RenterName { get; set; }

    public DateTime? AgreementFromDate { get; set; }

    public DateTime? AgreementDate { get; set; }

    public DateTime? AgreementToDate { get; set; }

    public int? SubTypeOfUseId { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public string? TaxLiability { get; set; }

    public bool? IsTaxable { get; set; }

    public DateTime? OccupancyDate { get; set; }

    public bool? OccupancyApplyOrNot { get; set; }

    [Column(TypeName = "varchar(30)")]
    public string? OccupancyNumber { get; set; }

    /// <summary>
    /// Indicates whether the entity is marked for deletion
    /// </summary>
    public bool MarkedForDeletion { get; set; } = false;

    /// <summary>
    /// Date when marked for deletion
    /// </summary>
    public DateTime? MarkedForDeletionDate { get; set; }
}