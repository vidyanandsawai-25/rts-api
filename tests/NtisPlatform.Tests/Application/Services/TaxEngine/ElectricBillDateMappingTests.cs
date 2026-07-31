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
/// Regression for the exact Electric Bill date-mapping examples from the 2026-07-23 audit:
/// - 07-Jun-2026 -> the certificate's own finance year (FY2026, current) -- no floor, no retro.
/// - 31-Mar-2026 -> FY2025 (the finance year 31-Mar-2026 actually belongs to) -- one retro year,
///   proving Electric-Bill-driven retro years ALSO get TaxPendingDetailsRetro/TaxPendingDetails
///   rows (same condition-agnostic mechanism OC already has coverage for).
/// - 01-Jan-2014 -> floored up to 01-Apr-2016 (ELECTRIC_BILL_MINIMUM_FINANCIAL_YEAR), proving the
///   floor -- not the literal 2014 date -- is what the engine actually computes from.
/// </summary>
public class ElectricBillDateMappingTests
{
    private const int CurrentFyYear = 2026; // FY2026 = 01-Apr-2026..31-Mar-2027

    private const int ElectricBillTypeId = 1;
    private const int NetTaxPolicyCodeId = 1;
    private const int ElectricBillPolicyCodeId = 2;
    private const int PartialElectricBillPolicyCodeId = 3;

    private const int GeneralTaxId = 1;
    private const decimal AnnualTax = 500m;

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static int Seed(ApplicationDbContext context, int[] years, int propertyId = 1)
    {
        context.PropertyMast.Add(new PropertyEntity { Id = propertyId, WardId = 1, PropertyNo = propertyId.ToString(), IsActive = true });

        foreach (var year in years)
        {
            context.YearMaster.Add(new YearMasterEntity { Id = year, Year = year, YearCode = $"{year}-{(year + 1) % 100:D2}", IsActive = true });
        }

        context.PropertyCertificateTypeMasters.Add(
            new PropertyCertificateTypeMasterEntity { Id = ElectricBillTypeId, CertificateTypeName = "Electric Bill", CertificateTypeCode = "ELECTRIC_BILL", IsTaxable = true, IsActive = true });

        context.PolicyCodeMaster.AddRange(
            new PolicyCodeMasterEntity { Id = NetTaxPolicyCodeId, PolicyCode = "NETTAX", IsActive = true },
            new PolicyCodeMasterEntity { Id = ElectricBillPolicyCodeId, PolicyCode = "ELECTRIC_BILL", IsActive = true },
            new PolicyCodeMasterEntity { Id = PartialElectricBillPolicyCodeId, PolicyCode = "PARTIAL_ELECTRIC_BILL", IsActive = true });

        context.TaxCategoryMaster.Add(new TaxCategoryMasterEntity { Id = 1, CategoryCode = "TAX", CategoryName = "Property Tax", IsActive = true });
        context.TaxMaster.Add(new TaxMasterEntity { Id = GeneralTaxId, TaxName = "General Tax", TaxCode = "GEN", DisplayOrder = 1, TaxCategoryId = 1, IsActive = true });

        // Exactly ONE active NETTAX row per (PropertyId, TaxId) -- the DBA-confirmed schema has no
        // PolicyYear column, so this single current rate is used uniformly for every finance year.
        context.PolicyTaxDetails.Add(new PolicyTaxDetailsEntity
        {
            Id = 1000,
            PropertyId = propertyId,
            PolicyCodeId = NetTaxPolicyCodeId,
            TaxId = GeneralTaxId,
            TaxAmount = AnnualTax,
            CalculationValue = 50_000m,
            IsActive = true,
            MarkedForDeletion = false
        });

        context.SaveChanges();
        return propertyId;
    }

    private static void AddElectricBillCertificate(ApplicationDbContext context, int propertyId, DateTime issueDate)
    {
        var cert = PropertyCertificateEntity.Create(
            propertyId: propertyId,
            certificateTypeId: ElectricBillTypeId,
            certificateNo: $"EB-{issueDate:yyyyMMdd}",
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
    public async Task ElectricBill_07Jun2026_MapsToCurrentFy2026_NoRetro()
    {
        using var context = CreateContext();
        var propertyId = Seed(context, new[] { 2026 });
        AddElectricBillCertificate(context, propertyId, new DateTime(2026, 6, 7)); // 07-06-2026

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        var transMast = context.TransMast.Where(t => t.PropertyId == propertyId && t.IsActive && t.CalculationType == "RV").ToList();
        Assert.Single(transMast);
        Assert.Equal(2026, transMast[0].FinanceYearId); // 07-Jun-2026 belongs to FY2026 (01-Apr-2026 start)

        Assert.Empty(context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.IsActive));
        Assert.Empty(context.TaxPendingDetails.Where(r => r.PropertyId == propertyId && r.IsActive));
    }

    [Fact]
    public async Task ElectricBill_31Mar2026_MapsToFy2025_OneRetroYear_WritesTaxPendingDetailsAndRetro()
    {
        using var context = CreateContext();
        var propertyId = Seed(context, new[] { 2025, 2026 });
        AddElectricBillCertificate(context, propertyId, new DateTime(2026, 3, 31)); // 31-03-2026

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        // 31-Mar-2026 is the LAST day of FY2025 (01-Apr-2025..31-Mar-2026), not FY2026. TransMast
        // holds the CURRENT FY only (2026) -- FY2025 is a retro year and must NOT appear in
        // TransMast; it belongs in TaxPendingDetailsRetro/TaxPendingDetails instead (asserted below).
        var transMast = context.TransMast.Where(t => t.PropertyId == propertyId && t.IsActive && t.CalculationType == "RV").ToList();
        Assert.Single(transMast);
        Assert.Contains(transMast, t => t.FinanceYearId == 2026);

        // FY2025 is strictly before the current FY2026 -- a retro/arrears year -- so it must ALSO
        // get a row in both pending tables, exactly like an OC-driven retro year does.
        var retro = context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.IsActive).ToList();
        var pending = context.TaxPendingDetails.Where(r => r.PropertyId == propertyId && r.IsActive).ToList();
        Assert.Single(retro);
        Assert.Equal(2025, retro[0].PendingYearId);
        Assert.Single(pending);
        Assert.Equal(2025, pending[0].PendingYearId);
    }

    [Fact]
    public async Task ElectricBill_01Jan2014_FloorsToFy2016_NotLiteral2014()
    {
        using var context = CreateContext();
        // Generous lookback so the retrospective-window cap isn't what limits the oldest year --
        // this test is specifically about the ELECTRIC_BILL_MINIMUM_FINANCIAL_YEAR floor, not the
        // separate LookbackYears cap (already covered by other tests).
        var years = Enumerable.Range(2016, 2026 - 2016 + 1).ToArray(); // 2016..2026 inclusive
        var propertyId = Seed(context, years);
        AddElectricBillCertificate(context, propertyId, new DateTime(2014, 1, 1)); // 01-01-2014

        var service = BuildService(context, BuildGuidelineReaderMock(lookbackYears: 15));
        await service.ApplyAsync(propertyId, userId: 1);

        // TransMast holds the CURRENT FY only (2026) regardless of how far back the retro window
        // is floored -- the floor applies to the year-wise retro breakup in TaxPendingDetailsRetro
        // (asserted below), not to TransMast.
        var transMast = context.TransMast.Where(t => t.PropertyId == propertyId && t.IsActive && t.CalculationType == "RV").ToList();
        Assert.Single(transMast);
        Assert.Equal(2026, transMast[0].FinanceYearId);

        var oldestRetro = context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.IsActive).ToList();
        Assert.NotEmpty(oldestRetro);
        Assert.Equal(2016, oldestRetro.Min(r => r.PendingYearId)); // floored to 01-Apr-2016 -- not 2014, not 2015
        Assert.DoesNotContain(oldestRetro, r => r.PendingYearId < 2016);
    }
}
