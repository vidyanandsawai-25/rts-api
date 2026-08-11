using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

/// <summary>
/// Asset Lease Rent Details entity for the Asset Management System.
/// Maps to the [AMS].[AssetLeaseRentDetails] table.
/// </summary>
public class AssetLeaseRentDetailsEntity : BaseEntity, IHardDeletable
{
    public int AssetId { get; set; }
    // Maps to schema column SubUnitDetailsId (FK to AMS.SubUnitsDetails).
    public int? FloorDetailsId { get; set; }
    public string? ShopNo { get; set; }
    public string? ShopName { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string TenantMobile { get; set; } = string.Empty;
    public string? TenantEmail { get; set; }
    public string TenantType { get; set; } = "Individual";
    public string? TenantAadhaarNo { get; set; }
    public string? TenantPanCardNo { get; set; }
    public string? TenantAddress { get; set; }
    public string? GSTNo { get; set; }
    public decimal? TotalAreaSqFt { get; set; }
    public int? ApplicationTypeId { get; set; }
    public string LeaseType { get; set; } = "Rent";
    public DateTime LeaseStartDate { get; set; }
    public DateTime? LeaseEndDate { get; set; }
    public int? Duration { get; set; }
    public decimal? RentAmount { get; set; }
    public decimal SecurityDeposit { get; set; }
    public string? DepositType { get; set; }
    public string PaymentFrequency { get; set; } = "Monthly";
    public string? AgreementId { get; set; }
    public bool? IsIncrement { get; set; }
    public string? IncrementFrequency { get; set; }
    public string? IncrementType { get; set; }
    public double? IncrementValue { get; set; }
    public string? IncrementMethod { get; set; }
    public string? Reason { get; set; }
    
    // Workflow fields
    public string WorkflowStatus { get; set; } = "Pending";
    public string? RejectionReason { get; set; }
    public bool IsRejection { get; set; }
    public int? RejectionBy { get; set; }
    public DateTime? RejectionDate { get; set; }
    public bool IsVerified { get; set; }
    public int? VerifiedBy { get; set; }
    public DateTime? VerifiedDate { get; set; }
    public bool IsApproved { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }

    // Soft delete audit overrides
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }

    // Navigation properties (no virtual keyword)
    public AssetMasterEntity? Asset { get; set; }
    public AssetApplicationTypeEntity? ApplicationType { get; set; }
    public ICollection<LeaseRentRegistrationHistoryEntity> History { get; set; } = new List<LeaseRentRegistrationHistoryEntity>();
}
