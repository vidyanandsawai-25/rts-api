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
/// QA-reported bug (2026-07-30): for a CC certificate, the persisted CC Total Tax did not match
/// NETTAX x CC_PERIOD_MULTIPLIER (1.5x). Real production figures: NETTAX total 8,82,091 x 1.5 =
/// 13,23,136.50, but the system persisted 13,23,138 -- 1.5 rupees over the raw multiplication and 1
/// rupee over the correctly-rounded 13,23,137. Root cause: General Tax and each of the (up to) 10
/// real NETTAX component rows were each independently rounded to the nearest rupee, letting the
/// group's combined rounding drift away from a single rounding of the true total. Fixed by scaling
/// General Tax and Component Tax as one combined total in
/// <see cref="OccupationTaxApplicationService"/>'s ScaleYearResult, and by allocating the real
/// per-tax-head rows via a largest-remainder split against that single rounded total instead of
/// rounding each row on its own (see AllocateByLargestRemainder).
///
/// Uses the exact 11 tax-head NETTAX breakdown from the reported bug (General Tax + 10 cesses,
/// summing to 8,82,091), a CC certificate dated in an OLD (already-past) finance year so the whole
/// year is a full, unprorated 1.5x scale -- the precise scenario QA reproduced -- with the real EF
/// InMemory ApplicationDbContext, real OccupationTaxEngine, and no mocked repositories.
/// </summary>
public class CcTaxRoundingBugTests
{
    private const int CurrentFyYear = 2026;
    private const int CcTypeId = 1;

    private const int NetTaxPolicyCodeId = 1;
    private const int OcPolicyCodeId = 2;
    private const int PartialOcPolicyCodeId = 3;
    private const int CcPolicyCodeId = 4;
    private const int PartialCcPolicyCodeId = 5;
    private const int ElectricBillPolicyCodeId = 6;
    private const int PartialElectricBillPolicyCodeId = 7;

    // The exact 11 tax-head NETTAX breakdown from the QA bug report's screenshot -- sums to 8,82,091.
    private static readonly Dictionary<int, decimal> AnnualRateByTaxId = new()
    {
        [1] = 282_139m, // General Tax
        [2] = 77_832m,  // State Education Tax
        [3] = 19_458m,  // State Employment Tax
        [4] = 6_486m,   // Tree Cess
        [5] = 64_860m,  // Special Water Cess
        [6] = 58_374m,  // Road Cess
        [7] = 6_486m,   // Fire Cess
        [8] = 84_317m,  // Light Cess
        [9] = 142_691m, // Water Benefit Cess
        [10] = 113_504m, // Sewage Disposal Cess
        [11] = 25_944m,  // Special Education Tax
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

        foreach (var year in new[] { CurrentFyYear - 2, CurrentFyYear - 1, CurrentFyYear })
        {
            context.YearMaster.Add(new YearMasterEntity { Id = year, Year = year, YearCode = $"{year}-{(year + 1) % 100:D2}", IsActive = true });
        }

        context.PropertyCertificateTypeMasters.Add(
            new PropertyCertificateTypeMasterEntity { Id = CcTypeId, CertificateTypeName = "Commencement/Completion Certificate", CertificateTypeCode = "CC", IsTaxable = true, IsActive = true });

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
            context.TaxMaster.Add(new TaxMasterEntity { Id = taxId, TaxName = $"Cess{taxId}", TaxCode = $"C{taxId}", DisplayOrder = taxId, TaxCategoryId = 1, IsActive = true });
        }

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
    public async Task CcFullYearAt1_5x_PersistedTotalMatchesSingleRoundingOfNetTaxTimesMultiplier()
    {
        using var context = CreateContext();
        var propertyId = Seed(context);

        // CC dated in an OLD finance year (well before the current one) -- CurrentFyYear itself then
        // becomes a FULL, unprorated year still governed by CC (no OC to hand off to), which is
        // exactly the "whole-year x 1.5" scenario QA reproduced (their screenshot's CC row is a full
        // prior-year figure, not a same-year day-prorated one).
        var ccDate = new DateTime(CurrentFyYear - 2, 4, 7);
        var cert = PropertyCertificateEntity.Create(propertyId, CcTypeId, "CC-BUG-REPRO", ccDate, propertyDetailsId: null);
        context.PropertyCertificates.Add(cert);
        context.SaveChanges();

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        var netTaxTotal = AnnualRateByTaxId.Values.Sum();
        Assert.Equal(882_091m, netTaxTotal); // sanity check against the reported figures

        // A single, correct rounding of NETTAX x 1.5 is 13,23,137 (882091 * 1.5 = 1,323,136.50,
        // rounds away from zero) -- NOT the buggy 13,23,138 QA observed, and not the raw
        // unrounded 13,23,136.50 either.
        var expectedTotal = Math.Round(netTaxTotal * 1.5m, 0, MidpointRounding.AwayFromZero);
        Assert.Equal(1_323_137m, expectedTotal);

        var ccRows = context.PolicyTaxDetails
            .Where(pt => pt.PropertyId == propertyId && pt.PolicyCodeId == CcPolicyCodeId && pt.IsActive && !pt.MarkedForDeletion)
            .ToList();

        Assert.Equal(11, ccRows.Count);
        Assert.Equal(expectedTotal, ccRows.Sum(r => r.TaxAmount ?? 0m));

        var transMastRows = context.TransMast
            .Where(t => t.PropertyId == propertyId && t.IsActive && !t.MarkedForDeletion && t.CalculationType == "RV")
            .ToList();
        Assert.Equal(11, transMastRows.Count);
        Assert.Equal(expectedTotal, transMastRows.Sum(t => t.TaxAmount));
    }
}
