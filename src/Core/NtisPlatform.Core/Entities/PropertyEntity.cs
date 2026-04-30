using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents a property in the PTIS system.
/// </summary>
[Table("PropertyMast", Schema = "PTIS")]
public class PropertyEntity : BaseEntity, IHardDeletable
{
    public int? PropertySeqNo { get; set; }

    public int? MoujaId { get; set; }
	
    // Location Information
    public int TaxZoneId { get; set; }

    public int WardId { get; set; }

    [Column(TypeName = "nvarchar(10)")]
    public string? PropertyNo { get; set; }

    [Column(TypeName = "nvarchar(10)")]
    public string? PartitionNo { get; set; }

    // Property Classification
    public int? PropertyTypeId { get; set; }

    [Column(TypeName = "nvarchar(30)")]
    public string? UPICId { get; set; }

    public bool? OpenPlot { get; set; }

    [Column(TypeName = "nvarchar(30)")]
    public string? CSN { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public string? SubZoneNo { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public string? PlotNo { get; set; }

    public int? CategoryId { get; set; }

    [Column(TypeName = "varchar(5)")]
    public string? Type { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public string? PartType { get; set; }

    // Owner Information
    [Column(TypeName = "nvarchar(20)")]
    public string? OwnerTitle { get; set; }

    [Column(TypeName = "nvarchar(1000)")]
    public string? OwnerName { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public string? OwnerTitleEnglish { get; set; }

    [Column(TypeName = "nvarchar(1000)")]
    public string? OwnerNameEnglish { get; set; }

    // Occupier Information
    [Column(TypeName = "nvarchar(20)")]
    public string? OccupierTitle { get; set; }

    [Column(TypeName = "nvarchar(1000)")]
    public string? OccupierName { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public string? OccupierTitleEnglish { get; set; }

    [Column(TypeName = "nvarchar(1000)")]
    public string? OccupierNameEnglish { get; set; }

    // Flat/Shop Information
    [Column(TypeName = "nvarchar(100)")]
    public string? FlatOrShopNo { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public string? FlatOrShopName { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string? FlatOrShopNoEnglish { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public string? FlatOrShopNameEnglish { get; set; }

    // Address Information
    [Column(TypeName = "nvarchar(500)")]
    public string? Address { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public string? Location { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? AddressEnglish { get; set; }

    [Column(TypeName = "nvarchar(200)")]
    public string? LocationEnglish { get; set; }

    // Contact Information
    [Column(TypeName = "varchar(13)")]
    public string? MobileNo { get; set; }

    [Column(TypeName = "nvarchar(100)")]
    public string? EmailId { get; set; }

    // Society Information
    public int? SocietyDetailId { get; set; }

    /// <summary>
    /// Foreign Key to PropertyAssessmentStatusMaster.Id
    /// </summary>
    public int? PropertyAssessmentStatusId { get; set; }

    /// <summary>
    /// Foreign Key to PropertyMastOld.Id
    /// </summary>
    public int? PropertyMastOldId { get; set; }

    /// <summary>
    /// Indicates whether the entity is marked for deletion.
    /// </summary>
    public bool MarkedForDeletion { get; set; } = false;

    /// <summary>
    /// Date when the entity was marked for deletion
    /// </summary>
    public DateTime? MarkedForDeletionDate { get; set; }

    // Navigation Properties
    /// <summary>
    /// Collection of policy tax details associated with this property
    /// </summary>
    public virtual ICollection<PolicyTaxDetailsEntity> PolicyTaxDetails { get; set; } = new List<PolicyTaxDetailsEntity>();
    
    /// <summary>
    /// Collection of policy tax details CV associated with this property
    /// </summary>
    public virtual ICollection<PolicyTaxDetailsCVEntity> PolicyTaxDetailsCV { get; set; } = new List<PolicyTaxDetailsCVEntity>();
}