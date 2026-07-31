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
/// Regression coverage for a real bug reported via a live screenshot: a property with only an
/// Occupancy Certificate + Electric Bill entered (no Commencement/Completion Certificate anywhere)
/// showed FOUR active Tax Details rows -- NETTAX, Completion Certificate, Occupancy Certificate,
/// and Partial Electric Bill -- all with identical figures. The stale "Completion Certificate" row
/// was never entered for this run; it was a leftover from an earlier computation that
/// SaveTaxesAsync's cleanup failed to deactivate.
///
/// Root cause: PTIS.PolicyTaxDetails' real unique index
/// (UX_PolicyTaxDetails_Property_Year_PolicyCode_TaxId, on PropertyId+PolicyCodeId+TaxId despite
/// the "Year" in its name -- there is no PolicyYear column) includes PolicyCodeId, so the database
/// genuinely allows two ACTIVE rows for the same (PropertyId, TaxId) under DIFFERENT PolicyCodeIds
/// (e.g. a leftover CC row alongside a newer OC row). SaveTaxesAsync collapsed all loaded existing
/// rows into a Dictionary keyed by TaxId ONLY -- when two rows shared a slot, the second one loaded
/// silently overwrote the first in the dictionary, and the final stale-cleanup loop iterated only
/// that dictionary's one-survivor-per-slot values, so the dropped duplicate was never found and
/// deactivated -- it stayed active in the database forever.
///
/// This test recreates that exact pre-existing-duplicate scenario (simulating data left behind by
/// an earlier bug or race condition) and proves the fix now finds and deactivates it.
/// </summary>
public class PolicyTaxDetailsDuplicateFamilyCleanupTests
{
    private const int CurrentFyYear = 2020;
    private static readonly DateTime OcDate = new(2020, 4, 20);
    private static readonly DateTime ElectricBillDate = new(2020, 6, 7);

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

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static int SeedCommon(ApplicationDbContext context, out int generalTaxId, out int yearMasterId)
    {
        var property = new PropertyEntity { Id = 1, WardId = 1, PropertyNo = "1", IsActive = true };
        context.PropertyMast.Add(property);

        var yearMaster = new YearMasterEntity { Id = 1, Year = CurrentFyYear, YearCode = $"{CurrentFyYear}-{CurrentFyYear + 1}", IsActive = true };
        context.YearMaster.Add(yearMaster);
        yearMasterId = yearMaster.Id;

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
        context.TaxMaster.Add(generalTax);
        generalTaxId = generalTax.Id;

        context.PolicyTaxDetails.Add(new PolicyTaxDetailsEntity
        {
            Id = 1,
            PropertyId = property.Id,
            PolicyCodeId = NetTaxPolicyCodeId,
            TaxId = generalTax.Id,
            TaxAmount = 1000m,
            CalculationValue = 100000m,
            IsActive = true,
            MarkedForDeletion = false
        });

        context.SaveChanges();
        return property.Id;
    }

    private static void AddCertificate(ApplicationDbContext context, int propertyId, int typeId, DateTime issueDate)
    {
        var cert = PropertyCertificateEntity.Create(
            propertyId: propertyId, certificateTypeId: typeId,
            certificateNo: $"CERT-{typeId}-{issueDate:yyyyMMdd}", issueDate: issueDate, propertyDetailsId: null);
        context.PropertyCertificates.Add(cert);
        context.SaveChanges();
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
                CcOcGapWithinAction: "APPLY_OC_ONLY",
                CcOcGapExceededAction: "APPLY_CC_THEN_OC",
                InvalidCcOcDateOrderAction: "USE_PRIORITY_AND_LOG",
                CcOnlyAction: "APPLY_FROM_CC_DATE",
                OcOnlyAction: "APPLY_FROM_OC_DATE",
                FinancialYearStartMonth: 4, FinancialYearStartDay: 1,
                CCPeriodMultiplier: 1.0m, OCPeriodMultiplier: 1.0m,
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

    private static OccupationTaxApplicationService BuildService(ApplicationDbContext context)
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
            policyCodeLookup, financeYearProvider, BuildGuidelineReaderMock().Object, unitOfWork,
            NullLogger<OccupationTaxApplicationService>.Instance);
    }

    [Fact]
    public async Task ApplyAsync_PreExistingDuplicateFamilyRowForSameYearAndTax_StaleRowGetsDeactivated()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context, out var generalTaxId, out _);

        // Simulate data left behind by the reported bug: a CC-family row ALREADY active for
        // (PropertyId, TaxId=GeneralTax) -- even though no CC certificate is ever entered in this
        // test. This is exactly what the live screenshot showed: a "Completion Certificate" row
        // nobody entered, sitting alongside a genuine Occupancy Certificate row -- BOTH already
        // active for the SAME (PropertyId, TaxId) slot, which PTIS.PolicyTaxDetails' real unique
        // index permits since PolicyCodeId is part of the key.
        var staleCcRow = new PolicyTaxDetailsEntity
        {
            Id = 100,
            PropertyId = propertyId,
            PolicyCodeId = PartialCcPolicyCodeId,
            TaxId = generalTaxId,
            TaxAmount = 189m,
            CalculationValue = 100000m,
            IsActive = true,
            MarkedForDeletion = false
        };
        var existingOcRow = new PolicyTaxDetailsEntity
        {
            Id = 101,
            PropertyId = propertyId,
            PolicyCodeId = PartialOcPolicyCodeId,
            TaxId = generalTaxId,
            TaxAmount = 189m,
            CalculationValue = 100000m,
            IsActive = true,
            MarkedForDeletion = false
        };
        context.PolicyTaxDetails.AddRange(staleCcRow, existingOcRow);
        context.SaveChanges();

        // Only OC + Electric Bill are entered -- no CC certificate exists for this property at all.
        AddCertificate(context, propertyId, OcTypeId, OcDate);
        AddCertificate(context, propertyId, ElectricBillTypeId, ElectricBillDate);

        var service = BuildService(context);
        await service.ApplyAsync(propertyId, userId: 1);

        var activeRowsForSlot = context.PolicyTaxDetails
            .Where(pt => pt.PropertyId == propertyId
                         && pt.TaxId == generalTaxId && pt.IsActive
                         && (pt.Id == staleCcRow.Id || pt.Id == existingOcRow.Id))
            .ToList();

        // Before the fix: BOTH rows stayed active forever, since the dictionary collapsed by
        // TaxId silently dropped whichever one it enumerated first, leaving it invisible to the
        // stale-cleanup loop. Exactly ONE must survive now -- never both.
        Assert.Single(activeRowsForSlot);

        var allActivePolicyCodes = context.PolicyTaxDetails
            .Where(pt => pt.PropertyId == propertyId && pt.IsActive)
            .Join(context.PolicyCodeMaster, pt => pt.PolicyCodeId, pc => pc.Id, (pt, pc) => pc.PolicyCode)
            .ToList();

        Assert.DoesNotContain(allActivePolicyCodes, c => c is "CC" or "PARTIAL_CC");
        Assert.Contains(allActivePolicyCodes, c => c is "OC" or "PARTIAL_OC");
        Assert.Contains(allActivePolicyCodes, c => c == "NETTAX");
    }
}
