using NtisPlatform.Core.Entities.Master;
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

    public string? PinCode { get; set; }
    public int? MobileNoRemarkId { get; set; }

    public string? AlternateMobileNo { get; set; }

    public string? OccupierMobileNo { get; set; }
    public int? OccupierMobileNoRemarkId { get; set; }

    // Society Information
    public int? SocietyDetailId { get; set; }

    /// Plot area in square meters.
    public double? TotalPlotArea { get; set; }
    public double? Length { get; set; }
    public double? Width { get; set; }
    /// <summary>
    /// Foreign Key to PropertyAssessmentStatusMaster.Id
    /// </summary>
    public int? PropertyAssessmentStatusId { get; set; }

    /// <summary>
    /// Foreign Key to PropertyMastOld.Id
    /// </summary>
    public int? PropertyMastOldId { get; set; }
    public int? PropertyFloorId { get; set; }

    /// <summary>
    /// Indicates whether the entity is marked for deletion.
    /// Set to true during PropertyService.DeletePropertyAsync() for soft deletion.
    /// </summary>
    public bool MarkedForDeletion { get; set; } = false;

    /// <summary>
    /// Date when the entity was marked for deletion.
    /// Set during PropertyService.DeletePropertyAsync() for audit trail.
    /// </summary>
    public DateTime? MarkedForDeletionDate { get; set; }

    // ===== Master Data Navigation Properties =====
    public virtual TaxZoneEntity? TaxZone { get; set; }
    public virtual WardEntity? Ward { get; set; }
    public virtual MoujaEntity? Mouja { get; set; }
    public virtual PropertyAssessmentStatusEntity? PropertyAssessmentStatus { get; set; }

    // ===== Child Entity Navigation Properties =====
    // 
    // ARCHITECTURE NOTE: Navigation properties serve THREE purposes:
    // 1. EF Core query navigation (Include/ThenInclude for eager loading)
    // 2. Relationship mapping (defines one-to-many relationships with child entities)
    // 3. Manual soft deletion tracking (PropertyService identifies related entities)
    //
    // DELETION ARCHITECTURE:
    // - This system uses MANUAL SOFT DELETE (not EF Core cascade delete)
    // - All foreign keys use DeleteBehavior.Restrict (prevents accidental hard delete)
    // - PropertyService.DeletePropertyAsync() orchestrates all deletions
    // - PropertyRepository methods fetch and mark entities for soft deletion
    // - All deletions set: MarkedForDeletion=true, IsActive=false, MarkedForDeletionDate=DateTime.Now
    // - Everything is saved in a single transaction via SaveChangesAsync()
    //
    // WHY NOT EF CASCADE DELETE?
    // - EF cascade delete only supports HARD deletion (physical row removal)
    // - Soft delete requires setting flags (MarkedForDeletion, IsActive, timestamp)
    // - Manual control provides audit trails, reversibility, and business rule enforcement
    //
    // See docs/PropertyEntityNavigationPropertiesExplained.md for detailed architecture.

    // Core Property Data
    public ICollection<FlagMasterEntity> FlagMaster { get; set; } = new List<FlagMasterEntity>();
    public ICollection<PropertyTaxCalculationCVResultsEntity> PropertyTaxCalculationCVResults { get; set; } = new List<PropertyTaxCalculationCVResultsEntity>();
    public ICollection<PropertyTaxCalculationRVResultsEntity> PropertyTaxCalculationRVResults { get; set; } = new List<PropertyTaxCalculationRVResultsEntity>();
    public ICollection<PlotDetailsEntity> PlotDetails { get; set; } = new List<PlotDetailsEntity>();
    public ICollection<TransMastCVEntity> TransMastCV { get; set; } = new List<TransMastCVEntity>();
    public ICollection<PropertyDetailsEntity> PropertyDetails { get; set; } = new List<PropertyDetailsEntity>();
    public ICollection<PropertyDetailsOldEntity> PropertyDetailsOld { get; set; } = new List<PropertyDetailsOldEntity>();
    public ICollection<PropertyMastOldEntity> PropertyMastOld { get; set; } = new List<PropertyMastOldEntity>();
    public ICollection<SocietyDetailsEntity> SocietyDetailsMast { get; set; } = new List<SocietyDetailsEntity>();
    public ICollection<PropertyAssessmentEntity> PropertyMastDetails { get; set; } = new List<PropertyAssessmentEntity>();

    public virtual ICollection<PolicyTaxDetailsCVEntity> PolicyTaxDetailsCV { get; set; } = new List<PolicyTaxDetailsCVEntity>();

    public virtual ICollection<PolicyTaxDetailsEntity> PolicyTaxDetails { get; set; } = new List<PolicyTaxDetailsEntity>();

    // Tax & Financial
    public virtual ICollection<ApplyTaxesMasterEntity> ApplyTaxesMaster { get; set; } = new List<ApplyTaxesMasterEntity>();
    public virtual ICollection<PropertyAssessmentDetailsEntity> PropertyAssessmentDetails { get; set; } = new List<PropertyAssessmentDetailsEntity>();
    public virtual ICollection<PropertyCertificateEntity> PropertyCertificates { get; set; } = new List<PropertyCertificateEntity>();
    public virtual ICollection<PropertyTaxCalculationSection129ResultsEntity> PropertyTaxCalculationSection129Results { get; set; } = new List<PropertyTaxCalculationSection129ResultsEntity>();
    public virtual ICollection<RoomWiseSubmissionDetailsEntity> RoomWiseSubmissionDetails { get; set; } = new List<RoomWiseSubmissionDetailsEntity>();
    public virtual ICollection<PropertyImagesMastEntity> PropertyImagesMast { get; set; } = new List<PropertyImagesMastEntity>();

    // PropertySocialDetails: Does NOT implement IHardDeletable - only IsActive is updated during deletion
    public virtual ICollection<PropertySocialDetailsEntity> PropertySocialDetails { get; set; } = new List<PropertySocialDetailsEntity>();

    // Tax Pending/Demand
    public virtual ICollection<TaxPendingDetailsEntity> TaxPendingDetails { get; set; } = new List<TaxPendingDetailsEntity>();

    // WaterConnectionMaster: Does NOT implement IHardDeletable - only IsActive is updated during deletion
    public virtual ICollection<WaterConnectionMasterEntity> WaterConnectionMaster { get; set; } = new List<WaterConnectionMasterEntity>();

    public virtual ICollection<TaxPendingDetailsArchiveEntity> TaxPendingDetailsArchive { get; set; } = new List<TaxPendingDetailsArchiveEntity>();
    public virtual ICollection<TaxPendingDetailsCVEntity> TaxPendingDetailsCV { get; set; } = new List<TaxPendingDetailsCVEntity>();
    public virtual ICollection<TaxPendingDetailsLookupEntity> TaxPendingDetailsLookup { get; set; } = new List<TaxPendingDetailsLookupEntity>();
    public virtual ICollection<TaxPendingDetailsRetroEntity> TaxPendingDetailsRetro { get; set; } = new List<TaxPendingDetailsRetroEntity>();
    public virtual ICollection<TaxPendingDetailsRVEntity> TaxPendingDetailsRV { get; set; } = new List<TaxPendingDetailsRVEntity>();

    // Tax Transactions
    public virtual ICollection<TransMastEntity> TransMast { get; set; } = new List<TransMastEntity>();
    public virtual ICollection<TransMastArchiveEntity> TransMastArchive { get; set; } = new List<TransMastArchiveEntity>();
    public virtual ICollection<TransMastLookupEntity> TransMastLookup { get; set; } = new List<TransMastLookupEntity>();
    public virtual ICollection<TransMastRVEntity> TransMastRV { get; set; } = new List<TransMastRVEntity>();

    public virtual ICollection<PropertyWorkflowDetailsEntity> WorkflowHistory { get; set; } = new List<PropertyWorkflowDetailsEntity>();

}