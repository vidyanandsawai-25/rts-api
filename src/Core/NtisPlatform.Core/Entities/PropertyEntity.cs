using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// DESIGN NOTE:
/// WardNo and PropertyNo are nullable in the database to support legacy records.
/// The API layer enforces these fields as required for all new records via DTO validation.
/// This ensures backward compatibility with migrated data while preventing new NULL inserts.
/// Consumers should handle potential NULL values when querying legacy records.
/// </summary>
[Table("PropertyMast", Schema = "PTIS")]
public class PropertyEntity : BaseEntity, IHardDeletable
{
    /// <summary>
    /// Primary key - Unique identifier for the property owner.
    /// </summary>
    [Key]
    public int OwnerID { get; set; }

    // Location Information
    [Column(TypeName = "nvarchar(10)")]
    public string? TaxZone { get; set; }

    [Column(TypeName = "nvarchar(10)")]
    public string? WardNo { get; set; }

    [Column(TypeName = "nvarchar(10)")]
    public string? PropertyNo { get; set; }

    [Column(TypeName = "nvarchar(10)")]
    public string? PartitionNo { get; set; }

    // Property Classification
    public int? PropertyTypeID { get; set; }

    [Column(TypeName = "nvarchar(30)")]
    public string? UPICID { get; set; }

    public bool? OpenPlot { get; set; }

    [Column(TypeName = "nvarchar(30)")]
    public string? CSN { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public string? SubZoneNo { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public string? PlotNo { get; set; }

    public int? CategoryID { get; set; }

    [Column(TypeName = "varchar(5)")]
    public string? Type { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public string? PartType { get; set; }

    // Owner Information
    [Column(TypeName = "nvarchar(10)")]
    public string? OwnerTitle { get; set; }

    [Column(TypeName = "nvarchar(1000)")]
    public string? OwnerName { get; set; }

    [Column(TypeName = "nvarchar(10)")]
    public string? OwnerTitleEnglish { get; set; }

    [Column(TypeName = "nvarchar(1000)")]
    public string? OwnerNameEnglish { get; set; }

    // Occupier Information
    [Column(TypeName = "nvarchar(10)")]
    public string? OccupierTitle { get; set; }

    [Column(TypeName = "nvarchar(1000)")]
    public string? OccupierName { get; set; }

    [Column(TypeName = "varchar(10)")]
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
    public int? SocietyID { get; set; }

    // Hard Deletion Support (IHardDeletable)
    /// <summary>
    /// Indicates whether the entity is marked for permanent deletion.
    /// When set to true, entity will be soft-deleted and removed by the nightly cleanup task.
    /// </summary>
    public bool MarkedForDeletion { get; set; } = false;
    
    /// <summary>
    /// Date and time when the entity was marked for deletion.
    /// Used by the cleanup task to determine when to perform hard deletion.
    /// </summary>
    public DateTime? MarkedForDeletionDate { get; set; }
}
