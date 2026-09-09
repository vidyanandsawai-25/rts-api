namespace NtisPlatform.Application.DTOs.RetrospectiveTax;

/// <summary>
/// Result of running the Retrospective Tax calculation engine for one property: which configured
/// rule matched, the resolved chargeable period, and the year count + totals + year-wise breakup
/// business points #1 and #2 ask for.
/// </summary>
public class RetrospectiveTaxEngineResultDto
{
    public int CalculationId { get; set; }
    public int PropertyId { get; set; }
    public int AppliedRuleId { get; set; }
    public string AppliedRuleCode { get; set; } = string.Empty;
    public string AppliedRuleName { get; set; } = string.Empty;

    public DateTime ChargeableStartDate { get; set; }
    public DateTime ChargeableEndDate { get; set; }

    /// <summary>How many financial years retrospective tax was charged for (business point #1).</summary>
    public int RetroYearCount { get; set; }

    public decimal TotalBaseTaxAmount { get; set; }
    public decimal TotalRetrospectiveTaxAmount { get; set; }
    public decimal TotalPenaltyAmount { get; set; }

    /// <summary>Total retrospective amount due, all years + penalty combined (business point #1).</summary>
    public decimal GrandTotalAmount { get; set; }

    /// <summary>True if the matched rule's penalty condition couldn't be conclusively evaluated and needs manual review.</summary>
    public bool RequiresManualReview { get; set; }

    /// <summary>Year-wise classification of the retrospective amount (business point #2).</summary>
    public List<RetrospectiveTaxEngineYearDto> YearWiseBreakdown { get; set; } = new();
}

public class RetrospectiveTaxEngineYearDto
{
    /// <summary>Example: "2019-20".</summary>
    public string FinancialYear { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    /// <summary>
    /// Which certificate family (PolicyCodes.Oc / Cc / ElectricBill) governs this segment — used to
    /// resolve the PolicyTaxDetails/TransMast policy code for the current year's persisted row.
    /// </summary>
    public string PolicyFamily { get; set; } = string.Empty;

    /// <summary>YEAR_WISE / CURRENT_YEAR — which rate/tax% basis priced this year.</summary>
    public string RateMode { get; set; } = string.Empty;

    public decimal BaseTaxAmount { get; set; }
    public decimal TaxMultiplier { get; set; }
    public decimal RetrospectiveTaxAmount { get; set; }
    public decimal? PenaltyPercent { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal TotalAmount { get; set; }
}
