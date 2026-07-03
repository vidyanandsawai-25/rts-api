namespace NtisPlatform.Application.DTOs.RetrospectiveTax;

/// <summary>
/// One finance-year row of the retrospective tax pivot. <see cref="Amounts"/> is index-aligned with
/// <see cref="RetrospectiveTaxDto.TaxHeadNames"/> — position N here is the amount for tax head N there.
/// </summary>
public class RetrospectiveTaxYearRowDto
{
    /// <summary>PTIS.TaxPendingDetailsRetro.PendingYearId — FK to CORE.YearMaster.</summary>
    public int PendingYearId { get; set; }

    /// <summary>CORE.YearMaster.Year, e.g. 2016.</summary>
    public int Year { get; set; }

    /// <summary>CORE.YearMaster.YearCode, e.g. "2016-17".</summary>
    public string FinanceYear { get; set; } = string.Empty;

    /// <summary>
    /// Number of days the tax liability applied during this finance year. Full years are
    /// YearMaster.EndDate - YearMaster.StartDate + 1; the property's earliest year is prorated from
    /// its registration date. Null when the year's start/end dates aren't set up.
    /// </summary>
    public int? Days { get; set; }

    /// <summary>Pending amount per tax head, in the same order as <see cref="RetrospectiveTaxDto.TaxHeadNames"/>.</summary>
    public List<decimal> Amounts { get; set; } = [];

    /// <summary>Sum of <see cref="Amounts"/> for this year.</summary>
    public decimal Total { get; set; }
}
