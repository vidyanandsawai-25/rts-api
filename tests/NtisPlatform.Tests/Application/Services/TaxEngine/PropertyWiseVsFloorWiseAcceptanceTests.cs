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
/// 2026-07-30 acceptance test suite, directly implementing the QA-authored "Golden Rule" spec for
/// property-wise vs floor-wise certificate handling: property-wise mode must use the property's full
/// NETTAX baseline, floor-wise mode must use each floor's own share, and neither must bleed into the
/// other. Covers: Section 1 (property-wise integrity, no regression), Section 2 (pure floor-wise
/// isolation, each floor independent), and Section 3 (master/child conflicts -- a floor's own
/// certificate must override an inherited property-wide one, but a certificate-less floor must still
/// inherit the property-wide one rather than going untaxed, and Electric Bill must never outrank an
/// inherited CC/OC regardless of whose date is more specific).
///
/// Base NETTAX = 15,000 throughout (3 floors x 5,000 each for floor-wise tests), matching the QA
/// spec's own worked numbers, so expected values can be taken directly from the spec instead of
/// re-derived. Uses the real EF InMemory ApplicationDbContext, real OccupationTaxEngine, and no
/// mocked repositories.
/// </summary>
public class PropertyWiseVsFloorWiseAcceptanceTests
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
    private const int CurrentFy = 2026;

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static int Seed(ApplicationDbContext context, decimal annualNetTax, int firstYear, int propertyId = 1)
    {
        context.PropertyMast.Add(new PropertyEntity { Id = propertyId, WardId = 1, PropertyNo = propertyId.ToString(), IsActive = true });

        for (var year = firstYear; year <= CurrentFy; year++)
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
            TaxAmount = annualNetTax,
            CalculationValue = 100_000m,
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

    private static Mock<ICertificateTaxGuidelineReaderService> BuildGuidelineReaderMock(bool allowFloorWise)
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
                // "Thane" default per the QA spec: a certificate-less scope (property or floor, with
                // no fallback either) bills NOTHING historical -- NO_TAX, not a retrospective formula.
                NoDateRule: "NO_TAX", LookbackYears: 6, DefaultRetrospectiveMultiplier: 1.0m,
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

    private static OccupationTaxApplicationService BuildService(ApplicationDbContext context, Mock<ICertificateTaxGuidelineReaderService> guidelineReader)
    {
        var propertyRepo = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == CurrentFy));
        var certRepo = new Repository<PropertyCertificateEntity, int>(context);
        var policyTaxRepo = new Repository<PolicyTaxDetailsEntity, int>(context);
        var transMastRepo = new Repository<TransMastEntity, int>(context);
        var yearRepo = new Repository<YearMasterEntity, int>(context);
        var taxPendingRepo = new Repository<TaxPendingDetailsEntity, int>(context);
        var taxPendingRetroRepo = new Repository<TaxPendingDetailsRetroEntity, int>(context);
        var policyCodeRepo = new Repository<PolicyCodeMasterEntity, int>(context);
        var policyCodeLookup = new PolicyCodeLookupService(policyCodeRepo);
        var unitOfWork = new UnitOfWork(context);
        var financeYearProvider = Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == CurrentFy);
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

    private static Dictionary<int, decimal> GetActiveRetroByYear(ApplicationDbContext context, int propertyId) =>
        context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.IsActive)
            .ToList()
            .GroupBy(r => r.PendingYearId)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.PendingAmount ?? 0m));

    // ============================================================================================
    // SECTION 1: Property-wise integrity (no floor data at all -- must not regress).
    // ============================================================================================

    [Fact]
    public async Task Test1_1_PropertyWise_StandardSplit_UsesFullPropertyNetTaxAcrossManyYears()
    {
        const decimal netTax = 15_000m;
        using var context = CreateContext();
        var propertyId = Seed(context, netTax, firstYear: 2019);

        AddCertificate(context, propertyId, CcTypeId, new DateTime(2019, 4, 1), propertyDetailsId: null);
        AddCertificate(context, propertyId, OcTypeId, new DateTime(2022, 4, 1), propertyDetailsId: null);

        var service = BuildService(context, BuildGuidelineReaderMock(allowFloorWise: false));
        await service.ApplyAsync(propertyId, userId: 1);

        var retroByYear = GetActiveRetroByYear(context, propertyId);
        Assert.Equal(22_500m, retroByYear[2019]); // 15,000 x 1.5 (CC)
        Assert.Equal(22_562m, retroByYear[2020]); // FY2020 is a leap finance year (BR7 add-back) -- (15,000 + 15,000/365) x 1.5, rounded
        Assert.Equal(22_500m, retroByYear[2021]);
        Assert.Equal(15_000m, retroByYear[2022]); // 15,000 x 1.0 (OC) -- switches here
        Assert.Equal(15_000m, retroByYear[2023]);
        Assert.Equal(15_041m, retroByYear[2024]); // FY2024 is a leap finance year too -- OC's 1.0x leaves the engine's own leap-adjusted rounding unscaled
        Assert.Equal(15_000m, retroByYear[2025]);

        var transMast = GetActiveTransMast(context, propertyId);
        Assert.Single(transMast);
        Assert.Equal((CurrentFy, 15_000m), transMast[0]); // 26-27 current year, still OC governing
    }

    [Fact]
    public async Task Test1_2_PropertyWise_AllDatesBlank_NoTaxRule_ZeroArrearsOnlyBaseNetTax()
    {
        const decimal netTax = 15_000m;
        using var context = CreateContext();
        var propertyId = Seed(context, netTax, firstYear: CurrentFy);

        // No certificates of any kind -- property-wide or floor-wise.

        var service = BuildService(context, BuildGuidelineReaderMock(allowFloorWise: false));
        await service.ApplyAsync(propertyId, userId: 1);

        Assert.Empty(GetActiveRetroByYear(context, propertyId));
        Assert.Empty(GetActiveTransMast(context, propertyId));
        Assert.Empty(context.PolicyTaxDetails.Where(p => p.PropertyId == propertyId && p.IsActive && p.PolicyCodeId != NetTaxPolicyCodeId));

        // Base NETTAX itself is untouched (owned by the RV pipeline, not this service).
        var netTaxRow = context.PolicyTaxDetails.Single(p => p.PropertyId == propertyId && p.PolicyCodeId == NetTaxPolicyCodeId);
        Assert.Equal(netTax, netTaxRow.TaxAmount);
    }

    // ============================================================================================
    // SECTION 2: Pure floor-wise isolation -- 3 floors x 5,000 each, no property-wide certificate.
    // ============================================================================================

    [Fact]
    public async Task Test2_1_FloorWise_StaggeredCcDates_EachFloorIndependentBlankFloorZero()
    {
        const decimal netTax = 15_000m;
        using var context = CreateContext();
        var propertyId = Seed(context, netTax, firstYear: 2022);

        const int floor1 = 701, floor2 = 702, floor3 = 703;
        context.PropertyDetails.AddRange(
            new PropertyDetailsEntity { Id = floor1, PropertyId = propertyId, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false },
            new PropertyDetailsEntity { Id = floor2, PropertyId = propertyId, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false },
            new PropertyDetailsEntity { Id = floor3, PropertyId = propertyId, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false });
        context.SaveChanges();

        AddCertificate(context, propertyId, CcTypeId, new DateTime(2022, 4, 1), propertyDetailsId: floor1);
        AddCertificate(context, propertyId, CcTypeId, new DateTime(2024, 4, 1), propertyDetailsId: floor2);
        // floor3: no certificate at all -- NO_TAX, contributes zero.

        var service = BuildService(context, BuildGuidelineReaderMock(allowFloorWise: true));
        await service.ApplyAsync(propertyId, userId: 1);

        var retroByYear = GetActiveRetroByYear(context, propertyId);
        Assert.Equal(7_500m, retroByYear[2022]);  // floor1 only (5,000 x 1.5)
        Assert.Equal(7_500m, retroByYear[2023]);  // floor1 only
        // FY2024 is a leap finance year (BR7 add-back): each floor's (5,000 + 5,000/365) x 1.5,
        // rounded, is 7,521 -- slightly above the QA spec's own worked non-leap-adjusted 7,500.
        Assert.Equal(15_042m, retroByYear[2024]); // floor1 (7,521) + floor2 (7,521)
        Assert.Equal(15_000m, retroByYear[2025]); // floor1 + floor2, not leap

        var transMast = GetActiveTransMast(context, propertyId);
        Assert.Single(transMast);
        Assert.Equal((CurrentFy, 15_000m), transMast[0]); // floor1 (7,500) + floor2 (7,500), floor3 still zero
    }

    [Fact]
    public async Task Test2_2_FloorWise_GapAnalysis_OnlyThatFloorSplitsCcThenOc_OthersStayZero()
    {
        const decimal netTax = 15_000m;
        using var context = CreateContext();
        var propertyId = Seed(context, netTax, firstYear: 2018);

        const int floor1 = 711, floor2 = 712, floor3 = 713;
        context.PropertyDetails.AddRange(
            new PropertyDetailsEntity { Id = floor1, PropertyId = propertyId, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false },
            new PropertyDetailsEntity { Id = floor2, PropertyId = propertyId, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false },
            new PropertyDetailsEntity { Id = floor3, PropertyId = propertyId, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false });
        context.SaveChanges();

        AddCertificate(context, propertyId, CcTypeId, new DateTime(2018, 4, 1), propertyDetailsId: floor1);
        AddCertificate(context, propertyId, OcTypeId, new DateTime(2021, 4, 1), propertyDetailsId: floor1);
        // floor2, floor3: no certificate at all -- NO_TAX, contribute zero throughout.

        var service = BuildService(context, BuildGuidelineReaderMock(allowFloorWise: true));
        await service.ApplyAsync(propertyId, userId: 1);

        var retroByYear = GetActiveRetroByYear(context, propertyId);
        Assert.Equal(7_500m, retroByYear[2018]);  // floor1 CC (5,000 x 1.5)
        Assert.Equal(7_500m, retroByYear[2019]);
        Assert.Equal(7_521m, retroByYear[2020]);  // FY2020 leap (BR7 add-back): (5,000 + 5,000/365) x 1.5, rounded
        Assert.Equal(5_000m, retroByYear[2021]);  // floor1 switches to OC (5,000 x 1.0) here
        Assert.Equal(5_000m, retroByYear[2022]);
        Assert.Equal(5_000m, retroByYear[2023]);
        Assert.Equal(5_014m, retroByYear[2024]);  // FY2024 leap too -- OC 1.0x leaves the leap-adjusted amount unscaled
        Assert.Equal(5_000m, retroByYear[2025]);

        var transMast = GetActiveTransMast(context, propertyId);
        Assert.Single(transMast);
        Assert.Equal((CurrentFy, 5_000m), transMast[0]); // floor1 only, still OC-governed; floor2/3 still zero
    }

    // ============================================================================================
    // SECTION 3: Master/child conflicts.
    // ============================================================================================

    [Fact]
    public async Task Test3_1_FloorOverride_OwnCertWins_BlankFloorsInheritMasterInsteadOfNoTax()
    {
        const decimal netTax = 15_000m;
        using var context = CreateContext();
        var propertyId = Seed(context, netTax, firstYear: 2018);

        const int floor1 = 721, floor2 = 722, floor3 = 723;
        context.PropertyDetails.AddRange(
            new PropertyDetailsEntity { Id = floor1, PropertyId = propertyId, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false },
            new PropertyDetailsEntity { Id = floor2, PropertyId = propertyId, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false },
            new PropertyDetailsEntity { Id = floor3, PropertyId = propertyId, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false });
        context.SaveChanges();

        // Master (property-wide) OC.
        AddCertificate(context, propertyId, OcTypeId, new DateTime(2018, 4, 1), propertyDetailsId: null);
        // Child (floor-wise) CC on floor1 only -- floor1 has no OC of its own.
        AddCertificate(context, propertyId, CcTypeId, new DateTime(2021, 4, 1), propertyDetailsId: floor1);
        // floor2, floor3: no certificate of their own -- must inherit the master OC, not NO_TAX.

        var service = BuildService(context, BuildGuidelineReaderMock(allowFloorWise: true));
        await service.ApplyAsync(propertyId, userId: 1);

        var retroByYear = GetActiveRetroByYear(context, propertyId);

        // 2018-2020: only floor2 + floor3 exist (via inherited master OC, 5,000 x 1.0 each) --
        // floor1 doesn't start until its OWN CC in 2021, proving it ignored the 2018 master OC
        // entirely rather than merging/splitting against it.
        Assert.Equal(10_000m, retroByYear[2018]); // floor2 (5,000) + floor3 (5,000), floor1 = 0
        Assert.Equal(10_000m, retroByYear[2019]);
        Assert.Equal(10_028m, retroByYear[2020]); // FY2020 leap: floor2 (5,014) + floor3 (5,014)

        // 2021 onward: floor1's own CC kicks in at 1.5x (7,500), floor2/floor3 continue at OC 1.0x
        // (5,000 each) via the inherited master date.
        Assert.Equal(17_500m, retroByYear[2021]); // 7,500 + 5,000 + 5,000
        Assert.Equal(17_500m, retroByYear[2022]);
        Assert.Equal(17_500m, retroByYear[2023]);
        Assert.Equal(17_549m, retroByYear[2024]); // FY2024 leap: floor1 (7,521) + floor2 (5,014) + floor3 (5,014)
        Assert.Equal(17_500m, retroByYear[2025]);

        var transMast = GetActiveTransMast(context, propertyId);
        Assert.Single(transMast);
        Assert.Equal((CurrentFy, 17_500m), transMast[0]);
    }

    [Fact]
    public async Task Test3_2_DatePriorityOverride_InheritedCcBeatsFloorsOwnElectricBillRegardlessOfDate()
    {
        const decimal netTax = 5_000m; // single floor -- isolates the priority tie-break specifically
        using var context = CreateContext();
        var propertyId = Seed(context, netTax, firstYear: 2020);

        const int floor1 = 731;
        context.PropertyDetails.Add(new PropertyDetailsEntity { Id = floor1, PropertyId = propertyId, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false });
        context.SaveChanges();

        // Master (property-wide) CC.
        AddCertificate(context, propertyId, CcTypeId, new DateTime(2020, 4, 1), propertyDetailsId: null);
        // Child (floor-wise) Electric Bill on floor1 -- an OLDER date, but CC always outranks
        // Electric Bill regardless of date per DATE_PRIORITY, even when EB is the floor's own and CC
        // is only inherited.
        AddCertificate(context, propertyId, ElectricBillTypeId, new DateTime(2015, 4, 1), propertyDetailsId: floor1);

        var service = BuildService(context, BuildGuidelineReaderMock(allowFloorWise: true));
        await service.ApplyAsync(propertyId, userId: 1);

        var retroByYear = GetActiveRetroByYear(context, propertyId);

        // Governed by the inherited CC from 2020 (1.5x = 7,500), NOT the floor's own 2015 Electric
        // Bill date -- if EB had wrongly won, retro would start in 2015, not 2020.
        Assert.DoesNotContain(2015, retroByYear.Keys);
        Assert.DoesNotContain(2018, retroByYear.Keys);
        Assert.Equal(7_521m, retroByYear[2020]); // FY2020 leap (BR7 add-back): (5,000 + 5,000/365) x 1.5, rounded
        Assert.Equal(7_500m, retroByYear[2021]);
        Assert.Equal(7_500m, retroByYear[2022]);
        Assert.Equal(7_500m, retroByYear[2023]);
        Assert.Equal(7_521m, retroByYear[2024]); // FY2024 leap too
        Assert.Equal(7_500m, retroByYear[2025]);

        var transMast = GetActiveTransMast(context, propertyId);
        Assert.Single(transMast);
        Assert.Equal((CurrentFy, 7_500m), transMast[0]);

        // Confirm it's tagged as CC (or its partial variant), never as an Electric Bill family.
        var policyCodes = context.PolicyTaxDetails
            .Where(pt => pt.PropertyId == propertyId && pt.IsActive && pt.PolicyCodeId != NetTaxPolicyCodeId)
            .Join(context.PolicyCodeMaster, pt => pt.PolicyCodeId, pc => pc.Id, (pt, pc) => pc.PolicyCode)
            .Distinct()
            .ToList();
        Assert.All(policyCodes, code => Assert.DoesNotContain("ELECTRIC", code));
    }
}
