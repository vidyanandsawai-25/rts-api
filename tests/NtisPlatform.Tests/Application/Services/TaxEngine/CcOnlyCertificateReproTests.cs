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
/// Reproduction harness for the reported bug: a property-level Commencement/Completion (CC)
/// certificate with a valid CertificateNo/IssueDate never produces a CC/PARTIAL_CC row in
/// PolicyTaxDetails, even though every individually-checked PTIS.CertificateTaxGuideline setting
/// is correctly configured. Uses the REAL OccupationTaxApplicationService, real PolicyCodeLookupService,
/// real OccupationTaxEngine, and a real EF InMemory ApplicationDbContext -- no mocked repositories --
/// so any behavior found here reflects actual runtime behavior, not test-double assumptions.
/// </summary>
public class CcOnlyCertificateReproTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static void SeedCommon(ApplicationDbContext context, out int propertyId, out int nettaxTaxId, out int currentFyYear)
    {
        var today = DateTime.Now.Date;
        currentFyYear = today.Month >= 4 ? today.Year : today.Year - 1;
        var issueDate = today.AddDays(-1);

        var property = new PropertyEntity { Id = 1, WardId = 1, PropertyNo = "1", IsActive = true };
        context.PropertyMast.Add(property);
        propertyId = property.Id;

        context.YearMaster.Add(new YearMasterEntity { Id = 1, Year = currentFyYear, YearCode = $"{currentFyYear}-{currentFyYear + 1}", IsActive = true });

        var certType = new PropertyCertificateTypeMasterEntity
        {
            Id = 1,
            CertificateTypeName = "Commencement/Completion Certificate",
            CertificateTypeCode = "CC",
            IsTaxable = true,
            IsActive = true
        };
        context.PropertyCertificateTypeMasters.Add(certType);

        var certificate = PropertyCertificateEntity.Create(
            propertyId: propertyId,
            certificateTypeId: certType.Id,
            certificateNo: "TEST-CC-001",
            issueDate: issueDate,
            propertyDetailsId: null);
        context.PropertyCertificates.Add(certificate);

        var nettax = new PolicyCodeMasterEntity { Id = 1, PolicyCode = "NETTAX", IsActive = true };
        var cc = new PolicyCodeMasterEntity { Id = 2, PolicyCode = "CC", IsActive = true };
        var partialCc = new PolicyCodeMasterEntity { Id = 3, PolicyCode = "PARTIAL_CC", IsActive = true };
        var oc = new PolicyCodeMasterEntity { Id = 4, PolicyCode = "OC", IsActive = true };
        var partialOc = new PolicyCodeMasterEntity { Id = 5, PolicyCode = "PARTIAL_OC", IsActive = true };
        var eb = new PolicyCodeMasterEntity { Id = 6, PolicyCode = "ELECTRIC_BILL", IsActive = true };
        var partialEb = new PolicyCodeMasterEntity { Id = 7, PolicyCode = "PARTIAL_ELECTRIC_BILL", IsActive = true };
        context.PolicyCodeMaster.AddRange(nettax, cc, partialCc, oc, partialOc, eb, partialEb);

        var generalTax = new TaxMasterEntity { Id = 1, TaxName = "GeneralTax", TaxCode = "GEN", DisplayOrder = 1, IsActive = true };
        context.TaxMaster.Add(generalTax);
        nettaxTaxId = generalTax.Id;

        // Realistic non-zero NETTAX baseline, as a normal RV-assessed property would have.
        context.PolicyTaxDetails.Add(new PolicyTaxDetailsEntity
        {
            Id = 1,
            PropertyId = propertyId,
            PolicyCodeId = nettax.Id,
            TaxId = generalTax.Id,
            TaxAmount = 1000m,
            CalculationValue = 100000m,
            IsActive = true,
            MarkedForDeletion = false
        });

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

    [Fact]
    public async Task ApplyAsync_PropertyLevelCcOnlyCertificate_PersistsPartialCcPolicyTaxDetailsRow()
    {
        using var context = CreateContext();
        SeedCommon(context, out var propertyId, out _, out var currentFyYear);

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

        var service = new OccupationTaxApplicationService(
            engine,
            propertyRepo,
            certRepo,
            policyTaxRepo,
            transMastRepo,
            yearRepo,
            taxPendingRepo,
            taxPendingRetroRepo,
            policyCodeLookup,
            financeYearProvider,
            BuildGuidelineReaderMock().Object,
            unitOfWork,
            NullLogger<OccupationTaxApplicationService>.Instance);

        await service.ApplyAsync(propertyId, userId: 1);

        var ccRows = context.PolicyTaxDetails
            .Where(pt => pt.PropertyId == propertyId && (pt.PolicyCodeId == 2 || pt.PolicyCodeId == 3) && pt.IsActive)
            .ToList();

        Assert.NotEmpty(ccRows);
    }

    [Fact]
    public async Task ApplyAsync_MissingOcPolicyCodeMasterRow_StillPersistsCcRowSinceOnlyCcWasRequested()
    {
        using var context = CreateContext();
        SeedCommon(context, out var propertyId, out _, out var currentFyYear);

        // Simulate the real-world data gap this fix targets: PARTIAL_OC was never seeded in
        // PolicyCodeMaster, even though CC/PARTIAL_CC exist and are correctly configured. Before
        // the fix, this unrelated gap aborted the ENTIRE computation (ResolveFamilyPolicyCodesAsync
        // eagerly required all six family codes); now it should only matter if OC is actually used.
        var partialOc = context.PolicyCodeMaster.Single(p => p.PolicyCode == "PARTIAL_OC");
        context.PolicyCodeMaster.Remove(partialOc);
        context.SaveChanges();

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

        var service = new OccupationTaxApplicationService(
            engine,
            propertyRepo,
            certRepo,
            policyTaxRepo,
            transMastRepo,
            yearRepo,
            taxPendingRepo,
            taxPendingRetroRepo,
            policyCodeLookup,
            financeYearProvider,
            BuildGuidelineReaderMock().Object,
            unitOfWork,
            NullLogger<OccupationTaxApplicationService>.Instance);

        // A CC-only certificate does not need PARTIAL_OC to exist -- it must not throw, and the
        // CC/PARTIAL_CC row must still be persisted.
        await service.ApplyAsync(propertyId, userId: 1);

        var ccRows = context.PolicyTaxDetails
            .Where(pt => pt.PropertyId == propertyId && (pt.PolicyCodeId == 2 || pt.PolicyCodeId == 3) && pt.IsActive)
            .ToList();

        Assert.NotEmpty(ccRows);
    }

    [Fact]
    public async Task ApplyAsync_MissingCcPolicyCodeMasterRow_StillThrowsSinceCcIsTheFamilyActuallyInUse()
    {
        using var context = CreateContext();
        SeedCommon(context, out var propertyId, out _, out var currentFyYear);

        // Unlike the OC-missing case above, PARTIAL_CC is the family THIS certificate actually
        // resolves to -- removing it must still fail loudly, proving the fix didn't just silence
        // all misconfiguration.
        var partialCc = context.PolicyCodeMaster.Single(p => p.PolicyCode == "PARTIAL_CC");
        context.PolicyCodeMaster.Remove(partialCc);
        context.SaveChanges();

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

        var service = new OccupationTaxApplicationService(
            engine,
            propertyRepo,
            certRepo,
            policyTaxRepo,
            transMastRepo,
            yearRepo,
            taxPendingRepo,
            taxPendingRetroRepo,
            policyCodeLookup,
            financeYearProvider,
            BuildGuidelineReaderMock().Object,
            unitOfWork,
            NullLogger<OccupationTaxApplicationService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApplyAsync(propertyId, userId: 1));

        var ccRows = context.PolicyTaxDetails
            .Where(pt => pt.PropertyId == propertyId && (pt.PolicyCodeId == 2 || pt.PolicyCodeId == 3) && pt.IsActive)
            .ToList();

        Assert.Empty(ccRows);
    }
}
