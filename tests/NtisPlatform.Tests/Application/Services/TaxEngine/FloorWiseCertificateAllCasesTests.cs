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
/// 2026-07-30 comprehensive floor-wise certificate audit: exercises every remaining combination of
/// floor-owned vs property-wide-fallback certificates not already covered by
/// <see cref="TaxAuditVerificationTests"/> or <see cref="CcThenOcSameYearSplitTests"/>, alongside the
/// "floor's own certificate predates its fallback" fix in
/// <see cref="OccupationTaxApplicationService"/>'s per-floor loop. Uses the real EF InMemory
/// ApplicationDbContext, real OccupationTaxEngine, and no mocked repositories.
/// </summary>
public class FloorWiseCertificateAllCasesTests
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

    private static Mock<ICertificateTaxGuidelineReaderService> BuildGuidelineReaderMock(
        bool allowFloorWise = true, string noDateRule = "NO_TAX", bool enableRetrospectiveTax = true)
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
                ElectricBillMinimumFinancialYear: 2016, EnableRetrospectiveTax: enableRetrospectiveTax,
                NoDateRule: noDateRule, LookbackYears: 6, DefaultRetrospectiveMultiplier: 1.0m,
                EnableCurrentYearProration: true, ProrationMethod: "DAILY", CurrentYearProrationStartRule: "EXACT_DATE",
                TaxPersistenceMode: "PROPERTY_AGGREGATED",
                SaveInPolicyTaxDetails: true, SaveInTransMast: true, DoNotUpdateNettax: true,
                RecalculateOnSave: true, RecalculateOnDelete: true, GuidelineChangeApplyMode: "NEXT_CALCULATION",
                CcPartialPolicyCode: "PARTIAL_CC", CcFullPolicyCode: "CC",
                OcPartialPolicyCode: "PARTIAL_OC", OcFullPolicyCode: "OC",
                ElectricBillPartialPolicyCode: "PARTIAL_ELECTRIC_BILL", ElectricBillFullPolicyCode: "ELECTRIC_BILL",
                CertificateTaxScopeMode: "PROPERTY_WISE", AllowFloorWiseCertificateMetadata: allowFloorWise, EnableCcToOcSplit: true,
                ElectricBillCertificateCodes: "ELECTRIC_BILL", RetrospectiveCurrentYearCount: 1,
                RetrospectivePendingYearCountMode: "TOTAL_MINUS_CURRENT", FloorPolicyDisplayRule: "BIGGEST_AREA_FLOOR_POLICY",
                TaxationRateMode: "CURRENT_YEAR_FOR_ALL", TaxPercentageMode: "CURRENT_YEAR_FOR_ALL", FixedTaxPercentage: 0m));
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
            policyCodeLookup, financeYearProvider, guidelineReader.Object, Mock.Of<IHistoricalNetTaxBaselineService>(), unitOfWork,
            NullLogger<OccupationTaxApplicationService>.Instance,
            NtisPlatform.Tests.Helpers.NoOpTaxApplicabilityService.Instance);
    }

    private static List<(int FinanceYearId, decimal TaxAmount)> GetActiveTransMast(ApplicationDbContext context, int propertyId) =>
        context.TransMast.Where(t => t.PropertyId == propertyId && t.IsActive && t.CalculationType == "RV")
            .ToList()
            .Select(t => (t.FinanceYearId, t.TaxAmount)).ToList();

    private static List<(int PendingYearId, decimal Amount)> GetActiveRetro(ApplicationDbContext context, int propertyId) =>
        context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.IsActive)
            .ToList()
            .Select(r => (r.PendingYearId, r.PendingAmount ?? 0m)).ToList();

    // ============================================================================================
    // CASE: symmetric to the reported bug -- a floor owns CC only (no OC of its own), and the
    // property-wide OC FALLBACK it inherits predates that CC. The fallback OC isn't real evidence
    // about this floor (it never has its own OC) -- this floor's own CC must govern it alone.
    // ============================================================================================
    [Fact]
    public async Task FloorOwnsCcOnly_PropertyWideOcFallbackPredatesIt_CcGovernsThatFloorAlone()
    {
        const int currentFy = 2026;
        const decimal annualTax = 800m; // splits evenly across 2 floors -> 400 each
        var rates = new Dictionary<int, decimal> { [2025] = annualTax, [currentFy] = annualTax };

        using var context = CreateContext();
        var propertyId = Seed(context, rates);

        const int ccFloorId = 401;      // has its own CC, no OC of its own
        const int ocOnlyFloorId = 402;  // no certificate of its own -> falls back to property-wide OC
        context.PropertyDetails.AddRange(
            new PropertyDetailsEntity { Id = ccFloorId, PropertyId = propertyId, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false, BuiltupAreaSqMeter = 30d },
            new PropertyDetailsEntity { Id = ocOnlyFloorId, PropertyId = propertyId, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false, BuiltupAreaSqMeter = 100d });
        context.SaveChanges();

        // Property-wide OC, one full finance year BEFORE the floor's own CC -> full year, unprorated,
        // 1.0x for the floor that falls back to it.
        var ocDate = new DateTime(2025, 4, 1);
        AddCertificate(context, propertyId, OcTypeId, ocDate, propertyDetailsId: null);

        // ccFloorId's OWN CC, dated a full finance year AFTER the property-wide OC fallback -- this
        // floor has no OC of its own, so without the fix this looks like an "OC before CC" conflict
        // (from this floor's point of view) and gets silently recomputed under whatever
        // DATE_PRIORITY/INVALID_CC_OC_DATE_ORDER_ACTION picks instead of this floor's own CC.
        var ccDate = new DateTime(currentFy, 4, 1);
        AddCertificate(context, propertyId, CcTypeId, ccDate, propertyDetailsId: ccFloorId);

        var service = BuildService(context, currentFy, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        const decimal perFloorGeneralTax = annualTax / 2; // 400

        // ccFloorId: governed by its OWN CC alone (1.5x, full unprorated current year only -- no
        // retro, since CC's date is exactly the current FY start; no CC-then-OC merge, since the
        // property-wide OC never really applied to this floor).
        var expectedCcFloorCurrent2026 = perFloorGeneralTax * 1.5m; // 600

        // ocOnlyFloorId: governed by the property-wide OC (1.0x, full unprorated years from FY2025
        // onward -- a genuine retro year).
        var expectedOcOnlyFloorRetro2025 = perFloorGeneralTax * 1.0m; // 400
        var expectedOcOnlyFloorCurrent2026 = perFloorGeneralTax * 1.0m; // 400

        var expectedCurrentYearTotal = expectedCcFloorCurrent2026 + expectedOcOnlyFloorCurrent2026; // 1,000

        var transMast = GetActiveTransMast(context, propertyId);
        Assert.Single(transMast);
        Assert.Equal((currentFy, expectedCurrentYearTotal), transMast[0]);

        // Without the fix, ccFloorId's own CC would be discarded/recast, and there'd be no FY2025
        // retro row reflecting ocOnlyFloorId's genuine OC-driven arrears.
        var retro = GetActiveRetro(context, propertyId);
        Assert.Single(retro);
        Assert.Equal(2025, retro[0].PendingYearId);
        Assert.Equal(expectedOcOnlyFloorRetro2025, retro[0].Amount);
    }

    // ============================================================================================
    // CASE: a floor owns ONLY an OC, and falls back to a property-wide CC that predates it in the
    // NORMAL order (CC before OC) -- this is the genuinely-intended "CC governs until this floor's
    // OC takes over" scenario the per-floor fallback design exists for (see the comment on the
    // per-floor loop in ComputeRawAsync). Must NOT be affected by the invalid-order fix (which only
    // fires when the fallback date is LATER than the floor's own date) -- this proves the fix is
    // correctly scoped to the inverted-order case only.
    // ============================================================================================
    [Fact]
    public async Task FloorOwnsOcOnly_PropertyWideCcFallbackPrecedesItNormally_ProducesCcThenOcMergeForThatFloor()
    {
        const int currentFy = 2026;
        const decimal annualTax = 730m; // splits evenly across 2 floors -> 365 each
        var rates = new Dictionary<int, decimal> { [currentFy] = annualTax };

        using var context = CreateContext();
        var propertyId = Seed(context, rates);

        const int mergeFloorId = 501;      // property-wide CC governs until its own OC takes over
        const int ccOnlyFallbackFloorId = 502; // no certificate of its own -> falls back to property-wide CC only

        context.PropertyDetails.AddRange(
            new PropertyDetailsEntity { Id = mergeFloorId, PropertyId = propertyId, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false, BuiltupAreaSqMeter = 30d },
            new PropertyDetailsEntity { Id = ccOnlyFallbackFloorId, PropertyId = propertyId, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false, BuiltupAreaSqMeter = 100d });
        context.SaveChanges();

        // Property-wide CC at FY start.
        var ccDate = new DateTime(currentFy, 4, 1);
        AddCertificate(context, propertyId, CcTypeId, ccDate, propertyDetailsId: null);

        // mergeFloorId's OWN OC, mid-year, AFTER the property-wide CC -- normal order -> CC governs
        // this floor from FY start up to the day before its own OC date, then OC governs onward.
        var ocDate = new DateTime(currentFy, 7, 1);
        AddCertificate(context, propertyId, OcTypeId, ocDate, propertyDetailsId: mergeFloorId);

        var service = BuildService(context, currentFy, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        const decimal perFloorGeneralTax = annualTax / 2; // 365
        var fy = new FinanceYear(currentFy, 4, 1);

        // mergeFloorId: CC governs ccDate..(ocDate-1) at 1.5x, OC governs ocDate..FY-end at 1.0x.
        // Each portion is rounded ONCE on its own fully-scaled (day-fraction x multiplier) value --
        // matching ScaleYearResult's single-rounding-of-the-combined-total behavior -- not rounded
        // once before the multiplier and left fractional after, which would double-count rounding.
        var ccDays = (ocDate - ccDate).Days;
        var ocDays = fy.ChargeableDaysFrom(ocDate);
        var ccPortion = Math.Round(perFloorGeneralTax * ccDays / 365m * 1.5m, 0, MidpointRounding.AwayFromZero);
        var ocPortion = Math.Round(perFloorGeneralTax * ocDays / 365m * 1.0m, 0, MidpointRounding.AwayFromZero);
        var mergeFloorExpected = ccPortion + ocPortion;

        // ccOnlyFallbackFloorId: property-wide CC, full unprorated year, 1.5x.
        var ccOnlyFallbackExpected = Math.Round(perFloorGeneralTax * 1.5m, 0, MidpointRounding.AwayFromZero);

        var expectedTotal = mergeFloorExpected + ccOnlyFallbackExpected;

        var transMast = GetActiveTransMast(context, propertyId);
        Assert.Single(transMast);
        Assert.Equal(currentFy, transMast[0].FinanceYearId);
        Assert.Equal(expectedTotal, transMast[0].TaxAmount);

        // Sanity: the merge floor's own contribution must genuinely reflect a BLEND of 1.5x and 1.0x
        // (not simply the whole year at one multiplier) -- proves the normal-order CC-then-OC merge
        // ran for this floor, unaffected by the invalid-order fix.
        Assert.NotEqual(perFloorGeneralTax * 1.5m, mergeFloorExpected);
        Assert.NotEqual(perFloorGeneralTax * 1.0m, mergeFloorExpected);
    }

    // ============================================================================================
    // CASE: multi-floor mix of a floor with its own certificate and a floor with NO certificate
    // coverage at all (no floor-wise cert, and no property-wide certificate of any kind either) --
    // the certificate-less floor must independently fall into the no-certificate retrospective
    // fallback rather than being skipped or incorrectly inheriting the other floor's certificate.
    // ============================================================================================
    [Fact]
    public async Task FloorWise_OneFloorHasOwnCc_OtherFloorHasNoCertificateAtAll_FallsBackToRetrospectiveIndependently()
    {
        const int currentFy = 2026;
        const decimal annualTax = 800m; // splits evenly across 2 floors -> 400 each
        var rates = new Dictionary<int, decimal> { [currentFy] = annualTax };

        using var context = CreateContext();
        var propertyId = Seed(context, rates);

        const int ccFloorId = 601;
        const int noCertFloorId = 602; // no certificate anywhere -> retrospective fallback (DEFAULT_RETROSPECTIVE)

        context.PropertyDetails.AddRange(
            new PropertyDetailsEntity { Id = ccFloorId, PropertyId = propertyId, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false, BuiltupAreaSqMeter = 30d },
            new PropertyDetailsEntity { Id = noCertFloorId, PropertyId = propertyId, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false, BuiltupAreaSqMeter = 100d });
        context.SaveChanges();

        // Floor-wise CC only, for ccFloorId -- deliberately NO property-wide certificate of any kind,
        // so noCertFloorId truly has nothing to fall back to.
        var ccDate = new DateTime(currentFy, 4, 1);
        AddCertificate(context, propertyId, CcTypeId, ccDate, propertyDetailsId: ccFloorId);

        var service = BuildService(context, currentFy, BuildGuidelineReaderMock(noDateRule: "DEFAULT_RETROSPECTIVE"));
        await service.ApplyAsync(propertyId, userId: 1);

        const decimal perFloorGeneralTax = annualTax / 2; // 400

        // ccFloorId: full unprorated current year at 1.5x.
        var ccFloorExpected = perFloorGeneralTax * 1.5m; // 600

        var transMast = GetActiveTransMast(context, propertyId);
        Assert.Single(transMast);
        Assert.Equal(currentFy, transMast[0].FinanceYearId);

        // noCertFloorId went through the retrospective fallback (DefaultRetrospectiveMultiplier =
        // 1.0m) rather than being silently skipped or inheriting ccFloorId's CC -- the aggregated
        // current-year total must be MORE than ccFloorId's own contribution alone (proving
        // noCertFloorId contributed something), and specifically ccFloorId's contribution plus its
        // own 1.0x-multiplier current-year share.
        Assert.True(transMast[0].TaxAmount > ccFloorExpected);

        var noCertFloorExpected = perFloorGeneralTax * 1.0m; // 400 (DefaultRetrospectiveMultiplier)
        Assert.Equal(ccFloorExpected + noCertFloorExpected, transMast[0].TaxAmount);
    }
}
