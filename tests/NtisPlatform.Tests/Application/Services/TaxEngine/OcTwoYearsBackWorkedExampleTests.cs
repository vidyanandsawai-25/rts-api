using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Application.Services;
using NtisPlatform.Application.Services.TaxEngine;
using NtisPlatform.Application.Services.TaxEngine.OccupationTax;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Application.Services.TaxEngine;

/// <summary>
/// End-to-end regression for the exact worked example from the 2026-07-23 audit: an OC
/// certificate dated 03-Apr-2024 while the live current finance year is 2026-27, for a single
/// GeneralTax head with an annual rate of 356. Proves the full chain in one place -- pending-year
/// persistence (TaxPendingDetailsRetro/TaxPendingDetails), current-year persistence (TransMast),
/// AND that the Tax Details grid (PropertyRepository.GetTaxDetailsAsync) shows only the current
/// year's 356 for the OC row (matching NETTAX), not a combined/summed figure across the three
/// PolicyTaxDetails rows this computation writes (one per finance year: 2024, 2025, 2026).
/// </summary>
public class OcTwoYearsBackWorkedExampleTests
{
    private const int CurrentFyYear = 2026; // FY2026 = 01-Apr-2026..31-Mar-2027

    private const int OcTypeId = 1;
    private const int NetTaxPolicyCodeId = 1;
    private const int OcPolicyCodeId = 2;
    private const int PartialOcPolicyCodeId = 3;
    private const int CcPolicyCodeId = 4;
    private const int PartialCcPolicyCodeId = 5;
    private const int ElectricBillPolicyCodeId = 6;
    private const int PartialElectricBillPolicyCodeId = 7;

    private const int GeneralTaxId = 1;
    private const decimal AnnualTax = 356m;

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static int Seed(ApplicationDbContext context, int propertyId = 1, decimal annualTax = AnnualTax, int[]? years = null)
    {
        years ??= new[] { 2024, 2025, 2026 };

        context.PropertyMast.Add(new PropertyEntity { Id = propertyId, WardId = 1, PropertyNo = propertyId.ToString(), IsActive = true });

        // The retro years the OC date spans, plus the live current year -- YearMaster.Id doubles
        // as PendingYearId/FinanceYearId in this test.
        foreach (var year in years)
        {
            context.YearMaster.Add(new YearMasterEntity { Id = year, Year = year, YearCode = $"{year}-{(year + 1) % 100:D2}", IsActive = true });
        }

        context.PropertyCertificateTypeMasters.Add(
            new PropertyCertificateTypeMasterEntity { Id = OcTypeId, CertificateTypeName = "Occupancy Certificate", CertificateTypeCode = "OC", IsTaxable = true, IsActive = true });

        context.PolicyCodeMaster.AddRange(
            new PolicyCodeMasterEntity { Id = NetTaxPolicyCodeId, PolicyCode = "NETTAX", IsActive = true },
            new PolicyCodeMasterEntity { Id = OcPolicyCodeId, PolicyCode = "OC", IsActive = true },
            new PolicyCodeMasterEntity { Id = PartialOcPolicyCodeId, PolicyCode = "PARTIAL_OC", IsActive = true },
            new PolicyCodeMasterEntity { Id = CcPolicyCodeId, PolicyCode = "CC", IsActive = true },
            new PolicyCodeMasterEntity { Id = PartialCcPolicyCodeId, PolicyCode = "PARTIAL_CC", IsActive = true },
            new PolicyCodeMasterEntity { Id = ElectricBillPolicyCodeId, PolicyCode = "ELECTRIC_BILL", IsActive = true },
            new PolicyCodeMasterEntity { Id = PartialElectricBillPolicyCodeId, PolicyCode = "PARTIAL_ELECTRIC_BILL", IsActive = true });

        context.TaxCategoryMaster.Add(new TaxCategoryMasterEntity { Id = 1, CategoryCode = "TAX", CategoryName = "Property Tax", IsActive = true });
        context.TaxMaster.Add(new TaxMasterEntity { Id = GeneralTaxId, TaxName = "General Tax", TaxCode = "GEN", DisplayOrder = 1, TaxCategoryId = 1, IsActive = true });
        context.TaxMaster.Add(new TaxMasterEntity { Id = 99, TaxName = "TaxTotal", TaxCode = "TaxTotal", DisplayOrder = 99, TaxCategoryId = 1, IsActive = true });

        // Exactly ONE active NETTAX row per (PropertyId, TaxId) -- the DBA-confirmed schema has no
        // PolicyYear column, so this single current rate is used uniformly for every finance year
        // (current and every retro/arrears year alike).
        context.PolicyTaxDetails.Add(new PolicyTaxDetailsEntity
        {
            Id = 1000,
            PropertyId = propertyId,
            PolicyCodeId = NetTaxPolicyCodeId,
            TaxId = GeneralTaxId,
            TaxAmount = annualTax,
            CalculationValue = 50_000m,
            IsActive = true,
            MarkedForDeletion = false
        });

        context.SaveChanges();
        return propertyId;
    }

    private static void AddOcCertificate(ApplicationDbContext context, int propertyId, DateTime issueDate)
    {
        var cert = PropertyCertificateEntity.Create(
            propertyId: propertyId,
            certificateTypeId: OcTypeId,
            certificateNo: $"OC-{issueDate:yyyyMMdd}",
            issueDate: issueDate,
            propertyDetailsId: null);
        context.PropertyCertificates.Add(cert);
        context.SaveChanges();
    }

    private static Mock<ICertificateTaxGuidelineReaderService> BuildGuidelineReaderMock(int lookbackYears = 6)
    {
        var mock = new Mock<ICertificateTaxGuidelineReaderService>();
        mock.Setup(g => g.GetActiveSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CertificateTaxGuidelineSettings(
                EnableCertificateBasedTax: true,
                ApplyOnlyTaxableCertTypes: true,
                DatePriority1: "CC", DatePriority2: "OC", DatePriority3: "ELECTRIC_BILL", DatePriority4: "RETROSPECTIVE",
                CertificateRequireNoAndDate: true,
                MissingCertificateNoAction: "IGNORE_FOR_TAX",
                MissingCertificateDateAction: "IGNORE_FOR_TAX",
                IgnoreCcToOcWithinValue: 6, IgnoreCcToOcWithinType: "MONTHS",
                CcOcGapComparison: "LESS_THAN_OR_EQUAL",
                CcOcGapWithinAction: "APPLY_CC_THEN_OC",
                CcOcGapExceededAction: "APPLY_CC_THEN_OC",
                InvalidCcOcDateOrderAction: "USE_PRIORITY_AND_LOG",
                CcOnlyAction: "APPLY_FROM_CC_DATE",
                OcOnlyAction: "APPLY_FROM_OC_DATE",
                FinancialYearStartMonth: 4, FinancialYearStartDay: 1,
                CCPeriodMultiplier: 1.5m, OCPeriodMultiplier: 1.0m,
                ElectricBillDateRule: "FROM_FY_START", ElectricBillAddMonths: 0, ElectricBillMultiplier: 1.0m,
                ElectricBillMinimumFinancialYear: 2016, EnableRetrospectiveTax: true,
                NoDateRule: "DEFAULT_RETROSPECTIVE", LookbackYears: lookbackYears, DefaultRetrospectiveMultiplier: 1.0m,
                EnableCurrentYearProration: true, ProrationMethod: "DAILY", CurrentYearProrationStartRule: "EXACT_DATE",
                TaxPersistenceMode: "PROPERTY_AGGREGATED",
                SaveInPolicyTaxDetails: true, SaveInTransMast: true, DoNotUpdateNettax: true,
                RecalculateOnSave: true, RecalculateOnDelete: true, GuidelineChangeApplyMode: "NEXT_CALCULATION",
                CcPartialPolicyCode: "PARTIAL_CC", CcFullPolicyCode: "CC",
                OcPartialPolicyCode: "PARTIAL_OC", OcFullPolicyCode: "OC",
                ElectricBillPartialPolicyCode: "PARTIAL_ELECTRIC_BILL", ElectricBillFullPolicyCode: "ELECTRIC_BILL",
                CertificateTaxScopeMode: "PROPERTY_WISE", AllowFloorWiseCertificateMetadata: false, EnableCcToOcSplit: true,
                ElectricBillCertificateCodes: "ELECTRIC_BILL", RetrospectiveCurrentYearCount: 1,
                RetrospectivePendingYearCountMode: "TOTAL_MINUS_CURRENT", FloorPolicyDisplayRule: "BIGGEST_AREA_FLOOR_POLICY",
                TaxationRateMode: "CURRENT_YEAR_FOR_ALL", TaxPercentageMode: "CURRENT_YEAR_FOR_ALL", FixedTaxPercentage: 0m));
        return mock;
    }

    private static OccupationTaxApplicationService BuildService(ApplicationDbContext context, Mock<ICertificateTaxGuidelineReaderService> guidelineReader)
    {
        var propertyRepo = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == CurrentFyYear));
        var certRepo = new Repository<PropertyCertificateEntity, int>(context);
        var policyTaxRepo = new Repository<PolicyTaxDetailsEntity, int>(context);
        var transMastRepo = new Repository<TransMastEntity, int>(context);
        var yearRepo = new Repository<YearMasterEntity, int>(context);
        var taxPendingRepo = new Repository<TaxPendingDetailsEntity, int>(context);
        var taxPendingRetroRepo = new Repository<TaxPendingDetailsRetroEntity, int>(context);
        var policyCodeRepo = new Repository<PolicyCodeMasterEntity, int>(context);
        var policyCodeLookup = new PolicyCodeLookupService(policyCodeRepo);
        var unitOfWork = new UnitOfWork(context);
        var financeYearProvider = Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == CurrentFyYear);
        var engine = new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance);

        return new OccupationTaxApplicationService(
            engine, propertyRepo, certRepo, policyTaxRepo, transMastRepo, yearRepo,
            taxPendingRepo, taxPendingRetroRepo,
            policyCodeLookup, financeYearProvider, guidelineReader.Object, Mock.Of<IHistoricalNetTaxBaselineService>(), unitOfWork,
            NullLogger<OccupationTaxApplicationService>.Instance,
            NtisPlatform.Tests.Helpers.NoOpTaxApplicabilityService.Instance);
    }

    [Fact]
    public async Task OcTwoYearsBack_ExactWorkedExample_354_356_710_356_AndGridShowsCurrentYearOnly()
    {
        using var context = CreateContext();
        var propertyId = Seed(context);
        AddOcCertificate(context, propertyId, new DateTime(2024, 4, 3)); // 03-04-2024

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        // ---- TaxPendingDetailsRetro: year-wise breakup ----
        var retro = context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.IsActive).ToList();
        Assert.Equal(2, retro.Count);
        Assert.Equal(354m, retro.Single(r => r.PendingYearId == 2024).PendingAmount);
        Assert.Equal(356m, retro.Single(r => r.PendingYearId == 2025).PendingAmount);

        // ---- TaxPendingDetails: ONE summary row per TaxId (not one per pending year), tagged
        // SPECIFICALLY with the previous finance year (2025 = CurrentFyYear-1), not 2024. ----
        var pending = context.TaxPendingDetails.Where(r => r.PropertyId == propertyId && r.IsActive).ToList();
        Assert.Single(pending);
        Assert.Equal(710m, pending[0].PendingAmount);
        Assert.Equal(2025, pending[0].PendingYearId);

        // ---- TransMast: CURRENT FY ONLY -- retro years (2024, 2025) must never appear here, since
        // TransMast is the "current financial year tax" table read WITHOUT a year filter by
        // CombinePropertyService.GetTaxDataAsync and PropertyReassessmentService; a retro-year row
        // left there would inflate those beyond the property's actual current-year demand. The
        // year-wise retro breakup belongs in TaxPendingDetailsRetro (asserted above), not here. ----
        var transMast = context.TransMast.Where(t => t.PropertyId == propertyId && t.IsActive && t.CalculationType == "RV").ToList();
        Assert.Single(transMast);
        Assert.Equal(356m, transMast.Single(t => t.FinanceYearId == 2026).TaxAmount);

        // ---- Tax Details grid: must show ONLY the current year's 356, matching NETTAX.
        // PolicyTaxDetails holds exactly ONE row now (current year only, per the DBA-confirmed
        // schema -- no PolicyYear column, unique index on PropertyId+PolicyCodeId+TaxId); the two
        // retro years (2024, 2025) live in TaxPendingDetailsRetro/TaxPendingDetails instead. ----
        var policyRows = context.PolicyTaxDetails.Where(p => p.PropertyId == propertyId && p.IsActive && p.PolicyCodeId != NetTaxPolicyCodeId && p.TaxId != 99).ToList();
        Assert.Single(policyRows); // current year only

        var propertyRepo = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == CurrentFyYear));
        var grid = await propertyRepo.GetTaxDetailsAsync(propertyId);

        Assert.NotNull(grid);
        var ocPolicy = grid!.Policies.Single(p => p.PolicyCode == "OC");
        var ocRow = Assert.Single(ocPolicy.TaxAmounts);
        Assert.Equal("General Tax", ocRow.TaxName);
        Assert.Equal(356m, ocRow.TaxAmount); // current year only -- matches NETTAX, not 1066
        Assert.Equal(356m, ocPolicy.TaxTotal);
    }

    // ------------------------------------------------------------------------------------------
    // Second worked example (2026-07-23 follow-up): OC = 01-Apr-2023, current FY = 2026-27,
    // annual tax = 500 -- three retro years (2023, 2024, 2025) instead of two. OC's onset date
    // lands EXACTLY on FY2023's start day (not after it), so FY2023 itself is a FULL year, not a
    // prorated one (OccupationTaxEngine.BuildRetroYears only prorates the onset year when
    // onsetDate > fy.Start -- see OccupationTaxEngine.cs line 201). FY2024's start year (2024) IS
    // a leap year, so its full-year amount gets BR7's one-day leap add-back (500/365, rounded into
    // the year total) -- 501, not 500. This is a real, easy-to-miss nuance the manual arithmetic in
    // chat could get wrong, which is exactly why this is asserted via the real engine, not by hand.
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task OcThreeYearsBack_ExactWorkedExample_500_501_500_1501_500_LeapYearAddback()
    {
        using var context = CreateContext();
        var years = new[] { 2023, 2024, 2025, 2026 };
        const decimal annualTax = 500m;
        var propertyId = Seed(context, annualTax: annualTax, years: years);
        AddOcCertificate(context, propertyId, new DateTime(2023, 4, 1)); // 01-04-2023

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        // ---- TaxPendingDetailsRetro: year-wise breakup -- FY2024-25 gets the leap add-back (501) ----
        var retro = context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.IsActive).ToList();
        Assert.Equal(3, retro.Count);
        Assert.Equal(500m, retro.Single(r => r.PendingYearId == 2023).PendingAmount);
        Assert.Equal(501m, retro.Single(r => r.PendingYearId == 2024).PendingAmount); // leap-year add-back (BR7)
        Assert.Equal(500m, retro.Single(r => r.PendingYearId == 2025).PendingAmount);

        // ---- TaxPendingDetails: ONE summary row (500 + 501 + 500 = 1501), not one per pending year --
        // tagged SPECIFICALLY with the previous finance year (2025 = CurrentFyYear-1), never the
        // older retro years 2023/2024. ----
        var pending = context.TaxPendingDetails.Where(r => r.PropertyId == propertyId && r.IsActive).ToList();
        Assert.Single(pending);
        Assert.Equal(1501m, pending[0].PendingAmount);
        Assert.Equal(2025, pending[0].PendingYearId);

        // ---- TransMast: CURRENT FY ONLY (2026-27 = 500, not a leap start year, no add-back) --
        // the three retro years (2023, 2024, 2025) must never appear here; they live only in
        // TaxPendingDetailsRetro (year-wise breakup, asserted above) and TaxPendingDetails
        // (their sum, 1501). ----
        var transMast = context.TransMast.Where(t => t.PropertyId == propertyId && t.IsActive && t.CalculationType == "RV").ToList();
        Assert.Single(transMast);
        Assert.Equal(500m, transMast.Single(t => t.FinanceYearId == 2026).TaxAmount);

        // ---- Table-responsibility enforcement (explicit negative assertions) ----
        // B: TaxPendingDetailsRetro must never carry a current-FY (2026) row.
        Assert.DoesNotContain(retro, r => r.PendingYearId == 2026);
        // C: TaxPendingDetails must never carry a current-FY (2026) row (its sum is retro-only, 1501).
        Assert.DoesNotContain(pending, p => p.PendingYearId == 2026);
        // D: TransMast must never carry the pending/retro total (1501) or any retro-year row.
        Assert.DoesNotContain(transMast, t => t.TaxAmount == 1501m);
        Assert.DoesNotContain(transMast, t => t.FinanceYearId != 2026);

        // ---- Tax Details grid: OC row shows ONLY the current year's 500, matching NETTAX --
        // none of the three retro years' rows (500, 501, 500) leak into or corrupt this figure. ----
        var propertyRepo = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == CurrentFyYear));
        var grid = await propertyRepo.GetTaxDetailsAsync(propertyId);

        Assert.NotNull(grid);
        var ocPolicy = grid!.Policies.Single(p => p.PolicyCode == "OC");
        // E: grid must show exactly one OC tax row (current year only) -- no retro/pending rows
        // (500/501/500) leaking in as extra TaxAmounts entries.
        var ocRow = Assert.Single(ocPolicy.TaxAmounts);
        Assert.Equal("General Tax", ocRow.TaxName);
        Assert.Equal(500m, ocRow.TaxAmount); // current year only -- matches NETTAX
        Assert.Equal(500m, ocPolicy.TaxTotal);
    }

    // ------------------------------------------------------------------------------------------
    // Regression for the 2026-07-29 report: an OC (or Electric Bill) date years in the past showed
    // NO pending years on the Tax Details grid when the live PTIS.CertificateTaxGuideline row for
    // NO_DATE_LOOKBACK_YEARS was missing/blank -- CertificateTaxGuidelineReaderService defaults a
    // missing/unparseable row to 0, and OccupationTaxApplicationService.BuildOptionsAsync used to
    // pass that straight through as DefaultRetroLookbackYears with no floor, which made the
    // engine's retro-year loop never execute (floorStartYear ends up one year past the current FY)
    // -- for ANY certificate date, no matter how far back. Once a real certificate date is known,
    // "lookback years" is not a legitimate truncation at all (tax is owed from that date forward,
    // full stop), so OccupationTaxEngine.BuildRetroYears no longer consults it -- the retro floor
    // is simply the onset FY, and NO_DATE_LOOKBACK_YEARS/LookbackYears is irrelevant to this path
    // regardless of its configured value. This test proves an OC dated 01-Apr-2022, evaluated in
    // FY2026-27 with LookbackYears simulated as unseeded (0), still produces its 4 retro years
    // (2022-2025) and a non-empty PendingYears list on the grid.
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task OcFourYearsBack_WithUnseededLookbackYears_StillProducesPendingYears()
    {
        using var context = CreateContext();
        var years = new[] { 2022, 2023, 2024, 2025, 2026 };
        const decimal annualTax = 500m;
        var propertyId = Seed(context, annualTax: annualTax, years: years);
        AddOcCertificate(context, propertyId, new DateTime(2022, 4, 1)); // 01-04-2022, matches the reported scenario

        var guidelineReader = BuildGuidelineReaderMock(lookbackYears: 0); // simulates a missing/blank NO_DATE_LOOKBACK_YEARS row
        var service = BuildService(context, guidelineReader);
        await service.ApplyAsync(propertyId, userId: 1);

        // ---- TaxPendingDetailsRetro: 4 retro years generated (2022, 2023, 2024, 2025) despite
        // LookbackYears resolving to 0 -- proves the engine no longer truncates by lookback years
        // once a real certificate date is known. ----
        var retro = context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.IsActive).ToList();
        Assert.Equal(4, retro.Count);
        Assert.All(retro, r => Assert.True(r.PendingAmount > 0m));

        // ---- TaxPendingDetails: non-zero summary row must exist (the reported symptom was an
        // empty/zero pending demand). ----
        var pending = context.TaxPendingDetails.Where(r => r.PropertyId == propertyId && r.IsActive).ToList();
        Assert.Single(pending);
        Assert.True(pending[0].PendingAmount > 0m);

        // ---- Tax Details grid: PendingYears must be populated, not empty -- this is exactly what
        // the user observed as missing ("no pending year taxes demand displaying"). ----
        var propertyRepo = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == CurrentFyYear));
        var grid = await propertyRepo.GetTaxDetailsAsync(propertyId);

        Assert.NotNull(grid);
        var ocPolicy = grid!.Policies.Single(p => p.PolicyCode == "OC");
        Assert.Equal(4, ocPolicy.PendingYears.Count);
        Assert.DoesNotContain(ocPolicy.PendingYears, py => py.PendingYearId == 2026); // current FY must never appear as "pending"
    }
}
