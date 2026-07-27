namespace NtisPlatform.Application.Services.TaxEngine.OccupationTax;

/// <summary>
/// Indian finance year helper. A finance year labelled <c>YYYY</c> runs from its configured
/// start date in <c>YYYY</c> through the day before that same date in <c>YYYY+1</c> (inclusive).
/// Defaults to 01-Apr..31-Mar, but the start month/day is admin-configurable via
/// PTIS.CertificateTaxGuideline (FinancialYearStartMonth/FinancialYearStartDay) — see
/// <see cref="OccupationTaxApplicationService"/>, which threads the guideline's configured value
/// into every FinanceYear it constructs so the whole engine run uses one consistent cutoff.
/// </summary>
/// <remarks>
/// Leap-year handling for the Occupation Tax engine keys the add-back off the finance-year
/// <em>start</em> year (BR7): FY2024 (01-Apr-2024..31-Mar-2025 by default) is treated as a leap
/// finance year because its start year, 2024, is a leap year. Day-count proration always uses a
/// fixed 365-day base so a partial year yields the approved figure regardless of leap status; the
/// leap day is only ever added back to full years.
/// </remarks>
public readonly record struct FinanceYear
{
    /// <summary>The start (label) year, e.g. 2026.</summary>
    public int StartYear { get; }

    /// <summary>Configured finance-year start month (1-12). Defaults to 4 (April).</summary>
    public int StartMonth { get; }

    /// <summary>Configured finance-year start day of month. Defaults to 1.</summary>
    public int StartDay { get; }

    /// <summary>Number of days used to prorate partial finance years. Fixed at 365 by policy.</summary>
    public const int ProrationBasisDays = 365;

    /// <summary>The amount added back to a full finance year when its start year is a leap year.</summary>
    public const decimal LeapAddbackDays = 1m;

    /// <summary>Constructs a finance year from its start (label) year and configured start month/day.</summary>
    public FinanceYear(int startYear, int startMonth = 4, int startDay = 1)
    {
        StartYear = startYear;
        StartMonth = startMonth;
        StartDay = startDay;
    }

    /// <summary>The configured start date within the start year.</summary>
    public DateTime Start => new(StartYear, StartMonth, StartDay);

    /// <summary>The day before the next occurrence of the configured start date (inclusive end).</summary>
    public DateTime End => Start.AddYears(1).AddDays(-1);

    /// <summary>True when the finance year's start year is a leap year (BR7 add-back key).</summary>
    public bool IsLeapFinanceYear => DateTime.IsLeapYear(StartYear);

    /// <summary>Actual inclusive day length of this finance year (365 or 366).</summary>
    public int ActualDays => (End - Start).Days + 1;

    /// <summary>
    /// Returns the finance year that contains the given date, using the configured finance-year
    /// cutoff (month/day, default 01-Apr). Compares the full date against the cutoff date rather
    /// than only the month, so a non-default <paramref name="startDay"/> (e.g. mid-month) is
    /// honored correctly instead of silently truncating to "the 1st" the way a month-only
    /// comparison would.
    /// </summary>
    public static FinanceYear ForDate(DateTime date, int startMonth = 4, int startDay = 1)
    {
        var cutoff = new DateTime(date.Year, startMonth, startDay);
        var startYear = date >= cutoff ? date.Year : date.Year - 1;
        return new FinanceYear(startYear, startMonth, startDay);
    }

    /// <summary>
    /// Inclusive count of chargeable days from <paramref name="from"/> (which must fall within
    /// this finance year) through the finance-year end.
    /// </summary>
    public int ChargeableDaysFrom(DateTime from)
    {
        var start = from < Start ? Start : from;
        return (End - start).Days + 1;
    }

    /// <inheritdoc />
    public override string ToString() => $"FY{StartYear} ({Start:dd-MMM-yyyy}..{End:dd-MMM-yyyy})";
}
