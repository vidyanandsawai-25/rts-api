using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
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
/// End-to-end proof that TAXATION_RATE_MODE/TAX_PERCENTAGE_MODE actually reach persisted output
/// through <see cref="OccupationTaxApplicationService"/> and a REAL (not mocked)
/// <see cref="HistoricalNetTaxBaselineService"/> -- not just the isolated computation covered by
/// <see cref="HistoricalNetTaxBaselineServiceTests"/>.
///
/// Fixture: OC dated exactly 01-Apr-2023 (FY2023 start -- a FULL, unprorated year, and 2023 is not
/// a leap finance year, keeping the arithmetic simple), current FY 2026-27, single General Tax
/// NETTAX = 9,000 -- deliberately chosen so it EXACTLY matches what the mocked RV master data's
/// "current" (FY2026) year-range recomputes to (100 sqm x 100/sqm rate, 10% maintenance, 100% tax
/// percentage -> RV 9,000 -> tax 9,000), making the current-year ratio exactly 1.0 by construction.
/// The FY2023 year-range is seeded at half that rate (50/sqm -> RV 4,500), so the historical ratio
/// is exactly 0.5. FY2024/FY2025 have no seeded year-range at all, proving the fail-open path.
/// </summary>
public class TaxationRateAndPercentageModeWiringTests
{
    private const int CurrentFyYear = 2026;
    private const int OcTypeId = 1;
    private const int NetTaxPolicyCodeId = 1;
    private const int OcPolicyCodeId = 2;
    private const int PartialOcPolicyCodeId = 3;
    private const int CcPolicyCodeId = 4;
    private const int PartialCcPolicyCodeId = 5;
    private const int ElectricBillPolicyCodeId = 6;
    private const int PartialElectricBillPolicyCodeId = 7;
    private const int GeneralTaxId = 1;
    private const decimal CurrentAnnualTax = 9_000m; // matches the mocked RV "current" baseline exactly

    private const int TaxZoneId = 1;
    private const int WardId = 1;
    private const int ConstructionTypeId = 1;
    private const int TypeOfUseId = 1;
    private const int TypeOfUseGroupId = 1;
    private const int HistoricalYearRangeId = 100; // covers ONLY FY2023
    private const int CurrentYearRangeId = 200;    // covers ONLY FY2026

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static int Seed(ApplicationDbContext context, int propertyId = 1)
    {
        context.PropertyMast.Add(new PropertyEntity { Id = propertyId, WardId = WardId, TaxZoneId = TaxZoneId, PropertyNo = propertyId.ToString(), IsActive = true });
        context.PropertyDetails.Add(new PropertyDetailsEntity
        {
            Id = 1,
            PropertyId = propertyId,
            TypeOfUseId = TypeOfUseId,
            ConstructionTypeId = ConstructionTypeId,
            CarpetAreaSqMeter = 100d,
            IsTaxable = true,
            MarkedForDeletion = false
        });

        foreach (var year in new[] { 2023, 2024, 2025, 2026 })
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

        context.PolicyTaxDetails.Add(new PolicyTaxDetailsEntity
        {
            Id = 1000,
            PropertyId = propertyId,
            PolicyCodeId = NetTaxPolicyCodeId,
            TaxId = GeneralTaxId,
            TaxAmount = CurrentAnnualTax,
            IsActive = true,
            MarkedForDeletion = false
        });

        context.SaveChanges();
        return propertyId;
    }

    private static void AddOcCertificate(ApplicationDbContext context, int propertyId, DateTime issueDate)
    {
        var cert = PropertyCertificateEntity.Create(
            propertyId: propertyId, certificateTypeId: OcTypeId, certificateNo: $"OC-{issueDate:yyyyMMdd}",
            issueDate: issueDate, propertyDetailsId: null);
        context.PropertyCertificates.Add(cert);
        context.SaveChanges();
    }

    private static Mock<ITaxMasterDataService> BuildMasterDataMock()
    {
        var typeOfUse = new TypeOfUseEntity { Id = TypeOfUseId, TypeOfUseGroupId = TypeOfUseGroupId, Type = "R" };
        var yearRanges = new List<AssessmentYearRangeEntity>
        {
            new() { Id = HistoricalYearRangeId, FromYear = 2023, ToYear = 2023 },
            new() { Id = CurrentYearRangeId, FromYear = 2026, ToYear = 2026 }
        };
        // 100 sqm x 100/sqm = 10,000 rent, 10% maintenance -> RV 9,000, 100% tax -> 9,000 (matches
        // CurrentAnnualTax exactly). Historical rate is half (50/sqm) -> RV 4,500 -> tax 4,500.
        var rates = new List<RateEntity>
        {
            new() { TaxZoneId = TaxZoneId, ConstructionTypeId = ConstructionTypeId, TypeOfUseGroupId = TypeOfUseGroupId, YearRangeRVId = HistoricalYearRangeId, RateSquareMeter = 50m, IsActive = true },
            new() { TaxZoneId = TaxZoneId, ConstructionTypeId = ConstructionTypeId, TypeOfUseGroupId = TypeOfUseGroupId, YearRangeRVId = CurrentYearRangeId, RateSquareMeter = 100m, IsActive = true }
        };
        var generalTax = new TaxMasterEntity { Id = GeneralTaxId, TaxName = "General Tax", TaxCode = "GEN", TaxCategoryId = 1, IsActive = true, TaxCategoryMaster = new TaxCategoryMasterEntity { Id = 1, CategoryCode = "TAX", CategoryName = "Property Tax" } };
        var taxPercentages = new List<TaxPercentageMasterRVEntity>
        {
            new() { TaxId = GeneralTaxId, TypeOfUseId = TypeOfUseId, YearRangeRVId = HistoricalYearRangeId, TaxPercentage = 100m, BaseType = "RV" },
            new() { TaxId = GeneralTaxId, TypeOfUseId = TypeOfUseId, YearRangeRVId = CurrentYearRangeId, TaxPercentage = 100m, BaseType = "RV" }
        };

        var mock = new Mock<ITaxMasterDataService>();
        mock.Setup(m => m.GetActiveTypeOfUsesAsync()).ReturnsAsync(new List<TypeOfUseEntity> { typeOfUse });
        mock.Setup(m => m.GetRateSectionIdForWardAsync(WardId)).ReturnsAsync(1);
        mock.Setup(m => m.GetRatesForSectionAsync(1)).ReturnsAsync(rates);
        mock.Setup(m => m.GetActiveDepreciationsAsync()).ReturnsAsync(new List<DepreciationMasterEntity>());
        mock.Setup(m => m.GetActiveYearRangesAsync()).ReturnsAsync(yearRanges);
        mock.Setup(m => m.GetActiveTaxesAsync()).ReturnsAsync(new List<TaxMasterEntity> { generalTax });
        mock.Setup(m => m.GetActiveTaxPercentagesAsync()).ReturnsAsync(taxPercentages);
        return mock;
    }

    private static IHistoricalNetTaxBaselineService BuildRealHistoricalBaselineService(ApplicationDbContext context)
    {
        var policyConfigMock = new Mock<IPolicyConfigurationService>();
        policyConfigMock
            .Setup(p => p.GetPolicyValuesAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>());

        return new HistoricalNetTaxBaselineService(
            new Repository<PropertyEntity, int>(context),
            new Repository<PropertyDetailsEntity, int>(context),
            new Repository<RenterMastEntity, int>(context),
            BuildMasterDataMock().Object,
            new RateableValueCalculatorService(NullLogger<RateableValueCalculatorService>.Instance),
            policyConfigMock.Object);
    }

    private static Mock<ICertificateTaxGuidelineReaderService> BuildGuidelineReaderMock(string taxationRateMode, string taxPercentageMode, decimal fixedTaxPercentage = 0m)
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
                TaxationRateMode: taxationRateMode, TaxPercentageMode: taxPercentageMode, FixedTaxPercentage: fixedTaxPercentage));
        return mock;
    }

    private static OccupationTaxApplicationService BuildService(
        ApplicationDbContext context, Mock<ICertificateTaxGuidelineReaderService> guidelineReader, IHistoricalNetTaxBaselineService historicalBaselineService)
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
            policyCodeLookup, financeYearProvider, guidelineReader.Object, historicalBaselineService, unitOfWork,
            NullLogger<OccupationTaxApplicationService>.Instance,
            NtisPlatform.Tests.Helpers.NoOpTaxApplicabilityService.Instance);
    }

    [Fact]
    public async Task DefaultMode_CurrentYearForAll_MatchesTheEngineAloneWithNoRescale()
    {
        using var context = CreateContext();
        var propertyId = Seed(context);
        AddOcCertificate(context, propertyId, new DateTime(2023, 4, 1)); // exact FY2023 start -> full, unprorated year

        var service = BuildService(context, BuildGuidelineReaderMock("CURRENT_YEAR_FOR_ALL", "CURRENT_YEAR_FOR_ALL"), BuildRealHistoricalBaselineService(context));
        await service.ApplyAsync(propertyId, userId: 1);

        // FY2023 and FY2025 are non-leap full years -> exactly 9,000 (the seeded NETTAX), untouched.
        var retro = context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.IsActive).ToList();
        Assert.Equal(9_000m, retro.Single(r => r.PendingYearId == 2023).PendingAmount);
        Assert.Equal(9_000m, retro.Single(r => r.PendingYearId == 2025).PendingAmount);
        var transMast = context.TransMast.Single(t => t.PropertyId == propertyId && t.IsActive && t.CalculationType == "RV");
        Assert.Equal(9_000m, transMast.TaxAmount); // FY2026, current year, also non-leap
    }

    [Fact]
    public async Task HistoricalYearWiseRate_RescalesOnlyYearsWithASeededRange()
    {
        using var context = CreateContext();
        var propertyId = Seed(context);
        AddOcCertificate(context, propertyId, new DateTime(2023, 4, 1));

        var service = BuildService(context, BuildGuidelineReaderMock("HISTORICAL_YEAR_WISE", "CURRENT_YEAR_FOR_ALL"), BuildRealHistoricalBaselineService(context));
        await service.ApplyAsync(propertyId, userId: 1);

        // FY2023: rescaled by (historical FY2023 baseline 4,500) / (options baseline 9,000) = 0.5
        // -> 9,000 * 0.5 = 4,500.
        var retro = context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.IsActive).ToList();
        Assert.Equal(4_500m, retro.Single(r => r.PendingYearId == 2023).PendingAmount);

        // FY2025: no seeded year-range covers it -- ComputeBaselineAsync returns null, fails open,
        // left exactly as the base engine computed it (9,000, non-leap full year), NOT zeroed.
        Assert.Equal(9_000m, retro.Single(r => r.PendingYearId == 2025).PendingAmount);

        // FY2026 (current year): rescaled by (current FY2026 baseline 9,000) / (options 9,000) =
        // 1.0 by construction -- unchanged.
        var transMast = context.TransMast.Single(t => t.PropertyId == propertyId && t.IsActive && t.CalculationType == "RV");
        Assert.Equal(9_000m, transMast.TaxAmount);
    }

    [Fact]
    public async Task FixedPercentage_AppliesUniformlyRegardlessOfYearOrSeededRange()
    {
        using var context = CreateContext();
        var propertyId = Seed(context);
        AddOcCertificate(context, propertyId, new DateTime(2023, 4, 1));

        // Fixed at 50% -- half of the 100% every seeded TaxPercentageMasterRV row uses, applied to
        // whatever RV the RATE side resolves (still CURRENT_YEAR_FOR_ALL here, so always FY2026's
        // rate -> RV 9,000 -> 50% = 4,500), for EVERY year including FY2025, which has no seeded
        // rate-year-range of its own -- proving the rate side still resolves to FY2026 regardless.
        var service = BuildService(context, BuildGuidelineReaderMock("CURRENT_YEAR_FOR_ALL", "FIXED_FOR_ALL", fixedTaxPercentage: 50m), BuildRealHistoricalBaselineService(context));
        await service.ApplyAsync(propertyId, userId: 1);

        var transMast = context.TransMast.Single(t => t.PropertyId == propertyId && t.IsActive && t.CalculationType == "RV");
        Assert.Equal(4_500m, transMast.TaxAmount); // 9,000 * 50%

        var retro = context.TaxPendingDetailsRetro.Where(r => r.PropertyId == propertyId && r.IsActive).ToList();
        Assert.Equal(4_500m, retro.Single(r => r.PendingYearId == 2023).PendingAmount);
        Assert.Equal(4_500m, retro.Single(r => r.PendingYearId == 2025).PendingAmount);
    }
}
