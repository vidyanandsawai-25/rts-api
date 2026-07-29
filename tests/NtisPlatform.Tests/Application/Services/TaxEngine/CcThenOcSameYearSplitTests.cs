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
/// Regression coverage for the "Sudharit Vajavi Bhade Ambalbajavani" point 5 business rule
/// (2026-07-21): CC applies at CC_PERIOD_MULTIPLIER (1.5x) from its own date; when OC also
/// exists, CC governs only up to the day before OC's date and OC takes over at
/// OC_PERIOD_MULTIPLIER (1x) from its own date onward -- including a genuine day-level split
/// WITHIN the same finance year, not just across finance years (that cross-year case was already
/// correctly handled by <see cref="OccupationTaxApplicationService"/>'s existing CC-then-OC merge;
/// this file specifically exercises the previously-unimplemented same-year split, see
/// OccupationTaxApplicationService.ComputeCcThenOcMerge/BuildDateRangeYear).
///
/// Electric Bill is fallback-only, never overriding CC or OC regardless of date order.
///
/// Uses the real EF InMemory ApplicationDbContext, real OccupationTaxEngine,
/// PolicyCodeLookupService, and OccupationTaxApplicationService -- no mocked repositories -- so
/// these tests exercise the actual persisted PolicyTaxDetails/TransMast rows, not just in-memory
/// booleans. All dates are anchored to a fixed past finance year (FY2020) instead of "today" so
/// the tests never depend on which real calendar date they happen to run on.
/// </summary>
public class CcThenOcSameYearSplitTests
{
    private const int CurrentFyYear = 2020; // FY2020 = 01-Apr-2020..31-Mar-2021 (365 actual days -- its Feb falls in 2021, a non-leap year)

    private const int CcTypeId = 1;
    private const int OcTypeId = 2;
    private const int ElectricBillTypeId = 3;

    private const int NetTaxPolicyCodeId = 1;
    private const int OcPolicyCodeId = 2;
    private const int PartialOcPolicyCodeId = 3;
    private const int CcPolicyCodeId = 4;
    private const int PartialCcPolicyCodeId = 5;
    private const int ElectricBillPolicyCodeId = 6;
    private const int PartialElectricBillPolicyCodeId = 7;

    private const decimal GeneralTaxAmount = 21_900m;
    private const decimal ComponentTaxAmount = 3_650m; // x4 components = 14,600; total NETTAX = 36,500

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static int SeedCommon(ApplicationDbContext context, int propertyId = 1)
    {
        var property = new PropertyEntity { Id = propertyId, WardId = 1, PropertyNo = propertyId.ToString(), IsActive = true };
        context.PropertyMast.Add(property);

        context.YearMaster.AddRange(
            new YearMasterEntity { Id = 1, Year = CurrentFyYear - 1, YearCode = $"{CurrentFyYear - 1}-{CurrentFyYear}", IsActive = true },
            new YearMasterEntity { Id = 2, Year = CurrentFyYear, YearCode = $"{CurrentFyYear}-{CurrentFyYear + 1}", IsActive = true });

        context.PropertyCertificateTypeMasters.AddRange(
            new PropertyCertificateTypeMasterEntity { Id = CcTypeId, CertificateTypeName = "Commencement/Completion Certificate", CertificateTypeCode = "CC", IsTaxable = true, IsActive = true },
            new PropertyCertificateTypeMasterEntity { Id = OcTypeId, CertificateTypeName = "Occupancy Certificate", CertificateTypeCode = "OC", IsTaxable = true, IsActive = true },
            new PropertyCertificateTypeMasterEntity { Id = ElectricBillTypeId, CertificateTypeName = "Electric Bill", CertificateTypeCode = "ELECTRIC_BILL", IsTaxable = true, IsActive = true });

        context.PolicyCodeMaster.AddRange(
            new PolicyCodeMasterEntity { Id = NetTaxPolicyCodeId, PolicyCode = "NETTAX", IsActive = true },
            new PolicyCodeMasterEntity { Id = OcPolicyCodeId, PolicyCode = "OC", IsActive = true },
            new PolicyCodeMasterEntity { Id = PartialOcPolicyCodeId, PolicyCode = "PARTIAL_OC", IsActive = true },
            new PolicyCodeMasterEntity { Id = CcPolicyCodeId, PolicyCode = "CC", IsActive = true },
            new PolicyCodeMasterEntity { Id = PartialCcPolicyCodeId, PolicyCode = "PARTIAL_CC", IsActive = true },
            new PolicyCodeMasterEntity { Id = ElectricBillPolicyCodeId, PolicyCode = "ELECTRIC_BILL", IsActive = true },
            new PolicyCodeMasterEntity { Id = PartialElectricBillPolicyCodeId, PolicyCode = "PARTIAL_ELECTRIC_BILL", IsActive = true });

        var generalTax = new TaxMasterEntity { Id = 1, TaxName = "GeneralTax", TaxCode = "GEN", DisplayOrder = 1, IsActive = true };
        var waterTax = new TaxMasterEntity { Id = 2, TaxName = "WaterTax", TaxCode = "WAT", DisplayOrder = 2, IsActive = true };
        var treeTax = new TaxMasterEntity { Id = 3, TaxName = "TreeTax", TaxCode = "TRE", DisplayOrder = 3, IsActive = true };
        var educationTax = new TaxMasterEntity { Id = 4, TaxName = "EducationTax", TaxCode = "EDU", DisplayOrder = 4, IsActive = true };
        var employmentTax = new TaxMasterEntity { Id = 5, TaxName = "EmploymentTax", TaxCode = "EMP", DisplayOrder = 5, IsActive = true };
        context.TaxMaster.AddRange(generalTax, waterTax, treeTax, educationTax, employmentTax);

        // NETTAX seeded once, uniformly. DBA-confirmed final schema: PTIS.PolicyTaxDetails holds
        // exactly ONE active row per (PropertyId, PolicyCodeId, TaxId), so this single snapshot IS
        // the property's NETTAX rate for every finance year the engine computes -- current year and
        // every retro/arrears year alike.
        var nextId = (propertyId * 10) + 1;
        context.PolicyTaxDetails.AddRange(
            new PolicyTaxDetailsEntity { Id = nextId++, PropertyId = propertyId, PolicyCodeId = NetTaxPolicyCodeId, TaxId = generalTax.Id, TaxAmount = GeneralTaxAmount, CalculationValue = 500_000m, IsActive = true, MarkedForDeletion = false },
            new PolicyTaxDetailsEntity { Id = nextId++, PropertyId = propertyId, PolicyCodeId = NetTaxPolicyCodeId, TaxId = waterTax.Id, TaxAmount = ComponentTaxAmount, CalculationValue = 500_000m, IsActive = true, MarkedForDeletion = false },
            new PolicyTaxDetailsEntity { Id = nextId++, PropertyId = propertyId, PolicyCodeId = NetTaxPolicyCodeId, TaxId = treeTax.Id, TaxAmount = ComponentTaxAmount, CalculationValue = 500_000m, IsActive = true, MarkedForDeletion = false },
            new PolicyTaxDetailsEntity { Id = nextId++, PropertyId = propertyId, PolicyCodeId = NetTaxPolicyCodeId, TaxId = educationTax.Id, TaxAmount = ComponentTaxAmount, CalculationValue = 500_000m, IsActive = true, MarkedForDeletion = false },
            new PolicyTaxDetailsEntity { Id = nextId, PropertyId = propertyId, PolicyCodeId = NetTaxPolicyCodeId, TaxId = employmentTax.Id, TaxAmount = ComponentTaxAmount, CalculationValue = 500_000m, IsActive = true, MarkedForDeletion = false });

        context.SaveChanges();
        return property.Id;
    }

    private static void AddCertificate(ApplicationDbContext context, int propertyId, int typeId, DateTime? issueDate)
    {
        var cert = PropertyCertificateEntity.Create(
            propertyId: propertyId,
            certificateTypeId: typeId,
            certificateNo: issueDate.HasValue ? $"CERT-{typeId}-{issueDate:yyyyMMdd}" : null,
            issueDate: issueDate,
            propertyDetailsId: null);
        context.PropertyCertificates.Add(cert);
        context.SaveChanges();
    }

    private static Mock<ICertificateTaxGuidelineReaderService> BuildGuidelineReaderMock(
        string invalidCcOcDateOrderAction = "USE_PRIORITY_AND_LOG",
        string noDateRule = "NO_TAX")
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
                InvalidCcOcDateOrderAction: invalidCcOcDateOrderAction,
                CcOnlyAction: "APPLY_FROM_CC_DATE",
                OcOnlyAction: "APPLY_FROM_OC_DATE",
                FinancialYearStartMonth: 4, FinancialYearStartDay: 1,
                CCPeriodMultiplier: 1.5m, OCPeriodMultiplier: 1.0m,
                ElectricBillDateRule: "FROM_FY_START", ElectricBillAddMonths: 0, ElectricBillMultiplier: 1.0m,
                ElectricBillMinimumFinancialYear: 2016, EnableRetrospectiveTax: true,
                NoDateRule: noDateRule, LookbackYears: 6, DefaultRetrospectiveMultiplier: 1.0m,
                MinimumBackdateFinancialYear: 0,
                EnableCurrentYearProration: true, ProrationMethod: "DAILY", CurrentYearProrationStartRule: "EXACT_DATE",
                TaxPersistenceMode: "PROPERTY_AGGREGATED",
                SaveInPolicyTaxDetails: true, SaveInTransMast: true, DoNotUpdateNettax: true,
                RecalculateOnSave: true, RecalculateOnDelete: true, GuidelineChangeApplyMode: "NEXT_CALCULATION",
                CcPartialPolicyCode: "PARTIAL_CC", CcFullPolicyCode: "CC",
                OcPartialPolicyCode: "PARTIAL_OC", OcFullPolicyCode: "OC",
                ElectricBillPartialPolicyCode: "PARTIAL_ELECTRIC_BILL", ElectricBillFullPolicyCode: "ELECTRIC_BILL",
                CertificateTaxScopeMode: "PROPERTY_WISE", AllowFloorWiseCertificateMetadata: false, EnableCcToOcSplit: true,
                ElectricBillCertificateCodes: "ELECTRIC_BILL", RetrospectiveCurrentYearCount: 1,
                RetrospectivePendingYearCountMode: "TOTAL_MINUS_CURRENT", FloorPolicyDisplayRule: "BIGGEST_AREA_FLOOR_POLICY"));
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
            policyCodeLookup, financeYearProvider, guidelineReader.Object, unitOfWork,
            NullLogger<OccupationTaxApplicationService>.Instance);
    }

    private static List<(string PolicyCode, int TaxId, decimal? TaxAmount)> GetActivePolicyRows(ApplicationDbContext context, int propertyId)
    {
        var rows = context.PolicyTaxDetails
            .Where(pt => pt.PropertyId == propertyId && pt.IsActive)
            .Join(context.PolicyCodeMaster, pt => pt.PolicyCodeId, pc => pc.Id,
                (pt, pc) => new { pc.PolicyCode, pt.TaxId, pt.TaxAmount })
            .Where(x => x.PolicyCode != "NETTAX")
            .ToList();

        return rows.Select(x => (x.PolicyCode, x.TaxId, x.TaxAmount)).ToList();
    }

    private static List<(int FinanceYearId, int TaxId, decimal TaxAmount)> GetActiveTransMastRows(ApplicationDbContext context, int propertyId)
    {
        var rows = context.TransMast
            .Where(t => t.PropertyId == propertyId && t.IsActive && t.CalculationType == "RV")
            .ToList();

        return rows.Select(t => (t.FinanceYearId, t.TaxId, t.TaxAmount)).ToList();
    }

    // ------------------------------------------------------------------------------------------
    // Case A: CC only, current FY -- expect PARTIAL_CC at 1.5x, Electric Bill never overrides CC.
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task CaseA_CcOnlyCurrentFy_AppliesPartialCcAt1_5x_ElectricBillNeverOverrides()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        var ccDate = new DateTime(CurrentFyYear, 4, 7);
        AddCertificate(context, propertyId, CcTypeId, ccDate);
        AddCertificate(context, propertyId, ElectricBillTypeId, new DateTime(CurrentFyYear, 6, 7));

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        // PolicyTaxDetails holds only ONE current/final row per (PropertyId, TaxId) under the
        // DBA-confirmed schema (no PolicyYear, unique index on PropertyId+PolicyCodeId+TaxId) --
        // no year filter needed or possible; retro years never get a PolicyTaxDetails row at all.
        var rows = GetActivePolicyRows(context, propertyId);
        Assert.Contains(rows, r => r.PolicyCode == "PARTIAL_CC");
        Assert.DoesNotContain(rows, r => r.PolicyCode is "ELECTRIC_BILL" or "PARTIAL_ELECTRIC_BILL");

        // 359 chargeable days (07-Apr..31-Mar inclusive) at 1.5x.
        var chargeableDays = new FinanceYear(CurrentFyYear, 4, 1).ChargeableDaysFrom(ccDate);
        var expectedGeneral = Math.Round(GeneralTaxAmount * chargeableDays / 365m * 1.5m, 0, MidpointRounding.AwayFromZero);
        var generalRow = rows.Single(r => r.PolicyCode == "PARTIAL_CC" && r.TaxId == 1);
        Assert.Equal(expectedGeneral, generalRow.TaxAmount);
    }

    // ------------------------------------------------------------------------------------------
    // Case: CC only, an OLD (already-past) finance year -- expect the FULL CC code, still 1.5x.
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task CaseA2_CcOnlyOldFy_AppliesFullCcAt1_5x()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        var ccDate = new DateTime(CurrentFyYear - 1, 4, 7);
        AddCertificate(context, propertyId, CcTypeId, ccDate);

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        // PolicyTaxDetails now holds only the CURRENT year's row (retro years live exclusively in
        // TaxPendingDetailsRetro/TaxPendingDetails -- see the DBA-confirmed schema comment on
        // OccupationTaxApplicationService.UpsertPolicyTaxDetail). CC has no OC to hand off to, so it
        // still governs the ONGOING current year too -- tagged full "CC" (not "PARTIAL_CC") since
        // the onset year (CurrentFyYear-1) isn't the current year, so the current year's own amount
        // is a FULL, unprorated year at 1.5x, not day-prorated.
        var rows = GetActivePolicyRows(context, propertyId);
        Assert.Contains(rows, r => r.PolicyCode == "CC");
        Assert.DoesNotContain(rows, r => r.PolicyCode == "PARTIAL_CC");

        // CurrentFyYear (2020) is itself a leap finance year (BR7 add-back applies to a FULL year).
        var expectedGeneral = Math.Round(GeneralTaxAmount + GeneralTaxAmount / 365m, 0, MidpointRounding.AwayFromZero) * 1.5m;
        var generalRow = rows.Single(r => r.PolicyCode == "CC" && r.TaxId == 1);
        Assert.Equal(expectedGeneral, generalRow.TaxAmount);
    }

    // ------------------------------------------------------------------------------------------
    // Case B: CC + OC in the SAME finance year -- the new day-split logic under test.
    // CC governs 07-Apr..06-Jun (61 days) at 1.5x; OC governs 07-Jun..31-Mar (304 days) at 1x.
    // Merged into ONE PolicyTaxDetails row (tagged OC/PARTIAL_OC) and ONE TransMast row per tax.
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task CaseB_CcAndOcSameFy_SplitsAtOcDate_MergesIntoOneRowPerTax()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        var ccDate = new DateTime(CurrentFyYear, 4, 7);
        var ocDate = new DateTime(CurrentFyYear, 6, 7);
        AddCertificate(context, propertyId, CcTypeId, ccDate);
        AddCertificate(context, propertyId, OcTypeId, ocDate);

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        var ccDays = (ocDate - ccDate).Days; // 61 -- 07-Apr..06-Jun inclusive
        var fy = new FinanceYear(CurrentFyYear, 4, 1);
        var ocDays = fy.ChargeableDaysFrom(ocDate); // 07-Jun..31-Mar inclusive (note: 6 days from
                                                     // 01-Apr..06-Apr are chargeable to neither CC
                                                     // nor OC, since CC's own date is 07-Apr, not
                                                     // the FY start -- there is no certificate
                                                     // governing that pre-CC gap)
        Assert.Equal(61, ccDays);

        var ccGeneral = Math.Round(GeneralTaxAmount * ccDays / 365m, 0, MidpointRounding.AwayFromZero) * 1.5m;
        var ocGeneral = Math.Round(GeneralTaxAmount * ocDays / 365m, 0, MidpointRounding.AwayFromZero) * 1.0m;
        var expectedGeneral = ccGeneral + ocGeneral;

        var ccComponent = Math.Round(ComponentTaxAmount * ccDays / 365m, 0, MidpointRounding.AwayFromZero) * 1.5m;
        var ocComponent = Math.Round(ComponentTaxAmount * ocDays / 365m, 0, MidpointRounding.AwayFromZero) * 1.0m;
        var expectedComponent = ccComponent + ocComponent;

        // PolicyTaxDetails holds only the current year's row per (PropertyId, TaxId) now -- both CC
        // and OC are current-year here, so `rows` already IS the current year's set.
        var rows = GetActivePolicyRows(context, propertyId);
        var yearRows = rows;

        // Exactly one policy row per tax for the shared year (no separate CC-tagged row) --
        // ONE merged row per tax, tagged PARTIAL_OC per this method's documented simplification.
        Assert.Equal(5, yearRows.Count); // GeneralTax + 4 components
        Assert.All(yearRows, r => Assert.Equal("PARTIAL_OC", r.PolicyCode));

        var generalRow = yearRows.Single(r => r.TaxId == 1);
        Assert.Equal(expectedGeneral, generalRow.TaxAmount);

        var componentRows = yearRows.Where(r => r.TaxId != 1).ToList();
        Assert.Equal(4, componentRows.Count);
        Assert.All(componentRows, r => Assert.Equal(expectedComponent, r.TaxAmount));

        // TransMast: exactly one row per (property, financeYearId, tax) -- no duplicates.
        var transMastRows = GetActiveTransMastRows(context, propertyId).Where(t => t.FinanceYearId == 2).ToList();
        Assert.Equal(5, transMastRows.Count);
        var transGeneral = transMastRows.Single(t => t.TaxId == 1);
        Assert.Equal(expectedGeneral, transGeneral.TaxAmount);
    }

    // ------------------------------------------------------------------------------------------
    // Case C: OC only, no CC -- expect OC at 1x from OC date.
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task CaseC_OcOnlyCurrentFy_AppliesPartialOcAt1x()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        var ocDate = new DateTime(CurrentFyYear, 6, 7);
        AddCertificate(context, propertyId, OcTypeId, ocDate);

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        var rows = GetActivePolicyRows(context, propertyId);
        Assert.Contains(rows, r => r.PolicyCode == "PARTIAL_OC"); // PolicyTaxDetails is current-year-only now, no year filter needed

        var fy = new FinanceYear(CurrentFyYear, 4, 1);
        var ocDays = fy.ChargeableDaysFrom(ocDate);
        var expectedGeneral = Math.Round(GeneralTaxAmount * ocDays / 365m, 0, MidpointRounding.AwayFromZero);
        var generalRow = rows.Single(r => r.PolicyCode == "PARTIAL_OC" && r.TaxId == 1);
        Assert.Equal(expectedGeneral, generalRow.TaxAmount);
    }

    // ------------------------------------------------------------------------------------------
    // Case D: neither CC nor OC -- Electric Bill fallback, from FY start.
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task CaseD_ElectricBillOnly_FallsBackFromFinanceYearStart()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        AddCertificate(context, propertyId, ElectricBillTypeId, new DateTime(CurrentFyYear, 6, 7));

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        var rows = GetActivePolicyRows(context, propertyId);
        Assert.Contains(rows, r => r.PolicyCode is "ELECTRIC_BILL" or "PARTIAL_ELECTRIC_BILL");
    }

    // ------------------------------------------------------------------------------------------
    // Case E: CC + OC + Electric Bill all present -- Electric Bill must be ignored entirely,
    // the CC-then-OC split still applies exactly as in Case B.
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task CaseE_CcAndOcAndElectricBill_ElectricBillIgnored_SplitStillApplies()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        var ccDate = new DateTime(CurrentFyYear, 4, 7);
        var ocDate = new DateTime(CurrentFyYear, 6, 7);
        AddCertificate(context, propertyId, CcTypeId, ccDate);
        AddCertificate(context, propertyId, OcTypeId, ocDate);
        AddCertificate(context, propertyId, ElectricBillTypeId, new DateTime(CurrentFyYear, 7, 7));

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        var rows = GetActivePolicyRows(context, propertyId);
        Assert.DoesNotContain(rows, r => r.PolicyCode is "ELECTRIC_BILL" or "PARTIAL_ELECTRIC_BILL");
        Assert.Contains(rows, r => r.PolicyCode == "PARTIAL_OC"); // PolicyTaxDetails is current-year-only now, no year filter needed
    }

    // ------------------------------------------------------------------------------------------
    // CC + Electric Bill (no OC) -- CC wins outright, Electric Bill ignored.
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task CaseF_CcAndElectricBillNoOc_CcWins()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        AddCertificate(context, propertyId, CcTypeId, new DateTime(CurrentFyYear, 4, 7));
        AddCertificate(context, propertyId, ElectricBillTypeId, new DateTime(CurrentFyYear, 6, 7));

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        var rows = GetActivePolicyRows(context, propertyId);
        Assert.Contains(rows, r => r.PolicyCode == "PARTIAL_CC");
        Assert.DoesNotContain(rows, r => r.PolicyCode is "ELECTRIC_BILL" or "PARTIAL_ELECTRIC_BILL");
    }

    // ------------------------------------------------------------------------------------------
    // OC + Electric Bill (no CC) -- OC wins outright, Electric Bill ignored.
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task CaseG_OcAndElectricBillNoCc_OcWins()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        AddCertificate(context, propertyId, OcTypeId, new DateTime(CurrentFyYear, 6, 7));
        AddCertificate(context, propertyId, ElectricBillTypeId, new DateTime(CurrentFyYear, 7, 7));

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        var rows = GetActivePolicyRows(context, propertyId);
        Assert.Contains(rows, r => r.PolicyCode == "PARTIAL_OC");
        Assert.DoesNotContain(rows, r => r.PolicyCode is "ELECTRIC_BILL" or "PARTIAL_ELECTRIC_BILL");
    }

    // ------------------------------------------------------------------------------------------
    // CC in an old FY, OC in the current FY -- the pre-existing cross-year merge path (unchanged
    // by this fix), confirmed to still tag each year with its own family.
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task CaseAcrossYears_CcOldFy_OcCurrentFy_TagsEachYearWithItsOwnFamily()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        AddCertificate(context, propertyId, CcTypeId, new DateTime(CurrentFyYear - 1, 4, 7));
        AddCertificate(context, propertyId, OcTypeId, new DateTime(CurrentFyYear, 4, 7));

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        // PolicyTaxDetails is current-year-only now: CC's own onset year (CurrentFyYear-1) is a
        // closed retro year on its own and no longer gets a PolicyTaxDetails row at all (its amount
        // lives in TaxPendingDetailsRetro/TaxPendingDetails instead) -- only the current year's OC
        // row remains.
        var rows = GetActivePolicyRows(context, propertyId);
        Assert.DoesNotContain(rows, r => r.PolicyCode is "CC" or "PARTIAL_CC");
        Assert.Contains(rows, r => r.PolicyCode is "OC" or "PARTIAL_OC");

        var retro = context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.IsActive).ToList();
        Assert.Contains(retro, r => r.PendingYearId == 1); // CurrentFyYear-1 is YearMaster.Id=1 per SeedCommon
    }

    // ------------------------------------------------------------------------------------------
    // CC in an old FY, OC mid-way through the current FY (NOT at FY start): CC's carryover must
    // cover the OC onset year's own FY-start..(OC date - 1 day) at CC_PERIOD_MULTIPLIER, merged
    // into the same PARTIAL_OC row as OC's own ocDate..FY-end portion -- regression coverage for a
    // gap where that carryover span previously received no tax at all whenever OC's date wasn't
    // exactly the finance year start (CaseAcrossYears above never caught it because its OC date,
    // 04-07, left only a tiny, unasserted 6-day gap).
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task CaseAcrossYears2_CcOldFy_OcMidYear_CarriesOverIntoOcOnsetYear()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        var ccDate = new DateTime(CurrentFyYear - 1, 4, 7);
        var ocDate = new DateTime(CurrentFyYear, 6, 7);
        AddCertificate(context, propertyId, CcTypeId, ccDate);
        AddCertificate(context, propertyId, OcTypeId, ocDate);

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        var fy = new FinanceYear(CurrentFyYear, 4, 1);
        var ccCarryoverDays = (ocDate - fy.Start).Days; // 67 -- 01-Apr..06-Jun, CC still active carrying over from the prior FY
        Assert.Equal(67, ccCarryoverDays);
        var ocDays = fy.ChargeableDaysFrom(ocDate); // 07-Jun..31-Mar inclusive

        var ccGeneral = Math.Round(GeneralTaxAmount * ccCarryoverDays / 365m, 0, MidpointRounding.AwayFromZero) * 1.5m;
        var ocGeneral = Math.Round(GeneralTaxAmount * ocDays / 365m, 0, MidpointRounding.AwayFromZero) * 1.0m;
        var expectedGeneral = ccGeneral + ocGeneral;

        // PolicyTaxDetails holds only the current year now -- `rows` already IS the current year's set.
        var rows = GetActivePolicyRows(context, propertyId);
        var currentYearRows = rows;

        // Exactly one merged row per tax for the OC onset year -- no separate CC-tagged row for
        // the carryover portion, same one-row-per-tax invariant as the same-FY split (CaseB).
        Assert.Equal(5, currentYearRows.Count);
        Assert.All(currentYearRows, r => Assert.Equal("PARTIAL_OC", r.PolicyCode));

        var generalRow = currentYearRows.Single(r => r.TaxId == 1);
        Assert.Equal(expectedGeneral, generalRow.TaxAmount);

        // The prior FY (CC's own onset year, closed/retro on its own) no longer gets a
        // PolicyTaxDetails row at all -- its amount lives in TaxPendingDetailsRetro instead.
        Assert.DoesNotContain(rows, r => r.PolicyCode is "CC" or "PARTIAL_CC");
        var retro = context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.IsActive).ToList();
        Assert.Contains(retro, r => r.PendingYearId == 1); // CurrentFyYear-1 is YearMaster.Id=1 per SeedCommon
    }

    // ------------------------------------------------------------------------------------------
    // Add -> delete -> re-add with CC only: must not throw a duplicate-key exception and must
    // leave exactly one active PARTIAL_CC row after the second add.
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task CaseH_AddDeleteReAdd_CcOnly_NoDuplicateKeyException()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        var ccDate = new DateTime(CurrentFyYear, 4, 7);
        var cert = PropertyCertificateEntity.Create(propertyId, CcTypeId, "CC-1", ccDate, null);
        context.PropertyCertificates.Add(cert);
        context.SaveChanges();

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);
        Assert.Contains(GetActivePolicyRows(context, propertyId), r => r.PolicyCode == "PARTIAL_CC");

        cert.MarkForDeletion();
        context.SaveChanges();
        await service.ApplyAsync(propertyId, userId: 1);
        Assert.DoesNotContain(GetActivePolicyRows(context, propertyId), r => r.PolicyCode == "PARTIAL_CC");

        var reAdded = PropertyCertificateEntity.Create(propertyId, CcTypeId, "CC-2", ccDate, null);
        context.PropertyCertificates.Add(reAdded);
        context.SaveChanges();

        var exception = await Record.ExceptionAsync(() => service.ApplyAsync(propertyId, userId: 1));
        Assert.Null(exception);

        var finalRows = GetActivePolicyRows(context, propertyId).Where(r => r.PolicyCode == "PARTIAL_CC" && r.TaxId == 1).ToList();
        Assert.Single(finalRows);
    }

    // ------------------------------------------------------------------------------------------
    // Invalid order: OC dated BEFORE CC. Verify each INVALID_CC_OC_DATE_ORDER_ACTION behavior.
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task CaseI_InvalidOcBeforeCc_Reject_MarksComputationInvalid()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        AddCertificate(context, propertyId, CcTypeId, new DateTime(CurrentFyYear, 6, 7));
        AddCertificate(context, propertyId, OcTypeId, new DateTime(CurrentFyYear, 4, 7)); // before CC

        var service = BuildService(context, BuildGuidelineReaderMock(invalidCcOcDateOrderAction: "REJECT"));
        await service.ApplyAsync(propertyId, userId: 1);

        // Rejected computation writes nothing new (only the NETTAX seed rows remain).
        var rows = GetActivePolicyRows(context, propertyId);
        Assert.DoesNotContain(rows, r => r.PolicyCode is "CC" or "PARTIAL_CC" or "OC" or "PARTIAL_OC");
    }

    [Fact]
    public async Task CaseI_InvalidOcBeforeCc_IgnoreInvalidDate_ContinuesWithCc()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        AddCertificate(context, propertyId, CcTypeId, new DateTime(CurrentFyYear, 6, 7));
        AddCertificate(context, propertyId, OcTypeId, new DateTime(CurrentFyYear, 4, 7)); // before CC

        var service = BuildService(context, BuildGuidelineReaderMock(invalidCcOcDateOrderAction: "IGNORE_INVALID_DATE"));
        await service.ApplyAsync(propertyId, userId: 1);

        var rows = GetActivePolicyRows(context, propertyId);
        Assert.Contains(rows, r => r.PolicyCode is "CC" or "PARTIAL_CC");
    }

    [Fact]
    public async Task CaseI_InvalidOcBeforeCc_UsePriorityAndLog_FallsBackToConfiguredPriority()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        AddCertificate(context, propertyId, CcTypeId, new DateTime(CurrentFyYear, 6, 7));
        AddCertificate(context, propertyId, OcTypeId, new DateTime(CurrentFyYear, 4, 7)); // before CC

        var service = BuildService(context, BuildGuidelineReaderMock(invalidCcOcDateOrderAction: "USE_PRIORITY_AND_LOG"));
        await service.ApplyAsync(propertyId, userId: 1);

        // DATE_PRIORITY_1..4 = CC,OC,ELECTRIC_BILL,RETROSPECTIVE -- CC wins the priority walk.
        var rows = GetActivePolicyRows(context, propertyId);
        Assert.Contains(rows, r => r.PolicyCode is "CC" or "PARTIAL_CC");
    }

    // ------------------------------------------------------------------------------------------
    // Case J: STRICT BUSINESS RULE (2026-07-21) -- no CC, no OC, no Electric Bill certificate at
    // all for this property: no certificate-based tax is ever applied and no PolicyTaxDetails/
    // TransMast row is created for it, even though this test's guideline mock has the
    // default-retrospective fallback turned ON (EnableRetrospectiveTax=true,
    // NoDateRule=DEFAULT_RETROSPECTIVE via BuildGuidelineReaderMock) -- proving the strict rule
    // overrides that fallback rather than merely happening to agree with it.
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task CaseJ_NoCertificateAtAll_NoTaxNoRow_StrictRule()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        // No CC/OC/Electric Bill certificate added at all.

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        var rows = GetActivePolicyRows(context, propertyId);
        Assert.Empty(rows); // no CC/OC/ELECTRIC_BILL/PARTIAL_x row of any kind

        var transMastRows = GetActiveTransMastRows(context, propertyId);
        Assert.Empty(transMastRows);
    }

    // ------------------------------------------------------------------------------------------
    // Case K: real production bug -- a property whose component taxes are NOT all equal (the
    // common, real-world case; e.g. WaterTax != TreeTax != EducationTax) must have EACH tax scaled
    // by ITS OWN rate under proration/multiplier, not averaged into one shared per-component
    // figure. Every other test in this file uses equal-valued components (all $3650), which is
    // exactly why this bug went unnoticed until a real property's Tax Details grid was compared
    // against manual math.
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task CaseK_NonUniformComponentTaxRates_EachTaxScaledByItsOwnRate_NotAveraged()
    {
        using var context = CreateContext();
        const int propertyId = 900;

        var property = new PropertyEntity { Id = propertyId, WardId = 1, PropertyNo = propertyId.ToString(), IsActive = true };
        context.PropertyMast.Add(property);

        context.YearMaster.Add(new YearMasterEntity { Id = 900, Year = CurrentFyYear, YearCode = $"{CurrentFyYear}-{CurrentFyYear + 1}", IsActive = true });

        context.PropertyCertificateTypeMasters.AddRange(
            new PropertyCertificateTypeMasterEntity { Id = 900, CertificateTypeName = "Occupancy Certificate", CertificateTypeCode = "OC", IsTaxable = true, IsActive = true });

        context.PolicyCodeMaster.AddRange(
            new PolicyCodeMasterEntity { Id = 900, PolicyCode = "NETTAX", IsActive = true },
            new PolicyCodeMasterEntity { Id = 901, PolicyCode = "OC", IsActive = true },
            new PolicyCodeMasterEntity { Id = 902, PolicyCode = "PARTIAL_OC", IsActive = true });

        var generalTax = new TaxMasterEntity { Id = 900, TaxName = "GeneralTax", TaxCode = "GEN", DisplayOrder = 1, IsActive = true };
        var waterTax = new TaxMasterEntity { Id = 901, TaxName = "WaterTax", TaxCode = "WAT", DisplayOrder = 2, IsActive = true };
        var treeTax = new TaxMasterEntity { Id = 902, TaxName = "TreeTax", TaxCode = "TRE", DisplayOrder = 3, IsActive = true };
        var educationTax = new TaxMasterEntity { Id = 903, TaxName = "EducationTax", TaxCode = "EDU", DisplayOrder = 4, IsActive = true };
        context.TaxMaster.AddRange(generalTax, waterTax, treeTax, educationTax);

        // Deliberately NON-uniform component amounts -- the whole point of this test.
        const decimal generalAmount = 124m;
        const decimal waterAmount = 16m;
        const decimal treeAmount = 4m;
        const decimal educationAmount = 40m;

        context.PolicyTaxDetails.AddRange(
            new PolicyTaxDetailsEntity { Id = 9001, PropertyId = propertyId, PolicyCodeId = 900, TaxId = generalTax.Id, TaxAmount = generalAmount, CalculationValue = 500_000m, IsActive = true, MarkedForDeletion = false },
            new PolicyTaxDetailsEntity { Id = 9002, PropertyId = propertyId, PolicyCodeId = 900, TaxId = waterTax.Id, TaxAmount = waterAmount, CalculationValue = 500_000m, IsActive = true, MarkedForDeletion = false },
            new PolicyTaxDetailsEntity { Id = 9003, PropertyId = propertyId, PolicyCodeId = 900, TaxId = treeTax.Id, TaxAmount = treeAmount, CalculationValue = 500_000m, IsActive = true, MarkedForDeletion = false },
            new PolicyTaxDetailsEntity { Id = 9004, PropertyId = propertyId, PolicyCodeId = 900, TaxId = educationTax.Id, TaxAmount = educationAmount, CalculationValue = 500_000m, IsActive = true, MarkedForDeletion = false });

        context.SaveChanges();

        var ocDate = new DateTime(CurrentFyYear, 6, 7);
        var cert = PropertyCertificateEntity.Create(propertyId, 900, "OC-1", ocDate, null);
        context.PropertyCertificates.Add(cert);
        context.SaveChanges();

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        var fy = new FinanceYear(CurrentFyYear, 4, 1);
        var ocDays = fy.ChargeableDaysFrom(ocDate);
        var factor = (decimal)ocDays / 365m;

        var rows = GetActivePolicyRows(context, propertyId);
        var yearRows = rows; // PolicyTaxDetails holds only the current year now
        Assert.Equal(4, yearRows.Count);
        Assert.All(yearRows, r => Assert.Equal("PARTIAL_OC", r.PolicyCode));

        // Each tax scaled by ITS OWN rate -- NOT averaged into one shared figure across water/tree/education.
        // Mirrors the actual two-step algorithm (engine rounds its own even-split ComponentTax
        // first, then AddYearRecords derives a ratio relative to that rounded value and reapplies
        // it per tax) rather than a naive single Math.Round(taxAmount * factor), since the ratio
        // step's own rounding is an accepted, minor artifact (same tradeoff as RescaleYear).
        var perComponent = (waterAmount + treeAmount + educationAmount) / 3m; // componentTotal / ComponentCount
        var rawComponentTax = Math.Round(perComponent * factor, 0, MidpointRounding.AwayFromZero);
        var componentRatio = rawComponentTax / perComponent;

        var expectedGeneral = Math.Round(generalAmount * factor, 0, MidpointRounding.AwayFromZero);
        var expectedWater = Math.Round(waterAmount * componentRatio, 0, MidpointRounding.AwayFromZero);
        var expectedTree = Math.Round(treeAmount * componentRatio, 0, MidpointRounding.AwayFromZero);
        var expectedEducation = Math.Round(educationAmount * componentRatio, 0, MidpointRounding.AwayFromZero);

        Assert.Equal(expectedGeneral, yearRows.Single(r => r.TaxId == generalTax.Id).TaxAmount);
        Assert.Equal(expectedWater, yearRows.Single(r => r.TaxId == waterTax.Id).TaxAmount);
        Assert.Equal(expectedTree, yearRows.Single(r => r.TaxId == treeTax.Id).TaxAmount);
        Assert.Equal(expectedEducation, yearRows.Single(r => r.TaxId == educationTax.Id).TaxAmount);

        // The old (buggy) behavior would have made water/tree/education all equal to each other
        // (an averaged per-component figure) -- explicitly assert they are NOT all equal, since
        // water=16 and tree=4 and education=40 are genuinely different rates.
        var waterRow = yearRows.Single(r => r.TaxId == waterTax.Id).TaxAmount;
        var treeRow = yearRows.Single(r => r.TaxId == treeTax.Id).TaxAmount;
        var educationRow = yearRows.Single(r => r.TaxId == educationTax.Id).TaxAmount;
        Assert.NotEqual(waterRow, treeRow);
        Assert.NotEqual(treeRow, educationRow);
    }

    // ------------------------------------------------------------------------------------------
    // Case L: a certificate backdated into a RETRO (closed, past) finance year must ALSO create
    // matching pending-tax rows in BOTH PTIS.TaxPendingDetailsRetro and PTIS.TaxPendingDetails --
    // previously neither table was ever written to by this engine at all. The LIVE current
    // finance year is ordinary, in-progress demand (already reflected in PolicyTaxDetails/
    // TransMast), not "pending" in the arrears sense, so it must NOT get a row in either table.
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task CaseL_OcOldFy_CreatesTaxPendingDetailsAndRetro_ForRetroYearOnly_NotCurrentYear()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        var ocDate = new DateTime(CurrentFyYear - 1, 4, 7);
        AddCertificate(context, propertyId, OcTypeId, ocDate);

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        var retroPending = context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.IsActive).ToList();
        var pending = context.TaxPendingDetails.Where(r => r.PropertyId == propertyId && r.IsActive).ToList();

        // 5 tax rows each (GeneralTax + 4 components), all for the retro year (YearMaster.Id=1),
        // never for the current year (YearMaster.Id=2).
        Assert.Equal(5, retroPending.Count);
        Assert.Equal(5, pending.Count);
        Assert.All(retroPending, r => Assert.Equal(1, r.PendingYearId));
        Assert.All(pending, r => Assert.Equal(1, r.PendingYearId));

        // PolicyTaxDetails no longer holds a row for the retro year at all (only the current year,
        // full/unprorated since the onset year isn't the current one) -- the retro year's amounts
        // are verifiable only via TaxPendingDetailsRetro/TaxPendingDetails now.
        var policyRows = GetActivePolicyRows(context, propertyId);
        Assert.Equal(5, policyRows.Count); // current year only
        Assert.All(policyRows, r => Assert.Equal("OC", r.PolicyCode)); // full, not PARTIAL_OC -- onset predates the current year
    }

    // ------------------------------------------------------------------------------------------
    // Case M: a TaxPendingDetails row marked PendingFixed=true (set after a property combine, per
    // its own doc comment, to prevent double-counting) must be left completely untouched by
    // recalculation -- neither its amount updated nor deactivated by cleanup -- even though the
    // SAME slot's TaxPendingDetailsRetro row (no such flag) is freely updated.
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task CaseM_ExistingPendingFixedRow_NeverUpdatedOrDeactivated()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        var ocDate = new DateTime(CurrentFyYear - 1, 4, 7);
        AddCertificate(context, propertyId, OcTypeId, ocDate);

        // Pre-existing, manually-fixed pending row for GeneralTax (TaxId=1) in the retro year
        // (YearMaster.Id=1), with a deliberately different amount than recalculation would
        // produce -- simulating a post-combine manual reconciliation.
        const decimal fixedAmount = 999_999m;
        context.TaxPendingDetails.Add(new TaxPendingDetailsEntity
        {
            PropertyId = propertyId,
            PendingYearId = 1,
            TaxId = 1,
            PendingAmount = fixedAmount,
            PendingFixed = true,
            IsActive = true,
            MarkedForDeletion = false
        });
        context.SaveChanges();

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        var fixedRow = context.TaxPendingDetails.Single(r => r.PropertyId == propertyId && r.PendingYearId == 1 && r.TaxId == 1);
        Assert.Equal(fixedAmount, fixedRow.PendingAmount); // untouched, not overwritten
        Assert.True(fixedRow.IsActive); // untouched, not deactivated
        Assert.True(fixedRow.PendingFixed);

        // TaxPendingDetailsRetro has no such flag -- its own row for the same slot IS updated normally.
        var retroRow = context.TaxPendingDetailsRetro.Single(r => r.PropertyId == propertyId && r.PendingYearId == 1 && r.TaxId == 1 && r.IsActive);
        Assert.NotEqual(fixedAmount, retroRow.PendingAmount);
    }

    // ------------------------------------------------------------------------------------------
    // Case N: "open plot" -- a property with ZERO PropertyDetailsEntity (floor/unit) rows at all.
    // Every certificate in this file already has PropertyDetailsId=null (see AddCertificate), and
    // no test here ever seeds a PropertyDetailsEntity, so this scenario is already exercised
    // implicitly by every other case -- this test exists purely to make that coverage explicit and
    // documented under its real name, proving the property-wide (non-floor-wise) resolution path
    // is reachable and correct even when IPropertyRepository.GetPropertyDetailsByPropertyIdAsync
    // returns an empty list (the floor-wise branch is architecturally unreachable in that case;
    // see OccupationTaxApplicationService's floorWiseCertificates.Count == 0 short-circuit).
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task CaseN_OpenPlot_ZeroPropertyDetailsRows_PropertyWideOcCertificateStillResolves()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        Assert.Empty(context.PropertyDetails.Where(pd => pd.PropertyId == propertyId)); // genuinely no floors/units
        var ocDate = new DateTime(CurrentFyYear, 4, 7);
        AddCertificate(context, propertyId, OcTypeId, ocDate);

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        var rows = GetActivePolicyRows(context, propertyId);
        Assert.Contains(rows, r => r.PolicyCode == "PARTIAL_OC"); // PolicyTaxDetails is current-year-only now, no year filter needed
    }

    // ------------------------------------------------------------------------------------------
    // Case O: deleting an OC certificate that produced retro pending rows must deactivate BOTH
    // TaxPendingDetailsRetro and TaxPendingDetails, not just PolicyTaxDetails/TransMast (which
    // already had this coverage via CaseH) -- these two newer tables had no delete-cleanup test.
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task CaseO_DeleteOcOldFy_DeactivatesTaxPendingDetailsAndRetro()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        var ocDate = new DateTime(CurrentFyYear - 1, 4, 7);
        var cert = PropertyCertificateEntity.Create(propertyId, OcTypeId, "OC-1", ocDate, null);
        context.PropertyCertificates.Add(cert);
        context.SaveChanges();

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        Assert.NotEmpty(context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.IsActive));
        Assert.NotEmpty(context.TaxPendingDetails.Where(r => r.PropertyId == propertyId && r.IsActive));

        cert.MarkForDeletion();
        context.SaveChanges();
        await service.ApplyAsync(propertyId, userId: 1);

        Assert.Empty(context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.IsActive));
        Assert.Empty(context.TaxPendingDetails.Where(r => r.PropertyId == propertyId && r.IsActive));
        // Rows still exist, just soft-deleted -- not physically removed.
        Assert.NotEmpty(context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.MarkedForDeletion));
        Assert.NotEmpty(context.TaxPendingDetails.Where(r => r.PropertyId == propertyId && r.MarkedForDeletion));
    }

    // ------------------------------------------------------------------------------------------
    // Case P: add -> delete -> re-add the same OC (old FY) certificate must REACTIVATE the existing
    // TaxPendingDetailsRetro/TaxPendingDetails rows in place, never create a duplicate active row
    // for the same (PropertyId, PendingYearId, TaxId) slot -- mirrors CaseH's TransMast/
    // PolicyTaxDetails coverage, extended to the two pending tables specifically.
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task CaseP_AddDeleteReAdd_OcOldFy_NoDuplicateTaxPendingRows()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        var ocDate = new DateTime(CurrentFyYear - 1, 4, 7);
        var cert = PropertyCertificateEntity.Create(propertyId, OcTypeId, "OC-1", ocDate, null);
        context.PropertyCertificates.Add(cert);
        context.SaveChanges();

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        cert.MarkForDeletion();
        context.SaveChanges();
        await service.ApplyAsync(propertyId, userId: 1);

        var reAdded = PropertyCertificateEntity.Create(propertyId, OcTypeId, "OC-2", ocDate, null);
        context.PropertyCertificates.Add(reAdded);
        context.SaveChanges();

        var exception = await Record.ExceptionAsync(() => service.ApplyAsync(propertyId, userId: 1));
        Assert.Null(exception);

        var retroForTax1 = context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.IsActive && r.TaxId == 1).ToList();
        var pendingForTax1 = context.TaxPendingDetails.Where(r => r.PropertyId == propertyId && r.IsActive && r.TaxId == 1).ToList();
        Assert.Single(retroForTax1);
        Assert.Single(pendingForTax1);
    }

    // ------------------------------------------------------------------------------------------
    // Case Q: a single standalone delete of a certificate that had produced rows in ALL FOUR
    // downstream tables (current-year PolicyTaxDetails/TransMast, retro-year
    // TaxPendingDetailsRetro/TaxPendingDetails) must clean up ALL FOUR at once -- combines what
    // CaseH (PolicyTaxDetails) and CaseO (TaxPendingDetailsRetro/TaxPendingDetails) each covered
    // separately, plus TransMast explicitly, in one place (2026-07-23 audit requirement).
    // ------------------------------------------------------------------------------------------
    [Fact]
    public async Task CaseQ_StandaloneDeleteOcOldFy_CleansAllFourTaxTables()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        var ocDate = new DateTime(CurrentFyYear - 1, 4, 7);
        var cert = PropertyCertificateEntity.Create(propertyId, OcTypeId, "OC-1", ocDate, null);
        context.PropertyCertificates.Add(cert);
        context.SaveChanges();

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        // Confirm all four tables actually got populated before deleting -- otherwise "zero active
        // rows after delete" would trivially (and wrongly) pass even if cleanup never ran.
        Assert.NotEmpty(GetActivePolicyRows(context, propertyId));
        Assert.NotEmpty(GetActiveTransMastRows(context, propertyId));
        Assert.NotEmpty(context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.IsActive));
        Assert.NotEmpty(context.TaxPendingDetails.Where(r => r.PropertyId == propertyId && r.IsActive));

        cert.MarkForDeletion();
        context.SaveChanges();
        await service.ApplyAsync(propertyId, userId: 1);

        Assert.Empty(GetActivePolicyRows(context, propertyId));
        Assert.Empty(GetActiveTransMastRows(context, propertyId));
        Assert.Empty(context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.IsActive));
        Assert.Empty(context.TaxPendingDetails.Where(r => r.PropertyId == propertyId && r.IsActive));
    }
}
