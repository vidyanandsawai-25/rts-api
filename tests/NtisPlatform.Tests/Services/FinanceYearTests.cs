using NtisPlatform.Application.Services.TaxEngine.OccupationTax;
using Xunit;

namespace NtisPlatform.Tests.Services;

/// <summary>
/// FinanceYear's start month/day is admin-configurable via PTIS.CertificateTaxGuideline
/// (FinancialYearStartMonth/FinancialYearStartDay), not hardcoded to 01-Apr. These tests cover
/// both the default (01-Apr) behavior and a non-default configured cutoff.
/// </summary>
public class FinanceYearTests
{
    [Fact]
    public void DefaultConstructor_UsesAprilFirst()
    {
        var fy = new FinanceYear(2026);

        Assert.Equal(new DateTime(2026, 4, 1), fy.Start);
        Assert.Equal(new DateTime(2027, 3, 31), fy.End);
        Assert.Equal(365, fy.ActualDays);
    }

    [Fact]
    public void ConfiguredStart_JulyFifteenth_ReflectsInStartAndEnd()
    {
        var fy = new FinanceYear(2026, startMonth: 7, startDay: 15);

        Assert.Equal(new DateTime(2026, 7, 15), fy.Start);
        Assert.Equal(new DateTime(2027, 7, 14), fy.End);
    }

    [Fact]
    public void ForDate_DefaultCutoff_MatchesLegacyMonthOnlyBehavior()
    {
        // 31-Mar belongs to the previous FY; 01-Apr belongs to the current FY -- exactly the
        // behavior the old Month >= 4 check produced, now derived from a full-date comparison.
        Assert.Equal(2025, FinanceYear.ForDate(new DateTime(2026, 3, 31)).StartYear);
        Assert.Equal(2026, FinanceYear.ForDate(new DateTime(2026, 4, 1)).StartYear);
    }

    [Fact]
    public void ForDate_ConfiguredMidMonthCutoff_HonorsDayOfMonth_NotJustMonth()
    {
        // Regression test for the bug where only Month >= 4 was checked, silently ignoring
        // FinancialYearStartDay. With a configured cutoff of 15-Jul, 10-Jul must fall in the
        // PREVIOUS finance year and 20-Jul in the CURRENT one, even though both share the same
        // calendar month as the cutoff.
        var before = FinanceYear.ForDate(new DateTime(2026, 7, 10), startMonth: 7, startDay: 15);
        var after = FinanceYear.ForDate(new DateTime(2026, 7, 20), startMonth: 7, startDay: 15);
        var exactlyOnCutoff = FinanceYear.ForDate(new DateTime(2026, 7, 15), startMonth: 7, startDay: 15);

        Assert.Equal(2025, before.StartYear);
        Assert.Equal(2026, after.StartYear);
        Assert.Equal(2026, exactlyOnCutoff.StartYear);
    }

    [Fact]
    public void ForDate_ReturnedFinanceYear_CarriesConfiguredStartMonthDay()
    {
        // The FinanceYear returned by ForDate must itself carry the configured cutoff forward,
        // so any FinanceYear derived from it (e.g. via `new FinanceYear(year)` inside the engine)
        // stays consistent for the rest of that computation.
        var fy = FinanceYear.ForDate(new DateTime(2026, 8, 1), startMonth: 7, startDay: 15);

        Assert.Equal(7, fy.StartMonth);
        Assert.Equal(15, fy.StartDay);
        Assert.Equal(new DateTime(2026, 7, 15), fy.Start);
    }

    [Fact]
    public void ChargeableDaysFrom_ConfiguredStart_CountsFromConfiguredEndNotAprilDefault()
    {
        var fy = new FinanceYear(2026, startMonth: 7, startDay: 15);

        // Full year from its own start = ActualDays.
        Assert.Equal(fy.ActualDays, fy.ChargeableDaysFrom(fy.Start));

        // From 01-Jan-2027 (within this FY, since it runs 15-Jul-2026..14-Jul-2027) through End.
        var days = fy.ChargeableDaysFrom(new DateTime(2027, 1, 1));
        Assert.Equal((fy.End - new DateTime(2027, 1, 1)).Days + 1, days);
    }
}
