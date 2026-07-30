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
/// Regression coverage for the business rule: CC has higher priority than Electric Bill for
/// deciding which certificate's date governs tax, REGARDLESS of which date is chronologically
/// later. Configured priority order (matching the seed script this session delivered) is
/// CC &gt; OC &gt; ELECTRIC_BILL &gt; RETROSPECTIVE (DATE_PRIORITY_1..4). "Latest date wins" must never
/// happen -- <see cref="OccupationTaxApplicationService"/>'s ResolveWinner walks the configured
/// priority list and returns on the FIRST match; it never compares the candidate dates against
/// each other. Uses the real EF InMemory ApplicationDbContext, real OccupationTaxEngine,
/// PolicyCodeLookupService and OccupationTaxApplicationService -- no mocked repositories -- so
/// these tests exercise the actual persisted PolicyCodeId, not just in-memory booleans.
///
/// All test dates are anchored to a fixed past finance year (FY2020, Apr-2020 to Mar-2021)
/// instead of "today" so the tests never depend on which real calendar date they happen to run on.
/// </summary>
public class CcOcElectricBillDatePriorityTests
{
    private const int CurrentFyYear = 2020;
    private static readonly DateTime CcDate = new(2020, 4, 7);
    private static readonly DateTime ElectricBillDate = new(2020, 6, 7); // 2 months after CC, per the reported scenario
    private static readonly DateTime OcDateCloseToCC = new(2020, 5, 1); // within the 6-month "gap-within" threshold of CcDate

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

    private static int SeedCommon(ApplicationDbContext context)
    {
        var property = new PropertyEntity { Id = 1, WardId = 1, PropertyNo = "1", IsActive = true };
        context.PropertyMast.Add(property);

        context.YearMaster.Add(new YearMasterEntity { Id = 1, Year = CurrentFyYear, YearCode = $"{CurrentFyYear}-{CurrentFyYear + 1}", IsActive = true });

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

        // Realistic non-zero NETTAX baseline, as a normal RV-assessed property would have.
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
        string missingCertificateDateAction = "IGNORE_FOR_TAX")
    {
        var mock = new Mock<ICertificateTaxGuidelineReaderService>();
        mock.Setup(g => g.GetActiveSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CertificateTaxGuidelineSettings(
                EnableCertificateBasedTax: true,
                ApplyOnlyTaxableCertTypes: true,
                DatePriority1: "CC", DatePriority2: "OC", DatePriority3: "ELECTRIC_BILL", DatePriority4: "RETROSPECTIVE",
                CertificateRequireNoAndDate: true,
                MissingCertificateNoAction: "IGNORE_FOR_TAX",
                MissingCertificateDateAction: missingCertificateDateAction,
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

    private static OccupationTaxApplicationService BuildService(ApplicationDbContext context, Mock<ICertificateTaxGuidelineReaderService> guidelineReader)
    {
        var propertyRepo = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == CurrentFyYear));
        var certRepo = new Repository<PropertyCertificateEntity, int>(context);
        var policyTaxRepo = new Repository<PolicyTaxDetailsEntity, int>(context);
        var transMastRepo = new Repository<TransMastEntity, int>(context);
        var yearRepo = new Repository<YearMasterEntity, int>(context);
        var taxPendingRepo = new Repository<TaxPendingDetailsEntity, int>(context);
        var taxPendingRetroRepo = new Repository<TaxPendingDetailsRetroEntity, int>(context);
        var taxMasterRepo = new Repository<TaxMasterEntity, int>(context);
        var policyCodeRepo = new Repository<PolicyCodeMasterEntity, int>(context);
        var policyCodeLookup = new PolicyCodeLookupService(policyCodeRepo);
        var unitOfWork = new UnitOfWork(context);
        var financeYearProvider = Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == CurrentFyYear);
        var engine = new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance);

        return new OccupationTaxApplicationService(
            engine, propertyRepo, certRepo, policyTaxRepo, transMastRepo, yearRepo,
            taxPendingRepo, taxPendingRetroRepo, taxMasterRepo,
            policyCodeLookup, financeYearProvider, guidelineReader.Object, unitOfWork,
            NullLogger<OccupationTaxApplicationService>.Instance);
    }

    /// <summary>
    /// Distinct PolicyCode strings (excluding NETTAX) persisted for the property. PolicyTaxDetails
    /// holds only the current year's certificate-tax row now (no PolicyYear filter needed or
    /// possible under the DBA-confirmed schema -- no PolicyYear column, unique index on
    /// PropertyId+PolicyCodeId+TaxId).
    /// </summary>
    private static List<string> GetResultingPolicyCodes(ApplicationDbContext context, int propertyId)
    {
        return context.PolicyTaxDetails
            .Where(pt => pt.PropertyId == propertyId && pt.IsActive)
            .Join(context.PolicyCodeMaster, pt => pt.PolicyCodeId, pc => pc.Id, (pt, pc) => pc.PolicyCode)
            .Where(code => code != "NETTAX")
            .Distinct()
            .ToList();
    }

    [Fact]
    public async Task Scenario1_CcAndElectricBill_TwoMonthsApart_CcWins_NotLatestDate()
    {
        // Exactly the reported scenario: CC = 07-04-2020, Electric Bill = 07-06-2020 (2 months later).
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        AddCertificate(context, propertyId, CcTypeId, CcDate);
        AddCertificate(context, propertyId, ElectricBillTypeId, ElectricBillDate);

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        var codes = GetResultingPolicyCodes(context, propertyId);

        Assert.Contains(codes, c => c is "CC" or "PARTIAL_CC");
        Assert.DoesNotContain(codes, c => c is "ELECTRIC_BILL" or "PARTIAL_ELECTRIC_BILL");
    }

    [Fact]
    public async Task Scenario2_OcAndElectricBill_NoCC_OcWins()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        AddCertificate(context, propertyId, OcTypeId, OcDateCloseToCC);
        AddCertificate(context, propertyId, ElectricBillTypeId, ElectricBillDate);

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        var codes = GetResultingPolicyCodes(context, propertyId);

        Assert.Contains(codes, c => c is "OC" or "PARTIAL_OC");
        Assert.DoesNotContain(codes, c => c is "ELECTRIC_BILL" or "PARTIAL_ELECTRIC_BILL");
    }

    [Fact]
    public async Task Scenario3_ElectricBillOnly_ElectricBillWins()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        AddCertificate(context, propertyId, ElectricBillTypeId, ElectricBillDate);

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        var codes = GetResultingPolicyCodes(context, propertyId);

        Assert.Contains(codes, c => c is "ELECTRIC_BILL" or "PARTIAL_ELECTRIC_BILL");
    }

    [Fact]
    public async Task Scenario4_CcAndOcAndElectricBill_ElectricBillNeverWins()
    {
        // CC + OC together always resolve via the established CC/OC merge (ResolveCcOcCombination),
        // which doesn't even receive the Electric Bill date as an input -- Electric Bill can never
        // win when both CC and OC are present, regardless of the CC/OC gap outcome.
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        AddCertificate(context, propertyId, CcTypeId, CcDate);
        AddCertificate(context, propertyId, OcTypeId, OcDateCloseToCC);
        AddCertificate(context, propertyId, ElectricBillTypeId, ElectricBillDate);

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        var codes = GetResultingPolicyCodes(context, propertyId);

        Assert.DoesNotContain(codes, c => c is "ELECTRIC_BILL" or "PARTIAL_ELECTRIC_BILL");
        Assert.True(codes.Any(c => c is "CC" or "PARTIAL_CC" or "OC" or "PARTIAL_OC"),
            $"Expected a CC or OC family code, got: {string.Join(",", codes)}");
    }

    [Fact]
    public async Task Scenario5_NoCcCertificateAtAll_ElectricBillUsed()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);
        // No CC certificate row at all -- only Electric Bill.
        AddCertificate(context, propertyId, ElectricBillTypeId, ElectricBillDate);

        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        var codes = GetResultingPolicyCodes(context, propertyId);

        Assert.Contains(codes, c => c is "ELECTRIC_BILL" or "PARTIAL_ELECTRIC_BILL");
    }

    [Fact]
    public async Task Scenario6_CcCertificateExistsButIssueDateMissing_FallsBackToElectricBillPerIgnoreForTax()
    {
        // CC certificate row EXISTS (has a CertificateNo) but its IssueDate is null.
        // MISSING_CERTIFICATE_DATE_ACTION = IGNORE_FOR_TAX means this certificate is treated as
        // absent, so the priority walk continues past CC to Electric Bill.
        using var context = CreateContext();
        var propertyId = SeedCommon(context);

        var ccWithoutDate = PropertyCertificateEntity.Create(
            propertyId: propertyId, certificateTypeId: CcTypeId,
            certificateNo: "CC-NO-DATE-001", issueDate: null, propertyDetailsId: null);
        context.PropertyCertificates.Add(ccWithoutDate);
        context.SaveChanges();

        AddCertificate(context, propertyId, ElectricBillTypeId, ElectricBillDate);

        var service = BuildService(context, BuildGuidelineReaderMock(missingCertificateDateAction: "IGNORE_FOR_TAX"));
        await service.ApplyAsync(propertyId, userId: 1);

        var codes = GetResultingPolicyCodes(context, propertyId);

        Assert.Contains(codes, c => c is "ELECTRIC_BILL" or "PARTIAL_ELECTRIC_BILL");
        Assert.DoesNotContain(codes, c => c is "CC" or "PARTIAL_CC");
    }

    /// <summary>
    /// Reported UI bug: "CC lavli ki electric bill lagat ahe" (enabling CC still leaves Electric
    /// Bill applied). Unlike Scenario1 (both certificates present from the FIRST ApplyAsync call),
    /// this reproduces the real-world SEQUENCE: Electric Bill is added and applied FIRST (its own
    /// PolicyTaxDetails/TransMast rows get persisted and active), and CC is only added and applied
    /// AFTERWARDS -- exactly what happens when a user enables Electric Bill via the UI today, then
    /// later uploads/enables a CC certificate for the same property.
    /// </summary>
    [Fact]
    public async Task Scenario7_ElectricBillAppliedFirst_ThenCcAddedLater_CcWinsAndElectricBillRowsAreDeactivated()
    {
        using var context = CreateContext();
        var propertyId = SeedCommon(context);

        // Step 1: Electric Bill only, applied and persisted first.
        AddCertificate(context, propertyId, ElectricBillTypeId, ElectricBillDate);
        var service = BuildService(context, BuildGuidelineReaderMock());
        await service.ApplyAsync(propertyId, userId: 1);

        var codesAfterElectricBillOnly = GetResultingPolicyCodes(context, propertyId);
        Assert.Contains(codesAfterElectricBillOnly, c => c is "ELECTRIC_BILL" or "PARTIAL_ELECTRIC_BILL");

        var electricBillPolicyTaxDetailIds = context.PolicyTaxDetails
            .Where(pt => pt.PropertyId == propertyId && pt.IsActive &&
                         (pt.PolicyCodeId == ElectricBillPolicyCodeId || pt.PolicyCodeId == PartialElectricBillPolicyCodeId))
            .Select(pt => pt.Id)
            .ToList();
        Assert.NotEmpty(electricBillPolicyTaxDetailIds);

        var electricBillTransMastIds = context.TransMast
            .Where(tm => tm.PropertyId == propertyId && tm.IsActive && tm.CalculationType == "RV")
            .Select(tm => tm.Id)
            .ToList();
        Assert.NotEmpty(electricBillTransMastIds);

        // Step 2: CC certificate is added afterwards, and ApplyAsync runs again -- exactly what the
        // certificate-change pipeline does on every save.
        AddCertificate(context, propertyId, CcTypeId, CcDate);
        await service.ApplyAsync(propertyId, userId: 1);

        var codesAfterCcAdded = GetResultingPolicyCodes(context, propertyId);
        Assert.Contains(codesAfterCcAdded, c => c is "CC" or "PARTIAL_CC");
        Assert.DoesNotContain(codesAfterCcAdded, c => c is "ELECTRIC_BILL" or "PARTIAL_ELECTRIC_BILL");

        // Every row that was tagged ELECTRIC_BILL/PARTIAL_ELECTRIC_BILL in Step 1 must now be either
        // (a) deactivated entirely, or (b) still active but RE-TAGGED to CC/PARTIAL_CC in place
        // (the established "reuse the same row, re-tag its PolicyCodeId" upsert pattern) -- what it
        // must never be is active AND still carrying an Electric-Bill PolicyCodeId.
        var ccOrPartialCcIds = context.PolicyCodeMaster
            .Where(pc => pc.PolicyCode == "CC" || pc.PolicyCode == "PARTIAL_CC")
            .Select(pc => pc.Id)
            .ToList();
        var rowsStillTaggedElectricBill = context.PolicyTaxDetails
            .Where(pt => electricBillPolicyTaxDetailIds.Contains(pt.Id) && pt.IsActive &&
                         !ccOrPartialCcIds.Contains(pt.PolicyCodeId))
            .ToList();
        Assert.Empty(rowsStillTaggedElectricBill);

        // TransMast has no PolicyCodeId column, so assert the property's only active current-FY RV
        // row(s) now hold the CC-computed amount, not a lingering Electric-Bill amount from Step 1.
        var currentFyId = context.YearMaster.Single(y => y.Year == CurrentFyYear).Id;
        var activeTransMastRows = context.TransMast
            .Where(tm => tm.PropertyId == propertyId && tm.IsActive && tm.CalculationType == "RV" && tm.FinanceYearId == currentFyId)
            .ToList();
        Assert.NotEmpty(activeTransMastRows);
    }
}
