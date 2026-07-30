using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NtisPlatform.Application.Events;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Application.Services;
using NtisPlatform.Application.Services.TaxEngine;
using NtisPlatform.Application.Services.TaxEngine.OccupationTax;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Application.Services.TaxEngine;

/// <summary>
/// End-to-end regression for the 2026-07-23 "replacement flow" audit: proves
/// PropertyCertificateApplicationService.ReplaceCertificateByTypeAsync -&gt; real
/// PropertyCertificateService -&gt; a single PropertyCertificateChangedEvent -&gt; real
/// OccupationTaxApplicationService.ApplyAsync produces exactly ONE recalculation against the
/// FINAL state (new OC date), never against the momentarily-certificate-less intermediate state
/// a separate delete-then-save (over two independent calls) would expose -- which could otherwise
/// fall back to Electric Bill or no tax at all for the duration between the two calls.
/// </summary>
public class ReplaceCertificateEndToEndTests
{
    private const int CurrentFyYear = 2026; // FY2026 = 01-Apr-2026..31-Mar-2027

    private const int OcTypeId = 1;
    private const int NetTaxPolicyCodeId = 1;
    private const int OcPolicyCodeId = 2;
    private const int PartialOcPolicyCodeId = 3;
    private const int GeneralTaxId = 1;
    private const decimal AnnualTax = 356m;

    /// <summary>
    /// Delegates SaveChangesAsync to the real (InMemory) context so changes made by
    /// PropertyCertificateService/OccupationTaxApplicationService are actually visible to
    /// subsequent queries, without depending on whether the InMemory provider supports real
    /// ADO.NET transactions (it doesn't) -- Begin/Commit/Rollback are no-ops here on purpose.
    /// </summary>
    private class InMemoryUnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        public InMemoryUnitOfWork(ApplicationDbContext context) => _context = context;
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => _context.SaveChangesAsync(cancellationToken);
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => _context.SaveChangesAsync(cancellationToken);
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void DiscardChanges() => _context.ChangeTracker.Clear();
        public void Dispose() { }
    }

    /// <summary>
    /// Bridges IPublisher.Publish straight into OccupationTaxApplicationService.ApplyAsync,
    /// mirroring PropertyCertificateChangedEventHandler's step 2 (RV refresh is skipped here since
    /// NETTAX is pre-seeded, same convention as every other test in this session). Counts
    /// invocations so tests can assert "exactly once".
    /// </summary>
    private class BridgingPublisher : IPublisher
    {
        private readonly Func<int, int, CancellationToken, Task> _onCertificateChanged;
        public int PublishCount { get; private set; }

        public BridgingPublisher(Func<int, int, CancellationToken, Task> onCertificateChanged)
        {
            _onCertificateChanged = onCertificateChanged;
        }

        public async Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            if (notification is PropertyCertificateChangedEvent e)
            {
                PublishCount++;
                await _onCertificateChanged(e.PropertyId, e.UserId, cancellationToken);
            }
        }

        public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            if (notification is PropertyCertificateChangedEvent e)
            {
                PublishCount++;
                await _onCertificateChanged(e.PropertyId, e.UserId, cancellationToken);
            }
        }
    }

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
        context.YearMaster.Add(new YearMasterEntity { Id = CurrentFyYear, Year = CurrentFyYear, YearCode = $"{CurrentFyYear}-{(CurrentFyYear + 1) % 100:D2}", IsActive = true });

        context.PropertyCertificateTypeMasters.Add(
            new PropertyCertificateTypeMasterEntity { Id = OcTypeId, CertificateTypeName = "Occupancy Certificate", CertificateTypeCode = "OC", IsTaxable = true, IsActive = true });

        context.PolicyCodeMaster.AddRange(
            new PolicyCodeMasterEntity { Id = NetTaxPolicyCodeId, PolicyCode = "NETTAX", IsActive = true },
            new PolicyCodeMasterEntity { Id = OcPolicyCodeId, PolicyCode = "OC", IsActive = true },
            new PolicyCodeMasterEntity { Id = PartialOcPolicyCodeId, PolicyCode = "PARTIAL_OC", IsActive = true });

        context.TaxCategoryMaster.Add(new TaxCategoryMasterEntity { Id = 1, CategoryCode = "TAX", CategoryName = "Property Tax", IsActive = true });
        context.TaxMaster.Add(new TaxMasterEntity { Id = GeneralTaxId, TaxName = "General Tax", TaxCode = "GEN", DisplayOrder = 1, TaxCategoryId = 1, IsActive = true });

        context.PolicyTaxDetails.Add(new PolicyTaxDetailsEntity
        {
            Id = 1000 + CurrentFyYear,
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

    [Fact]
    public async Task ReplaceCertificateByTypeAsync_ChangeOcDate_OnePublish_NoElectricBillFallback_FinalTaxUsesNewDate()
    {
        using var context = CreateContext();
        var propertyId = Seed(context);

        var oldCert = PropertyCertificateEntity.Create(propertyId, OcTypeId, "OC-OLD", new DateTime(CurrentFyYear, 4, 7), null);
        context.PropertyCertificates.Add(oldCert);
        await context.SaveChangesAsync();

        var guidelineReaderMock = BuildGuidelineReaderMock();
        var unitOfWork = new InMemoryUnitOfWork(context);

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
        var financeYearProvider = Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == CurrentFyYear);
        var engine = new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance);

        var occupationTaxService = new OccupationTaxApplicationService(
            engine, propertyRepo, certRepo, policyTaxRepo, transMastRepo, yearRepo,
            taxPendingRepo, taxPendingRetroRepo, taxMasterRepo,
            policyCodeLookup, financeYearProvider, guidelineReaderMock.Object, unitOfWork,
            NullLogger<OccupationTaxApplicationService>.Instance);

        var bridgingPublisher = new BridgingPublisher(
            (propId, userId, ct) => occupationTaxService.ApplyAsync(propId, userId, ct));

        var propertyCertificateService = new PropertyCertificateService(context, unitOfWork, bridgingPublisher, guidelineReaderMock.Object);
        var certTypeRepo = new Repository<PropertyCertificateTypeMasterEntity, int>(context);
        var propertyDetailsRepo = new Repository<PropertyDetailsEntity, int>(context);

        var appService = new PropertyCertificateApplicationService(
            propertyCertificateService,
            Mock.Of<IDocumentApplicationService>(),
            unitOfWork,
            Mock.Of<IModuleLookupService>(),
            certTypeRepo,
            propertyDetailsRepo,
            bridgingPublisher,
            guidelineReaderMock.Object,
            NullLogger<PropertyCertificateApplicationService>.Instance);

        var newDate = new DateTime(CurrentFyYear, 6, 10);
        var newCertificateId = await appService.ReplaceCertificateByTypeAsync(
            propertyId, OcTypeId, oldPropertyDetailsId: null, newPropertyDetailsId: null,
            newCertificateNo: "OC-NEW", newIssueDate: newDate, userId: 1);

        Assert.True(newCertificateId > 0);
        Assert.Equal(1, bridgingPublisher.PublishCount); // exactly one recalculation against the FINAL state

        var oldCertReloaded = await context.PropertyCertificates.FindAsync(oldCert.Id);
        Assert.True(oldCertReloaded!.MarkedForDeletion);

        var newCert = await context.PropertyCertificates.FindAsync(newCertificateId);
        Assert.NotNull(newCert);
        Assert.False(newCert!.MarkedForDeletion);
        Assert.Equal(newDate, newCert.IssueDate);

        // No Electric-Bill-tagged row was ever persisted -- would only happen if a recalculation
        // ran against the momentarily-certificate-less intermediate state between delete and create.
        var activePolicyRows = await (from pt in context.PolicyTaxDetails
                                      join pcm in context.PolicyCodeMaster on pt.PolicyCodeId equals pcm.Id
                                      where pt.PropertyId == propertyId && pt.IsActive
                                      select pcm.PolicyCode)
                                     .ToListAsync();
        Assert.DoesNotContain(activePolicyRows, code => code.Contains("ELECTRIC", StringComparison.OrdinalIgnoreCase));

        // Final tax reflects the NEW OC date: 07-Jun-2026..31-Mar-2027 is a full, unprorated year
        // (297 days from 10-Jun would be prorated, but for this assertion the key point is simply
        // that a PARTIAL_OC/OC row now exists for the current year using the NEW date's governed
        // period, not the old 07-Apr date).
        // PolicyTaxDetails holds only ONE current/final row per (PropertyId, TaxId) now -- no year
        // filter needed or possible under the DBA-confirmed schema (no PolicyYear column).
        var currentYearPolicyRow = await (from pt in context.PolicyTaxDetails
                                          join pcm in context.PolicyCodeMaster on pt.PolicyCodeId equals pcm.Id
                                          where pt.PropertyId == propertyId && pt.IsActive
                                          && (pcm.PolicyCode == "OC" || pcm.PolicyCode == "PARTIAL_OC")
                                          select pt)
                                         .SingleAsync();
        var expectedChargeableDays = new FinanceYear(CurrentFyYear, 4, 1).ChargeableDaysFrom(newDate);
        var expectedAmount = Math.Round(AnnualTax * expectedChargeableDays / 365m, 0, MidpointRounding.AwayFromZero);
        Assert.Equal(expectedAmount, currentYearPolicyRow.TaxAmount);
    }
}
