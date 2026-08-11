namespace NtisPlatform.Core.Entities.Asset_Management;

/// <summary>
/// Append-only audit log for a lease registration. Each row is a full point-in-time snapshot
/// of the registration, captured after a change. Maps to [AMS].[LeaseRentRegistrationHistory].
/// </summary>
public class LeaseRentRegistrationHistoryEntity : BaseEntity
{
    public int LeaseRentRegistrationId { get; set; }

    /// <summary>FK to AssetMaster for direct asset reference.</summary>
    public int AssetId { get; set; }

    public string? Remarks { get; set; }

    // ── Point-in-time snapshot of [AssetLeaseRentDetails] ─────────────────────
    /// <summary>Captured AFTER the change is applied. Prefix Snap_ to avoid ambiguity.</summary>
    public string? Snap_GrievanceNo { get; set; }
    public string? Snap_ShopNo { get; set; }
    public string? Snap_Floor { get; set; }
    public string? Snap_ShopName { get; set; }

    // Tenant
    public string Snap_TenantName { get; set; } = string.Empty;
    public string Snap_TenantMobile { get; set; } = string.Empty;
    public string? Snap_TenantEmail { get; set; }
    public string Snap_TenantType { get; set; } = string.Empty;
    public string? Snap_TenantAadhaarNo { get; set; }
    public string? Snap_TenantPanCardNo { get; set; }
    public string? Snap_TenantAddress { get; set; }
    public string? Snap_GSTNo { get; set; }

    // Previous tenant
    public string? Snap_PreviousTenantName { get; set; }
    public string? Snap_PreviousTenantMobile { get; set; }

    public decimal? Snap_TotalAreaSqFt { get; set; }

    // Application / lease
    public string? Snap_ApplicationType { get; set; }
    public string Snap_LeaseType { get; set; } = string.Empty;
    public string? Snap_LeaseRentType { get; set; }

    // Dates
    public DateTime? Snap_OldLeaseStartDate { get; set; }
    public DateTime? Snap_OldLeaseEndDate { get; set; }
    public DateTime? Snap_LeaseStartDate { get; set; }
    public DateTime? Snap_LeaseEndDate { get; set; }
    public DateTime? Snap_TerminationDate { get; set; }
    public int? Snap_Duration { get; set; }

    // Financial
    public decimal? Snap_PreviousMonthlyRent { get; set; }
    public decimal Snap_MonthlyRent { get; set; }
    public decimal? Snap_RentAmount { get; set; }
    public decimal Snap_SecurityDeposit { get; set; }
    public string? Snap_DepositType { get; set; }
    public string Snap_PaymentFrequency { get; set; } = string.Empty;
    public string? Snap_AgreementId { get; set; }
    public string? Snap_IncrementFrequency { get; set; }
    public string? Snap_IncrementType { get; set; }
    public double? Snap_IncrementValue { get; set; }
    public string? Snap_IncrementMethod { get; set; }
    public DateTime? Snap_DurationFrom { get; set; }
    public DateTime? Snap_DurationTo { get; set; }
    public double? Snap_Increment { get; set; }
    public bool? Snap_IncrementStatus { get; set; }
    public double? Snap_RentMonthly { get; set; }

    // Minor correction
    public string? Snap_CorrectionField { get; set; }
    public string? Snap_CorrectedValue { get; set; }

    public string? Snap_Reason { get; set; }

    // Workflow state (AFTER this action)
    public string Snap_WorkflowStatus { get; set; } = string.Empty;
    public string? Snap_RejectionReason { get; set; }
    public bool? Snap_IsRejection { get; set; }
    public bool? Snap_IsVerified { get; set; }
    public bool? Snap_IsApproved { get; set; }
    public string Snap_RentStatus { get; set; } = string.Empty;
    public string? Snap_PaymentStatus { get; set; }
    public bool Snap_IsActive { get; set; }

    // ── Actor ────────────────────────────────────────────────────────────────
    public int PerformedBy { get; set; }
    public DateTime PerformedDate { get; set; }

    // Navigation
    public AssetLeaseRentDetailsEntity? LeaseRentRegistration { get; set; }
    public AssetMasterEntity? Asset { get; set; }
}
