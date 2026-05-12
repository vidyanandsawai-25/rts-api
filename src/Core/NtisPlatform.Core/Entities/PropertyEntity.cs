using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;
/// <summary>
/// Represents a property in the PTIS system.
/// </summary>
public class PropertyEntity : BaseEntity, IHardDeletable
{
    public int? PropertySeqNo { get; set; }

    public int? MoujaId { get; set; }
	
    // Location Information
    public int TaxZoneId { get; set; }

    public int WardId { get; set; }

    public string? PropertyNo { get; set; }
    
    public string? PartitionNo { get; set; }

    // Property Classification
    public int? PropertyTypeId { get; set; }

    public string? UPICId { get; set; }

    public bool? OpenPlot { get; set; }

    public string? CSN { get; set; }

    public string? SubZoneNo { get; set; }
    
    public string? PlotNo { get; set; }

    public int? CategoryId { get; set; }

    public string? Type { get; set; }

    public string? PartType { get; set; }

    // Owner Information
    public string? OwnerTitle { get; set; }

    public string? OwnerName { get; set; }

    public string? OwnerTitleEnglish { get; set; }

    public string? OwnerNameEnglish { get; set; }

    // Occupier Information
    public string? OccupierTitle { get; set; }

    public string? OccupierName { get; set; }

    public string? OccupierTitleEnglish { get; set; }

    public string? OccupierNameEnglish { get; set; }

    // Flat/Shop Information
    public string? FlatOrShopNo { get; set; }

    public string? FlatOrShopName { get; set; }

    public string? FlatOrShopNoEnglish { get; set; }

    public string? FlatOrShopNameEnglish { get; set; }

    // Address Information
    public string? Address { get; set; }

    public string? Location { get; set; }

    public string? AddressEnglish { get; set; }

    public string? LocationEnglish { get; set; }

    // Contact Information
    public string? MobileNo { get; set; }

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