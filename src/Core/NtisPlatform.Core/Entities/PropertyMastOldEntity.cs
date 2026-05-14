using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents old property data in the PTIS system (PropertyMastOld table)
/// Stores historical property information for reference
/// </summary>
[Table("PropertyMastOld", Schema = "PTIS")]
public class PropertyMastOldEntity : BaseEntity
{
    [Column(TypeName = "nvarchar(10)")]
    public string? OldWardNo { get; set; }

    [Column(TypeName = "nvarchar(10)")]
    public string? OldPropertyNo { get; set; }

    [Column(TypeName = "nvarchar(10)")]
    public string? OldPartitionNo { get; set; }

    [Column(TypeName = "nvarchar(10)")]
    public string? OldEgovNo { get; set; }

    public int? OldPropertyTypeId { get; set; }

    [Column(TypeName = "float")]
    public double? OldALV { get; set; }

    [Column(TypeName = "float")]
    public double? OldRV { get; set; }

    [Column(TypeName = "float")]
    public double? OldGeneralTax { get; set; }

    [Column(TypeName = "float")]
    public double? OldTotalTax { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public string? OldZoneNo { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public string? OldSubZoneNo { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public string? OldPlotNo { get; set; }

    [Column(TypeName = "nvarchar(30)")]
    public string? OldCSN { get; set; }

    [Column(TypeName = "float")]
    public double? OldPlotArea { get; set; }

    public int? OldAssessmentYear { get; set; }

    [Column(TypeName = "nvarchar(10)")]
    public string? OldFloor { get; set; }

    [Column(TypeName = "nvarchar(7)")]
    public string? OldConstructionTypeOfUseId { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string? OldUseType { get; set; }

    [Column(TypeName = "float")]
    public double? OldConstructionArea { get; set; }

    [Column(TypeName = "nvarchar(1000)")]
    public string? OldOwnerName { get; set; }

    [Column(TypeName = "nvarchar(1000)")]
    public string? OldOccupierName { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? OldAddress { get; set; }

    [Column(TypeName = "nvarchar(1000)")]
    public string? OldOwnerNameEnglish { get; set; }

    [Column(TypeName = "nvarchar(1000)")]
    public string? OldOccupierNameEnglish { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? OldAddressEnglish { get; set; }

    public int? NoOfOldToilets { get; set; }

    public int? OldTotalRooms { get; set; }

    [Column(TypeName = "nvarchar(300)")]
    public string? OldSocietyName { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string? OldEmailId { get; set; }

    [Column(TypeName = "float")]
    public double? OldParkingAreaSqFt { get; set; }

    [Column(TypeName = "float")]
    public double? OldParkingAreaSqMtr { get; set; }

    public DateTime? OldAssessmentDate { get; set; }

    [Column(TypeName = "nvarchar(50)")]
    public string? OldFlatOrShopNumber { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public string? OldWing { get; set; }

    [Column(TypeName = "varchar(13)")]
    public string? OldMobileNo { get; set; }

    public bool MarkedForDeletion { get; set; } = false;

    public DateTime? MarkedForDeletionDate { get; set; }
}
