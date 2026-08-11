using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Core.Entities.Asset_Management;

/// <summary>
/// One month's rent demand for a lease. Maps to the [AMS].[MonthWiseDemand] table.
/// <see cref="TotalDemandAmount"/> and <see cref="PendingAmount"/> are SQL computed
/// (PERSISTED) columns — they are read back by EF but never written.
/// </summary>
public class MonthWiseDemandEntity : BaseEntity
{
    /// <summary>FK to AssetMaster.</summary>
    public int AssetId { get; set; }

    /// <summary>Loose reference to AssetLeaseRentDetails.Id (no DB FK).</summary>
    public int LeaseRegistrationId { get; set; }

    /// <summary>Indian finance-year start calendar year (e.g. 2025 = FY 2025-26).</summary>
    public int FinanceYear { get; set; }

    /// <summary>Calendar year of <see cref="DemandMonth"/>.</summary>
    public int DemandYear { get; set; }

    /// <summary>Fiscal quarter 1..4 (Q1 = Apr-Jun).</summary>
    public byte QuarterNo { get; set; }

    /// <summary>Calendar month 1..12.</summary>
    public byte DemandMonth { get; set; }

    public decimal MonthlyRentAmount { get; set; }

    public int? PenaltyRuleMasterId { get; set; }
    public decimal PenaltyAmount { get; set; }

    public int? GSTMasterId { get; set; }
    public decimal GSTAmount { get; set; }

    /// <summary>Computed (PERSISTED): MonthlyRentAmount + PenaltyAmount + GSTAmount.</summary>
    public decimal TotalDemandAmount { get; private set; }

    public decimal PaidAmount { get; set; }

    /// <summary>Computed (PERSISTED): TotalDemandAmount - PaidAmount.</summary>
    public decimal PendingAmount { get; private set; }

    /// <summary>Pending | Partial | Paid | Cancelled.</summary>
    public string DemandStatus { get; set; } = "Pending";

    public DateTime? LastPaymentDate { get; set; }
    public DateTime? DueDate { get; set; }

    // Navigation
    public GSTMasterEntity? GSTMaster { get; set; }
    public PenaltyRuleMasterEntity? PenaltyRuleMaster { get; set; }
}
