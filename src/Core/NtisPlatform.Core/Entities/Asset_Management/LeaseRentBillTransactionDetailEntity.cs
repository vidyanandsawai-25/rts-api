namespace NtisPlatform.Core.Entities.Asset_Management;

/// <summary>
/// Month-wise payment detail line of a <see cref="LeaseRentBillTransactionEntity"/>.
/// Maps to the [AMS].[LeaseRentBillTransactionDetails] table. One line settles one
/// <see cref="MonthWiseDemandEntity"/>. <see cref="TotalDemandAmount"/> is a SQL
/// computed (PERSISTED) column — read back by EF, never written.
/// </summary>
public class LeaseRentBillTransactionDetailEntity : BaseEntity
{
    public int LeaseRentBillTransactionId { get; set; }
    public int MonthWiseDemandId { get; set; }

    public int AssetId { get; set; }
    public int LeaseRegistrationId { get; set; }

    public int FinanceYear { get; set; }
    public int DemandYear { get; set; }

    public byte QuarterNo { get; set; }
    public byte DemandMonth { get; set; }

    public decimal MonthlyRentAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal GSTAmount { get; set; }

    /// <summary>Computed (PERSISTED): rent + penalty + gst.</summary>
    public decimal TotalDemandAmount { get; private set; }

    public decimal PreviousPaidAmount { get; set; }
    public decimal CurrentPaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }

    /// <summary>Partial | Paid | Cancelled.</summary>
    public string PaymentStatus { get; set; } = "Paid";

    // Navigation
    public LeaseRentBillTransactionEntity? Transaction { get; set; }
    public MonthWiseDemandEntity? MonthWiseDemand { get; set; }
}
