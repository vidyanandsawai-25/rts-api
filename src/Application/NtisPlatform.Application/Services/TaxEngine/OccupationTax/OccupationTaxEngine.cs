using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Interfaces.TaxEngine;

namespace NtisPlatform.Application.Services.TaxEngine.OccupationTax;

/// <summary>
/// Pure Occupation Tax calculation engine. Given a property's certificate dates and approved
/// NETTAX figures, it determines the governing condition (BR2), prorates the certificate-onset
/// finance year by day count (BR1), rolls up retrospective years with leap-year add-backs
/// (BR4/BR5/BR7), and rejects inputs that fail preconditions (BR-6).
/// </summary>
/// <remarks>
/// The engine is deliberately dependency-light: it computes a fully-resolved
/// <see cref="OccupationTaxResult"/> and never touches persistence. Orchestration and writes are
/// the responsibility of <c>OccupationTaxService</c>, keeping the golden-figure math unit-testable
/// in isolation.
/// </remarks>
public sealed class OccupationTaxEngine : IOccupationTaxEngine
{
    private readonly ILogger<OccupationTaxEngine> _logger;

    public OccupationTaxEngine(ILogger<OccupationTaxEngine> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Computes the Occupation Tax result for <paramref name="input"/> relative to the supplied
    /// current finance year.
    /// </summary>
    /// <param name="input">Certificate dates and NETTAX configuration for the property.</param>
    /// <param name="currentFinanceYear">The active finance year (e.g. FY2026).</param>
    public OccupationTaxResult Compute(OccupationTaxInput input, FinanceYear currentFinanceYear)
    {
        ArgumentNullException.ThrowIfNull(input);
        var options = input.Options ?? throw new ArgumentException("Options are required.", nameof(input));

        // ---- BR6: Preconditions. Reject before any calculation or write. ----
        if (options.AnnualNetTax <= 0m)
        {
            _logger.LogWarning(
                "Occupation Tax rejected for property {PropertyId}: NETTAX is zero or negative.",
                input.PropertyId);
            return OccupationTaxResult.Rejected(input.PropertyId, "NETTAX must be greater than zero.");
        }

        var hasAnyCertificateDate =
            input.OccupationCertificateDate.HasValue ||
            input.CompletionCertificateDate.HasValue ||
            input.ElectricityBillDate.HasValue;

        if (!hasAnyCertificateDate)
        {
            _logger.LogWarning(
                "Occupation Tax rejected for property {PropertyId}: no certificate or electricity-bill date.",
                input.PropertyId);
            return OccupationTaxResult.Rejected(
                input.PropertyId,
                "At least one of OC, CC, or Electricity Bill date is required.");
        }

        // ---- Guard: CC→OC timeline split (D-1 rule) not yet implemented. ----
        if (input.CompletionCertificateDate.HasValue && input.OccupationCertificateDate.HasValue)
        {
            _logger.LogWarning(
                "Occupation Tax rejected for property {PropertyId}: CC→OC timeline split not yet supported.",
                input.PropertyId);
            return OccupationTaxResult.Rejected(
                input.PropertyId,
                "CC→OC timeline split (D-1 rule) is not yet implemented. " +
                "Please resubmit with either CC or OC, not both.");
        }

        // ---- NOTE: Floor-wise guard belongs in Application layer, not here. ----
        // OccupationTaxInput does not have PropertyDetailsId field yet.
        // When floor-wise is implemented, OccupationTaxApplicationService must detect
        // PropertyDetailsId on loaded certificates and either:
        //   a) Reject: "floor-wise not yet supported" (like CC→OC guard here), OR
        //   b) Route: aggregate per-floor calculations to property-level before calling engine
        // Without that guard/route in the Application layer, a floor-wise property would
        // silently flow to the engine and be computed at property-level (WRONG SCOPE).

        // ---- BR2: Determine the governing condition and the onset date. ----
        var (condition, onsetDate) = ResolveCondition(input, currentFinanceYear);

        // ---- BR4/BR5: Determine the retrospective window and current-year handling. ----
        // Every FinanceYear built during this run shares currentFinanceYear's configured
        // start month/day (from PTIS.CertificateTaxGuideline), not the 01-Apr default.
        var onsetFinanceYear = FinanceYear.ForDate(onsetDate, currentFinanceYear.StartMonth, currentFinanceYear.StartDay);

        var retroYears = BuildRetroYears(options, onsetFinanceYear, onsetDate, currentFinanceYear);

        // BR1/BR5: The current finance year always carries the FULL annual TransMast amount.
        var currentYear = BuildFullYear(options, currentFinanceYear);

        // BR1 special case: when the onset falls WITHIN the current finance year (e.g. an OC dated
        // 15-Nov-2026 in FY2026), the current year itself is prorated from the onset date rather
        // than applied in full. Applies symmetrically to OC and CC -- a CC granted mid-way through
        // the current year is prorated exactly like an OC would be. Electricity Bill is excluded:
        // its onset is always normalized to the finance-year start (see ResolveCondition), so it
        // can never land mid-year here -- day-accurate Electric Bill billing is a separate,
        // undocumented business rule, not implemented here to avoid inventing one.
        if (onsetFinanceYear.StartYear == currentFinanceYear.StartYear &&
            (condition == OccupationCondition.OccupationCertificate || condition == OccupationCondition.CompletionCertificate) &&
            onsetDate > currentFinanceYear.Start)
        {
            currentYear = BuildProratedYear(options, currentFinanceYear, onsetDate);
        }

        _logger.LogInformation(
            "Occupation Tax computed for property {PropertyId}: condition {Condition}, " +
            "current FY {CurrentFy} net {CurrentNet}, {RetroCount} retro years rolling up to {RollUp}.",
            input.PropertyId, condition, currentFinanceYear.StartYear, currentYear.NetTax,
            retroYears.Count, retroYears.Sum(y => y.NetTax));

        return new OccupationTaxResult
        {
            PropertyId = input.PropertyId,
            IsValid = true,
            Condition = condition,
            CurrentYear = currentYear,
            RetroYears = retroYears,
        };
    }

    /// <summary>
    /// BR2: OC-only => OC condition (onset = OC date). CC-only => CC condition (onset = CC date).
    /// Both present => rejected (CC→OC timeline split is not yet implemented).
    /// Neither => Electricity Bill condition, with the bill date normalized to the finance-year
    /// start so tax applies for the whole year.
    /// </summary>
    private static (OccupationCondition Condition, DateTime OnsetDate) ResolveCondition(
        OccupationTaxInput input, FinanceYear currentFinanceYear)
    {
        var oc = input.OccupationCertificateDate;
        var cc = input.CompletionCertificateDate;

        if (oc.HasValue)
        {
            // OC present => OC governs from the OC date. (CC+OC inputs are rejected earlier; timeline split not implemented.)
            return (OccupationCondition.OccupationCertificate, oc.Value);
        }

        if (cc.HasValue)
        {
            return (OccupationCondition.CompletionCertificate, cc.Value);
        }

        // Neither OC nor CC: fall back to electricity bill, normalized to the FY start so the
        // charge covers the whole current finance year (BR2).
        var billFinanceYear = input.ElectricityBillDate.HasValue
            ? FinanceYear.ForDate(input.ElectricityBillDate.Value, currentFinanceYear.StartMonth, currentFinanceYear.StartDay)
            : currentFinanceYear;
        return (OccupationCondition.ElectricityBill, billFinanceYear.Start);
    }

    /// <summary>
    /// BR4: Build the retrospective finance years between the onset FY and the current FY
    /// (exclusive of the current FY, which is handled separately). The window floor is the
    /// later of (onset FY) and the configured cut-off; when no cut-off is configured the default
    /// look-back cap applies. <see cref="OccupationTaxOptions.DefaultRetroLookbackYears"/> is the
    /// TOTAL span of years (retro + current) per the business definition -- e.g. 6 means 5 retro
    /// years plus the 1 current year, not 6 retro years on top of the current one -- so the floor
    /// is CurrentFY - (DefaultRetroLookbackYears - 1).
    /// The onset year itself is prorated from the onset date (BR5); all later retro years are full.
    /// </summary>
    private List<OccupationTaxYearResult> BuildRetroYears(
        OccupationTaxOptions options,
        FinanceYear onsetFinanceYear,
        DateTime onsetDate,
        FinanceYear currentFinanceYear)
    {
        var results = new List<OccupationTaxYearResult>();

        // Nothing retrospective if the onset is in (or after) the current finance year.
        if (onsetFinanceYear.StartYear >= currentFinanceYear.StartYear)
        {
            return results;
        }

        // Determine the earliest retro finance year (the window floor).
        int floorStartYear;
        if (options.RetroCutoffDate.HasValue)
        {
            // Config-driven cut-off overrides the default look-back cap (BR4).
            var cutoffFy = FinanceYear.ForDate(options.RetroCutoffDate.Value, currentFinanceYear.StartMonth, currentFinanceYear.StartDay);
            floorStartYear = Math.Max(onsetFinanceYear.StartYear, cutoffFy.StartYear);
        }
        else
        {
            // Default cut-off: cap the look-back so the TOTAL span (retro + current) is N years.
            var defaultFloor = currentFinanceYear.StartYear - (options.DefaultRetroLookbackYears - 1);
            floorStartYear = Math.Max(onsetFinanceYear.StartYear, defaultFloor);
        }

        for (var year = floorStartYear; year < currentFinanceYear.StartYear; year++)
        {
            var fy = new FinanceYear(year, currentFinanceYear.StartMonth, currentFinanceYear.StartDay);

            // The onset year is prorated from the onset date; every other retro year is full.
            if (fy.StartYear == onsetFinanceYear.StartYear && onsetDate > fy.Start)
            {
                results.Add(BuildProratedYear(options, fy, onsetDate));
            }
            else
            {
                results.Add(BuildFullYear(options, fy));
            }
        }

        return results;
    }

    /// <summary>
    /// Builds a full finance-year result: the whole annual NETTAX, with the leap-year add-back
    /// applied when the finance year's start year is a leap year (BR7). The add-back distributes
    /// the extra day's charge across the general portion and components in proportion to the
    /// daily rate, so a full leap year totals annual NETTAX + one day's NETTAX.
    /// </summary>
    private static OccupationTaxYearResult BuildFullYear(OccupationTaxOptions options, FinanceYear fy)
    {
        var componentTotal = options.AnnualNetTax - options.GeneralTaxPortion;
        var perComponent = options.ComponentCount > 0
            ? componentTotal / options.ComponentCount
            : 0m;

        var general = options.GeneralTaxPortion;
        var component = perComponent;
        var leap = fy.IsLeapFinanceYear;

        if (leap)
        {
            // Add back exactly one day's worth of charge (annual NETTAX / 365), split across the
            // general and component daily rates so the components remain equal and the year totals
            // annual NETTAX + 100 for a 36,500 NETTAX.
            var dailyGeneral = options.GeneralTaxPortion / FinanceYear.ProrationBasisDays;
            var dailyComponent = perComponent / FinanceYear.ProrationBasisDays;
            general += dailyGeneral * FinanceYear.LeapAddbackDays;
            component += dailyComponent * FinanceYear.LeapAddbackDays;
        }

        return new OccupationTaxYearResult
        {
            FinanceYear = fy.StartYear,
            FinanceYearStart = fy.Start,
            FinanceYearEnd = fy.End,
            GeneralTax = Round(general),
            ComponentTax = Round(component),
            ComponentCount = options.ComponentCount,
            IsProrated = false,
            ChargeableDays = fy.ActualDays,
            LeapAddbackApplied = leap,
        };
    }

    /// <summary>
    /// Builds a prorated finance-year result: each amount is scaled by
    /// (chargeable days / 365) and rounded independently, matching the approved figures
    /// (BR1: 137 days => 8,220 + 4 x 1,370; BR5: 225 days => 22,500).
    /// </summary>
    private static OccupationTaxYearResult BuildProratedYear(
        OccupationTaxOptions options, FinanceYear fy, DateTime onsetDate)
    {
        var componentTotal = options.AnnualNetTax - options.GeneralTaxPortion;
        var perComponent = options.ComponentCount > 0
            ? componentTotal / options.ComponentCount
            : 0m;

        var days = fy.ChargeableDaysFrom(onsetDate);
        var factor = (decimal)days / FinanceYear.ProrationBasisDays;

        return new OccupationTaxYearResult
        {
            FinanceYear = fy.StartYear,
            FinanceYearStart = fy.Start,
            FinanceYearEnd = fy.End,
            GeneralTax = Round(options.GeneralTaxPortion * factor),
            ComponentTax = Round(perComponent * factor),
            ComponentCount = options.ComponentCount,
            IsProrated = true,
            ChargeableDays = days,
            LeapAddbackApplied = false,
        };
    }

    /// <summary>Rounds to whole currency units, away from zero, matching approved golden figures.</summary>
    private static decimal Round(decimal value) => Math.Round(value, 0, MidpointRounding.AwayFromZero);
}
