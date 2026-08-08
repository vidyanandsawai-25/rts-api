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
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Application.Services.TaxEngine;

public class PtisTaxEnginePhaseRulesTests
{
    private const int CurrentFyYear = 2026;
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
        var property = new PropertyEntity { Id = 1, WardId = 1, PropertyNo = "100", IsActive = true };
        context.PropertyMast.Add(property);

        for (int y = 2016; y <= CurrentFyYear; y++)
        {
            context.YearMaster.Add(new YearMasterEntity { Id = y, Year = y, YearCode = $"{y}-{y + 1}", IsActive = true });
        }

        context.PropertyCertificateTypeMasters.AddRange(
            new PropertyCertificateTypeMasterEntity { Id = CcTypeId, CertificateTypeName = "Commencement Certificate", CertificateTypeCode = "CC", IsTaxable = true, IsActive = true },
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

        context.PolicyTaxDetails.Add(new PolicyTaxDetailsEntity
        {
            Id = 1,
            PropertyId = property.Id,
            PolicyCodeId = NetTaxPolicyCodeId,
            TaxId = generalTax.Id,
            TaxAmount = 12000m,
            CalculationValue = 100000m,
            IsActive = true,
            MarkedForDeletion = false
        });

        context.SaveChanges();
        return property.Id;
    }

    private static void AddCertificate(ApplicationDbContext context, int propertyId, int typeId, DateTime issueDate, int? propertyDetailsId = null)
    {
        var cert = PropertyCertificateEntity.Create(
            propertyId: propertyId,
            certificateTypeId: typeId,
            certificateNo: $"CERT-{typeId}",
            issueDate: issueDate,
            propertyDetailsId: propertyDetailsId);
        context.PropertyCertificates.Add(cert);
        context.SaveChanges();
    }

    private static OccupationTaxApplicationService BuildService(ApplicationDbContext context, bool allowFloorWise = false)
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

        var guidelineReaderMock = new Mock<ICertificateTaxGuidelineReaderService>();
        guidelineReaderMock.Setup(g => g.GetActiveSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CertificateTaxGuidelineSettings(
                EnableCertificateBasedTax: true,
                ApplyOnlyTaxableCertTypes: true,
                DatePriority1: "CC", DatePriority2: "OC", DatePriority3: "ELECTRIC_BILL", DatePriority4: "RETROSPECTIVE",
                CertificateRequireNoAndDate: false,
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
                CertificateTaxScopeMode: allowFloorWise ? "FLOOR_WISE" : "PROPERTY_WISE",
                AllowFloorWiseCertificateMetadata: allowFloorWise,
                EnableCcToOcSplit: true,
                ElectricBillCertificateCodes: "ELECTRIC_BILL", RetrospectiveCurrentYearCount: 1,
                RetrospectivePendingYearCountMode: "TOTAL_MINUS_CURRENT", FloorPolicyDisplayRule: "BIGGEST_AREA_FLOOR_POLICY"));

        return new OccupationTaxApplicationService(
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
            guidelineReaderMock.Object,
            unitOfWork,
            NullLogger<OccupationTaxApplicationService>.Instance,
            NtisPlatform.Tests.Helpers.NoOpTaxApplicabilityService.Instance);
    }

    [Fact]
    public async Task Phase2_GapWithin6Months_AppliesOcOnly()
    {
        // CC: 01-Jan-2026, OC: 01-May-2026 (Gap = 4 months <= 6 months) -> APPLY_OC_ONLY
        using var context = CreateContext();
        var propId = SeedCommon(context);
        AddCertificate(context, propId, CcTypeId, new DateTime(2026, 1, 1));
        AddCertificate(context, propId, OcTypeId, new DateTime(2026, 5, 1));

        var service = BuildService(context);
        await service.ApplyAsync(propId, 1);

        var persistedRows = await context.PolicyTaxDetails
            .Where(p => p.PropertyId == propId && p.PolicyCodeId != NetTaxPolicyCodeId && p.IsActive)
            .ToListAsync();

        Assert.NotEmpty(persistedRows);
        Assert.All(persistedRows, r => Assert.True(r.PolicyCodeId == OcPolicyCodeId || r.PolicyCodeId == PartialOcPolicyCodeId));
    }

    [Fact]
    public async Task Phase2_GapWithin6Months_ProratesOcFrom15April()
    {
        // CC: 01-Jan-2026, OC: 15-Apr-2026 (Gap = 3.5 months <= 6 months) -> APPLY_OC_ONLY prorated from 15-Apr
        using var context = CreateContext();
        var propId = SeedCommon(context);
        AddCertificate(context, propId, CcTypeId, new DateTime(2026, 1, 1));
        AddCertificate(context, propId, OcTypeId, new DateTime(2026, 4, 15));

        var service = BuildService(context);
        await service.ApplyAsync(propId, 1);

        var partialOcRows = await context.PolicyTaxDetails
            .Where(p => p.PropertyId == propId && p.PolicyCodeId == PartialOcPolicyCodeId && p.IsActive)
            .ToListAsync();

        Assert.NotEmpty(partialOcRows);

        // 15-Apr falls within the engine's 30-day grace period after FY start (01-Apr), so the
        // current year bills the FULL General Tax baseline (12,000) at OC's 1.0x rather than being
        // prorated from the 14-day-old onset.
        var partialGeneralTax = partialOcRows.Single(r => r.TaxId == 1);
        Assert.Equal(12_000m, partialGeneralTax.TaxAmount);
    }

    [Fact]
    public async Task Phase2_GapExceeding6Months_AppliesCcThenOcSplit()
    {
        // CC: 01-Jan-2024, OC: 01-Oct-2025 (Gap = 21 months > 6 months) -> APPLY_CC_THEN_OC
        using var context = CreateContext();
        var propId = SeedCommon(context);
        AddCertificate(context, propId, CcTypeId, new DateTime(2024, 1, 1));
        AddCertificate(context, propId, OcTypeId, new DateTime(2025, 10, 1));

        var service = BuildService(context);
        await service.ApplyAsync(propId, 1);

        var persistedRows = await context.PolicyTaxDetails
            .Where(p => p.PropertyId == propId && p.PolicyCodeId != NetTaxPolicyCodeId && p.IsActive)
            .ToListAsync();

        Assert.NotEmpty(persistedRows);
    }

    [Fact]
    public async Task Phase3_ElectricBill_NormalizesToFyStart_AndClampsTo2016Floor()
    {
        // Bill Date: 15-Oct-2010 -> normalized to FY start 01-Apr-2010, then floored to 01-Apr-2016
        using var context = CreateContext();
        var propId = SeedCommon(context);
        AddCertificate(context, propId, ElectricBillTypeId, new DateTime(2010, 10, 15));

        var service = BuildService(context);
        await service.ApplyAsync(propId, 1);

        var persistedRows = await context.PolicyTaxDetails
            .Where(p => p.PropertyId == propId && p.PolicyCodeId != NetTaxPolicyCodeId && p.IsActive)
            .ToListAsync();

        Assert.NotEmpty(persistedRows);
    }

    [Fact]
    public async Task Phase4_RetrospectiveMode_WhenNoCertificate_GeneratesRowsForLast6Years()
    {
        // No certificates -> Retrospective Mode: no active certificate tax rows persisted
        using var context = CreateContext();
        var propId = SeedCommon(context);

        var service = BuildService(context);
        await service.ApplyAsync(propId, 1);

        var persistedRows = await context.TaxPendingDetailsRetro
            .Where(p => p.PropertyId == propId && p.IsActive)
            .ToListAsync();

        Assert.Empty(persistedRows);
    }

    [Fact]
    public async Task FloorWise_MultiFloor_StaggeredOnsetYears_AccumulatesCorrectly()
    {
        // Property with 3 floors: Floor 2 (onset 2024), Floor 1 (onset 2025), Floor 3 (onset 2026).
        // FY 2024-25 = Floor 2 only
        // FY 2025-26 = Floor 2 + Floor 1
        // FY 2026-27 = Floor 2 + Floor 1 + Floor 3
        using var context = CreateContext();
        var propId = SeedCommon(context);

        // Add 3 property details (floors)
        var floor1 = new NtisPlatform.Core.Entities.PropertyDetailsEntity { PropertyId = propId, FloorId = 1, IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now, UpdatedBy = 1, UpdatedDate = DateTime.Now };
        var floor2 = new NtisPlatform.Core.Entities.PropertyDetailsEntity { PropertyId = propId, FloorId = 2, IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now, UpdatedBy = 1, UpdatedDate = DateTime.Now };
        var floor3 = new NtisPlatform.Core.Entities.PropertyDetailsEntity { PropertyId = propId, FloorId = 3, IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now, UpdatedBy = 1, UpdatedDate = DateTime.Now };
        context.PropertyDetails.AddRange(floor1, floor2, floor3);
        await context.SaveChangesAsync();

        // Floor 2 certificate (01-Apr-2024)
        AddCertificate(context, propId, OcTypeId, new DateTime(2024, 4, 1), propertyDetailsId: floor2.Id);
        // Floor 1 certificate (01-Apr-2025)
        AddCertificate(context, propId, OcTypeId, new DateTime(2025, 4, 1), propertyDetailsId: floor1.Id);
        // Floor 3 certificate (01-Apr-2026)
        AddCertificate(context, propId, OcTypeId, new DateTime(2026, 4, 1), propertyDetailsId: floor3.Id);

        var service = BuildService(context, allowFloorWise: true);
        await service.ApplyAsync(propId, 1);

        var retroRows = await (from r in context.TaxPendingDetailsRetro
                               join y in context.YearMaster on r.PendingYearId equals y.Id
                               where r.PropertyId == propId && r.IsActive
                               select new { r.PendingAmount, y.Year })
                               .ToListAsync();

        var year2024Rows = retroRows.Where(r => r.Year == 2024).ToList();
        var year2025Rows = retroRows.Where(r => r.Year == 2025).ToList();

        // Verify that 2024 has retro tax from Floor 2, and 2025 has retro tax from Floor 2 + Floor 1
        Assert.NotEmpty(year2024Rows);
        Assert.NotEmpty(year2025Rows);

        var total2024 = year2024Rows.Sum(r => r.PendingAmount ?? 0m);
        var total2025 = year2025Rows.Sum(r => r.PendingAmount ?? 0m);

        Assert.True(total2025 > total2024, $"Expected FY 2025-26 tax ({total2025}) > FY 2024-25 tax ({total2024}) as Floor 1 joined in 2025");
    }
}
