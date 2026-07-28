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
/// 2026-07-24 compliance audit: closes four specific gaps identified while independently
/// re-verifying every CC/OC/Electric-Bill/Retrospective scenario against manually-derived expected
/// amounts (not just re-running existing tests). Each test embeds its own independent expected-value
/// derivation (day-count via the real <see cref="FinanceYear"/> helper, leap add-back, multiplier)
/// and asserts against the REAL persisted PolicyTaxDetails/TransMast/TaxPendingDetails/
/// TaxPendingDetailsRetro rows -- never just the in-memory OccupationTaxResult -- using the real EF
/// InMemory ApplicationDbContext, matching the existing rigor established by
/// CcThenOcSameYearSplitTests.cs and OcTwoYearsBackWorkedExampleTests.cs.
///
/// Gaps closed:
/// 2) No test used an Electric Bill date literally in January/February (only the exact 31-Mar
///    boundary) to prove the "maps to the previous finance year" rule.
/// 3) No real-DB test wired actual PropertyDetailsEntity + PropertyCertificateEntity.PropertyDetailsId
///    FK rows through the real ApplicationDbContext/PropertyRepository for floor-wise scoping
///    (existing floor-wise coverage was mocked-repository only).
/// 4) No single test exercised add-&gt;delete-&gt;re-add with all four tables populated together.
/// </summary>
public class TaxAuditVerificationTests
{
    private const int OcTypeId = 1;
    private const int CcTypeId = 2;
    private const int ElectricBillTypeId = 3;

    private const int NetTaxPolicyCodeId = 1;
    private const int OcPolicyCodeId = 2;
    private const int PartialOcPolicyCodeId = 3;
    private const int CcPolicyCodeId = 4;
    private const int PartialCcPolicyCodeId = 5;
    private const int ElectricBillPolicyCodeId = 6;
    private const int PartialElectricBillPolicyCodeId = 7;

    private const int GeneralTaxId = 1;

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    /// <summary>
    /// Seeds property/masters/policy codes/tax head, then a single NETTAX row -- the DBA-confirmed
    /// schema has no PolicyYear column, so exactly ONE active row can exist per (PropertyId, TaxId)
    /// and it is used uniformly for every finance year in <paramref name="ratesByYear"/>'s keys
    /// (every caller of this helper passes the SAME rate for every year it lists).
    /// </summary>
    private static int Seed(ApplicationDbContext context, IReadOnlyDictionary<int, decimal> ratesByYear, int propertyId = 1)
    {
        context.PropertyMast.Add(new PropertyEntity { Id = propertyId, WardId = 1, PropertyNo = propertyId.ToString(), IsActive = true });

        foreach (var year in ratesByYear.Keys)
        {
            context.YearMaster.Add(new YearMasterEntity { Id = year, Year = year, YearCode = $"{year}-{(year + 1) % 100:D2}", IsActive = true });
        }

        context.PropertyCertificateTypeMasters.AddRange(
            new PropertyCertificateTypeMasterEntity { Id = OcTypeId, CertificateTypeName = "Occupancy Certificate", CertificateTypeCode = "OC", IsTaxable = true, IsActive = true },
            new PropertyCertificateTypeMasterEntity { Id = CcTypeId, CertificateTypeName = "Commencement/Completion Certificate", CertificateTypeCode = "CC", IsTaxable = true, IsActive = true },
            new PropertyCertificateTypeMasterEntity { Id = ElectricBillTypeId, CertificateTypeName = "Electric Bill", CertificateTypeCode = "ELECTRIC_BILL", IsTaxable = true, IsActive = true });

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

        context.PolicyTaxDetails.Add(new PolicyTaxDetailsEntity
        {
            Id = 1000,
            PropertyId = propertyId,
            PolicyCodeId = NetTaxPolicyCodeId,
            TaxId = GeneralTaxId,
            TaxAmount = ratesByYear.Values.First(),
            CalculationValue = 50_000m,
            IsActive = true,
            MarkedForDeletion = false
        });

        context.SaveChanges();
        return propertyId;
    }

    private static PropertyCertificateEntity AddCertificate(
        ApplicationDbContext context, int propertyId, int typeId, DateTime issueDate, int? propertyDetailsId = null, string? certNo = null)
    {
        var cert = PropertyCertificateEntity.Create(
            propertyId: propertyId,
            certificateTypeId: typeId,
            certificateNo: certNo ?? $"CERT-{typeId}-{issueDate:yyyyMMdd}-{propertyDetailsId}",
            issueDate: issueDate,
            propertyDetailsId: propertyDetailsId);
        context.PropertyCertificates.Add(cert);
        context.SaveChanges();
        return cert;
    }

    private static Mock<ICertificateTaxGuidelineReaderService> BuildGuidelineReaderMock(bool allowFloorWise = false)
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
                NoDateRule: "DEFAULT_RETROSPECTIVE", LookbackYears: 6, DefaultRetrospectiveMultiplier: 1.0m,
                MinimumBackdateFinancialYear: 0,
                EnableCurrentYearProration: true, ProrationMethod: "DAILY", CurrentYearProrationStartRule: "EXACT_DATE",
                TaxPersistenceMode: "PROPERTY_AGGREGATED",
                SaveInPolicyTaxDetails: true, SaveInTransMast: true, DoNotUpdateNettax: true,
                RecalculateOnSave: true, RecalculateOnDelete: true, GuidelineChangeApplyMode: "NEXT_CALCULATION",
                CcPartialPolicyCode: "PARTIAL_CC", CcFullPolicyCode: "CC",
                OcPartialPolicyCode: "PARTIAL_OC", OcFullPolicyCode: "OC",
                ElectricBillPartialPolicyCode: "PARTIAL_ELECTRIC_BILL", ElectricBillFullPolicyCode: "ELECTRIC_BILL",
                CertificateTaxScopeMode: "PROPERTY_WISE", AllowFloorWiseCertificateMetadata: allowFloorWise, EnableCcToOcSplit: true,
                ElectricBillCertificateCodes: "ELECTRIC_BILL", RetrospectiveCurrentYearCount: 1,
                RetrospectivePendingYearCountMode: "TOTAL_MINUS_CURRENT", FloorPolicyDisplayRule: "BIGGEST_AREA_FLOOR_POLICY"));
        return mock;
    }

    private static OccupationTaxApplicationService BuildService(ApplicationDbContext context, int currentFyYear, Mock<ICertificateTaxGuidelineReaderService> guidelineReader)
    {
        var propertyRepo = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == currentFyYear));
        var certRepo = new Repository<PropertyCertificateEntity, int>(context);
        var policyTaxRepo = new Repository<PolicyTaxDetailsEntity, int>(context);
        var transMastRepo = new Repository<TransMastEntity, int>(context);
        var yearRepo = new Repository<YearMasterEntity, int>(context);
        var taxPendingRepo = new Repository<TaxPendingDetailsEntity, int>(context);
        var taxPendingRetroRepo = new Repository<TaxPendingDetailsRetroEntity, int>(context);
        var policyCodeRepo = new Repository<PolicyCodeMasterEntity, int>(context);
        var policyCodeLookup = new PolicyCodeLookupService(policyCodeRepo);
        var unitOfWork = new UnitOfWork(context);
        var financeYearProvider = Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == currentFyYear);
        var engine = new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance);

        return new OccupationTaxApplicationService(
            engine, propertyRepo, certRepo, policyTaxRepo, transMastRepo, yearRepo,
            taxPendingRepo, taxPendingRetroRepo,
            policyCodeLookup, financeYearProvider, guidelineReader.Object, unitOfWork,
            NullLogger<OccupationTaxApplicationService>.Instance);
    }

    private static List<(int FinanceYearId, decimal TaxAmount)> GetActiveTransMast(ApplicationDbContext context, int propertyId) =>
        context.TransMast.Where(t => t.PropertyId == propertyId && t.IsActive && t.CalculationType == "RV")
            .ToList()
            .Select(t => (t.FinanceYearId, t.TaxAmount)).ToList();

    private static List<(int PendingYearId, decimal Amount)> GetActiveRetro(ApplicationDbContext context, int propertyId) =>
        context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.IsActive)
            .ToList()
            .Select(r => (r.PendingYearId, r.PendingAmount ?? 0m)).ToList();

    private static List<(int PendingYearId, decimal Amount)> GetActivePending(ApplicationDbContext context, int propertyId) =>
        context.TaxPendingDetails.Where(r => r.PropertyId == propertyId && r.IsActive)
            .ToList()
            .Select(r => (r.PendingYearId, r.PendingAmount ?? 0m)).ToList();

    // ============================================================================================
    // GAP 2: Electric Bill date literally in January -> must map to the PREVIOUS finance year
    // (only the exact 31-Mar boundary was previously tested).
    // ============================================================================================
    [Fact]
    public async Task ElectricBillOnly_15Jan2026_MapsToPreviousFy2025_NotCurrentFy2026()
    {
        const int currentFy = 2026;
        var rates = new Dictionary<int, decimal> { [2025] = 500m, [2026] = 500m };

        using var context = CreateContext();
        var propertyId = Seed(context, rates);
        // 15-Jan-2026 falls within FY2025 (01-Apr-2025..31-Mar-2026), NOT FY2026.
        AddCertificate(context, propertyId, ElectricBillTypeId, new DateTime(2026, 1, 15));

        var service = BuildService(context, currentFy, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        var retro = GetActiveRetro(context, propertyId);
        Assert.Single(retro);
        Assert.Equal(2025, retro[0].PendingYearId);
        Assert.Equal(500m, retro[0].Amount);
        Assert.DoesNotContain(retro, r => r.PendingYearId == currentFy);

        var pending = GetActivePending(context, propertyId);
        Assert.Single(pending);
        Assert.Equal(2025, pending[0].PendingYearId);

        var transMast = GetActiveTransMast(context, propertyId);
        Assert.Single(transMast);
        Assert.Equal((currentFy, 500m), transMast[0]);
    }

    // ============================================================================================
    // GAP 3: floor-wise certificate scoping, proven at the real-DB level with actual
    // PropertyDetailsEntity rows (not mocked repositories).
    // ============================================================================================
    [Fact]
    public async Task FloorWise_RealDb_OcOverridesOnlyThatFloor_PropertyWideCcAppliesToOtherFloor()
    {
        const int currentFy = 2026;
        const decimal annualTax = 730m; // splits evenly across 2 floors -> 365 each
        var rates = new Dictionary<int, decimal> { [currentFy] = annualTax };

        using var context = CreateContext();
        var propertyId = Seed(context, rates);

        const int floor1Id = 101;
        const int floor2Id = 102;
        context.PropertyDetails.AddRange(
            new PropertyDetailsEntity { Id = floor1Id, PropertyId = propertyId, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false, BuiltupAreaSqMeter = 100d },
            new PropertyDetailsEntity { Id = floor2Id, PropertyId = propertyId, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false, BuiltupAreaSqMeter = 50d });
        context.SaveChanges();

        // Property-wide OC, dated at FY start -> full year, no proration. Deliberately the SAME
        // certificate TYPE (OC) as Floor1's own floor-wise certificate below: per the "override is
        // PER CERTIFICATE TYPE, not all-or-nothing per floor" design, a floor-wise certificate is
        // concatenated ahead of the property-wide list and ExtractDates keeps the FIRST match per
        // type (`ocDate ??= ...`) -- so Floor1's own OC date wins outright and the property-wide OC
        // is simply redundant/ignored for Floor1. Using the SAME type here (rather than CC) avoids
        // triggering the separate CC-then-OC merge machinery, keeping this test isolated to proving
        // override-vs-fallback scoping specifically (that interaction is covered by scenario 6's own
        // dedicated tests in CcThenOcSameYearSplitTests.cs).
        var propertyWideOcDate = new DateTime(currentFy, 4, 1);
        AddCertificate(context, propertyId, OcTypeId, propertyWideOcDate, propertyDetailsId: null);

        // Floor-wise OC for Floor1 only, mid-year -> prorated, and OVERRIDES the property-wide OC
        // date for Floor1 specifically.
        var floor1OcDate = new DateTime(currentFy, 7, 1);
        AddCertificate(context, propertyId, OcTypeId, floor1OcDate, propertyDetailsId: floor1Id);

        // Floor2 gets NO floor-wise certificate of its own -> must fall back to the property-wide OC.

        var service = BuildService(context, currentFy, BuildGuidelineReaderMock(allowFloorWise: true));
        await service.ApplyAsync(propertyId, userId: 1);

        // ---- Independent per-floor manual calculation ----
        const decimal perFloorGeneralTax = annualTax / 2; // 365
        var fy = new FinanceYear(currentFy, 4, 1);

        // Floor1: its OWN OC date (01-Jul), mid-year proration, OC_PERIOD_MULTIPLIER = 1.0 -- NOT
        // the property-wide OC's FY-start date.
        var floor1Days = fy.ChargeableDaysFrom(floor1OcDate);
        var floor1Expected = Math.Round(perFloorGeneralTax * floor1Days / 365m, 0, MidpointRounding.AwayFromZero);

        // Floor2: property-wide OC fallback (FY start) -> full year, unprorated.
        var floor2Expected = perFloorGeneralTax;

        var expectedTotal = floor1Expected + floor2Expected;

        var transMast = GetActiveTransMast(context, propertyId);
        Assert.Single(transMast);
        Assert.Equal((currentFy, expectedTotal), transMast[0]);

        // The aggregated total must differ from what EITHER floor's date would produce if wrongly
        // applied to the WHOLE property -- proves each floor genuinely used its own applicable date
        // against its own 50% share, not one date applied to the whole property.
        var wholePropertyFloor1DateOnly = Math.Round(annualTax * floor1Days / 365m, 0, MidpointRounding.AwayFromZero);
        Assert.NotEqual(wholePropertyFloor1DateOnly, transMast[0].TaxAmount); // would be wrong if Floor2 also got Floor1's date
        Assert.NotEqual(annualTax, transMast[0].TaxAmount); // would be wrong if both floors got a full, unprorated year
    }

    // ============================================================================================
    // GAP 5: floor-wise MIXED FAMILY representative labeling -- reported UI bug ("CC lavli ki
    // electric bill lagat ahe"): a formal certificate (CC) governing one floor must not be masked
    // by a BIGGER floor that only has the property-wide Electric Bill fallback. ResolveRepresentative
    // (BIGGEST_AREA_FLOOR_POLICY) previously picked purely by floor area, so a bigger Electric-Bill
    // floor could out-rank a smaller CC floor in the single property-level summary row -- even
    // though CC always outranks Electric Bill at the single-floor DATE_PRIORITY level.
    // ============================================================================================
    [Fact]
    public async Task FloorWise_MixedFamily_CcOnSmallerFloor_RepresentativePrefersCcOverBiggerElectricBillFloor()
    {
        const int currentFy = 2026;
        const decimal annualTax = 730m;
        var rates = new Dictionary<int, decimal> { [currentFy] = annualTax };

        using var context = CreateContext();
        var propertyId = Seed(context, rates);

        const int smallFloorId = 201; // has the CC certificate
        const int bigFloorId = 202;   // no floor-wise cert -> falls back to property-wide Electric Bill
        context.PropertyDetails.AddRange(
            new PropertyDetailsEntity { Id = smallFloorId, PropertyId = propertyId, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false, BuiltupAreaSqMeter = 30d },
            new PropertyDetailsEntity { Id = bigFloorId, PropertyId = propertyId, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false, BuiltupAreaSqMeter = 100d });
        context.SaveChanges();

        // Property-wide Electric Bill -- the fallback for whichever floor has no floor-wise
        // certificate of its own (the bigger floor, here).
        var electricBillDate = new DateTime(currentFy, 4, 1);
        AddCertificate(context, propertyId, ElectricBillTypeId, electricBillDate, propertyDetailsId: null);

        // Floor-wise CC for the SMALLER floor only.
        var ccDate = new DateTime(currentFy, 4, 1);
        AddCertificate(context, propertyId, CcTypeId, ccDate, propertyDetailsId: smallFloorId);

        var service = BuildService(context, currentFy, BuildGuidelineReaderMock(allowFloorWise: true));
        await service.ApplyAsync(propertyId, userId: 1);

        // The property genuinely has a formal certificate (CC) governing part of it -- the Tax
        // Details grid's single representative summary row must reflect that, not the bigger
        // floor's Electric-Bill fallback, purely because that floor has more area.
        var codes = context.PolicyTaxDetails
            .Where(pt => pt.PropertyId == propertyId && pt.IsActive)
            .Join(context.PolicyCodeMaster, pt => pt.PolicyCodeId, pc => pc.Id, (pt, pc) => pc.PolicyCode)
            .Where(code => code != "NETTAX")
            .Distinct()
            .ToList();

        Assert.Contains(codes, c => c is "CC" or "PARTIAL_CC");
    }

    // ============================================================================================
    // GAP 4: add -> delete -> re-add, with all FOUR tables populated and verified together
    // (existing coverage split this across separate tests, none combined all four in one flow).
    // ============================================================================================
    [Fact]
    public async Task AddDeleteReAdd_OcOldFy_AllFourTablesCleanAndReactivateTogether()
    {
        const int currentFy = 2026;
        const decimal annualTax = 356m;
        var rates = new Dictionary<int, decimal> { [2025] = annualTax, [currentFy] = annualTax };

        using var context = CreateContext();
        var propertyId = Seed(context, rates);
        var ocDate = new DateTime(2025, 4, 1); // FY2025 -- one retro year back from currentFy 2026

        var service = BuildService(context, currentFy, BuildGuidelineReaderMock());

        // ---- Add #1 ----
        var cert1 = AddCertificate(context, propertyId, OcTypeId, ocDate, certNo: "OC-1");
        await service.ApplyAsync(propertyId, userId: 1);

        Assert.Single(GetActiveRetro(context, propertyId));
        Assert.Single(GetActivePending(context, propertyId));
        Assert.Single(GetActiveTransMast(context, propertyId));
        Assert.NotEmpty(context.PolicyTaxDetails.Where(p => p.PropertyId == propertyId && p.IsActive && p.PolicyCodeId != NetTaxPolicyCodeId));

        Assert.Equal(annualTax, GetActiveRetro(context, propertyId)[0].Amount);
        Assert.Equal(annualTax, GetActiveTransMast(context, propertyId)[0].TaxAmount);

        // ---- Delete ----
        cert1.MarkForDeletion();
        context.SaveChanges();
        await service.ApplyAsync(propertyId, userId: 1);

        Assert.Empty(GetActiveRetro(context, propertyId));
        Assert.Empty(GetActivePending(context, propertyId));
        Assert.Empty(GetActiveTransMast(context, propertyId));
        Assert.Empty(context.PolicyTaxDetails.Where(p => p.PropertyId == propertyId && p.IsActive && p.PolicyCodeId != NetTaxPolicyCodeId));

        // ---- Re-add (same date, new certificate row) ----
        AddCertificate(context, propertyId, OcTypeId, ocDate, certNo: "OC-2");
        var exception = await Record.ExceptionAsync(() => service.ApplyAsync(propertyId, userId: 1));
        Assert.Null(exception); // no duplicate-key violation

        var retroAfterReAdd = GetActiveRetro(context, propertyId);
        var pendingAfterReAdd = GetActivePending(context, propertyId);
        var transMastAfterReAdd = GetActiveTransMast(context, propertyId);

        Assert.Single(retroAfterReAdd); // reactivated in place, not duplicated
        Assert.Single(pendingAfterReAdd);
        Assert.Single(transMastAfterReAdd);
        Assert.Equal(annualTax, retroAfterReAdd[0].Amount);
        Assert.Equal(annualTax, transMastAfterReAdd[0].TaxAmount);

        // Exactly one row per (PropertyId, PendingYearId/FinanceYearId, TaxId) slot across the WHOLE
        // table history (active + soft-deleted) -- proves reactivation-in-place, not a second row
        // alongside the first soft-deleted one.
        var allRetroRows = context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.PendingYearId == 2025 && r.TaxId == GeneralTaxId).ToList();
        Assert.Single(allRetroRows);
        var allTransMastRows = context.TransMast.Where(t => t.PropertyId == propertyId && t.FinanceYearId == currentFy && t.TaxId == GeneralTaxId && t.CalculationType == "RV").ToList();
        Assert.Single(allTransMastRows);
    }
}
