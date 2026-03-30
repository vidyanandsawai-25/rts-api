using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents property assessment data in the PTIS system (PropertyMastDetails table)
/// </summary>
[Table("PropertyMastDetails", Schema = "PTIS")]
public class PropertyAssessmentEntity : BaseEntity
{
    [Key]
    public int PropertyDetailsId { get; set; }

    public int PropertyId { get; set; }

    public int? OwnerTypeId { get; set; }

    [Column(TypeName = "nvarchar(30)")]
    public string? WingName { get; set; }

    public int? WingId { get; set; }

    [StringLength(20, ErrorMessage = "WingNo cannot exceed 20 characters.")]
    public string? WingNo { get; set; }

    [Column(TypeName = "nvarchar(400)")]
    public string? AssessmentRemark { get; set; }

    [Column(TypeName = "nvarchar(400)")]
    public string? SurveyRemark { get; set; }

    [Column(TypeName = "nvarchar(400)")]
    public string? FlatSystemRemark { get; set; }

    [Column(TypeName = "nvarchar(400)")]
    public string? CombPropRemark { get; set; }

    [Column(TypeName = "varchar(12)")]
    public string? AdharCardNo { get; set; }

    [Column(TypeName = "varchar(13)")]
    public string? ManagerMobileNo { get; set; }

    [Column(TypeName = "varchar(13)")]
    public string? RenterMobileNo { get; set; }

    [Column(TypeName = "varchar(13)")]
    public string? SecretaryMobileNo { get; set; }

    [Column(TypeName = "nvarchar(10)")]
    public string? AssessmentNo { get; set; }

    public DateTime? PrarupYadiPublishDate { get; set; }

    public DateTime? AntimYadiPublishDate { get; set; }

    public DateTime? PropertyRegDate { get; set; }

    public short? ApplyTaxesFrom { get; set; }

    public DateTime? PartOCDate { get; set; }

    [Column(TypeName = "nvarchar(50)")]
    public string? BHK { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public string? BlockNo { get; set; }

    public int? UsageCategoryId { get; set; }

    [Column("AlternetivEmailId", TypeName = "varchar(100)")]
    public string? AlternativeEmailId { get; set; }

    public double? TotalBuiltupAreaSqFeet { get; set; }

    public double? TotalBuiltupAreaSqMeter { get; set; }

    [Column(TypeName = "varchar(20)")]
    public string? Latitude { get; set; }

    [Column(TypeName = "varchar(20)")]
    public string? Longitude { get; set; }

    public int? NoOfResidentialToilets { get; set; }

    public int? NoOfCommercialToilets { get; set; }

    /// <summary>
    /// Indicates whether the entity is marked for deletion
    /// </summary>
    public bool MarkedForDeletion { get; set; } = false;
}