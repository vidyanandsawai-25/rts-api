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
/// 2026-07-24 regression: PTIS.TaxPendingDetails must be a SUMMARY table -- exactly one row per
/// (PropertyId, TaxId) -- never a row-per-pending-year copy of PTIS.TaxPendingDetailsRetro. A UI/SSMS
/// check against real PropertyId 549451 found 22 active TaxPendingDetails rows (11 tax heads x 2
/// pending years) instead of the expected 11 summary rows, because SaveTaxesAsync's AddYearRecords
/// previously called UpsertTaxPending once PER retro year (identical shape to UpsertTaxPendingRetro),
/// instead of accumulating each TaxId's total across every retro year and writing ONE row after.
///
/// This scenario reproduces PropertyId 549451's real shape: OC dated 08-Aug-2024, current FY
/// 2026-27, 11 tax heads. The ~1.5466x ratio between every tax head's two pending-year amounts in
/// the real DB data (e.g. 2,126,841 / 1,375,163) matches EXACTLY a 236-day prorated FY2024 (the OC
/// onset year) against a full, unprorated FY2025 -- confirming this is the same real scenario, not a
/// coincidence -- so the annual rates below are set to reproduce that same shape (not hand-picked
/// arbitrary numbers), and the test asserts the INVARIANT (summary amount = sum of that TaxId's own
/// retro rows) rather than hardcoding rounding-sensitive rupee figures.
/// </summary>
public class TaxPendingDetailsSummaryTests
{
    private const int CurrentFyYear = 2026; // FY2026-27; OC FY2024 and FY2025 are both retro
    private const int OcTypeId = 1;

    private const int NetTaxPolicyCodeId = 1;
    private const int OcPolicyCodeId = 2;
    private const int PartialOcPolicyCodeId = 3;
    private const int CcPolicyCodeId = 4;
    private const int PartialCcPolicyCodeId = 5;
    private const int ElectricBillPolicyCodeId = 6;
    private const int PartialElectricBillPolicyCodeId = 7;

    // 11 tax heads, annual (full-year) rate per head -- TaxId 1 is General Tax (the largest, matching
    // the real data's shape), TaxId 2-11 are components with genuinely different, non-uniform rates.
    private static readonly Dictionary<int, decimal> AnnualRateByTaxId = new()
    {
        [1] = 2_126_841m,
        [2] = 586_714m,
        [3] = 146_679m,
        [4] = 48_893m,
        [5] = 488_929m,
        [6] = 440_036m,
        [7] = 48_893m,
        [8] = 635_607m,
        [9] = 1_075_643m,
        [10] = 855_625m,
        [11] = 195_571m,
    };

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static int Seed(ApplicationDbContext context, int propertyId = 1)
    {
        context.PropertyMast.Add(new PropertyEntity { Id = propertyId, WardId = 1, PropertyNo = propertyId.ToString(), IsActive = true });

        foreach (var year in new[] { 2024, 2025, CurrentFyYear })
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
        context.TaxMaster.Add(new TaxMasterEntity { Id = 1, TaxName = "General Tax", TaxCode = "GEN", DisplayOrder = 1, TaxCategoryId = 1, IsActive = true });
        for (var taxId = 2; taxId <= 11; taxId++)
        {
            context.TaxMaster.Add(new TaxMasterEntity { Id = taxId, TaxName = $"Component{taxId}", TaxCode = $"C{taxId}", DisplayOrder = taxId, TaxCategoryId = 1, IsActive = true });
        }

        // Exactly ONE active NETTAX row per (PropertyId, TaxId) -- the DBA-confirmed schema has no
        // PolicyYear column, so this single current rate is used uniformly for every finance year
        // (current and every retro/arrears year alike).
        var nextId = 1000;
        foreach (var (taxId, rate) in AnnualRateByTaxId)
        {
            context.PolicyTaxDetails.Add(new PolicyTaxDetailsEntity
            {
                Id = nextId++,
                PropertyId = propertyId,
                PolicyCodeId = NetTaxPolicyCodeId,
                TaxId = taxId,
                TaxAmount = rate,
                CalculationValue = 500_000m,
                IsActive = true,
                MarkedForDeletion = false
            });
        }

        context.SaveChanges();
        return propertyId;
    }

    private static Mock<ICertificateTaxGuidelineReaderService> BuildGuidelineReaderMock()
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

    [Fact]
    public async Task OcDate08Aug2024_11TaxHeads_TwoPendingYears_TaxPendingDetailsIsOneSummaryRowPerTaxId()
    {
        using var context = CreateContext();
        var propertyId = Seed(context);

        var ocDate = new DateTime(2024, 8, 8);
        var cert = PropertyCertificateEntity.Create(propertyId, OcTypeId, "OC-549451", ocDate, propertyDetailsId: null);
        context.PropertyCertificates.Add(cert);
        context.SaveChanges();

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        // ---- TaxPendingDetailsRetro: year-wise breakup, 11 tax heads x 2 pending years (2024, 2025) ----
        var retroRows = context.TaxPendingDetailsRetro
            .Where(r => r.PropertyId == propertyId && r.IsActive && !r.MarkedForDeletion)
            .ToList();
        Assert.Equal(22, retroRows.Count);
        Assert.DoesNotContain(retroRows, r => r.PendingYearId == CurrentFyYear); // rule: no current-FY row in Retro

        var retroSumByTaxId = retroRows
            .GroupBy(r => r.TaxId)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.PendingAmount ?? 0m));
        Assert.Equal(11, retroSumByTaxId.Count);

        // ---- THE BUG: TaxPendingDetails must be exactly 11 SUMMARY rows (one per TaxId), NOT 22 ----
        var pendingRows = context.TaxPendingDetails
            .Where(p => p.PropertyId == propertyId && p.IsActive && !p.MarkedForDeletion)
            .ToList();

        Assert.Equal(11, pendingRows.Count); // NOT 22 -- this is what production showed before the fix
        Assert.Equal(pendingRows.Count, pendingRows.Select(p => p.TaxId).Distinct().Count()); // no duplicate TaxId

        foreach (var row in pendingRows)
        {
            Assert.Equal(retroSumByTaxId[row.TaxId], row.PendingAmount); // summary == sum of that TaxId's own retro rows
        }

        Assert.DoesNotContain(pendingRows, p => p.PendingYearId == CurrentFyYear); // rule: no current-FY row

        // Final business rule: every summary row's PendingYearId is SPECIFICALLY the previous finance
        // year (2025, i.e. CurrentFyYear-1) -- not just "some retro year" -- so it must never be 2024
        // (the OLDER of the two retro years) either.
        const int previousFyYear = CurrentFyYear - 1; // 2025
        Assert.All(pendingRows, p => Assert.Equal(previousFyYear, p.PendingYearId));
        Assert.DoesNotContain(pendingRows, p => p.PendingYearId == 2024);

        // Sanity cross-check against the real production figures for the two largest tax heads
        // (General Tax and the largest component) -- confirms this isn't just internally
        // self-consistent but actually the same order of magnitude/shape as PropertyId 549451.
        Assert.Equal(3_502_004m, pendingRows.Single(p => p.TaxId == 1).PendingAmount); // General Tax
        Assert.Equal(1_771_128m, pendingRows.Single(p => p.TaxId == 9).PendingAmount); // largest component

        // TaxPendingDetails must NOT include the current-FY TransMast amount anywhere in its total.
        var transMast = context.TransMast
            .Where(t => t.PropertyId == propertyId && t.IsActive && !t.MarkedForDeletion && t.CalculationType == "RV")
            .ToList();
        var currentFyTotal = transMast.Sum(t => t.TaxAmount);
        var pendingGrandTotal = pendingRows.Sum(p => p.PendingAmount ?? 0m);
        Assert.NotEqual(currentFyTotal, pendingGrandTotal);
        Assert.DoesNotContain(pendingRows.Select(p => p.PendingAmount), amt => transMast.Any(t => t.TaxAmount == amt));

        // ---- TransMast: current FY (2026) only ----
        Assert.Equal(11, transMast.Count); // one row per tax head, current FY only
        Assert.DoesNotContain(transMast, t => t.FinanceYearId != CurrentFyYear);

        // ---- PolicyTaxDetails: current/final active policy rows only -- no retro-year duplicates,
        // no PolicyYear dependency, exactly one active row per (PropertyId, TaxId) under the
        // DBA-confirmed unique index (PropertyId, PolicyCodeId, TaxId) -- proves the certificate
        // family (OC) never produced a second active row per TaxId for 2024/2025's retro amounts. ----
        var policyRows = context.PolicyTaxDetails
            .Where(p => p.PropertyId == propertyId && p.IsActive && !p.MarkedForDeletion && p.PolicyCodeId != NetTaxPolicyCodeId)
            .ToList();
        Assert.Equal(11, policyRows.Count); // one row per TaxId, current year only
        Assert.Equal(11, policyRows.Select(p => p.TaxId).Distinct().Count()); // no duplicate TaxId
        Assert.All(policyRows, p => Assert.Equal(OcPolicyCodeId, p.PolicyCodeId)); // full "OC", not PARTIAL_OC (onset predates current FY)

        // ---- Tax Details grid: current FY OC row only, no retro/pending bleed-through ----
        var propertyRepo = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == CurrentFyYear));
        var grid = await propertyRepo.GetTaxDetailsAsync(propertyId);
        Assert.NotNull(grid);
        var ocPolicy = grid!.Policies.Single(p => p.PolicyCode == "OC");
        Assert.Equal(11, ocPolicy.TaxAmounts.Count); // one amount per tax head, current year only
        Assert.Equal(currentFyTotal, ocPolicy.TaxTotal);
    }

    [Fact]
    public async Task AddDeleteReAdd_ElevenTaxHeads_TaxPendingDetailsStaysElevenSummaryRows_NoDuplicatesOnReactivation()
    {
        using var context = CreateContext();
        var propertyId = Seed(context);
        var ocDate = new DateTime(2024, 8, 8);

        var service = BuildService(context, BuildGuidelineReaderMock());

        var cert = PropertyCertificateEntity.Create(propertyId, OcTypeId, "OC-1", ocDate, propertyDetailsId: null);
        context.PropertyCertificates.Add(cert);
        context.SaveChanges();
        await service.ApplyAsync(propertyId, userId: 1);

        Assert.Equal(11, context.TaxPendingDetails.Count(p => p.PropertyId == propertyId && p.IsActive));

        cert.MarkForDeletion();
        context.SaveChanges();
        await service.ApplyAsync(propertyId, userId: 1);

        Assert.Empty(context.TaxPendingDetails.Where(p => p.PropertyId == propertyId && p.IsActive));

        var cert2 = PropertyCertificateEntity.Create(propertyId, OcTypeId, "OC-2", ocDate, propertyDetailsId: null);
        context.PropertyCertificates.Add(cert2);
        context.SaveChanges();
        var exception = await Record.ExceptionAsync(() => service.ApplyAsync(propertyId, userId: 1));
        Assert.Null(exception);

        var afterReAdd = context.TaxPendingDetails.Where(p => p.PropertyId == propertyId && p.IsActive).ToList();
        Assert.Equal(11, afterReAdd.Count); // reactivated in place -- still 11, never 22
        Assert.Equal(11, afterReAdd.Select(p => p.TaxId).Distinct().Count());

        // Exactly one row per (PropertyId, TaxId) across the WHOLE history (active + soft-deleted),
        // per TaxId -- proves reactivation-in-place, not a second summary row alongside the first.
        foreach (var taxId in AnnualRateByTaxId.Keys)
        {
            var allRowsForTax = context.TaxPendingDetails.Where(p => p.PropertyId == propertyId && p.TaxId == taxId).ToList();
            Assert.Single(allRowsForTax);
        }
    }
}
