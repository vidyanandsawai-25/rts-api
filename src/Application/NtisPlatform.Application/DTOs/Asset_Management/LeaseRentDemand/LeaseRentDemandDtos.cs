using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs;

namespace NtisPlatform.Application.DTOs.Asset_Management.LeaseRentDemand;

/// <summary>Read model for a single month's demand row.</summary>
public class MonthWiseDemandDto : BaseDtos
{
    public int AssetId { get; set; }
    public int LeaseRegistrationId { get; set; }
    public int FinanceYear { get; set; }
    public int DemandYear { get; set; }
    public byte QuarterNo { get; set; }
    public byte DemandMonth { get; set; }
    public decimal MonthlyRentAmount { get; set; }
    public int? PenaltyRuleMasterId { get; set; }
    public decimal PenaltyAmount { get; set; }
    public int? GSTMasterId { get; set; }
    public decimal GSTAmount { get; set; }
    public decimal TotalDemandAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public string DemandStatus { get; set; } = string.Empty;
    public DateTime? LastPaymentDate { get; set; }
    public DateTime? DueDate { get; set; }
}

/// <summary>Request body for generating/refreshing a finance year's demand.</summary>
public class GenerateDemandDto
{
    [Range(2000, 2100, ErrorMessage = "AMS_GenerateDemand_FinanceYear_InvalidRange")]
    public int FinanceYear { get; set; }
}

/// <summary>Outcome of a demand-generation run.</summary>
public class GenerateDemandResultDto
{
    public int LeaseRegistrationId { get; set; }
    public int FinanceYear { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int SkippedOutOfWindow { get; set; }
    public int TotalRows { get; set; }
}

/// <summary>Aggregated demand totals for a lease (optionally one finance year).</summary>
public class DemandSummaryDto
{
    public int LeaseRegistrationId { get; set; }
    public int? FinanceYear { get; set; }

    /// <summary>The finance year (Apr-Mar start year) treated as "current" for the pending/current split.</summary>
    public int CurrentFinanceYear { get; set; }

    public decimal TotalRent { get; set; }
    public decimal TotalPenalty { get; set; }
    public decimal TotalGst { get; set; }
    public decimal TotalDemand { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalPending { get; set; }

    /// <summary>Current finance year demand (rent + penalty + GST).</summary>
    public decimal CurrentDemand { get; set; }

    /// <summary>Amount collected against the current finance year.</summary>
    public decimal CurrentPaid { get; set; }

    /// <summary>Current finance year outstanding (demand - paid).</summary>
    public decimal CurrentPending { get; set; }

    /// <summary>Penalty component of the current finance year demand.</summary>
    public decimal CurrentPenalty { get; set; }

    /// <summary>GST component of the current finance year demand.</summary>
    public decimal CurrentGst { get; set; }

    /// <summary>Arrears: demand raised in earlier finance years (rent + penalty + GST).</summary>
    public decimal PendingDemand { get; set; }

    /// <summary>Amount collected against earlier finance years' arrears.</summary>
    public decimal PendingPaid { get; set; }

    /// <summary>Arrears outstanding (pending demand - pending paid).</summary>
    public decimal PendingOutstanding { get; set; }

    /// <summary>Penalty component of the arrears demand.</summary>
    public decimal PendingPenalty { get; set; }

    /// <summary>GST component of the arrears demand.</summary>
    public decimal PendingGst { get; set; }

    public int DemandCount { get; set; }
    public int PaidCount { get; set; }
    public int PartialCount { get; set; }
    public int PendingCount { get; set; }
}
