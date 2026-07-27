using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MockQueryable;
using NtisPlatform.Application.EventHandlers;
using NtisPlatform.Application.Events;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Core.Constants;
using NtisPlatform.Application.Services;
using NtisPlatform.Application.Services.TaxEngine;
using NtisPlatform.Application.Services.TaxEngine.OccupationTax;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NtisPlatform.Tests.Services;

/// <summary>
/// Golden-figure unit tests for the Occupation Tax engine (BR1..BR7) plus the ordering guarantee
/// of the certificate-change pipeline handler (RV refresh strictly before engine).
///
/// Baselines held constant across the suite (CurrentFY 2026 = 01-Apr-2026 .. 31-Mar-2027):
///   - Annual NETTAX = 36,500 split as 21,900 + 3,650 x 4
///   - CC baseline   = 54,750 (NETTAX x 1.5)
///   - Floor annual  = 18,250 (half NETTAX)
/// Every asserted amount below is an approved golden figure; none are invented.
/// </summary>
public class OccupationTaxEngineTests
{
    // ----- Approved baseline constants -----
    private const decimal AnnualNetTax = 36_500m;
    private const decimal GeneralTaxPortion = 21_900m;   // 21,900
    private const decimal ComponentEach = 3_650m;        // 3,650 x 4
    private const int ComponentCount = 4;

    private static readonly FinanceYear CurrentFy = new(2026); // 01-Apr-2026 .. 31-Mar-2027

    private readonly OccupationTaxEngine _engine =
        new(NullLogger<OccupationTaxEngine>.Instance);

    private static OccupationTaxOptions Options(DateTime? retroCutoff = null) => new()
    {
        AnnualNetTax = AnnualNetTax,
        GeneralTaxPortion = GeneralTaxPortion,
        ComponentCount = ComponentCount,
        CompletionCertificateMultiplier = 1.5m,
        FloorDivisor = 2,
        DefaultRetroLookbackYears = 6,
        RetroCutoffDate = retroCutoff,
    };

    // =========================================================================================
    // 1. BR1 - Prorated onset year (OC 15-Nov-2026) over 137 days within current FY.
    //    NOTE: CCâ†’OC timeline split (when both CC and OC exist) is NOT YET IMPLEMENTED.
    //    This test uses OC only, which IS implemented.
    // =========================================================================================
    [Fact]
    public void BR1_ProratedOnsetYear_OC15Nov2026_Returns13700()
    {
        // Arrange: OC present, within current FY -> prorated from OC date to end of FY.
        var input = new OccupationTaxInput
        {
            PropertyId = 101,
            OccupationCertificateDate = new DateTime(2026, 11, 15),  // OC only (CCâ†’OC not impl)
            Options = Options(),
        };

        // Act
        var result = _engine.Compute(input, CurrentFy);

        // Assert: current FY is prorated from 15-Nov-2026 to 31-Mar-2027 = 137 days.
        result.IsValid.Should().BeTrue();
        result.Condition.Should().Be(OccupationCondition.OccupationCertificate);

        var current = result.CurrentYear!;
        current.IsProrated.Should().BeTrue();
        current.ChargeableDays.Should().Be(137);
        current.GeneralTax.Should().Be(8_220m);       // 21,900 x 137/365
        current.ComponentTax.Should().Be(1_370m);      // 3,650 x 137/365
        current.NetTax.Should().Be(13_700m);           // 8,220 + 4 x 1,370 â€” GOLDEN FIGURE
        result.RetroYears.Should().BeEmpty();
    }

    // =========================================================================================
    // 2. BR2 - Only OC present -> OC condition applied throughout.
    // =========================================================================================
    [Fact]
    public void BR2_OnlyOC_AppliesOCThroughout()
    {
        var input = new OccupationTaxInput
        {
            PropertyId = 102,
            OccupationCertificateDate = new DateTime(2026, 4, 1), // FY start -> full year
            CompletionCertificateDate = null,
            ElectricityBillDate = null,
            Options = Options(),
        };

        var result = _engine.Compute(input, CurrentFy);

        result.IsValid.Should().BeTrue();
        result.Condition.Should().Be(OccupationCondition.OccupationCertificate);
        result.CurrentYear!.NetTax.Should().Be(AnnualNetTax); // full annual, not prorated
        result.CurrentYear!.IsProrated.Should().BeFalse();
    }

    // =========================================================================================
    // 3. BR2 - Only CC present -> CC condition applied throughout.
    // =========================================================================================
    [Fact]
    public void BR2_OnlyCC_AppliesCCThroughout()
    {
        var input = new OccupationTaxInput
        {
            PropertyId = 103,
            OccupationCertificateDate = null,
            CompletionCertificateDate = new DateTime(2026, 5, 10),
            ElectricityBillDate = null,
            Options = Options(),
        };

        var result = _engine.Compute(input, CurrentFy);

        result.IsValid.Should().BeTrue();
        result.Condition.Should().Be(OccupationCondition.CompletionCertificate);
        // CC baseline = NETTAX x 1.5 = 54,750.
        input.Options.CompletionCertificateBaseline.Should().Be(54_750m);
    }

    // =========================================================================================
    // 4. BR2 - Neither OC nor CC -> Electricity Bill condition; EleBillDt normalized to FY start.
    // =========================================================================================
    [Fact]
    public void BR2_Neither_AppliesElectricityBill()
    {
        var input = new OccupationTaxInput
        {
            PropertyId = 104,
            OccupationCertificateDate = null,
            CompletionCertificateDate = null,
            ElectricityBillDate = new DateTime(2026, 9, 12), // mid-year bill
            Options = Options(),
        };

        var result = _engine.Compute(input, CurrentFy);

        result.IsValid.Should().BeTrue();
        result.Condition.Should().Be(OccupationCondition.ElectricityBill);
        // Bill date is normalized to the finance-year start, so the whole current FY is charged.
        result.CurrentYear!.FinanceYearStart.Should().Be(new DateTime(2026, 4, 1));
        result.CurrentYear!.IsProrated.Should().BeFalse();
        result.CurrentYear!.NetTax.Should().Be(AnnualNetTax);
    }

    // =========================================================================================
    // 5. BR4/BR7 - Retro years with leap add-back; OC 20-Aug-2023.
    //    FY2023 partial 22,500 + FY2024 full leap 36,600 + FY2025 full 36,500 = roll-up 95,600.
    //    CurrentFY (2026) TransMast = FULL annual 36,500.
    // =========================================================================================
    [Fact]
    public void BR4BR7_RetroYears_WithLeapAddback()
    {
        var input = new OccupationTaxInput
        {
            PropertyId = 105,
            OccupationCertificateDate = new DateTime(2023, 8, 20),
            Options = Options(),
        };

        var result = _engine.Compute(input, CurrentFy);

        result.IsValid.Should().BeTrue();
        result.Condition.Should().Be(OccupationCondition.OccupationCertificate);

        // Retro window: FY2023 (partial from OC), FY2024, FY2025. Oldest first.
        result.RetroYears.Should().HaveCount(3);

        var fy2023 = result.RetroYears[0];
        fy2023.FinanceYear.Should().Be(2023);
        fy2023.IsProrated.Should().BeTrue();
        fy2023.ChargeableDays.Should().Be(225);   // 20-Aug-2023 .. 31-Mar-2024 inclusive
        fy2023.NetTax.Should().Be(22_500m);        // prorated on 365-day basis

        var fy2024 = result.RetroYears[1];
        fy2024.FinanceYear.Should().Be(2024);
        fy2024.LeapAddbackApplied.Should().BeTrue();
        fy2024.NetTax.Should().Be(36_600m);        // full year + leap add-back (+100)

        var fy2025 = result.RetroYears[2];
        fy2025.FinanceYear.Should().Be(2025);
        fy2025.LeapAddbackApplied.Should().BeFalse();
        fy2025.NetTax.Should().Be(36_500m);        // plain full year

        // Roll-up = 22,500 + 36,600 + 36,500 = 95,600.
        result.RetroRollUp.Should().Be(95_600m);

        // CurrentFY TransMast is the FULL annual amount, applied separately from the retro roll-up.
        result.CurrentYear!.FinanceYear.Should().Be(2026);
        result.CurrentYear!.IsProrated.Should().BeFalse();
        result.CurrentYear!.NetTax.Should().Be(36_500m);
    }

    // =========================================================================================
    // 6. BR4 - Config-driven cut-off.
    //    EleBillDt 10-Feb-2015, RetroCutoffDate ABSENT -> default 6-year cap -> retro FY2020..FY2025,
    //    total 2,19,200 (incl. FY2020 & FY2024 leap add-backs).
    //    With RetroCutoffDate = 2016-04-01 -> retro FY2016..FY2025.
    // =========================================================================================
    [Fact]
    public void BR4_CutoffDate_ConfigDriven()
    {
        // --- Case A: no configured cut-off -> default 6-year look-back cap. ---
        var noCutoff = new OccupationTaxInput
        {
            PropertyId = 106,
            ElectricityBillDate = new DateTime(2015, 2, 10), // far older than the default cap
            Options = Options(retroCutoff: null),
        };

        var resultA = _engine.Compute(noCutoff, CurrentFy);

        resultA.IsValid.Should().BeTrue();
        resultA.Condition.Should().Be(OccupationCondition.ElectricityBill);

        // Default cut-off caps the TOTAL span (retro + current) to 6 years: floor = CurrentFY -
        // (6-1) = FY2021, spanning FY2021..FY2025 (5 retro years) + FY2026 (current).
        resultA.RetroYears.Select(y => y.FinanceYear)
            .Should().Equal(2021, 2022, 2023, 2024, 2025);

        // FY2024 is a leap finance year (start year 2024) -> +100.
        resultA.RetroYears.Single(y => y.FinanceYear == 2024).LeapAddbackApplied.Should().BeTrue();

        // Total across the retro window = 1,82,600.
        resultA.RetroRollUp.Should().Be(182_600m);

        // --- Case B: explicit RetroCutoffDate = 2016-04-01 overrides the default cap. ---
        var withCutoff = new OccupationTaxInput
        {
            PropertyId = 106,
            ElectricityBillDate = new DateTime(2015, 2, 10),
            Options = Options(retroCutoff: new DateTime(2016, 4, 1)),
        };

        var resultB = _engine.Compute(withCutoff, CurrentFy);

        resultB.IsValid.Should().BeTrue();
        // Retro now spans FY2016..FY2025 (cut-off wins over the 6-year default).
        resultB.RetroYears.Select(y => y.FinanceYear)
            .Should().Equal(2016, 2017, 2018, 2019, 2020, 2021, 2022, 2023, 2024, 2025);
    }

    // =========================================================================================
    // 7. BR6 - Precondition: zero NETTAX is rejected; result invalid, no writes performed.
    // =========================================================================================
    [Fact]
    public void BR6_Precondition_ZeroNetTax_Rejected()
    {
        var input = new OccupationTaxInput
        {
            PropertyId = 107,
            OccupationCertificateDate = new DateTime(2026, 11, 15),
            Options = new OccupationTaxOptions
            {
                AnnualNetTax = 0m,                 // precondition failure
                GeneralTaxPortion = 0m,
                ComponentCount = ComponentCount,
            },
        };

        var result = _engine.Compute(input, CurrentFy);

        result.IsValid.Should().BeFalse();
        result.RejectionReason.Should().NotBeNullOrEmpty();
        result.CurrentYear.Should().BeNull();      // nothing to write
        result.RetroYears.Should().BeEmpty();
    }

    // =========================================================================================
    // 8. BR6 - Precondition: no certificate date at all is rejected; result invalid, no writes.
    // =========================================================================================
    [Fact]
    public void BR6_Precondition_NoCertificateDate_Rejected()
    {
        var input = new OccupationTaxInput
        {
            PropertyId = 108,
            OccupationCertificateDate = null,
            CompletionCertificateDate = null,
            ElectricityBillDate = null,            // all dates NULL -> precondition failure
            Options = Options(),
        };

        var result = _engine.Compute(input, CurrentFy);

        result.IsValid.Should().BeFalse();
        result.RejectionReason.Should().NotBeNullOrEmpty();
        result.CurrentYear.Should().BeNull();
        result.RetroYears.Should().BeEmpty();
    }

    // =========================================================================================
    // Pipeline ordering guarantee: the certificate-change handler MUST refresh Rateable Value
    // (which recomputes NETTAX) BEFORE invoking the Occupation Tax engine/service.
    //
    // Note on tooling: the project references Moq 4.20.72 and NOT the separate Moq.Sequences
    // package, so this proof uses Moq's built-in MockSequence (the idiomatic ordering primitive
    // in Moq 4.x). Both dependencies are pinned to the same MockSequence and each is verified to
    // occur exactly once (Times.Once); if the engine were called before RV, the sequence would
    // throw on the out-of-order invocation.
    // =========================================================================================
    [Fact]
    public async Task Pipeline_RefreshesRateableValue_BeforeApplyingOccupationTax()
    {
        // Arrange
        var sequence = new MockSequence();
        var rvClient = new Mock<IRateableValueApiClient>(MockBehavior.Strict);
        var taxService = new Mock<IOccupationTaxService>(MockBehavior.Strict);

        const int propertyId = 200;
        const int userId = 42;

        // STEP 1 first: RV refresh.
        rvClient.InSequence(sequence)
            .Setup(c => c.RecalculateAsync(propertyId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // STEP 2 second: Occupation Tax application.
        taxService.InSequence(sequence)
            .Setup(s => s.ApplyAsync(propertyId, userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new PropertyCertificateChangedEventHandler(
            rvClient.Object, taxService.Object,
            NullLogger<PropertyCertificateChangedEventHandler>.Instance);

        // Act
        await handler.Handle(new PropertyCertificateChangedEvent(propertyId, userId), default);

        // Assert: RV refresh occurs exactly once, engine application occurs exactly once, and the
        // strict MockSequence guarantees RV happened before the engine.
        rvClient.Verify(
            c => c.RecalculateAsync(propertyId, It.IsAny<CancellationToken>()), Times.Once());
        taxService.Verify(
            s => s.ApplyAsync(propertyId, userId, It.IsAny<CancellationToken>()), Times.Once());
    }

    // =========================================================================================
    // Shared helpers: replace the previous ad-hoc PolicyConfiguration-key mocks with a
    // CertificateTaxGuideline row (matching the previously-hardcoded 1.5x CC multiplier / 6-year
    // look-back / OC-CC-ELECTRIC_BILL-RETROSPECTIVE priority order) plus PolicyCodeMaster and
    // PropertyPolicyStage repository mocks the rewritten service now depends on.
    // =========================================================================================
    private static CertificateTaxGuidelineSettings DefaultGuideline(string noDateRule = "DEFAULT_RETROSPECTIVE") => new(
        EnableCertificateBasedTax: true,
        ApplyOnlyTaxableCertTypes: true,
        DatePriority1: "CC",
        DatePriority2: "OC",
        DatePriority3: "ELECTRIC_BILL",
        DatePriority4: "RETROSPECTIVE",
        // Existing certificate fixtures across this suite don't populate CertificateNo/IssueDate
        // as separate fields â€” keep the validation gate off by default so golden tests are
        // unaffected; dedicated tests turn this on to exercise the gate itself.
        CertificateRequireNoAndDate: false,
        MissingCertificateNoAction: "IGNORE_FOR_TAX",
        MissingCertificateDateAction: "IGNORE_FOR_TAX",
        IgnoreCcToOcWithinValue: 6,
        IgnoreCcToOcWithinType: "MONTHS",
        CcOcGapComparison: "LESS_THAN_OR_EQUAL",
        CcOcGapWithinAction: "APPLY_OC_ONLY",
        CcOcGapExceededAction: "APPLY_CC_THEN_OC",
        InvalidCcOcDateOrderAction: "USE_PRIORITY_AND_LOG",
        CcOnlyAction: "APPLY_FROM_CC_DATE",
        OcOnlyAction: "APPLY_FROM_OC_DATE",
        FinancialYearStartMonth: 4,
        FinancialYearStartDay: 1,
        // 1.0 (no-op) by default, matching the real Thane seed -- CC_PERIOD_MULTIPLIER scaling is
        // exercised by a dedicated test (RulesEngine_CcPeriodMultiplier_*) via `with { ... }`,
        // rather than silently scaling every other CC-involving golden test in this suite.
        CCPeriodMultiplier: 1.0m,
        OCPeriodMultiplier: 1.0m,
        ElectricBillDateRule: "FROM_FY_START",
        ElectricBillAddMonths: 0,
        ElectricBillMultiplier: 1.0m,
        ElectricBillMinimumFinancialYear: 2016,
        EnableRetrospectiveTax: true,
        NoDateRule: noDateRule,
        LookbackYears: 6,
        DefaultRetrospectiveMultiplier: 1.0m,
        MinimumBackdateFinancialYear: 0,
        EnableCurrentYearProration: true,
        ProrationMethod: "DAILY",
        CurrentYearProrationStartRule: "EXACT_DATE",
        TaxPersistenceMode: "PROPERTY_AGGREGATED",
        SaveInPolicyTaxDetails: true,
        SaveInTransMast: true,
        DoNotUpdateNettax: true,
        RecalculateOnSave: true,
        RecalculateOnDelete: true,
        GuidelineChangeApplyMode: "NEXT_CALCULATION",
        CcPartialPolicyCode: "PARTIAL_CC",
        CcFullPolicyCode: "CC",
        OcPartialPolicyCode: "PARTIAL_OC",
        OcFullPolicyCode: "OC",
        ElectricBillPartialPolicyCode: "PARTIAL_ELECTRIC_BILL",
        ElectricBillFullPolicyCode: "ELECTRIC_BILL",
        // Defaults chosen to reproduce every existing golden-figure test's behavior unchanged --
        // dedicated RulesEngine_* tests flip each of these via `with { ... }` to prove the guideline
        // actually drives behavior.
        CertificateTaxScopeMode: "FLOOR_WISE",
        AllowFloorWiseCertificateMetadata: true,
        EnableCcToOcSplit: true,
        ElectricBillCertificateCodes: "ELECTRIC_BILL",
        RetrospectiveCurrentYearCount: 1,
        RetrospectivePendingYearCountMode: "TOTAL_MINUS_CURRENT",
        FloorPolicyDisplayRule: "BIGGEST_AREA_FLOOR_POLICY");

    private static Mock<ICertificateTaxGuidelineReaderService> GuidelineService(CertificateTaxGuidelineSettings? guideline = null)
    {
        var mock = new Mock<ICertificateTaxGuidelineReaderService>();
        mock.Setup(s => s.GetActiveSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(guideline ?? DefaultGuideline());
        return mock;
    }

    private static readonly Dictionary<string, int> PolicyCodeIds = new()
    {
        ["OC"] = 1,
        ["PARTIAL_OC"] = 2,
        ["CC"] = 3,
        ["PARTIAL_CC"] = 4,
        ["ELECTRIC_BILL"] = 5,
        ["PARTIAL_ELECTRIC_BILL"] = 6,
        ["NETTAX"] = 7,
        // Test-only stand-in policy code, used to prove a *_POLICY_CODE guideline setting is
        // actually honored rather than a hardcoded default sneaking through.
        ["CUSTOM_POLICY_CODE"] = 8,
    };

    private static Mock<IPolicyCodeLookupService> PolicyCodeLookup()
    {
        var mock = new Mock<IPolicyCodeLookupService>();
        mock.Setup(s => s.GetIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string code, CancellationToken _) => PolicyCodeIds[code]);
        mock.Setup(s => s.GetIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> codes, CancellationToken _) => codes.ToDictionary(c => c, c => PolicyCodeIds[c]));
        mock.Setup(s => s.GetExistingIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> codes, CancellationToken _) => codes
                .Where(c => PolicyCodeIds.ContainsKey(c))
                .ToDictionary(c => c, c => PolicyCodeIds[c]));
        return mock;
    }

    /// <summary>
    /// Empty-by-default TaxPendingDetails repo -- SaveTaxesAsync unconditionally queries this table
    /// (retro years only) regardless of any guideline toggle, so every test that reaches
    /// SaveTaxesAsync needs a non-null GetQueryable() result even when it doesn't care about
    /// pending-tax rows specifically.
    /// </summary>
    private static IRepository<TaxPendingDetailsEntity, int> EmptyTaxPendingRepo()
    {
        var mock = new Mock<IRepository<TaxPendingDetailsEntity, int>>();
        mock.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsEntity>().BuildMock());
        return mock.Object;
    }

    /// <summary>Empty-by-default TaxPendingDetailsRetro repo -- see EmptyTaxPendingRepo.</summary>
    private static IRepository<TaxPendingDetailsRetroEntity, int> EmptyTaxPendingRetroRepo()
    {
        var mock = new Mock<IRepository<TaxPendingDetailsRetroEntity, int>>();
        mock.Setup(r => r.GetQueryable()).Returns(new List<TaxPendingDetailsRetroEntity>().BuildMock());
        return mock.Object;
    }

    private static PropertyCertificateEntity BuildCertificate(
        int propertyId, DateTime issueDate, string certificateTypeName, int? propertyDetailsId = null, string? certificateTypeCode = null)
    {
        var cert = PropertyCertificateEntity.Create(
            propertyId: propertyId,
            certificateTypeId: 1,
            issueDate: issueDate,
            propertyDetailsId: propertyDetailsId);

        var certificateTypeProperty = typeof(PropertyCertificateEntity).GetProperty(nameof(PropertyCertificateEntity.CertificateType));
        certificateTypeProperty!.SetValue(cert, new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = certificateTypeName,
            CertificateTypeCode = certificateTypeCode ?? string.Empty
        });

        return cert;
    }

    // =========================================================================================
    // 9. T4 - Floor-wise certificate OVERRIDES the property-wise certificate for that specific
    //    floor; the property-wise certificate is the FALLBACK for floors with no floor-wise
    //    certificate of their own. NETTAX (36,500 = 21,900 General + 4x3,650 components) is split
    //    evenly across the property's 2 floors before running the engine per floor, then summed
    //    back to one TransMast write.
    //
    //    Floor A (5001): floor-wise OC dated 15-May-2026 (override) -> prorated 321 days
    //      (same onset date/day-count as the already-verified T6 test: General 19,260 / Component
    //      3,210 for the WHOLE property). Per-floor (half): General 9,630 / Component 1,605.
    //    Floor B (5002): no floor-wise certificate -> falls back to the property-wise OC
    //      dated 01-Apr-2020 (an old FY start, full year, no proration) -> per-floor full year:
    //      General 10,950 / Component 1,825.
    //    Aggregate CurrentYear: General 20,580 (9,630+10,950), Component 3,430 (1,605+1,825).
    // =========================================================================================
    [Fact]
    public async Task T4_FloorWise_OverridesPropertyWise_ForThatFloor_AndFallsBackForOthers()
    {
        const int propertyId = 109;

        var floors = new List<PropertyDetailsEntity>
        {
            new() { Id = 5001, PropertyId = propertyId },   // Floor A - has its own floor-wise certificate
            new() { Id = 5002, PropertyId = propertyId },   // Floor B - no floor-wise certificate, falls back
        };

        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(floors);

        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        var mockUow = new Mock<IUnitOfWork>();

        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);

        // Fallback certificate dated exactly at the current FY start: full year, no proration,
        // no retro years (onset FY == current FY) â€” keeps the aggregate deterministic.
        var propertyWiseCert = BuildCertificate(propertyId, new DateTime(2026, 4, 1), "Occupancy Certificate");
        var floorACert = BuildCertificate(propertyId, new DateTime(2026, 5, 15), "Occupancy Certificate", propertyDetailsId: 5001);

        var certs = new List<PropertyCertificateEntity> { propertyWiseCert, floorACert };
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(certs.BuildMock());

        var taxes = new List<PolicyTaxDetailsEntity>
        {
            new()
            {
                PropertyId = propertyId,
                PolicyCodeId = PolicyCodeIds["NETTAX"],
                IsActive = true,
                TaxId = 1,
                TaxAmount = 21900m,
                TaxMaster = new TaxMasterEntity { Id = 1, TaxName = "GeneralTax", TaxCode = "GeneralTax" }
            },
            new()
            {
                PropertyId = propertyId,
                PolicyCodeId = PolicyCodeIds["NETTAX"],
                IsActive = true,
                TaxId = 2,
                TaxAmount = 3650m,
                TaxMaster = new TaxMasterEntity { Id = 2, TaxName = "Component1" }
            },
            new()
            {
                PropertyId = propertyId,
                PolicyCodeId = PolicyCodeIds["NETTAX"],
                IsActive = true,
                TaxId = 3,
                TaxAmount = 3650m,
                TaxMaster = new TaxMasterEntity { Id = 3, TaxName = "Component2" }
            },
            new()
            {
                PropertyId = propertyId,
                PolicyCodeId = PolicyCodeIds["NETTAX"],
                IsActive = true,
                TaxId = 4,
                TaxAmount = 3650m,
                TaxMaster = new TaxMasterEntity { Id = 4, TaxName = "Component3" }
            },
            new()
            {
                PropertyId = propertyId,
                PolicyCodeId = PolicyCodeIds["NETTAX"],
                IsActive = true,
                TaxId = 5,
                TaxAmount = 3650m,
                TaxMaster = new TaxMasterEntity { Id = 5, TaxName = "Component4" }
            }
        };
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(taxes.BuildMock());

        var years = new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } };
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(years.BuildMock());

        var savedTrans = new List<TransMastEntity>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        mockTransRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TransMastEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TransMastEntity>, CancellationToken>((entities, _) => savedTrans.AddRange(entities))
            .Returns(Task.CompletedTask);

        var service = new OccupationTaxApplicationService(
            _engine,
            repo.Object,
            mockCertRepo.Object,
            mockPolicyRepo.Object,
            mockTransRepo.Object,
            mockYearRepo.Object,
            EmptyTaxPendingRepo(),
            EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object,
            mockFyProvider.Object,
            GuidelineService().Object,
            mockUow.Object,
            NullLogger<OccupationTaxApplicationService>.Instance);

        // Act
        await service.ApplyAsync(propertyId, userId: 1);

        // Assert: aggregate of Floor A (prorated, override) + Floor B (full year, fallback).
        savedTrans.Should().HaveCount(5); // 1 general + 4 components
        var genTax = savedTrans.Single(t => t.TaxId == 1);
        genTax.TaxAmount.Should().Be(20_580m); // 9,630 (Floor A prorated half) + 10,950 (Floor B full-year half)

        var componentTaxes = savedTrans.Where(t => t.TaxId is 2 or 3 or 4 or 5).ToList();
        componentTaxes.Should().HaveCount(4);
        componentTaxes.Should().OnlyContain(t => t.TaxAmount == 3_430m); // 1,605 (Floor A) + 1,825 (Floor B)

        repo.Verify(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()), Times.Once());
    }

    // =========================================================================================
    // T4g - Regression test for a real production bug: a property-wise CC certificate plus a
    // floor-wise OC certificate on that SAME floor used to silently discard the property-wise CC
    // entirely, because the floor-wise/property-wise swap was all-or-nothing PER FLOOR rather than
    // per certificate TYPE -- the floor's own floor-wise OC certificate replaced the ENTIRE
    // property-wise certificate list for that floor, including CC, instead of only overriding the
    // property-wise OC (of which there was none here). CC governs a property unless a floor-wise
    // CC overrides it for a specific floor; a floor-wise OC on its own must not erase that. Fixed
    // by concatenating floor-wise certs before property-wise ones so ExtractDates's
    // first-match-wins per type still lets the floor's own OC win, while CC (present only
    // property-wise) falls through untouched -- producing the correct CC-then-OC split for this
    // floor (CC 1.5x up to the day before OC's date, OC 1x from OC's date onward).
    // =========================================================================================
    [Fact]
    public async Task T4g_FloorWiseOcOverridesOnlyOcType_PropertyWiseCcStillAppliesToThatFloor()
    {
        const int propertyId = 620;

        var floors = new List<PropertyDetailsEntity>
        {
            new() { Id = 9001, PropertyId = propertyId },
        };

        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(floors);

        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        var mockUow = new Mock<IUnitOfWork>();

        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);

        // CC is property-wide (no floor override of its own); OC is floor-wise, scoped only to
        // this single floor.
        var ccDate = new DateTime(2026, 4, 7);
        var ocDate = new DateTime(2026, 6, 7);
        var propertyWiseCcCert = BuildCertificate(propertyId, ccDate, "Completion Certificate", certificateTypeCode: "CC");
        var floorOcCert = BuildCertificate(propertyId, ocDate, "Occupancy Certificate", propertyDetailsId: 9001, certificateTypeCode: "OC");

        var certs = new List<PropertyCertificateEntity> { propertyWiseCcCert, floorOcCert };
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(certs.BuildMock());

        var taxes = new List<PolicyTaxDetailsEntity>
        {
            new() { PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["NETTAX"], IsActive = true, TaxId = 1, TaxAmount = 21900m, TaxMaster = new TaxMasterEntity { Id = 1, TaxName = "GeneralTax", TaxCode = "GeneralTax" } },
            new() { PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["NETTAX"], IsActive = true, TaxId = 2, TaxAmount = 3650m, TaxMaster = new TaxMasterEntity { Id = 2, TaxName = "Component1" } },
            new() { PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["NETTAX"], IsActive = true, TaxId = 3, TaxAmount = 3650m, TaxMaster = new TaxMasterEntity { Id = 3, TaxName = "Component2" } },
            new() { PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["NETTAX"], IsActive = true, TaxId = 4, TaxAmount = 3650m, TaxMaster = new TaxMasterEntity { Id = 4, TaxName = "Component3" } },
            new() { PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["NETTAX"], IsActive = true, TaxId = 5, TaxAmount = 3650m, TaxMaster = new TaxMasterEntity { Id = 5, TaxName = "Component4" } }
        };
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(taxes.BuildMock());

        var years = new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } };
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(years.BuildMock());

        var savedTrans = new List<TransMastEntity>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        mockTransRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TransMastEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TransMastEntity>, CancellationToken>((entities, _) => savedTrans.AddRange(entities))
            .Returns(Task.CompletedTask);

        // Matches the real production guideline: CC_PERIOD_MULTIPLIER=1.5, and a within-threshold
        // CC/OC gap (61 days here, well under the 6-month default) still applies the CC-then-OC
        // split rather than discarding CC outright.
        var guideline = DefaultGuideline() with
        {
            CCPeriodMultiplier = 1.5m,
            CcOcGapWithinAction = "APPLY_CC_THEN_OC",
        };

        var service = new OccupationTaxApplicationService(
            _engine,
            repo.Object,
            mockCertRepo.Object,
            mockPolicyRepo.Object,
            mockTransRepo.Object,
            mockYearRepo.Object,
            EmptyTaxPendingRepo(),
            EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object,
            mockFyProvider.Object,
            GuidelineService(guideline).Object,
            mockUow.Object,
            NullLogger<OccupationTaxApplicationService>.Instance);

        await service.ApplyAsync(propertyId, userId: 1);

        // Single floor -> perFloorOptions equal the full property options (divided by 1), so the
        // math is identical to a pure property-wide CC-then-OC split.
        var fy = new FinanceYear(2026, 4, 1);
        var ccDays = (ocDate - ccDate).Days; // 61
        var ocDays = fy.ChargeableDaysFrom(ocDate);
        var ccGeneral = Math.Round(21_900m * ccDays / 365m, 0, MidpointRounding.AwayFromZero) * 1.5m;
        var ocGeneral = Math.Round(21_900m * ocDays / 365m, 0, MidpointRounding.AwayFromZero) * 1.0m;
        var expectedGeneral = ccGeneral + ocGeneral;

        savedTrans.Should().HaveCount(5);
        var genTax = savedTrans.Single(t => t.TaxId == 1);
        genTax.TaxAmount.Should().Be(expectedGeneral); // NOT ocGeneral alone -- proves CC still contributed
    }

    [Fact]
    public async Task T4b_NoFloorWiseCertificates_UsesPropertyWiseScope_Unchanged()
    {
        // A property with floors but NO floor-wise certificates must behave exactly like the
        // pre-floor-wise property-scope computation (backward compatible golden figures).
        const int propertyId = 200;

        var floors = new List<PropertyDetailsEntity>
        {
            new() { Id = 6001, PropertyId = propertyId },
        };

        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(floors);

        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        var mockUow = new Mock<IUnitOfWork>();

        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);

        var cert = BuildCertificate(propertyId, new DateTime(2026, 5, 15), "Occupancy Certificate");
        var certs = new List<PropertyCertificateEntity> { cert };
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(certs.BuildMock());

        var taxes = new List<PolicyTaxDetailsEntity>
        {
            new()
            {
                PropertyId = propertyId,
                PolicyCodeId = PolicyCodeIds["NETTAX"],
                IsActive = true,
                TaxId = 1,
                TaxAmount = 36500m,
                TaxMaster = new TaxMasterEntity
                {
                    Id = 1,
                    TaxName = "GeneralTax",
                    TaxCode = "GeneralTax"
                }
            }
        };
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(taxes.BuildMock());

        var years = new List<YearMasterEntity>
        {
            new() { Year = 2026, Id = 10 }
        };
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(years.BuildMock());

        var transMasts = new List<TransMastEntity>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(transMasts.BuildMock());

        var mockEngine = new Mock<IOccupationTaxEngine>();
        mockEngine.Setup(e => e.Compute(It.IsAny<OccupationTaxInput>(), It.IsAny<FinanceYear>()))
            .Returns(new OccupationTaxResult
            {
                IsValid = true,
                CurrentYear = new OccupationTaxYearResult
                {
                    FinanceYear = 2026,
                    GeneralTax = 8220m,
                    ComponentTax = 1370m,
                    ComponentCount = 4
                },
                RetroYears = new List<OccupationTaxYearResult>()
            });

        var service = new OccupationTaxApplicationService(
            mockEngine.Object,
            repo.Object,
            mockCertRepo.Object,
            mockPolicyRepo.Object,
            mockTransRepo.Object,
            mockYearRepo.Object,
            EmptyTaxPendingRepo(),
            EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object,
            mockFyProvider.Object,
            GuidelineService().Object,
            mockUow.Object,
            NullLogger<OccupationTaxApplicationService>.Instance);

        // Should complete without throwing, and compute at property scope (no per-floor split
        // since no floor-wise certificates exist at all).
        await service.ApplyAsync(propertyId, userId: 1);

        mockEngine.Verify(e => e.Compute(
            It.Is<OccupationTaxInput>(i => i.Options.AnnualNetTax == 36500m),
            It.IsAny<FinanceYear>()), Times.Once());
    }

    [Fact]
    public async Task ComputeAsync_NonDefaultGuidelineFinanceYearStart_FlowsThroughToEngineCall()
    {
        // CertificateTaxGuideline.FinancialYearStartMonth/Day is admin-configurable and must
        // actually reach the engine's FinanceYear argument, not just sit unused in the DB row.
        const int propertyId = 201;

        var repo = new Mock<IPropertyRepository>();
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        var cert = BuildCertificate(propertyId, new DateTime(2026, 5, 15), "Occupancy Certificate");
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity> { cert }.BuildMock());

        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var taxes = new List<PolicyTaxDetailsEntity>
        {
            new() { PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["NETTAX"], IsActive = true, TaxId = 1, TaxAmount = 36500m, TaxMaster = new TaxMasterEntity { Id = 1, TaxName = "GeneralTax", TaxCode = "GeneralTax" } }
        };
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(taxes.BuildMock());

        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());

        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());

        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var guideline = DefaultGuideline() with { FinancialYearStartMonth = 7, FinancialYearStartDay = 1 };

        FinanceYear? capturedFy = null;
        var mockEngine = new Mock<IOccupationTaxEngine>();
        mockEngine.Setup(e => e.Compute(It.IsAny<OccupationTaxInput>(), It.IsAny<FinanceYear>()))
            .Callback<OccupationTaxInput, FinanceYear>((_, fy) => capturedFy = fy)
            .Returns(new OccupationTaxResult
            {
                IsValid = true,
                CurrentYear = new OccupationTaxYearResult { FinanceYear = 2026, GeneralTax = 21900m, ComponentTax = 3650m, ComponentCount = 4 },
                RetroYears = new List<OccupationTaxYearResult>()
            });

        var service = new OccupationTaxApplicationService(
            mockEngine.Object,
            repo.Object,
            mockCertRepo.Object,
            mockPolicyRepo.Object,
            mockTransRepo.Object,
            mockYearRepo.Object,
            EmptyTaxPendingRepo(),
            EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object,
            mockFyProvider.Object,
            GuidelineService(guideline).Object,
            mockUow.Object,
            NullLogger<OccupationTaxApplicationService>.Instance);

        await service.ApplyAsync(propertyId, userId: 1);

        Assert.NotNull(capturedFy);
        Assert.Equal(7, capturedFy!.Value.StartMonth);
        Assert.Equal(1, capturedFy.Value.StartDay);
        Assert.Equal(new DateTime(2026, 7, 1), capturedFy.Value.Start);
    }

    // =========================================================================================
    // T4c - A floor with NO certificate coverage at all (no floor-wise certificate of its own,
    // AND no property-wise certificate to fall back to), when
    // OccupationTax_NoCertificateFallbackMode is explicitly set to "SKIP" (the escape hatch â€”
    // production default is DEFAULT_RETROSPECTIVE, see T4e/T4f), must be SKIPPED rather than
    // block tax computation for the property's other, properly-documented floors.
    // Floor A (5001): floor-wise OC 15-May-2026 -> computed normally.
    // Floors B (5002) and C (5003): no floor-wise certificate, no property-wise certificate at
    // all exists for this property -> skipped (logged), excluded from the aggregate.
    // =========================================================================================
    [Fact]
    public async Task T4c_FloorWithNoCertificateCoverage_SkipMode_IsSkipped_OthersStillComputed()
    {
        const int propertyId = 111;

        var floors = new List<PropertyDetailsEntity>
        {
            new() { Id = 5001, PropertyId = propertyId }, // has its own floor-wise certificate
            new() { Id = 5002, PropertyId = propertyId }, // no coverage at all
            new() { Id = 5003, PropertyId = propertyId }, // no coverage at all
        };

        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(floors);

        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        var mockUow = new Mock<IUnitOfWork>();

        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);

        // Only ONE certificate exists in the whole system: floor-wise, for Floor A. No
        // property-wise certificate exists to cover Floors B/C.
        var floorACert = BuildCertificate(propertyId, new DateTime(2026, 5, 15), "Occupancy Certificate", propertyDetailsId: 5001);
        var certs = new List<PropertyCertificateEntity> { floorACert };
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(certs.BuildMock());

        var taxes = new List<PolicyTaxDetailsEntity>
        {
            new()
            {
                PropertyId = propertyId,
                PolicyCodeId = PolicyCodeIds["NETTAX"],
                IsActive = true,
                TaxId = 1,
                TaxAmount = 21900m,
                TaxMaster = new TaxMasterEntity { Id = 1, TaxName = "GeneralTax", TaxCode = "GeneralTax" }
            },
            new()
            {
                PropertyId = propertyId,
                PolicyCodeId = PolicyCodeIds["NETTAX"],
                IsActive = true,
                TaxId = 2,
                TaxAmount = 3650m,
                TaxMaster = new TaxMasterEntity { Id = 2, TaxName = "Component1" }
            },
            new()
            {
                PropertyId = propertyId,
                PolicyCodeId = PolicyCodeIds["NETTAX"],
                IsActive = true,
                TaxId = 3,
                TaxAmount = 3650m,
                TaxMaster = new TaxMasterEntity { Id = 3, TaxName = "Component2" }
            },
            new()
            {
                PropertyId = propertyId,
                PolicyCodeId = PolicyCodeIds["NETTAX"],
                IsActive = true,
                TaxId = 4,
                TaxAmount = 3650m,
                TaxMaster = new TaxMasterEntity { Id = 4, TaxName = "Component3" }
            },
            new()
            {
                PropertyId = propertyId,
                PolicyCodeId = PolicyCodeIds["NETTAX"],
                IsActive = true,
                TaxId = 5,
                TaxAmount = 3650m,
                TaxMaster = new TaxMasterEntity { Id = 5, TaxName = "Component4" }
            }
        };
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(taxes.BuildMock());

        var years = new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } };
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(years.BuildMock());

        var savedTrans = new List<TransMastEntity>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        mockTransRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TransMastEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TransMastEntity>, CancellationToken>((entities, _) => savedTrans.AddRange(entities))
            .Returns(Task.CompletedTask);

        var service = new OccupationTaxApplicationService(
            _engine,
            repo.Object,
            mockCertRepo.Object,
            mockPolicyRepo.Object,
            mockTransRepo.Object,
            mockYearRepo.Object,
            EmptyTaxPendingRepo(),
            EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object,
            mockFyProvider.Object,
            GuidelineService(DefaultGuideline(noDateRule: "NO_TAX")).Object,
            mockUow.Object,
            NullLogger<OccupationTaxApplicationService>.Instance);

        // Must NOT throw / reject overall -- Floors B and C are skipped, Floor A still taxed.
        await service.ApplyAsync(propertyId, userId: 1);

        // Only Floor A's share (1/3 of NETTAX, prorated 321 days) was applied -- not 3x that
        // amount, proving B and C were excluded rather than silently defaulted to something.
        savedTrans.Should().HaveCount(5); // 1 general + 4 components
        var genTax = savedTrans.Single(t => t.TaxId == 1);
        genTax.TaxAmount.Should().Be(6_420m); // (21900/3) * 321/365 = 7300 * 321/365 = 6420
    }

    [Fact]
    public async Task T4d_FloorWithBothCcAndOc_OcWinsPerDatePriority_UncoveredFloorSkipped()
    {
        // DatePriority (OC, CC, ELECTRIC_BILL, RETROSPECTIVE) resolves which date wins BEFORE the
        // engine ever runs -- a floor carrying both CC and OC is no longer an unsupported/rejected
        // combination (that was a workaround for CC->OC split not being implemented); OC simply
        // wins per priority and CC is ignored for that floor. 6002 has no coverage at all and
        // NoDateRule=NO_TAX means it is skipped (not rescued by the retrospective fallback) --
        // proving the two floors are handled independently rather than the whole property being
        // rejected just because one floor is uncovered.
        const int propertyId = 222;

        var floors = new List<PropertyDetailsEntity>
        {
            new() { Id = 6001, PropertyId = propertyId },
            new() { Id = 6002, PropertyId = propertyId },
        };

        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(floors);

        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        var mockUow = new Mock<IUnitOfWork>();

        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);

        // Floor 6001 carries BOTH an OC (15-May-2026) and a CC (01-May-2026); OC wins per
        // DatePriority. 6002 has no coverage at all.
        var floorOcCert = PropertyCertificateEntity.Create(
            propertyId, certificateTypeId: 1, issueDate: new DateTime(2026, 5, 15), propertyDetailsId: 6001);
        var certificateTypeProperty = typeof(PropertyCertificateEntity).GetProperty(nameof(PropertyCertificateEntity.CertificateType));
        certificateTypeProperty!.SetValue(floorOcCert, new PropertyCertificateTypeMasterEntity { CertificateTypeName = "Occupancy Certificate" });

        var floorCcCert = PropertyCertificateEntity.Create(
            propertyId, certificateTypeId: 2, issueDate: new DateTime(2026, 5, 1), propertyDetailsId: 6001);
        certificateTypeProperty!.SetValue(floorCcCert, new PropertyCertificateTypeMasterEntity { CertificateTypeName = "Completion Certificate" });

        var certs = new List<PropertyCertificateEntity> { floorOcCert, floorCcCert };
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(certs.BuildMock());

        var taxes = new List<PolicyTaxDetailsEntity>
        {
            new()
            {
                PropertyId = propertyId,
                PolicyCodeId = PolicyCodeIds["NETTAX"],
                IsActive = true,
                TaxId = 1,
                TaxAmount = 36500m,
                TaxMaster = new TaxMasterEntity { Id = 1, TaxName = "GeneralTax", TaxCode = "GeneralTax" }
            }
        };
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(taxes.BuildMock());

        var years = new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } };
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(years.BuildMock());

        var savedTrans = new List<TransMastEntity>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        mockTransRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TransMastEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TransMastEntity>, CancellationToken>((entities, _) => savedTrans.AddRange(entities))
            .Returns(Task.CompletedTask);

        // Non-DEFAULT_RETROSPECTIVE mode: floor 6002's "no certificate at all" case must not be
        // rescued by the default retrospective fallback, so only floor 6001 contributes.
        // This test's intent is "OC wins when DatePriority1=OC" -- pin that explicitly rather
        // than relying on the suite's ambient default, which is CC-first per the current
        // business configuration.
        var ocFirstGuideline = DefaultGuideline(noDateRule: "NO_TAX") with { DatePriority1 = "OC", DatePriority2 = "CC" };
        var service = new OccupationTaxApplicationService(
            _engine,
            repo.Object,
            mockCertRepo.Object,
            mockPolicyRepo.Object,
            mockTransRepo.Object,
            mockYearRepo.Object,
            EmptyTaxPendingRepo(),
            EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object,
            mockFyProvider.Object,
            GuidelineService(ocFirstGuideline).Object,
            mockUow.Object,
            NullLogger<OccupationTaxApplicationService>.Instance);

        await service.ApplyAsync(propertyId, userId: 1);

        // Only Floor 6001's OC-based share (half NETTAX, prorated 321 days) was applied.
        savedTrans.Should().HaveCount(1);
        var genTax = savedTrans.Single(t => t.TaxId == 1);
        genTax.TaxAmount.Should().Be(16_050m); // (36500/2) * 321/365 = 18250 * 321/365 = 16050
    }

    // =========================================================================================
    // T4e - Business rule (per PTIS.PolicyConfiguration): a floor with NO certificate at all
    // must NOT simply go untaxed. Default mode (OccupationTax_NoCertificateFallbackMode =
    // DEFAULT_RETROSPECTIVE) applies a synthetic onset date = current FY start - LookbackYears,
    // fed to the engine as the Electricity-Bill condition, scaled by the configured multiplier.
    //
    // Floor A (7001): OC dated exactly 01-Apr-2026 (FY start) -> full year, no proration.
    // Floor B (7002): no certificate at all -> default retrospective, synthetic onset
    //   at least 6 years before FY2026, Electricity-Bill condition. The engine's own floor
    //   (TOTAL span of 6 years = 5 retro + 1 current) caps this to FY2021..FY2025.
    // STRICT BUSINESS RULE (2026-07-21): a floor with no certificate at all gets NO tax and
    // contributes NOTHING to the aggregate -- the no-certificate default-retrospective fallback
    // is permanently disabled (see ComputeNoCertificateFallback). Floor A (which has its own OC
    // certificate) is unaffected and still contributes its half-share alone: NETTAX 36,500 split
    // evenly across the property's 2 floors gives Floor A 18,250 (10,950 General + 4x1,825), and
    // since OC is dated exactly at FY start there is no proration and no retro years at all.
    // =========================================================================================
    [Fact]
    public async Task T4e_FloorWithNoCertificateCoverage_SkipsThatFloor_OnlyCoveredFloorContributes()
    {
        const int propertyId = 444;

        var floors = new List<PropertyDetailsEntity>
        {
            new() { Id = 7001, PropertyId = propertyId }, // has its own floor-wise certificate
            new() { Id = 7002, PropertyId = propertyId }, // no certificate at all
        };

        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(floors);

        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        var mockUow = new Mock<IUnitOfWork>();

        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);

        // Floor A: OC dated exactly at FY start -> full year, no proration (clean numbers).
        var floorACert = BuildCertificate(propertyId, new DateTime(2026, 4, 1), "Occupancy Certificate", propertyDetailsId: 7001);
        var certs = new List<PropertyCertificateEntity> { floorACert };
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(certs.BuildMock());

        var taxes = new List<PolicyTaxDetailsEntity>
        {
            new() { PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["NETTAX"], IsActive = true, TaxId = 1, TaxAmount = 21900m, TaxMaster = new TaxMasterEntity { Id = 1, TaxName = "GeneralTax", TaxCode = "GeneralTax" } },
            new() { PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["NETTAX"], IsActive = true, TaxId = 2, TaxAmount = 3650m, TaxMaster = new TaxMasterEntity { Id = 2, TaxName = "Component1" } },
            new() { PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["NETTAX"], IsActive = true, TaxId = 3, TaxAmount = 3650m, TaxMaster = new TaxMasterEntity { Id = 3, TaxName = "Component2" } },
            new() { PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["NETTAX"], IsActive = true, TaxId = 4, TaxAmount = 3650m, TaxMaster = new TaxMasterEntity { Id = 4, TaxName = "Component3" } },
            new() { PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["NETTAX"], IsActive = true, TaxId = 5, TaxAmount = 3650m, TaxMaster = new TaxMasterEntity { Id = 5, TaxName = "Component4" } }
        };
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(taxes.BuildMock());

        var service = new OccupationTaxApplicationService(
            _engine,
            repo.Object,
            mockCertRepo.Object,
            mockPolicyRepo.Object,
            mockTransRepo.Object,
            mockYearRepo.Object,
            EmptyTaxPendingRepo(),
            EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object,
            mockFyProvider.Object,
            GuidelineService().Object, // default mode = DEFAULT_RETROSPECTIVE
            mockUow.Object,
            NullLogger<OccupationTaxApplicationService>.Instance);

        var result = await service.PreviewAsync(propertyId);

        result.IsValid.Should().BeTrue();
        result.CurrentYear!.NetTax.Should().Be(18_250m); // Floor A's half-share only; Floor B contributes nothing
        result.RetroYears.Should().BeEmpty(); // Floor B's fallback no longer exists to produce any
        result.RetroRollUp.Should().Be(0m);
    }

    // =========================================================================================
    // T4f - STRICT BUSINESS RULE (2026-07-21): a property with NO certificate at all (no
    // floor-wise, no property-wise -- neither CC, OC, nor Electric Bill has a date) gets NO
    // certificate-based tax at all. The previously-implemented default-retrospective fallback
    // (a synthetic onset date instead of rejecting outright) is permanently disabled; this is now
    // always rejected, so PreviewAsync throws.
    // =========================================================================================
    [Fact]
    public async Task T4f_NoCertificateAtAllForProperty_NoTaxNoRow_StrictRule()
    {
        const int propertyId = 555;

        var repo = new Mock<IPropertyRepository>(); // unused: no floor-wise certs exist, simple path
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>().BuildMock());

        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var taxes = new List<PolicyTaxDetailsEntity>
        {
            new() { PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["NETTAX"], IsActive = true, TaxId = 1, TaxAmount = 21900m, TaxMaster = new TaxMasterEntity { Id = 1, TaxName = "GeneralTax", TaxCode = "GeneralTax" } },
            new() { PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["NETTAX"], IsActive = true, TaxId = 2, TaxAmount = 3650m, TaxMaster = new TaxMasterEntity { Id = 2, TaxName = "Component1" } },
            new() { PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["NETTAX"], IsActive = true, TaxId = 3, TaxAmount = 3650m, TaxMaster = new TaxMasterEntity { Id = 3, TaxName = "Component2" } },
            new() { PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["NETTAX"], IsActive = true, TaxId = 4, TaxAmount = 3650m, TaxMaster = new TaxMasterEntity { Id = 4, TaxName = "Component3" } },
            new() { PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["NETTAX"], IsActive = true, TaxId = 5, TaxAmount = 3650m, TaxMaster = new TaxMasterEntity { Id = 5, TaxName = "Component4" } }
        };
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(taxes.BuildMock());

        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var service = new OccupationTaxApplicationService(
            _engine, repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object,
            mockYearRepo.Object, EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(), PolicyCodeLookup().Object,
            mockFyProvider.Object, GuidelineService().Object, mockUow.Object,
            NullLogger<OccupationTaxApplicationService>.Instance);

        var act = async () => await service.PreviewAsync(propertyId);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*No CC/OC/Electric Bill certificate date is available*");
    }

    // =========================================================================================
    // 10. T5 - CC then OC timeline split (D-1 business rule).
    //     CC 01-Jun-2024 + OC 15-Sep-2025 => two separate segments.
    //     FY2024 CC: 304 days (01-Jun-2024..31-Mar-2025) Ã— 150/day (CC rate 1.5Ã—) = 45,600
    //     FY2025 CC: 167 days (01-Apr-2025..14-Sep-2025) Ã— 150/day = 25,050
    //     FY2025 OC: 198 days (15-Sep-2025..31-Mar-2026) Ã— 100/day (OC rate 1Ã—) = 19,800
    //     Roll-up: 45,600 + 25,050 + 19,800 = 90,450
    //     CRITICAL: Assert THREE separate segment amounts (not one total).
    //     NOTE: Requires CCâ†’OC timeline split in engine â€” currently NOT IMPLEMENTED.
    // =========================================================================================
    [Fact]
    public void T5_CCthenOC_TimelineSplit_ThrowsNotYetSupported()
    {
        // GUARD TEST: When both CC and OC exist, engine should throw "not yet supported"
        // (CCâ†’OC timeline split is NOT YET IMPLEMENTED).
        // This prevents silent miscalculation; property gets explicit error instead of wrong answer.
        var input = new OccupationTaxInput
        {
            PropertyId = 110,
            CompletionCertificateDate = new DateTime(2024, 6, 1),   // CC 01-Jun-2024
            OccupationCertificateDate = new DateTime(2025, 9, 15),  // OC 15-Sep-2025
            Options = new OccupationTaxOptions
            {
                AnnualNetTax = 36_500m,
                GeneralTaxPortion = 21_900m,
                ComponentCount = 4,
                CompletionCertificateMultiplier = 1.5m,
                DefaultRetroLookbackYears = 6,
                RetroCutoffDate = null,
            }
        };

        // Should return rejected with explicit reason, not silently compute wrong answer
        var result = _engine.Compute(input, new FinanceYear(2026));

        // Must be explicitly rejected with clear reason
        result.IsValid.Should().BeFalse("CCâ†’OC should be rejected, not silently computed wrong");
        result.RejectionReason.Should().Contain("not yet",
            "Rejection reason must explain why (CCâ†’OC split not implemented)");
        result.RejectionReason.Should().ContainAny("CC", "timeline", "split",
            "Rejection reason must mention CCâ†’OC timeline split");
    }

    [Fact]
    public async Task T6_ApplyAsync_SavesTaxesToTransMast()
    {
        // Arrange
        const int propertyId = 300;
        const int userId = 99;

        var floors = new List<PropertyDetailsEntity>
        {
            new() { Id = 7001, PropertyId = propertyId },
        };

        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(floors);

        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        var mockUow = new Mock<IUnitOfWork>();

        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);

        var cert = PropertyCertificateEntity.Create(
            propertyId: propertyId,
            certificateTypeId: 1,
            issueDate: new DateTime(2026, 5, 15)
        );
        var propCert = typeof(PropertyCertificateEntity).GetProperty(nameof(PropertyCertificateEntity.CertificateType));
        propCert!.SetValue(cert, new PropertyCertificateTypeMasterEntity
        {
            CertificateTypeName = "Occupancy Certificate"
        });

        var certs = new List<PropertyCertificateEntity> { cert };
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(certs.BuildMock());

        var taxes = new List<PolicyTaxDetailsEntity>
        {
            new()
            {
                PropertyId = propertyId,
                PolicyCodeId = PolicyCodeIds["NETTAX"],
                IsActive = true,
                TaxId = 1,
                TaxAmount = 21900m,
                TaxMaster = new TaxMasterEntity { Id = 1, TaxName = "GeneralTax" }
            },
            new()
            {
                PropertyId = propertyId,
                PolicyCodeId = PolicyCodeIds["NETTAX"],
                IsActive = true,
                TaxId = 2,
                TaxAmount = 3650m,
                TaxMaster = new TaxMasterEntity { Id = 2, TaxName = "WaterTax" }
            }
        };
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(taxes.BuildMock());

        var years = new List<YearMasterEntity>
        {
            new() { Year = 2026, Id = 10 }
        };
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(years.BuildMock());

        var existingTrans = new List<TransMastEntity>
        {
            new() { PropertyId = propertyId, FinanceYearId = 10, TaxId = 1, TaxAmount = 1000m, CalculationType = "RV", IsActive = true }
        };
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(existingTrans.BuildMock());

        var savedTrans = new List<TransMastEntity>();
        mockTransRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TransMastEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TransMastEntity>, CancellationToken>((entities, _) => savedTrans.AddRange(entities))
            .Returns(Task.CompletedTask);

        var service = new OccupationTaxApplicationService(
            _engine,
            repo.Object,
            mockCertRepo.Object,
            mockPolicyRepo.Object,
            mockTransRepo.Object,
            mockYearRepo.Object,
            EmptyTaxPendingRepo(),
            EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object,
            mockFyProvider.Object,
            GuidelineService().Object,
            mockUow.Object,
            NullLogger<OccupationTaxApplicationService>.Instance);

        // Act
        await service.ApplyAsync(propertyId, userId);

        // Assert
        // The existing row's slot (FinanceYearId=10, TaxId=1) matches this computation's GeneralTax
        // record, so it's updated in place rather than soft-deleted-and-reinserted (see
        // OccupationTaxApplicationService.SaveTaxesAsync's upsert-by-slot rationale) -- reusing the
        // same physical row avoids a duplicate-key violation on re-save if the live database's
        // unique index isn't filtered the same way the EF model declares it.
        var genTax = existingTrans.Single(t => t.TaxId == 1);
        genTax.MarkedForDeletion.Should().BeFalse();
        genTax.IsActive.Should().BeTrue();
        genTax.TaxAmount.Should().Be(19260m); // 21900 * 321 / 365
        genTax.UpdatedBy.Should().Be(userId);

        // TaxId=2 (WaterTax) has no existing row for this slot, so it's a genuine new insert.
        savedTrans.Should().HaveCount(1);
        var waterTax = savedTrans.Single(t => t.TaxId == 2);
        waterTax.TaxAmount.Should().Be(3210m); // 3650 * 321 / 365
        waterTax.CreatedBy.Should().Be(userId);

        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================================
    // CertificateTaxGuideline rules engine tests (guideline-gated behavior beyond BR1-BR7).
    // =========================================================================================

    private static List<PolicyTaxDetailsEntity> StandardNetTaxDetails(int propertyId) => new()
    {
        new()
        {
            PropertyId = propertyId,
            PolicyCodeId = PolicyCodeIds["NETTAX"],
            IsActive = true,
            TaxId = 1,
            TaxAmount = 21_900m,
            TaxMaster = new TaxMasterEntity { Id = 1, TaxName = "GeneralTax", TaxCode = "GeneralTax" }
        },
        new()
        {
            PropertyId = propertyId,
            PolicyCodeId = PolicyCodeIds["NETTAX"],
            IsActive = true,
            TaxId = 2,
            TaxAmount = 3_650m,
            TaxMaster = new TaxMasterEntity { Id = 2, TaxName = "Component1" }
        }
    };

    private static OccupationTaxApplicationService BuildRulesEngineService(
        int propertyId,
        List<PropertyCertificateEntity> certs,
        CertificateTaxGuidelineSettings guideline,
        Mock<IPropertyRepository> repo,
        Mock<IRepository<PolicyTaxDetailsEntity, int>> mockPolicyRepo,
        Mock<IRepository<TransMastEntity, int>> mockTransRepo,
        Mock<IRepository<YearMasterEntity, int>> mockYearRepo,
        Mock<IFinanceYearProvider> mockFyProvider,
        Mock<IUnitOfWork> mockUow,
        IOccupationTaxEngine? engineOverride = null)
    {
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(certs.BuildMock());

        return new OccupationTaxApplicationService(
            engineOverride ?? new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object,
            mockCertRepo.Object,
            mockPolicyRepo.Object,
            mockTransRepo.Object,
            mockYearRepo.Object,
            EmptyTaxPendingRepo(),
            EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object,
            mockFyProvider.Object,
            GuidelineService(guideline).Object,
            mockUow.Object,
            NullLogger<OccupationTaxApplicationService>.Instance);
    }

    [Fact]
    public async Task RulesEngine_CertificateBasedTaxDisabled_NeverCallsEngineOrPersists()
    {
        const int propertyId = 301;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 5, 15), "Occupancy Certificate") };
        var mockEngine = new Mock<IOccupationTaxEngine>();

        var guideline = DefaultGuideline() with { EnableCertificateBasedTax = false };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow, mockEngine.Object);

        await service.ApplyAsync(propertyId, userId: 1);

        mockEngine.Verify(e => e.Compute(It.IsAny<OccupationTaxInput>(), It.IsAny<FinanceYear>()), Times.Never());
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task RulesEngine_ApplyOnlyTaxableCertTypesOff_MeansNoTaxAtAll()
    {
        const int propertyId = 302;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 5, 15), "Occupancy Certificate") };
        var mockEngine = new Mock<IOccupationTaxEngine>();

        // Per the confirmed business correction, 0 here means "no tax at all", NOT "allow all types".
        var guideline = DefaultGuideline() with { ApplyOnlyTaxableCertTypes = false };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow, mockEngine.Object);

        await service.ApplyAsync(propertyId, userId: 1);

        mockEngine.Verify(e => e.Compute(It.IsAny<OccupationTaxInput>(), It.IsAny<FinanceYear>()), Times.Never());
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task RulesEngine_MissingCertificateNo_RejectAction_RejectsWholeComputation()
    {
        const int propertyId = 303;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // BuildCertificate never sets CertificateNo -- it stays null, so this certificate is
        // "missing a certificate number" under CertificateRequireNoAndDate.
        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 5, 15), "Occupancy Certificate") };
        var mockEngine = new Mock<IOccupationTaxEngine>();

        var guideline = DefaultGuideline() with { CertificateRequireNoAndDate = true, MissingCertificateNoAction = "REJECT" };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow, mockEngine.Object);

        await service.ApplyAsync(propertyId, userId: 1);

        mockEngine.Verify(e => e.Compute(It.IsAny<OccupationTaxInput>(), It.IsAny<FinanceYear>()), Times.Never());
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task RulesEngine_MissingCertificateNo_IgnoreForTaxAction_FallsThroughToNoTax()
    {
        const int propertyId = 304;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 5, 15), "Occupancy Certificate") };
        var mockEngine = new Mock<IOccupationTaxEngine>();

        var guideline = DefaultGuideline(noDateRule: "DEFAULT_RETROSPECTIVE") with
        {
            CertificateRequireNoAndDate = true,
            MissingCertificateNoAction = "IGNORE_FOR_TAX",
        };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow, mockEngine.Object);

        await service.ApplyAsync(propertyId, userId: 1);

        // The OC certificate was ignored (no CertificateNo), and there is no other certificate to
        // fall back to -- STRICT BUSINESS RULE (2026-07-21): no date means no tax, so the engine is
        // never invoked at all (the no-certificate fallback no longer computes anything) and
        // nothing is persisted.
        mockEngine.Verify(e => e.Compute(It.IsAny<OccupationTaxInput>(), It.IsAny<FinanceYear>()), Times.Never());
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task RulesEngine_CcOcGapWithinThreshold_AppliesOcOnly_CcDiscarded()
    {
        const int propertyId = 305;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var netTaxDetails = StandardNetTaxDetails(propertyId);
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(netTaxDetails.BuildMock());
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // CC and OC 2 months apart (within the default 6-month threshold) -> OC wins, CC discarded.
        var certs = new List<PropertyCertificateEntity>
        {
            BuildCertificate(propertyId, new DateTime(2026, 4, 1), "Completion Certificate"),
            BuildCertificate(propertyId, new DateTime(2026, 6, 1), "Occupancy Certificate"),
        };

        var guideline = DefaultGuideline() with { CcOcGapWithinAction = "APPLY_OC_ONLY" };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["OC"] || p.PolicyCodeId == PolicyCodeIds["PARTIAL_OC"]);
    }

    [Fact]
    public async Task RulesEngine_CcOcGapExceedsThreshold_MergesCcThenOc_EachYearTaggedToItsOwnFamily()
    {
        const int propertyId = 306;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(
            Enumerable.Range(2020, 7).Select(y => new YearMasterEntity { Year = y, Id = y }).ToList().BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // CC dated 2020, OC dated 2024 -- a multi-year gap, far exceeding the default 6-month
        // threshold -> CC governs 2020-2023, OC governs 2024 onward (see ComputeCcThenOcMerge).
        var certs = new List<PropertyCertificateEntity>
        {
            BuildCertificate(propertyId, new DateTime(2020, 6, 15), "Completion Certificate"),
            BuildCertificate(propertyId, new DateTime(2024, 8, 10), "Occupancy Certificate"),
        };

        var guideline = DefaultGuideline() with { CcOcGapExceededAction = "APPLY_CC_THEN_OC" };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        // Verify the per-year family tagging via the computed result's YearPolicyCodes (still an
        // in-memory, per-year map) rather than persisted PolicyTaxDetails rows -- PolicyTaxDetails
        // no longer stores retro years at all under the DBA-confirmed schema (no PolicyYear column,
        // unique index on PropertyId+PolicyCodeId+TaxId); only the current year (2026) gets a row.
        // Real-DB coverage for retro-year TaxPendingDetailsRetro persistence in this exact scenario
        // is CcThenOcSameYearSplitTests.CaseAcrossYears_CcOldFy_OcCurrentFy_TagsEachYearWithItsOwnFamily.
        var result = await service.PreviewAsync(propertyId);
        result.IsValid.Should().BeTrue();

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().ContainSingle(p => p.TaxId == 1); // current year (2026) only
        savedPolicy.Single(p => p.TaxId == 1).PolicyCodeId.Should().Be(PolicyCodeIds["OC"]);

        // Neither family's PARTIAL variant is used: 2026 is not a true in-progress onset year for
        // either certificate (OC governs from 2024 onward, itself not the same FY as its own onset).
        savedPolicy.Should().NotContain(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"] || p.PolicyCodeId == PolicyCodeIds["PARTIAL_OC"]);
    }

    [Fact]
    public async Task RulesEngine_InvalidCcOcDateOrder_UsePriorityAndLog_FallsBackToPriorityWinner()
    {
        const int propertyId = 307;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // Data-entry error: OC dated BEFORE CC. USE_PRIORITY_AND_LOG falls back to the configured
        // priority order (default CC first) rather than attempting a CC-then-OC merge.
        var certs = new List<PropertyCertificateEntity>
        {
            BuildCertificate(propertyId, new DateTime(2026, 4, 1), "Occupancy Certificate"),
            BuildCertificate(propertyId, new DateTime(2026, 5, 1), "Completion Certificate"),
        };

        var guideline = DefaultGuideline() with { InvalidCcOcDateOrderAction = "USE_PRIORITY_AND_LOG" };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["CC"] || p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"]);
    }

    [Fact]
    public async Task RulesEngine_OldBackdatedOcOnsetYear_UsesFullCode_NeverPartial()
    {
        const int propertyId = 308;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(
            Enumerable.Range(2020, 7).Select(y => new YearMasterEntity { Year = y, Id = y }).ToList().BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // OC dated mid-year 2022 -- within the 6-year lookback window (floor 2020), so 2022 IS
        // generated and day-prorated within its own onset year, but 2022 != the true current FY
        // (2026), so it must still be tagged the plain "OC" code, never "PARTIAL_OC".
        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2022, 6, 15), "Occupancy Certificate") };

        var guideline = DefaultGuideline();
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["OC"]);
    }

    [Fact]
    public async Task RulesEngine_OcOnsetInCurrentFinanceYear_TaggedPartial()
    {
        const int propertyId = 309;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 5, 15), "Occupancy Certificate") };

        var guideline = DefaultGuideline();
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_OC"]);
    }

    [Fact]
    public async Task RulesEngine_CcOnsetInCurrentFinanceYear_TaggedPartial_EngineSymmetryFix()
    {
        const int propertyId = 310;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // CC dated mid-way through the CURRENT finance year -- before this fix, only OC got the
        // current-year proration special case; CC would have gotten a full, unprorated year.
        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 6, 15), "Completion Certificate") };

        var guideline = DefaultGuideline();
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"]);

        var genTax = savedPolicy.Single(p => p.TaxId == 1);
        genTax.TaxAmount.Should().BeLessThan(21_900m); // prorated, not the full annual amount
    }

    [Fact]
    public async Task RulesEngine_GuidelineChangeApplyModeAuto_LogsWarning_DoesNotBlockCalculation()
    {
        const int propertyId = 312;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(
            new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 4, 1), "Occupancy Certificate") }.BuildMock());
        var mockLogger = new Mock<ILogger<OccupationTaxApplicationService>>();

        var guideline = DefaultGuideline() with { GuidelineChangeApplyMode = "AUTO_RECALCULATION" };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object,
            mockCertRepo.Object,
            mockPolicyRepo.Object,
            mockTransRepo.Object,
            mockYearRepo.Object,
            EmptyTaxPendingRepo(),
            EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object,
            mockFyProvider.Object,
            GuidelineService(guideline).Object,
            mockUow.Object,
            mockLogger.Object);

        await service.ApplyAsync(propertyId, userId: 1);

        // Bulk auto-recalculation isn't built -- the calculation for THIS property must still
        // proceed normally, with a warning surfaced rather than a silent no-op.
        savedPolicy.Should().NotBeEmpty();
        mockLogger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("AUTO_RECALCULATION")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RulesEngine_OldBackdatedCcOnsetYear_UsesFullCode_NeverPartial()
    {
        const int propertyId = 320;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(
            Enumerable.Range(2020, 7).Select(y => new YearMasterEntity { Year = y, Id = y }).ToList().BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // CC dated mid-year 2022 -- within the 6-year lookback window, so it's day-prorated within
        // its own onset year, but 2022 is not the true current FY (2026), so every row -- including
        // the continuation into the current year -- must be tagged the plain "CC" code, never
        // "PARTIAL_CC".
        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2022, 6, 15), "Completion Certificate") };

        var guideline = DefaultGuideline();
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["CC"]);
    }

    [Fact]
    public async Task RulesEngine_ElectricBillOnsetInCurrentFinanceYear_TaggedPartial()
    {
        const int propertyId = 321;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // Electric Bill dated within the current FY -- even under the default FROM_FY_START rule
        // (which normalizes the onset to the FY start, so the year's own amount is never
        // day-prorated), the bill's own date still falls in the current, still-open year, so it
        // must be tagged PARTIAL_ELECTRIC_BILL.
        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 6, 15), "Electric Bill") };

        var guideline = DefaultGuideline();
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_ELECTRIC_BILL"]);
    }

    [Fact]
    public async Task RulesEngine_OldElectricBillOnsetYear_UsesFullCode_NeverPartial()
    {
        const int propertyId = 322;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(
            Enumerable.Range(2020, 7).Select(y => new YearMasterEntity { Year = y, Id = y }).ToList().BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2022, 9, 15), "Electric Bill") };

        var guideline = DefaultGuideline();
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["ELECTRIC_BILL"]);
    }

    [Fact]
    public async Task RulesEngine_ThaneElectricBill_UsesFinancialYearStartOfBillDate()
    {
        // Business example: Electric Bill date = 15/09/2024 -> tax start date = 01/04/2024. The
        // pure engine's own ElectricityBill condition always normalizes onset to that date's
        // finance-year start internally (BR2), so the bill's own FY (2024) must compute as a FULL,
        // unprorated year running 01-Apr-2024..31-Mar-2025 -- never day-prorated from 15-Sep.
        const int propertyId = 323;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(
            Enumerable.Range(2024, 3).Select(y => new YearMasterEntity { Year = y, Id = y }).ToList().BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2024, 9, 15), "Electric Bill") };

        var guideline = DefaultGuideline(); // ElectricBillDateRule defaults to FROM_FY_START
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        var result = await service.PreviewAsync(propertyId);

        result.IsValid.Should().BeTrue();
        var billYear = result.RetroYears.Single(y => y.FinanceYear == 2024);
        billYear.FinanceYearStart.Should().Be(new DateTime(2024, 4, 1));
        billYear.IsProrated.Should().BeFalse(); // full year from the FY start, not prorated from 15-Sep
        billYear.ChargeableDays.Should().Be(365);
    }

    [Fact]
    public async Task RulesEngine_ThaneElectricBillBefore2016_FloorsAtMinimumBackdateYear()
    {
        // Business example: Electric Bill date = 2014 -> minimum start date = 01/04/2016.
        const int propertyId = 324;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(
            Enumerable.Range(2016, 11).Select(y => new YearMasterEntity { Year = y, Id = y }).ToList().BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // A generous LookbackYears (20) isolates MINIMUM_BACKDATE_FINANCIAL_YEAR as the actual
        // binding floor here, rather than the lookback-years cap.
        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2014, 6, 1), "Electric Bill") };

        var guideline = DefaultGuideline() with { LookbackYears = 20, MinimumBackdateFinancialYear = 2016 };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        // Verify the floor via the computed result directly (RetroYears), not via a persisted
        // PolicyTaxDetails row -- PolicyTaxDetails no longer stores retro years at all under the
        // DBA-confirmed schema (no PolicyYear column); retro years live in TaxPendingDetailsRetro.
        var result = await service.PreviewAsync(propertyId);
        result.IsValid.Should().BeTrue();
        result.RetroYears.Select(y => y.FinanceYear).Min().Should().Be(2016); // never goes back before FY2016-17
    }

    [Fact]
    public async Task RulesEngine_NoDateCase_RetrospectiveDisabled_CleansUpStaleCertificateTaxRows()
    {
        // Thane does not have retrospective enabled (NoDateRule = NO_TAX): no certificate at all
        // must leave NETTAX untouched, but any EXISTING certificate-tax-family row (like a stale OC
        // row left over from before the certificate was deleted) must be cleaned up -- not left
        // stale on the Tax Details grid (go-live blocker fix: no new rows are added, since there is
        // nothing to compute, but the old row must not survive either).
        const int propertyId = 325;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var netTaxAndExisting = StandardNetTaxDetails(propertyId);
        var existingOcRow = new PolicyTaxDetailsEntity
        {
            PropertyId = propertyId,
            PolicyCodeId = PolicyCodeIds["OC"],
            IsActive = true,
            MarkedForDeletion = false,
            TaxId = 1,
            TaxAmount = 21_900m,
        };
        netTaxAndExisting.Add(existingOcRow);
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(netTaxAndExisting.BuildMock());
        var addRangeCalled = false;
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback(() => addRangeCalled = true)
            .Returns(Task.CompletedTask);

        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        var existingTransRow = new TransMastEntity { PropertyId = propertyId, FinanceYearId = 10, TaxId = 1, TaxAmount = 21_900m, CalculationType = "RV", IsActive = true };
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity> { existingTransRow }.BuildMock());

        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity>(); // no CC/OC/Electric Bill at all

        var guideline = DefaultGuideline(noDateRule: "NO_TAX");
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        addRangeCalled.Should().BeFalse(); // nothing new to compute -- no new rows added
        existingOcRow.MarkedForDeletion.Should().BeTrue("a stale OC row must not remain once no valid certificate exists");
        existingTransRow.MarkedForDeletion.Should().BeTrue();
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task RulesEngine_NoDateSettingsIgnored_NoCertificateAlwaysMeansNoTax()
    {
        // STRICT BUSINESS RULE (2026-07-21): retired RulesEngine_RetrospectiveFlagOn_
        // SixYearsMeansFiveBackPlusOneCurrent and RulesEngine_RetrospectiveCurrentYearCount_
        // TotalMinusCurrent_ChangesPendingYearSpan, which used to prove LOOKBACK_YEARS/
        // RETROSPECTIVE_CURRENT_YEAR_COUNT/RETROSPECTIVE_PENDING_YEAR_COUNT_MODE shaped the
        // no-certificate default-retrospective fallback's window. That fallback is now
        // permanently disabled -- no certificate date at all always means no tax, no row,
        // regardless of NoDateRule/EnableRetrospectiveTax/LookbackYears/*_CURRENT_YEAR_COUNT.
        const int propertyId = 326;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(
            Enumerable.Range(2020, 7).Select(y => new YearMasterEntity { Year = y, Id = y }).ToList().BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity>(); // no certificate at all

        var guideline = DefaultGuideline(noDateRule: "DEFAULT_RETROSPECTIVE") with
        {
            EnableRetrospectiveTax = true,
            LookbackYears = 6,
            RetrospectiveCurrentYearCount = 2,
            RetrospectivePendingYearCountMode = "TOTAL_MINUS_CURRENT",
        };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        var act = async () => await service.PreviewAsync(propertyId);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*No CC/OC/Electric Bill certificate date is available*");
    }

    [Fact]
    public async Task RulesEngine_FloorsDisagreeOnPolicy_BiggestFloorAreaWinsAsRepresentative()
    {
        // Tax Details grid display rule: when floor-wise certificates disagree on which policy
        // applies, the biggest floor (by area) is used as the representative for the single,
        // property-wise persisted row -- not simply whichever floor happened to compute first.
        const int propertyId = 327;

        var floors = new List<PropertyDetailsEntity>
        {
            new() { Id = 8001, PropertyId = propertyId, BuiltupAreaSqMeter = 50 },  // small floor, computed first
            new() { Id = 8002, PropertyId = propertyId, BuiltupAreaSqMeter = 200 }, // biggest floor
        };

        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(floors);

        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(
            Enumerable.Range(2022, 5).Select(y => new YearMasterEntity { Year = y, Id = y }).ToList().BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // Small floor (8001): OC dated this year -> would be PARTIAL_OC if picked as representative.
        var smallFloorCert = BuildCertificate(propertyId, new DateTime(2026, 5, 15), "Occupancy Certificate", propertyDetailsId: 8001);
        // Biggest floor (8002): CC dated an old year -> would be plain CC if picked as representative.
        var bigFloorCert = BuildCertificate(propertyId, new DateTime(2022, 6, 15), "Completion Certificate", propertyDetailsId: 8002);

        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity> { smallFloorCert, bigFloorCert }.BuildMock());

        var guideline = DefaultGuideline();
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object,
            mockCertRepo.Object,
            mockPolicyRepo.Object,
            mockTransRepo.Object,
            mockYearRepo.Object,
            EmptyTaxPendingRepo(),
            EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object,
            mockFyProvider.Object,
            GuidelineService(guideline).Object,
            mockUow.Object,
            NullLogger<OccupationTaxApplicationService>.Instance);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["CC"]); // biggest floor's family, not OC/PARTIAL_OC
    }

    [Fact]
    public async Task RulesEngine_SaveInTransMastDisabled_OnlyPolicyTaxDetailsWritten()
    {
        const int propertyId = 311;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var transMastAddCalled = false;
        mockTransRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TransMastEntity>>(), It.IsAny<CancellationToken>()))
            .Callback(() => transMastAddCalled = true)
            .Returns(Task.CompletedTask);
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 4, 1), "Occupancy Certificate") };

        var guideline = DefaultGuideline() with { SaveInTransMast = false, SaveInPolicyTaxDetails = true };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        transMastAddCalled.Should().BeFalse();
        savedPolicy.Should().NotBeEmpty();
    }

    // =========================================================================================
    // Gap-fix pass: CC_PERIOD_MULTIPLIER, PRORATION_METHOD, ENABLE_RETROSPECTIVE_TAX,
    // ELECTRIC_BILL_MINIMUM_FINANCIAL_YEAR, TAX_PERSISTENCE_MODE, DO_NOT_UPDATE_NETTAX.
    // =========================================================================================

    [Fact]
    public async Task RulesEngine_CcPeriodMultiplier_ScalesCcTax()
    {
        const int propertyId = 340;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // CC dated exactly on the FY start -- a full, unprorated year, so the multiplier's effect
        // is the only thing changing the amount.
        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 4, 1), "Completion Certificate") };

        var baselineGuideline = DefaultGuideline(); // CCPeriodMultiplier = 1.0 (no-op)
        var baselineService = BuildRulesEngineService(propertyId, certs, baselineGuideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);
        var baselineResult = await baselineService.PreviewAsync(propertyId);

        var scaledGuideline = DefaultGuideline() with { CCPeriodMultiplier = 1.5m };
        var scaledService = BuildRulesEngineService(propertyId, certs, scaledGuideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);
        var scaledResult = await scaledService.PreviewAsync(propertyId);

        baselineResult.CurrentYear!.GeneralTax.Should().Be(21_900m); // unscaled annual GeneralTax
        scaledResult.CurrentYear!.GeneralTax.Should().Be(32_850m); // 21,900 * 1.5 -- multiplier actually applied
        scaledResult.CurrentYear!.ComponentTax.Should().Be(baselineResult.CurrentYear!.ComponentTax * 1.5m);
    }

    [Fact]
    public async Task RulesEngine_ProrationMethodMonthly_NormalizesOnsetToMonthStart()
    {
        const int propertyId = 341;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // OC dated mid-month, mid current FY.
        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 5, 15), "Occupancy Certificate") };

        var dailyGuideline = DefaultGuideline() with { ProrationMethod = "DAILY", CurrentYearProrationStartRule = "EXACT_DATE" };
        var dailyService = BuildRulesEngineService(propertyId, certs, dailyGuideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);
        var dailyResult = await dailyService.PreviewAsync(propertyId);

        var monthlyGuideline = DefaultGuideline() with { ProrationMethod = "MONTHLY", CurrentYearProrationStartRule = "MONTH_START" };
        var monthlyService = BuildRulesEngineService(propertyId, certs, monthlyGuideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);
        var monthlyResult = await monthlyService.PreviewAsync(propertyId);

        // MONTHLY normalizes onset to 1-May instead of the exact 15-May date -- more chargeable
        // days, so PRORATION_METHOD is genuinely changing the computed onset, not just being read.
        monthlyResult.CurrentYear!.ChargeableDays.Should().BeGreaterThan(dailyResult.CurrentYear!.ChargeableDays);
    }

    [Fact]
    public async Task RulesEngine_ProrationMethodDisagreesWithStartRule_LogsWarning_ProrationMethodWins()
    {
        const int propertyId = 342;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 5, 15), "Occupancy Certificate") };
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(certs.BuildMock());
        var mockLogger = new Mock<ILogger<OccupationTaxApplicationService>>();

        // Deliberately inconsistent guideline row pair.
        var guideline = DefaultGuideline() with { ProrationMethod = "MONTHLY", CurrentYearProrationStartRule = "EXACT_DATE" };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object, mockUow.Object,
            mockLogger.Object);

        var mismatchedResult = await service.PreviewAsync(propertyId);

        var agreeingMonthlyGuideline = DefaultGuideline() with { ProrationMethod = "MONTHLY", CurrentYearProrationStartRule = "MONTH_START" };
        var agreeingService = BuildRulesEngineService(propertyId, certs, agreeingMonthlyGuideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);
        var monthStartResult = await agreeingService.PreviewAsync(propertyId);

        // PRORATION_METHOD (MONTHLY) wins over the disagreeing CURRENT_YEAR_PRORATION_START_RULE
        // (EXACT_DATE): the mismatched run behaves exactly like an agreeing MONTH_START run.
        mismatchedResult.CurrentYear!.ChargeableDays.Should().Be(monthStartResult.CurrentYear!.ChargeableDays);
        mockLogger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("PRORATION_METHOD")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RulesEngine_NoDateCase_EnableRetrospectiveTaxFalse_BlocksFallback_EvenWithDefaultRetrospectiveRule()
    {
        // A ULB could set NO_DATE_RULE = DEFAULT_RETROSPECTIVE while leaving ENABLE_RETROSPECTIVE_TAX
        // off -- the flag must be the master switch, blocking the fallback regardless of NoDateRule.
        const int propertyId = 343;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var netTaxAndExisting = StandardNetTaxDetails(propertyId);
        var existingOcRow = new PolicyTaxDetailsEntity
        {
            PropertyId = propertyId,
            PolicyCodeId = PolicyCodeIds["OC"],
            IsActive = true,
            MarkedForDeletion = false,
            TaxId = 1,
            TaxAmount = 21_900m,
        };
        netTaxAndExisting.Add(existingOcRow);
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(netTaxAndExisting.BuildMock());
        var addRangeCalled = false;
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback(() => addRangeCalled = true)
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        var existingTransRow = new TransMastEntity { PropertyId = propertyId, FinanceYearId = 10, TaxId = 1, TaxAmount = 21_900m, CalculationType = "RV", IsActive = true };
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity> { existingTransRow }.BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity>(); // no CC/OC/Electric Bill at all

        var guideline = DefaultGuideline(noDateRule: "DEFAULT_RETROSPECTIVE") with { EnableRetrospectiveTax = false };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        // Fallback stays blocked (no new retrospective rows are added), but a stale existing OC row
        // must still be cleaned up -- not left behind just because the fallback itself is disabled.
        addRangeCalled.Should().BeFalse();
        existingOcRow.MarkedForDeletion.Should().BeTrue();
        existingTransRow.MarkedForDeletion.Should().BeTrue();
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task RulesEngine_ElectricBillBeforeMinimumFinancialYear_FloorsAtConfiguredYear()
    {
        // Business example: Electric Bill date = 2014, ELECTRIC_BILL_MINIMUM_FINANCIAL_YEAR = 2016
        // -> effective start date = 01/04/2016. Isolated from MINIMUM_BACKDATE_FINANCIAL_YEAR (left
        // at 0/disabled) and a generous LookbackYears, so the Electric-Bill-only floor is the only
        // thing that can be binding here.
        const int propertyId = 344;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(
            Enumerable.Range(2016, 11).Select(y => new YearMasterEntity { Year = y, Id = y }).ToList().BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2014, 6, 1), "Electric Bill") };

        var guideline = DefaultGuideline() with { LookbackYears = 20, MinimumBackdateFinancialYear = 0, ElectricBillMinimumFinancialYear = 2016 };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        var result = await service.PreviewAsync(propertyId);

        result.IsValid.Should().BeTrue();
        var floorYear = result.RetroYears.OrderBy(y => y.FinanceYear).First();
        floorYear.FinanceYear.Should().Be(2016); // never goes back before FY2016-17
        floorYear.FinanceYearStart.Should().Be(new DateTime(2016, 4, 1));
        floorYear.IsProrated.Should().BeFalse(); // floored to the FY start -- a full, unprorated year
    }

    [Fact]
    public async Task RulesEngine_ElectricBillMinimumFinancialYear_DoesNotApplyToCc()
    {
        // ELECTRIC_BILL_MINIMUM_FINANCIAL_YEAR must be Electric-Bill-only -- a CC dated even earlier
        // than the configured floor must NOT be floored by it.
        const int propertyId = 345;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(
            Enumerable.Range(2010, 17).Select(y => new YearMasterEntity { Year = y, Id = y }).ToList().BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2010, 6, 1), "Completion Certificate") };

        var guideline = DefaultGuideline() with { LookbackYears = 20, MinimumBackdateFinancialYear = 0, ElectricBillMinimumFinancialYear = 2016 };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        var result = await service.PreviewAsync(propertyId);

        result.IsValid.Should().BeTrue();
        result.RetroYears.OrderBy(y => y.FinanceYear).First().FinanceYear.Should().Be(2010); // untouched by the Electric-Bill-only floor
    }

    [Fact]
    public async Task RulesEngine_UnsupportedTaxPersistenceMode_LogsWarning_StillPersistsPropertyAggregated()
    {
        const int propertyId = 346;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(
            new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 4, 1), "Occupancy Certificate") }.BuildMock());
        var mockLogger = new Mock<ILogger<OccupationTaxApplicationService>>();

        var guideline = DefaultGuideline() with { TaxPersistenceMode = "FLOOR_LEDGER" }; // unsupported
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object, mockUow.Object,
            mockLogger.Object);

        await service.ApplyAsync(propertyId, userId: 1);

        // No floor-wise ledger is built -- persistence still proceeds as PROPERTY_AGGREGATED.
        savedPolicy.Should().NotBeEmpty();
        mockLogger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TAX_PERSISTENCE_MODE")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RulesEngine_DoNotUpdateNettaxFalse_LogsWarning_NettaxNeverUpdated()
    {
        const int propertyId = 347;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(
            new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 4, 1), "Occupancy Certificate") }.BuildMock());
        var mockLogger = new Mock<ILogger<OccupationTaxApplicationService>>();

        var guideline = DefaultGuideline() with { DoNotUpdateNettax = false }; // unsupported configuration
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object, mockUow.Object,
            mockLogger.Object);

        await service.ApplyAsync(propertyId, userId: 1);

        // NETTAX is never touched regardless -- no update path exists for it.
        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId != PolicyCodeIds["NETTAX"]);
        mockPolicyRepo.Verify(r => r.UpdateAsync(
            It.Is<PolicyTaxDetailsEntity>(p => p.PolicyCodeId == PolicyCodeIds["NETTAX"]),
            It.IsAny<CancellationToken>()), Times.Never());
        mockLogger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DO_NOT_UPDATE_NETTAX")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    // =========================================================================================
    // Final business-decision verification pass: IGNORE_INVALID_DATE vs USE_PRIORITY_AND_LOG,
    // and the both-fields-missing certificate case.
    // =========================================================================================

    [Fact]
    public async Task RulesEngine_InvalidCcOcDateOrder_IgnoreInvalidDate_UsesCcDirectly_NotPriorityOrder()
    {
        const int propertyId = 350;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();

        // Data-entry error: OC dated BEFORE CC.
        var certs = new List<PropertyCertificateEntity>
        {
            BuildCertificate(propertyId, new DateTime(2026, 4, 1), "Occupancy Certificate"),
            BuildCertificate(propertyId, new DateTime(2026, 5, 1), "Completion Certificate"),
        };
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(certs.BuildMock());
        var mockLogger = new Mock<ILogger<OccupationTaxApplicationService>>();

        // Priority order deliberately puts OC ahead of CC -- proves IGNORE_INVALID_DATE does NOT
        // consult DATE_PRIORITY at all (unlike USE_PRIORITY_AND_LOG, which would pick OC here); it
        // goes straight to CC because CC is the only certificate not implicated by the invalid order.
        var guideline = DefaultGuideline() with
        {
            InvalidCcOcDateOrderAction = "IGNORE_INVALID_DATE",
            DatePriority1 = "OC", DatePriority2 = "CC", DatePriority3 = "ELECTRIC_BILL", DatePriority4 = "RETROSPECTIVE",
        };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object, mockUow.Object,
            mockLogger.Object);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["CC"] || p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"]);

        // Info, not Warning -- nothing here is being treated as a failure worth escalating.
        mockLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("IGNORE_INVALID_DATE")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
        mockLogger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never());
    }

    [Fact]
    public async Task RulesEngine_CertificateMissingBothNoAndDate_NeverRejectsWholeProperty_EvenWhenActionsAreReject()
    {
        const int propertyId = 351;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // Missing BOTH CertificateNo and IssueDate -- must always be ignored, even though both
        // individual actions below are configured to REJECT.
        var cert = BuildCertificate(propertyId, new DateTime(2026, 5, 15), "Occupancy Certificate");
        var issueDateProperty = typeof(PropertyCertificateEntity).GetProperty(nameof(PropertyCertificateEntity.IssueDate));
        issueDateProperty!.SetValue(cert, null);
        var certs = new List<PropertyCertificateEntity> { cert };

        var guideline = DefaultGuideline(noDateRule: "DEFAULT_RETROSPECTIVE") with
        {
            CertificateRequireNoAndDate = true,
            MissingCertificateNoAction = "REJECT",
            MissingCertificateDateAction = "REJECT",
        };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        // PreviewAsync throws either way now (no other certificate exists, and the no-certificate
        // fallback is permanently disabled -- see T4f) -- the fix under test is which REASON it
        // throws for. If the bug were still present, missingNo would be checked first and
        // MISSING_CERTIFICATE_NO_ACTION = REJECT would incorrectly reject the whole property with a
        // message naming this specific certificate's missing number/date. The certificate being
        // correctly ignored instead (both fields missing) means the exception carries the neutral
        // "no certificate at all" message, not one naming this certificate.
        var act = async () => await service.PreviewAsync(propertyId);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*No CC/OC/Electric Bill certificate date is available*")
            .Which.Message.Should().NotContain("is missing");
    }

    // =========================================================================================
    // Dynamic policy-code wiring: CC_FULL_POLICY_CODE / CC_PARTIAL_POLICY_CODE /
    // OC_FULL_POLICY_CODE / OC_PARTIAL_POLICY_CODE / ELECTRIC_BILL_FULL_POLICY_CODE /
    // ELECTRIC_BILL_PARTIAL_POLICY_CODE now actually select which PolicyCodeMaster row is used,
    // replacing the previously-hardcoded FamilyPolicyCodes dictionary.
    // =========================================================================================

    [Fact]
    public async Task RulesEngine_ResaveWithFamilySwitch_ReusesExistingRowInsteadOfDuplicateKey()
    {
        // Regression test: a property that already has an active OC-family PolicyTaxDetails row
        // for (PropertyId, FinanceYear=2026, TaxId=1) gets its certificate re-saved as CC instead
        // of OC. Before the upsert-by-slot fix, SaveTaxesAsync soft-deleted the old OC row and
        // inserted a brand-new CC row for the identical (Property, Year, PolicyCode-family-slot,
        // Tax) key -- if the live database's unique index isn't filtered on
        // IsActive/MarkedForDeletion the same way the EF model declares it, that insert collides
        // with the still-physically-present soft-deleted row. The fix reuses and re-tags the same
        // row in place, so no new row is ever inserted for a slot that already has one.
        const int propertyId = 360;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();

        var existingPolicy = new List<PolicyTaxDetailsEntity>
        {
            new()
            {
                PropertyId = propertyId,
                PolicyCodeId = PolicyCodeIds["OC"],
                TaxId = 1,
                TaxAmount = 1000m,
                IsActive = true,
                MarkedForDeletion = false
            }
        };
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() =>
            existingPolicy.Concat(StandardNetTaxDetails(propertyId)).ToList().BuildMock());

        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);

        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(
            Enumerable.Range(2020, 7).Select(y => new YearMasterEntity { Year = y, Id = y }).ToList().BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // Property now has a Completion Certificate (CC family) instead of the OC it had before.
        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2022, 6, 15), "Completion Certificate") };

        var service = BuildRulesEngineService(propertyId, certs, DefaultGuideline(), repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        // The old OC row for this exact slot (FinanceYear=2026, TaxId=1) is re-tagged to CC in
        // place -- never soft-deleted, never duplicated by a fresh insert for the same slot.
        var reused = existingPolicy.Single(p => p.TaxId == 1);
        reused.PolicyCodeId.Should().Be(PolicyCodeIds["CC"]);
        reused.MarkedForDeletion.Should().BeFalse();
        reused.IsActive.Should().BeTrue();

        // Retro years never get a PolicyTaxDetails row at all under the DBA-confirmed schema (only
        // the current year does) -- so the reused current-year slot itself (TaxId=1) must never
        // appear among any newly-inserted rows, since that would mean a second physical row was
        // created for an already-occupied key.
        savedPolicy.Should().NotContain(p => p.TaxId == 1);
    }

    [Fact]
    public async Task RulesEngine_CcFullPolicyCode_ChangesSelectedFullCcCode()
    {
        const int propertyId = 352;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(
            Enumerable.Range(2020, 7).Select(y => new YearMasterEntity { Year = y, Id = y }).ToList().BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // CC dated an old, closed year -- resolves to the FULL code, not partial.
        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2022, 6, 15), "Completion Certificate") };

        var guideline = DefaultGuideline() with { CcFullPolicyCode = "CUSTOM_POLICY_CODE" };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["CUSTOM_POLICY_CODE"]);
    }

    [Fact]
    public async Task RulesEngine_CcPartialPolicyCode_ChangesSelectedPartialCcCode()
    {
        const int propertyId = 353;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // CC dated the current, still-open financial year -- resolves to the PARTIAL code.
        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 4, 1), "Completion Certificate") };

        var guideline = DefaultGuideline() with { CcPartialPolicyCode = "CUSTOM_POLICY_CODE" };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["CUSTOM_POLICY_CODE"]);
    }

    [Fact]
    public async Task RulesEngine_OcFullPolicyCode_ChangesSelectedFullOcCode()
    {
        const int propertyId = 354;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(
            Enumerable.Range(2020, 7).Select(y => new YearMasterEntity { Year = y, Id = y }).ToList().BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // OC dated an old, closed year -- resolves to the FULL code, not partial.
        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2022, 6, 15), "Occupancy Certificate") };

        var guideline = DefaultGuideline() with { OcFullPolicyCode = "CUSTOM_POLICY_CODE" };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["CUSTOM_POLICY_CODE"]);
    }

    [Fact]
    public async Task RulesEngine_OcPartialPolicyCode_ChangesSelectedPartialOcCode()
    {
        const int propertyId = 355;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // OC dated the current, still-open financial year -- resolves to the PARTIAL code.
        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 4, 1), "Occupancy Certificate") };

        var guideline = DefaultGuideline() with { OcPartialPolicyCode = "CUSTOM_POLICY_CODE" };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["CUSTOM_POLICY_CODE"]);
    }

    [Fact]
    public async Task RulesEngine_ElectricBillFullPolicyCode_ChangesSelectedFullElectricBillCode()
    {
        const int propertyId = 356;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(
            Enumerable.Range(2020, 7).Select(y => new YearMasterEntity { Year = y, Id = y }).ToList().BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // Electric Bill dated an old, closed year -- resolves to the FULL code, not partial.
        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2022, 9, 15), "Electric Bill") };

        var guideline = DefaultGuideline() with { ElectricBillFullPolicyCode = "CUSTOM_POLICY_CODE" };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["CUSTOM_POLICY_CODE"]);
    }

    [Fact]
    public async Task RulesEngine_ElectricBillPartialPolicyCode_ChangesSelectedPartialElectricBillCode()
    {
        const int propertyId = 357;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // Electric Bill dated the current, still-open financial year -- resolves to the PARTIAL code.
        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 6, 15), "Electric Bill") };

        var guideline = DefaultGuideline() with { ElectricBillPartialPolicyCode = "CUSTOM_POLICY_CODE" };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["CUSTOM_POLICY_CODE"]);
    }

    // =========================================================================================
    // Previously-untested branches, added per explicit business-completion request: every one of
    // these code paths already existed but had no test proving the guideline value actually flips
    // the behavior. EnableRetrospectiveTax is turned off in the "should produce zero tax" tests so
    // the no-certificate DEFAULT_RETROSPECTIVE fallback can't mask the branch under test.
    // =========================================================================================

    [Fact]
    public async Task RulesEngine_CcOnlyActionNoTax_AppliesNoCertificateTax()
    {
        const int propertyId = 370;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 5, 15), "Completion Certificate") };

        var guideline = DefaultGuideline() with { CcOnlyAction = "NO_TAX", EnableRetrospectiveTax = false };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PreviewAsync(propertyId));
    }

    [Fact]
    public async Task RulesEngine_OcOnlyActionNoTax_AppliesNoCertificateTax()
    {
        const int propertyId = 371;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 5, 15), "Occupancy Certificate") };

        var guideline = DefaultGuideline() with { OcOnlyAction = "NO_TAX", EnableRetrospectiveTax = false };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PreviewAsync(propertyId));
    }

    [Fact]
    public async Task RulesEngine_InvalidCcOcDateOrder_Reject_RejectsWithClearReason()
    {
        const int propertyId = 372;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // OC dated BEFORE CC -- invalid order.
        var certs = new List<PropertyCertificateEntity>
        {
            BuildCertificate(propertyId, new DateTime(2026, 6, 1), "Completion Certificate"),
            BuildCertificate(propertyId, new DateTime(2025, 1, 1), "Occupancy Certificate"),
        };

        var guideline = DefaultGuideline() with { InvalidCcOcDateOrderAction = "REJECT" };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.PreviewAsync(propertyId));
        ex.Message.Should().Contain("earlier than");
    }

    [Fact]
    public async Task RulesEngine_ElectricBillDateRuleAddMonths_ShiftsDateByAddMonths()
    {
        const int propertyId = 373;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(
            Enumerable.Range(2020, 7).Select(y => new YearMasterEntity { Year = y, Id = y }).ToList().BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // 2025-10-15 is in FY2025 (before FY2026 starts 2026-04-01) -- +6 months shifts it to
        // 2026-04-15, which falls INSIDE the current FY2026, so no retro year should be produced.
        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2025, 10, 15), "Electric Bill") };

        var guideline = DefaultGuideline() with { ElectricBillDateRule = "ADD_MONTHS", ElectricBillAddMonths = 6 };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        var result = await service.PreviewAsync(propertyId);

        result.IsValid.Should().BeTrue();
        result.RetroYears.Should().BeEmpty("the shifted date now falls in the current FY, leaving nothing before it to compute");
        result.CurrentYear!.FinanceYear.Should().Be(2026);
    }

    [Fact]
    public async Task RulesEngine_ElectricBillDateRuleNoTax_IgnoresBill_NoOtherCertificateMeansNoTax()
    {
        const int propertyId = 374;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 5, 15), "Electric Bill") };

        var guideline = DefaultGuideline() with { ElectricBillDateRule = "NO_TAX", EnableRetrospectiveTax = false };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PreviewAsync(propertyId));
    }

    [Fact]
    public async Task RulesEngine_ProrationMethodFullYear_CurrentYearUsesFullFinancialYear()
    {
        const int propertyId = 375;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // OC dated mid-year -- would normally be day-prorated.
        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 6, 15), "Occupancy Certificate") };

        var dailyGuideline = DefaultGuideline(); // ProrationMethod = DAILY (default)
        var dailyService = BuildRulesEngineService(propertyId, certs, dailyGuideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);
        var dailyResult = await dailyService.PreviewAsync(propertyId);

        var fullYearGuideline = DefaultGuideline() with { ProrationMethod = "FULL_YEAR", CurrentYearProrationStartRule = "FULL_YEAR" };
        var fullYearService = BuildRulesEngineService(propertyId, certs, fullYearGuideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);
        var fullYearResult = await fullYearService.PreviewAsync(propertyId);

        dailyResult.CurrentYear!.GeneralTax.Should().BeLessThan(21_900m); // day-prorated, mid-year onset
        fullYearResult.CurrentYear!.GeneralTax.Should().Be(21_900m); // full, unprorated annual GeneralTax
    }

    [Fact]
    public async Task RulesEngine_OcPeriodMultiplier_ScalesOcTax()
    {
        const int propertyId = 376;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // OC dated exactly on the FY start -- a full, unprorated year, so the multiplier's effect
        // is the only thing changing the amount.
        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 4, 1), "Occupancy Certificate") };

        var baselineGuideline = DefaultGuideline(); // OCPeriodMultiplier = 1.0 (no-op)
        var baselineService = BuildRulesEngineService(propertyId, certs, baselineGuideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);
        var baselineResult = await baselineService.PreviewAsync(propertyId);

        var scaledGuideline = DefaultGuideline() with { OCPeriodMultiplier = 1.5m };
        var scaledService = BuildRulesEngineService(propertyId, certs, scaledGuideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);
        var scaledResult = await scaledService.PreviewAsync(propertyId);

        baselineResult.CurrentYear!.GeneralTax.Should().Be(21_900m);
        scaledResult.CurrentYear!.GeneralTax.Should().Be(32_850m); // 21,900 * 1.5
    }

    [Fact]
    public async Task RulesEngine_ElectricBillMultiplier_ScalesElectricBillTax()
    {
        const int propertyId = 377;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // The engine always normalizes an Electric Bill onset to its own FY start (BR2), so any
        // date within FY2026 yields the same full-year baseline regardless of day-of-month.
        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 6, 15), "Electric Bill") };

        var baselineGuideline = DefaultGuideline(); // ElectricBillMultiplier = 1.0 (no-op)
        var baselineService = BuildRulesEngineService(propertyId, certs, baselineGuideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);
        var baselineResult = await baselineService.PreviewAsync(propertyId);

        var scaledGuideline = DefaultGuideline() with { ElectricBillMultiplier = 2.0m };
        var scaledService = BuildRulesEngineService(propertyId, certs, scaledGuideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);
        var scaledResult = await scaledService.PreviewAsync(propertyId);

        baselineResult.CurrentYear!.GeneralTax.Should().Be(21_900m);
        scaledResult.CurrentYear!.GeneralTax.Should().Be(43_800m); // 21,900 * 2.0
    }

    [Fact]
    public async Task RulesEngine_EnableCcToOcSplitFalse_NoSplitEvenWhenGapActionsSuggestMerge()
    {
        const int propertyId = 378;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(
            Enumerable.Range(2021, 6).Select(y => new YearMasterEntity { Year = y, Id = y }).ToList().BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // Gap between CC and OC is 11 years -- way past the 6-month threshold, so
        // CC_OC_GAP_EXCEEDED_ACTION=APPLY_CC_THEN_OC would normally trigger the CC-then-OC merge.
        var certs = new List<PropertyCertificateEntity>
        {
            BuildCertificate(propertyId, new DateTime(2015, 6, 1), "Completion Certificate"),
            BuildCertificate(propertyId, new DateTime(2026, 5, 15), "Occupancy Certificate"),
        };

        var guideline = DefaultGuideline() with { EnableCcToOcSplit = false };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        // DATE_PRIORITY_1 is CC by default, so CC wins outright -- OC never contributes a single year.
        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["CC"]);
        savedPolicy.Should().NotContain(p => p.PolicyCodeId == PolicyCodeIds["OC"] || p.PolicyCodeId == PolicyCodeIds["PARTIAL_OC"]);
    }

    [Fact]
    public async Task RulesEngine_ElectricBillCertificateCodesCustomCode_TreatedAsElectricBill()
    {
        const int propertyId = 379;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // CertificateTypeCode is a custom, guideline-configured value; the display name deliberately
        // does NOT contain "electric"/"electricity"/"bill", so only the CSV-code match (not the name
        // fallback) can recognize this as an Electric Bill certificate.
        var certs = new List<PropertyCertificateEntity>
        {
            BuildCertificate(propertyId, new DateTime(2026, 5, 15), "Utility Statement", certificateTypeCode: "MY_CUSTOM_BILL_CODE")
        };

        var guideline = DefaultGuideline() with { ElectricBillCertificateCodes = "MY_CUSTOM_BILL_CODE" };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_ELECTRIC_BILL"]);
    }

    [Fact]
    public async Task RulesEngine_AllowFloorWiseCertificateMetadataFalse_IgnoresFloorWiseCertificates()
    {
        const int propertyId = 380;

        var floors = new List<PropertyDetailsEntity>
        {
            new() { Id = 8010, PropertyId = propertyId, BuiltupAreaSqMeter = 100 },
        };

        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(floors);

        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // Only a floor-wise certificate exists -- no property-wise certificate at all.
        var floorWiseCert = BuildCertificate(propertyId, new DateTime(2026, 5, 15), "Completion Certificate", propertyDetailsId: 8010);
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity> { floorWiseCert }.BuildMock());

        var guideline = DefaultGuideline() with { AllowFloorWiseCertificateMetadata = false, EnableRetrospectiveTax = false };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object,
            mockCertRepo.Object,
            mockPolicyRepo.Object,
            mockTransRepo.Object,
            mockYearRepo.Object,
            EmptyTaxPendingRepo(),
            EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object,
            mockFyProvider.Object,
            GuidelineService(guideline).Object,
            mockUow.Object,
            NullLogger<OccupationTaxApplicationService>.Instance);

        // The floor-wise certificate is ignored entirely and there is no property-wise fallback,
        // so with the no-certificate retrospective fallback also disabled, nothing is left to tax.
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PreviewAsync(propertyId));
    }

    [Fact]
    public async Task RulesEngine_FloorPolicyDisplayRulePropertyPolicyOnly_DoesNotUseBiggestFloor()
    {
        const int propertyId = 381;

        var floors = new List<PropertyDetailsEntity>
        {
            new() { Id = 8021, PropertyId = propertyId, BuiltupAreaSqMeter = 50 },  // small floor
            new() { Id = 8022, PropertyId = propertyId, BuiltupAreaSqMeter = 200 }, // biggest floor
        };

        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(floors);

        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(
            Enumerable.Range(2022, 5).Select(y => new YearMasterEntity { Year = y, Id = y }).ToList().BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // Biggest floor (8022): CC -- would normally win as representative (BIGGEST_AREA_FLOOR_POLICY).
        var bigFloorCert = BuildCertificate(propertyId, new DateTime(2022, 6, 15), "Completion Certificate", propertyDetailsId: 8022);
        // Small floor (8021): OC.
        var smallFloorCert = BuildCertificate(propertyId, new DateTime(2026, 5, 15), "Occupancy Certificate", propertyDetailsId: 8021);
        // Property-wise: Electric Bill -- this is what PROPERTY_POLICY_ONLY should pick instead.
        var propertyWiseCert = BuildCertificate(propertyId, new DateTime(2022, 1, 15), "Electric Bill");

        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(
            new List<PropertyCertificateEntity> { bigFloorCert, smallFloorCert, propertyWiseCert }.BuildMock());

        var guideline = DefaultGuideline() with { FloorPolicyDisplayRule = "PROPERTY_POLICY_ONLY" };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object,
            mockCertRepo.Object,
            mockPolicyRepo.Object,
            mockTransRepo.Object,
            mockYearRepo.Object,
            EmptyTaxPendingRepo(),
            EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object,
            mockFyProvider.Object,
            GuidelineService(guideline).Object,
            mockUow.Object,
            NullLogger<OccupationTaxApplicationService>.Instance);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["ELECTRIC_BILL"]); // property-wise family, not the biggest floor's CC
    }


    // =========================================================================================
    // GO-LIVE BLOCKER regression: deleting a certificate (metadata, date, or number) must clean up
    // any CC/PARTIAL_CC/OC/PARTIAL_OC/ELECTRIC_BILL/PARTIAL_ELECTRIC_BILL rows left over from a
    // previous valid computation. Root cause: ApplyAsync's `if (!computation.Result.IsValid) {
    // log; return; }` never called SaveTaxesAsync, so nothing ever cleaned up stale rows once a
    // property/floor transitioned from "has a valid certificate" to "has none". Fixed by
    // CleanupStaleCertificateTaxRowsAsync, called from that same branch whenever the rejection
    // reason isn't one of the two global feature-off toggles.
    // =========================================================================================

    [Fact]
    public async Task RulesEngine_PropertyWiseCcDeleted_RemovesStaleCcRows_NettaxUntouched()
    {
        const int propertyId = 390;
        var repo = new Mock<IPropertyRepository>();

        var netTaxRows = StandardNetTaxDetails(propertyId);
        var existingCcPolicy = new PolicyTaxDetailsEntity
        {
            PropertyId = propertyId,
            PolicyCodeId = PolicyCodeIds["CC"],
            TaxId = 1,
            TaxAmount = 1000m,
            IsActive = true,
            MarkedForDeletion = false
        };
        var allPolicyRows = new List<PolicyTaxDetailsEntity> { existingCcPolicy }.Concat(netTaxRows).ToList();

        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => allPolicyRows.BuildMock());

        var existingCcTrans = new TransMastEntity
        {
            PropertyId = propertyId, FinanceYearId = 10, TaxId = 1, TaxAmount = 1000m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false
        };
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(() => new List<TransMastEntity> { existingCcTrans }.BuildMock());

        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity>(); // property-wise CC metadata deleted -- none remain

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        existingCcPolicy.IsActive.Should().BeFalse();
        existingCcPolicy.MarkedForDeletion.Should().BeTrue();
        existingCcTrans.IsActive.Should().BeFalse();
        existingCcTrans.MarkedForDeletion.Should().BeTrue();
        netTaxRows.Should().OnlyContain(nt => nt.IsActive && !nt.MarkedForDeletion, "NETTAX must never be touched by certificate-tax cleanup");
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task RulesEngine_FloorWiseCcDeleted_NoPropertyWiseExists_RemovesStaleCcRows()
    {
        const int propertyId = 391;
        var floors = new List<PropertyDetailsEntity> { new() { Id = 9001, PropertyId = propertyId, BuiltupAreaSqMeter = 100 } };
        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(floors);

        var netTaxRows = StandardNetTaxDetails(propertyId);
        var existingCcPolicy = new PolicyTaxDetailsEntity
        {
            PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["CC"], TaxId = 1,
            TaxAmount = 1000m, IsActive = true, MarkedForDeletion = false
        };
        var allPolicyRows = new List<PolicyTaxDetailsEntity> { existingCcPolicy }.Concat(netTaxRows).ToList();

        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => allPolicyRows.BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity>(); // floor-wise CC metadata deleted, no property-wise cert either

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        existingCcPolicy.IsActive.Should().BeFalse();
        existingCcPolicy.MarkedForDeletion.Should().BeTrue();
        netTaxRows.Should().OnlyContain(nt => nt.IsActive && !nt.MarkedForDeletion);
    }

    [Fact]
    public async Task RulesEngine_FloorWiseCcDeleted_PropertyWiseCcExists_FallsBackWithoutStaleOrDuplicateRow()
    {
        const int propertyId = 392;

        // Two floors: 9101 still has its own floor-wise CC (unaffected); 9102's floor-wise CC was
        // deleted, so it must fall back to the still-existing property-wise CC.
        var floors = new List<PropertyDetailsEntity>
        {
            new() { Id = 9101, PropertyId = propertyId, BuiltupAreaSqMeter = 50 },
            new() { Id = 9102, PropertyId = propertyId, BuiltupAreaSqMeter = 50 },
        };
        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(floors);

        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(
            Enumerable.Range(2021, 6).Select(y => new YearMasterEntity { Year = y, Id = y }).ToList().BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var floorWiseCert = BuildCertificate(propertyId, new DateTime(2022, 6, 15), "Completion Certificate", propertyDetailsId: 9101);
        var propertyWiseCert = BuildCertificate(propertyId, new DateTime(2022, 3, 1), "Completion Certificate"); // fallback source for 9102

        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity> { floorWiseCert, propertyWiseCert }.BuildMock());

        var guideline = DefaultGuideline();
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        await service.ApplyAsync(propertyId, userId: 1);

        // Exactly one set of CC rows persisted (aggregated across both floors) -- no duplicate
        // representative row from the now-gone floor-wise certificate on 9102.
        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["CC"]);
        savedPolicy.GroupBy(p => new { p.TaxId }).Should().OnlyContain(g => g.Count() == 1, "no duplicate rows for the same year/tax slot");
    }

    [Fact]
    public async Task RulesEngine_PropertyWiseCcDeleted_FloorWiseCcExists_AllowFloorWiseTrue_TaxRemainsFromFloorWise()
    {
        const int propertyId = 393;
        var floors = new List<PropertyDetailsEntity> { new() { Id = 9201, PropertyId = propertyId, BuiltupAreaSqMeter = 100 } };
        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(floors);

        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // Property-wise CC deleted -- only the floor-wise CC on 9201 remains.
        var floorWiseCert = BuildCertificate(propertyId, new DateTime(2026, 4, 1), "Completion Certificate", propertyDetailsId: 9201);
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity> { floorWiseCert }.BuildMock());

        var guideline = DefaultGuideline() with { AllowFloorWiseCertificateMetadata = true };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"]);
    }

    [Fact]
    public async Task RulesEngine_OcDeleted_RemovesStaleOcRows_NettaxUntouched()
    {
        const int propertyId = 394;
        var repo = new Mock<IPropertyRepository>();

        var netTaxRows = StandardNetTaxDetails(propertyId);
        var existingOcPolicy = new PolicyTaxDetailsEntity
        {
            PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["OC"], TaxId = 1,
            TaxAmount = 1000m, IsActive = true, MarkedForDeletion = false
        };
        var allPolicyRows = new List<PolicyTaxDetailsEntity> { existingOcPolicy }.Concat(netTaxRows).ToList();

        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => allPolicyRows.BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity>(); // OC certificate deleted

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        existingOcPolicy.IsActive.Should().BeFalse();
        existingOcPolicy.MarkedForDeletion.Should().BeTrue();
        netTaxRows.Should().OnlyContain(nt => nt.IsActive && !nt.MarkedForDeletion);
    }

    [Fact]
    public async Task RulesEngine_ElectricBillDeleted_RemovesStaleElectricBillRows_NettaxUntouched()
    {
        const int propertyId = 395;
        var repo = new Mock<IPropertyRepository>();

        var netTaxRows = StandardNetTaxDetails(propertyId);
        var existingBillPolicy = new PolicyTaxDetailsEntity
        {
            PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["ELECTRIC_BILL"], TaxId = 1,
            TaxAmount = 1000m, IsActive = true, MarkedForDeletion = false
        };
        var allPolicyRows = new List<PolicyTaxDetailsEntity> { existingBillPolicy }.Concat(netTaxRows).ToList();

        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => allPolicyRows.BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity>(); // Electric Bill certificate deleted

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        existingBillPolicy.IsActive.Should().BeFalse();
        existingBillPolicy.MarkedForDeletion.Should().BeTrue();
        netTaxRows.Should().OnlyContain(nt => nt.IsActive && !nt.MarkedForDeletion);
    }

    [Fact]
    public async Task RulesEngine_CertificateIssueDateCleared_RowStillActive_RemovesStaleCcRows()
    {
        const int propertyId = 396;
        var repo = new Mock<IPropertyRepository>();

        var netTaxRows = StandardNetTaxDetails(propertyId);
        var existingCcPolicy = new PolicyTaxDetailsEntity
        {
            PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["CC"], TaxId = 1,
            TaxAmount = 1000m, IsActive = true, MarkedForDeletion = false
        };
        var allPolicyRows = new List<PolicyTaxDetailsEntity> { existingCcPolicy }.Concat(netTaxRows).ToList();

        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => allPolicyRows.BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // Certificate row is still active (not deleted), but its IssueDate has been cleared to
        // null -- ExtractDates then sees no usable CC date at all.
        var cert = PropertyCertificateEntity.Create(propertyId, certificateTypeId: 1, "CERT-001", issueDate: null);
        typeof(PropertyCertificateEntity).GetProperty(nameof(PropertyCertificateEntity.CertificateType))!.SetValue(
            cert, new PropertyCertificateTypeMasterEntity { CertificateTypeName = "Completion Certificate" });
        var certs = new List<PropertyCertificateEntity> { cert };

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        existingCcPolicy.IsActive.Should().BeFalse();
        existingCcPolicy.MarkedForDeletion.Should().BeTrue();
    }

    [Fact]
    public async Task RulesEngine_CertificateNoCleared_RequireNoAndDateOn_RemovesStaleCcRows()
    {
        const int propertyId = 397;
        var repo = new Mock<IPropertyRepository>();

        var netTaxRows = StandardNetTaxDetails(propertyId);
        var existingCcPolicy = new PolicyTaxDetailsEntity
        {
            PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["CC"], TaxId = 1,
            TaxAmount = 1000m, IsActive = true, MarkedForDeletion = false
        };
        var allPolicyRows = new List<PolicyTaxDetailsEntity> { existingCcPolicy }.Concat(netTaxRows).ToList();

        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => allPolicyRows.BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // CertificateNo cleared to blank; date is still present, but CERTIFICATE_REQUIRE_NO_AND_DATE
        // is on and MissingCertificateNoAction=IGNORE_FOR_TAX treats this certificate as absent.
        var cert = PropertyCertificateEntity.Create(propertyId, certificateTypeId: 1, certificateNo: "", issueDate: new DateTime(2026, 5, 1));
        typeof(PropertyCertificateEntity).GetProperty(nameof(PropertyCertificateEntity.CertificateType))!.SetValue(
            cert, new PropertyCertificateTypeMasterEntity { CertificateTypeName = "Completion Certificate" });
        var certs = new List<PropertyCertificateEntity> { cert };

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false, CertificateRequireNoAndDate = true };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        existingCcPolicy.IsActive.Should().BeFalse();
        existingCcPolicy.MarkedForDeletion.Should().BeTrue();
    }

    [Fact]
    public async Task RulesEngine_DocumentDeletedOnly_MetadataStillValid_TaxRemains()
    {
        // Documents the exact distinction the business asked for: unlinking a document does NOT
        // touch CertificateNo/IssueDate (see PropertyCertificateService.UnlinkDocumentBindingAsync),
        // so a still-valid certificate keeps its tax -- SaveTaxesAsync runs normally (reusing the
        // existing row), CleanupStaleCertificateTaxRowsAsync is never reached.
        const int propertyId = 398;
        var repo = new Mock<IPropertyRepository>();

        var netTaxRows = StandardNetTaxDetails(propertyId);
        var existingCcPolicy = new PolicyTaxDetailsEntity
        {
            PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["CC"], TaxId = 1,
            TaxAmount = 500m, IsActive = true, MarkedForDeletion = false
        };
        var allPolicyRows = new List<PolicyTaxDetailsEntity> { existingCcPolicy }.Concat(netTaxRows).ToList();

        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => allPolicyRows.BuildMock());
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(
            Enumerable.Range(2021, 6).Select(y => new YearMasterEntity { Year = y, Id = y }).ToList().BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // Document unlinked, but CertificateNo/IssueDate are untouched -- still a fully valid CC.
        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2022, 6, 15), "Completion Certificate") };

        var guideline = DefaultGuideline();
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        existingCcPolicy.IsActive.Should().BeTrue("document-only removal must not touch a still-valid certificate's tax");
        existingCcPolicy.MarkedForDeletion.Should().BeFalse();
    }

    // =========================================================================================
    // REGRESSION: a floor-wise-only CC certificate (no property-wise certificate at all) produced
    // no tax row whenever CERTIFICATE_TAX_SCOPE_MODE was PROPERTY_WISE, because
    // ResolveUseFloorWiseCertificates treated that value as "ignore floor-wise input entirely" --
    // silently excluding the only certificate the property had, with nothing left to fall back to.
    // Fixed: ALLOW_FLOOR_WISE_CERTIFICATE_METADATA is now the sole gate; CERTIFICATE_TAX_SCOPE_MODE
    // only describes final persistence (always property-aggregated) and never blocks floor-wise
    // input. Every test below deliberately sets CertificateTaxScopeMode = "PROPERTY_WISE" to prove
    // the exact regression condition no longer blocks floor-wise tax.
    // =========================================================================================

    [Fact]
    public async Task RulesEngine_FloorWiseCcAdded_ScopeModePropertyWise_NoPropertyWiseCert_StillCreatesTaxRow()
    {
        const int propertyId = 399;
        var floors = new List<PropertyDetailsEntity> { new() { Id = 9301, PropertyId = propertyId, BuiltupAreaSqMeter = 100 } };
        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(floors);

        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        var savedTrans = new List<TransMastEntity>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        mockTransRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TransMastEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TransMastEntity>, CancellationToken>((entities, _) => savedTrans.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // Floor-wise CC only -- no property-wise certificate exists.
        var floorWiseCert = BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Completion Certificate", propertyDetailsId: 9301);
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity> { floorWiseCert }.BuildMock());

        var guideline = DefaultGuideline() with { CertificateTaxScopeMode = "PROPERTY_WISE", AllowFloorWiseCertificateMetadata = true };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty("a floor-wise-only CC certificate must still produce a tax row even when CERTIFICATE_TAX_SCOPE_MODE=PROPERTY_WISE");
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"]);
        savedTrans.Should().NotBeEmpty("SAVE_CERTIFICATE_TAX_IN_TRANSMAST defaults to 1");
    }

    [Fact]
    public async Task RulesEngine_FloorWiseCcAdded_DateInCurrentFy_UsesPartialCc()
    {
        const int propertyId = 400;
        var floors = new List<PropertyDetailsEntity> { new() { Id = 9302, PropertyId = propertyId, BuiltupAreaSqMeter = 100 } };
        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(floors);

        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // 17-04-2026 -- the exact date from the reported repro, inside current FY2026.
        var floorWiseCert = BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Completion Certificate", propertyDetailsId: 9302);
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity> { floorWiseCert }.BuildMock());

        var guideline = DefaultGuideline() with { CertificateTaxScopeMode = "PROPERTY_WISE" };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"]);
    }

    [Fact]
    public async Task RulesEngine_FloorWiseCcAdded_DateInOldFy_UsesFullCc()
    {
        const int propertyId = 401;
        var floors = new List<PropertyDetailsEntity> { new() { Id = 9303, PropertyId = propertyId, BuiltupAreaSqMeter = 100 } };
        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(floors);

        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(
            Enumerable.Range(2021, 6).Select(y => new YearMasterEntity { Year = y, Id = y }).ToList().BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var floorWiseCert = BuildCertificate(propertyId, new DateTime(2022, 6, 15), "Completion Certificate", propertyDetailsId: 9303);
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity> { floorWiseCert }.BuildMock());

        var guideline = DefaultGuideline() with { CertificateTaxScopeMode = "PROPERTY_WISE" };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().Contain(p => p.PolicyCodeId == PolicyCodeIds["CC"]); // the old-FY row uses the full code
    }

    [Fact]
    public async Task RulesEngine_FloorWiseCcAddThenDelete_RowAppearsThenDisappears_NettaxUntouched()
    {
        const int propertyId = 402;
        var floors = new List<PropertyDetailsEntity> { new() { Id = 9304, PropertyId = propertyId, BuiltupAreaSqMeter = 100 } };
        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(floors);

        var netTaxRows = StandardNetTaxDetails(propertyId);
        var backingPolicyStore = new List<PolicyTaxDetailsEntity>(netTaxRows);
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => backingPolicyStore.BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => backingPolicyStore.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var floorWiseCert = BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Completion Certificate", propertyDetailsId: 9304);
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(() => new List<PropertyCertificateEntity> { floorWiseCert }.BuildMock());

        var guideline = DefaultGuideline() with { CertificateTaxScopeMode = "PROPERTY_WISE", EnableRetrospectiveTax = false };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        // Add.
        await service.ApplyAsync(propertyId, userId: 1);
        backingPolicyStore.Should().Contain(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"] && p.IsActive, "CC row must appear after add");

        // Delete: no certificates remain at all.
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>().BuildMock());
        await service.ApplyAsync(propertyId, userId: 1);

        backingPolicyStore.Where(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"])
            .Should().OnlyContain(p => p.MarkedForDeletion, "CC row must disappear after delete");
        backingPolicyStore.Where(p => p.PolicyCodeId == PolicyCodeIds["NETTAX"])
            .Should().OnlyContain(p => p.IsActive && !p.MarkedForDeletion, "NETTAX must remain throughout");
    }

    [Fact]
    public async Task RulesEngine_PropertyWiseCcAdded_ScopeModePropertyWise_StillWorks()
    {
        const int propertyId = 403;
        var repo = new Mock<IPropertyRepository>();
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        var savedPolicy = new List<PolicyTaxDetailsEntity>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(StandardNetTaxDetails(propertyId).BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => savedPolicy.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Completion Certificate") };

        var guideline = DefaultGuideline() with { CertificateTaxScopeMode = "PROPERTY_WISE" };
        var service = BuildRulesEngineService(propertyId, certs, guideline, repo, mockPolicyRepo, mockTransRepo, mockYearRepo, mockFyProvider, mockUow);

        await service.ApplyAsync(propertyId, userId: 1);

        savedPolicy.Should().NotBeEmpty();
        savedPolicy.Should().OnlyContain(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"]);
    }

    [Fact]
    public async Task RulesEngine_PropertyWiseCcAddThenDelete_RowDisappears()
    {
        const int propertyId = 404;
        var repo = new Mock<IPropertyRepository>();
        var netTaxRows = StandardNetTaxDetails(propertyId);
        var backingPolicyStore = new List<PolicyTaxDetailsEntity>(netTaxRows);
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => backingPolicyStore.BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => backingPolicyStore.AddRange(entities))
            .Returns(Task.CompletedTask);
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Completion Certificate") };
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(certs.BuildMock());

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        await service.ApplyAsync(propertyId, userId: 1);
        backingPolicyStore.Should().Contain(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"] && p.IsActive);

        mockCertRepo.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>().BuildMock());
        await service.ApplyAsync(propertyId, userId: 1);

        backingPolicyStore.Where(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"])
            .Should().OnlyContain(p => p.MarkedForDeletion);
    }

    // =========================================================================================
    // REPRODUCTION for the reported bulk multi-floor duplicate-key investigation: BulkSaveAllAsync
    // saves one certificate row per selected floor sequentially, and each save (via
    // PropertyCertificateService.CreateAsync) publishes PropertyCertificateChangedEvent inline,
    // so ApplyAsync runs once per floor within the SAME logical bulk operation -- the second run
    // sees BOTH floors' certificates already active. This test proves whether today's upsert-by-
    // slot logic in SaveTaxesAsync is already idempotent across repeated ApplyAsync calls that
    // share the same backing store (simulating the same DbContext/tracked-entity instances a real
    // request would reuse), or whether it actually double-inserts.
    // =========================================================================================
    [Fact]
    public async Task RulesEngine_BulkMultiFloorCcSave_SecondApplyAsyncCall_ReusesRowInsteadOfDuplicating()
    {
        const int propertyId = 405;
        var floors = new List<PropertyDetailsEntity>
        {
            new() { Id = 9401, PropertyId = propertyId, BuiltupAreaSqMeter = 100 },
            new() { Id = 9402, PropertyId = propertyId, BuiltupAreaSqMeter = 100 }
        };
        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(floors);

        var backingPolicyStore = new List<PolicyTaxDetailsEntity>(StandardNetTaxDetails(propertyId));
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => backingPolicyStore.BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => backingPolicyStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var backingTransStore = new List<TransMastEntity>();
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(() => backingTransStore.BuildMock());
        mockTransRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TransMastEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TransMastEntity>, CancellationToken>((entities, _) => backingTransStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // Floor A's certificate only, at first -- mirrors BulkSaveAllAsync processing the first
        // selected floor's certDto before the second one has been created yet.
        var floorACert = BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Completion Certificate", propertyDetailsId: 9401);
        var certs = new List<PropertyCertificateEntity> { floorACert };
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(() => certs.BuildMock());

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        // Run 1: only floor A has a certificate (mirrors the event fired right after floor A's
        // certDto is saved, before floor B's certDto is processed).
        await service.ApplyAsync(propertyId, userId: 1);

        // Run 2: floor B now also has the SAME certificate applied (mirrors the event fired right
        // after floor B's certDto is saved, later in the same bulk request) -- same PropertyId,
        // same certificate date, so the aggregated computation lands on the exact same
        // TaxId slots as run 1.
        var floorBCert = BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Completion Certificate", propertyDetailsId: 9402);
        certs.Add(floorBCert);

        var applySecondTime = async () => await service.ApplyAsync(propertyId, userId: 1);
        await applySecondTime.Should().NotThrowAsync("a second floor's certificate save within the same bulk operation must update the existing row, not attempt a duplicate insert");

        // No duplicate active row per PropertyId+PolicyCodeId+TaxId.
        backingPolicyStore
            .Where(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"] && p.IsActive)
            .GroupBy(p => (p.PropertyId, p.PolicyCodeId, p.TaxId))
            .Should().OnlyContain(g => g.Count() == 1, "no duplicate active PolicyTaxDetails row should exist per unique key");

        // No duplicate row per PropertyId+FinanceYearId+TaxId in TransMast.
        backingTransStore
            .Where(t => t.IsActive)
            .GroupBy(t => (t.PropertyId, t.FinanceYearId, t.TaxId))
            .Should().OnlyContain(g => g.Count() == 1, "no duplicate active TransMast row should exist per unique key");

        backingPolicyStore.Should().Contain(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"] && p.IsActive);
    }

    [Fact]
    public async Task RulesEngine_BulkMultiFloorCcSave_ThenAllFloorsDeleted_CleansStaleRows_NettaxUntouched()
    {
        const int propertyId = 406;
        var floors = new List<PropertyDetailsEntity>
        {
            new() { Id = 9403, PropertyId = propertyId, BuiltupAreaSqMeter = 100 },
            new() { Id = 9404, PropertyId = propertyId, BuiltupAreaSqMeter = 100 }
        };
        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(floors);

        var netTaxRows = StandardNetTaxDetails(propertyId);
        var backingPolicyStore = new List<PolicyTaxDetailsEntity>(netTaxRows);
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => backingPolicyStore.BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => backingPolicyStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var backingTransStore = new List<TransMastEntity>();
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(() => backingTransStore.BuildMock());
        mockTransRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TransMastEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TransMastEntity>, CancellationToken>((entities, _) => backingTransStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var floorACert = BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Completion Certificate", propertyDetailsId: 9403);
        var floorBCert = BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Completion Certificate", propertyDetailsId: 9404);
        var certs = new List<PropertyCertificateEntity> { floorACert, floorBCert };
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(() => certs.BuildMock());

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        // Bulk save: both floors carry the same CC certificate.
        await service.ApplyAsync(propertyId, userId: 1);
        backingPolicyStore.Should().Contain(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"] && p.IsActive);

        // Bulk delete: both floors' certificates removed in the same logical operation.
        certs.Clear();
        await service.ApplyAsync(propertyId, userId: 1);

        backingPolicyStore.Where(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"])
            .Should().OnlyContain(p => p.MarkedForDeletion, "stale CC row must be cleaned up once no floor has a certificate left");
        backingPolicyStore.Where(p => p.PolicyCodeId == PolicyCodeIds["NETTAX"])
            .Should().OnlyContain(p => p.IsActive && !p.MarkedForDeletion, "NETTAX must remain untouched throughout");
    }

    // =========================================================================================
    // Follow-up audit: re-verified the bulk multi-floor duplicate-key fix (82a5961c3), the
    // floor-wise scope-mode fix (1609b328d), and the year-wise NETTAX fix (da6ac15ed) are all
    // present and still correct. No currently-reachable code path was found that produces a
    // duplicate finance year within one computed OccupationTaxResult (the pure engine,
    // AggregateFloorResults, and the CC-then-OC merge are each provably duplicate-free by
    // construction). As defense-in-depth per explicit request, UpsertTransMast/UpsertPolicyTaxDetail
    // now also track freshly-created (not-yet-saved) entities by slot so a second call for the same
    // slot within one SaveTaxesAsync invocation updates that entity instead of adding a duplicate,
    // and the CurrentYear/RetroYears list is explicitly deduplicated by FinanceYear before
    // persisting. These tests exercise every scenario requested.
    // =========================================================================================

    [Fact]
    public async Task RulesEngine_BulkMultiFloorCcSave_OneCall_NoDuplicateRows_CcAppears()
    {
        const int propertyId = 505;
        var floors = new List<PropertyDetailsEntity>
        {
            new() { Id = 9501, PropertyId = propertyId, BuiltupAreaSqMeter = 100 },
            new() { Id = 9502, PropertyId = propertyId, BuiltupAreaSqMeter = 100 },
            new() { Id = 9503, PropertyId = propertyId, BuiltupAreaSqMeter = 100 }
        };
        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(floors);

        var backingPolicyStore = new List<PolicyTaxDetailsEntity>(StandardNetTaxDetails(propertyId));
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => backingPolicyStore.BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => backingPolicyStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var backingTransStore = new List<TransMastEntity>();
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(() => backingTransStore.BuildMock());
        mockTransRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TransMastEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TransMastEntity>, CancellationToken>((entities, _) => backingTransStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // All 3 floors carry the exact same CC certificate in ONE bulk request from the very start
        // (mirrors BulkSaveAllAsync's single end-of-batch recalculation, which sees all floors
        // already saved before ApplyAsync ever runs).
        var certs = Enumerable.Range(0, 3)
            .Select(i => BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Completion Certificate", propertyDetailsId: floors[i].Id))
            .ToList();
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(() => certs.BuildMock());

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        var apply = async () => await service.ApplyAsync(propertyId, userId: 1);
        await apply.Should().NotThrowAsync("3 floors saved in one bulk request must never collide on the unique keys");

        backingPolicyStore.Where(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"] && p.IsActive)
            .GroupBy(p => (p.PropertyId, p.PolicyCodeId, p.TaxId))
            .Should().OnlyContain(g => g.Count() == 1, "one active PolicyTaxDetails row per PropertyId+PolicyCodeId+TaxId");
        backingTransStore.Where(t => t.IsActive)
            .GroupBy(t => (t.PropertyId, t.FinanceYearId, t.TaxId))
            .Should().OnlyContain(g => g.Count() == 1, "one TransMast row per PropertyId+FinanceYearId+TaxId");
        backingPolicyStore.Should().Contain(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"] && p.IsActive, "CC/PARTIAL_CC must appear");
    }

    [Fact]
    public async Task RulesEngine_BulkSaveRepeatedAgain_UpdatesExistingRows_NoDuplicateKeyException()
    {
        const int propertyId = 506;
        var floors = new List<PropertyDetailsEntity>
        {
            new() { Id = 9504, PropertyId = propertyId, BuiltupAreaSqMeter = 100 },
            new() { Id = 9505, PropertyId = propertyId, BuiltupAreaSqMeter = 100 }
        };
        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(floors);

        var backingPolicyStore = new List<PolicyTaxDetailsEntity>(StandardNetTaxDetails(propertyId));
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => backingPolicyStore.BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => backingPolicyStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var backingTransStore = new List<TransMastEntity>();
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(() => backingTransStore.BuildMock());
        mockTransRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TransMastEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TransMastEntity>, CancellationToken>((entities, _) => backingTransStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = Enumerable.Range(0, 2)
            .Select(i => BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Completion Certificate", propertyDetailsId: floors[i].Id))
            .ToList();
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(() => certs.BuildMock());

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        // Bulk save once (e.g. certificate number/date entered), then the exact same bulk save is
        // repeated again (e.g. the user re-confirms the popup, or replaces the uploaded file, which
        // triggers the SAME certificate-changed recalculation a second time for the SAME data).
        await service.ApplyAsync(propertyId, userId: 1);
        var activeCountAfterFirst = backingPolicyStore.Count(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"] && p.IsActive);

        var applyAgain = async () => await service.ApplyAsync(propertyId, userId: 1);
        await applyAgain.Should().NotThrowAsync("repeating the same bulk save must update existing rows, never throw a duplicate-key exception");

        var activeCountAfterSecond = backingPolicyStore.Count(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"] && p.IsActive);
        activeCountAfterSecond.Should().Be(activeCountAfterFirst, "the second identical save must update in place, not add more rows");

        backingPolicyStore.Where(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"] && p.IsActive)
            .GroupBy(p => (p.PropertyId, p.PolicyCodeId, p.TaxId))
            .Should().OnlyContain(g => g.Count() == 1);
        backingTransStore.Where(t => t.IsActive)
            .GroupBy(t => (t.PropertyId, t.FinanceYearId, t.TaxId))
            .Should().OnlyContain(g => g.Count() == 1);
    }

    [Fact]
    public async Task RulesEngine_ApplyAsyncCalledTwiceForSameProperty_SecondCallUpdatesSameRows()
    {
        const int propertyId = 507;
        var repo = new Mock<IPropertyRepository>();

        var backingPolicyStore = new List<PolicyTaxDetailsEntity>(StandardNetTaxDetails(propertyId));
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => backingPolicyStore.BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => backingPolicyStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());

        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Occupancy Certificate") };
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(() => certs.BuildMock());

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        await service.ApplyAsync(propertyId, userId: 1);
        var rowAfterFirst = backingPolicyStore.Single(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_OC"] && p.IsActive && p.TaxId == 1);

        await service.ApplyAsync(propertyId, userId: 2);
        var rowAfterSecond = backingPolicyStore.Single(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_OC"] && p.IsActive && p.TaxId == 1);

        rowAfterSecond.Should().BeSameAs(rowAfterFirst, "the second ApplyAsync call must update the SAME row, not add a second one");
        rowAfterSecond.UpdatedBy.Should().Be(2, "the update must reflect the second call's user");
    }

    [Fact]
    public async Task RulesEngine_ExistingActivePolicyTaxDetailsRow_ApplyAsyncUpdatesReactivatesRatherThanInserting()
    {
        const int propertyId = 508;
        var repo = new Mock<IPropertyRepository>();

        var preExistingCcRow = new PolicyTaxDetailsEntity
        {
            PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["PARTIAL_CC"], TaxId = 1,
            TaxAmount = 1m, IsActive = true, MarkedForDeletion = false
        };
        var backingPolicyStore = new List<PolicyTaxDetailsEntity>(StandardNetTaxDetails(propertyId)) { preExistingCcRow };
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => backingPolicyStore.BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => backingPolicyStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());

        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Completion Certificate") };
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(certs.BuildMock());

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        await service.ApplyAsync(propertyId, userId: 1);

        backingPolicyStore.Where(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"] && p.IsActive && p.TaxId == 1)
            .Should().ContainSingle("the pre-existing row for this exact unique key must be updated, not duplicated")
            .Which.Should().BeSameAs(preExistingCcRow);
        preExistingCcRow.TaxAmount.Should().NotBe(1m, "the pre-existing row must be updated with the newly computed amount");
    }

    [Fact]
    public async Task RulesEngine_ExistingActiveTransMastRow_ApplyAsyncUpdatesRatherThanInserting()
    {
        const int propertyId = 509;
        var repo = new Mock<IPropertyRepository>();

        var backingPolicyStore = new List<PolicyTaxDetailsEntity>(StandardNetTaxDetails(propertyId));
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => backingPolicyStore.BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => backingPolicyStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var preExistingTransMast = new TransMastEntity
        {
            PropertyId = propertyId, FinanceYearId = 10, TaxId = 1, TaxAmount = 1m, CalculationType = "RV", IsActive = true, MarkedForDeletion = false
        };
        var backingTransStore = new List<TransMastEntity> { preExistingTransMast };
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(() => backingTransStore.BuildMock());
        mockTransRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TransMastEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TransMastEntity>, CancellationToken>((entities, _) => backingTransStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Completion Certificate") };
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(certs.BuildMock());

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        await service.ApplyAsync(propertyId, userId: 1);

        backingTransStore.Where(t => t.IsActive && t.TaxId == 1)
            .Should().ContainSingle("the pre-existing TransMast row for this exact unique key must be updated, not duplicated")
            .Which.Should().BeSameAs(preExistingTransMast);
        preExistingTransMast.TaxAmount.Should().NotBe(1m, "the pre-existing row must be updated with the newly computed amount");
    }

    [Fact]
    public async Task RulesEngine_MultipleFloorWiseCcCertificates_SamePropertyWiseTaxKey_ProducesOneRowOnly()
    {
        const int propertyId = 510;
        var floors = new List<PropertyDetailsEntity>
        {
            new() { Id = 9506, PropertyId = propertyId, BuiltupAreaSqMeter = 50 },
            new() { Id = 9507, PropertyId = propertyId, BuiltupAreaSqMeter = 50 },
            new() { Id = 9508, PropertyId = propertyId, BuiltupAreaSqMeter = 50 },
            new() { Id = 9509, PropertyId = propertyId, BuiltupAreaSqMeter = 50 }
        };
        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(floors);

        var backingPolicyStore = new List<PolicyTaxDetailsEntity>(StandardNetTaxDetails(propertyId));
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => backingPolicyStore.BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => backingPolicyStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());

        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        // All 4 floors carry their OWN floor-wise CC certificate, all with the exact same date --
        // final persistence is property-wise, so all 4 floors must aggregate to exactly ONE row per
        // unique key, never one row per floor.
        var certs = floors.Select(f => BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Completion Certificate", propertyDetailsId: f.Id)).ToList();
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(certs.BuildMock());

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        await service.ApplyAsync(propertyId, userId: 1);

        backingPolicyStore.Where(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"] && p.IsActive && p.TaxId == 1)
            .Should().ContainSingle("4 floors with the same certificate must aggregate/deduplicate to exactly one property-wise row");
    }

    [Fact]
    public async Task RulesEngine_DeleteAfterBulkSave_RemovesStaleRows_NettaxUntouched()
    {
        const int propertyId = 511;
        var floors = new List<PropertyDetailsEntity>
        {
            new() { Id = 9510, PropertyId = propertyId, BuiltupAreaSqMeter = 100 },
            new() { Id = 9511, PropertyId = propertyId, BuiltupAreaSqMeter = 100 }
        };
        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(floors);

        var netTaxRows = StandardNetTaxDetails(propertyId);
        var backingPolicyStore = new List<PolicyTaxDetailsEntity>(netTaxRows);
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => backingPolicyStore.BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => backingPolicyStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());

        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = floors.Select(f => BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Completion Certificate", propertyDetailsId: f.Id)).ToList();
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(() => certs.BuildMock());

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        await service.ApplyAsync(propertyId, userId: 1);
        backingPolicyStore.Should().Contain(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"] && p.IsActive);

        certs.Clear();
        await service.ApplyAsync(propertyId, userId: 1);

        backingPolicyStore.Where(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"])
            .Should().OnlyContain(p => p.MarkedForDeletion, "stale CC/PARTIAL_CC rows must be removed once no certificate data remains");
        backingPolicyStore.Where(p => p.PolicyCodeId == PolicyCodeIds["NETTAX"])
            .Should().OnlyContain(p => p.IsActive && !p.MarkedForDeletion, "NETTAX must remain untouched");
    }

    [Fact]
    public async Task RulesEngine_OcMultiFloorSave_NoDuplicateRows()
    {
        const int propertyId = 512;
        var floors = new List<PropertyDetailsEntity>
        {
            new() { Id = 9512, PropertyId = propertyId, BuiltupAreaSqMeter = 100 },
            new() { Id = 9513, PropertyId = propertyId, BuiltupAreaSqMeter = 100 }
        };
        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(floors);

        var backingPolicyStore = new List<PolicyTaxDetailsEntity>(StandardNetTaxDetails(propertyId));
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => backingPolicyStore.BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => backingPolicyStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var backingTransStore = new List<TransMastEntity>();
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(() => backingTransStore.BuildMock());
        mockTransRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TransMastEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TransMastEntity>, CancellationToken>((entities, _) => backingTransStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = floors.Select(f => BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Occupancy Certificate", propertyDetailsId: f.Id)).ToList();
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(() => certs.BuildMock());

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        var apply = async () => await service.ApplyAsync(propertyId, userId: 1);
        await apply.Should().NotThrowAsync();

        backingPolicyStore.Where(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_OC"] && p.IsActive)
            .GroupBy(p => (p.PropertyId, p.PolicyCodeId, p.TaxId))
            .Should().OnlyContain(g => g.Count() == 1);
        backingTransStore.Where(t => t.IsActive)
            .GroupBy(t => (t.PropertyId, t.FinanceYearId, t.TaxId))
            .Should().OnlyContain(g => g.Count() == 1);
    }

    [Fact]
    public async Task RulesEngine_ElectricBillMultiFloorSave_NoDuplicateRows()
    {
        const int propertyId = 513;
        var floors = new List<PropertyDetailsEntity>
        {
            new() { Id = 9514, PropertyId = propertyId, BuiltupAreaSqMeter = 100 },
            new() { Id = 9515, PropertyId = propertyId, BuiltupAreaSqMeter = 100 }
        };
        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(floors);

        var backingPolicyStore = new List<PolicyTaxDetailsEntity>(StandardNetTaxDetails(propertyId));
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => backingPolicyStore.BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => backingPolicyStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var backingTransStore = new List<TransMastEntity>();
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(() => backingTransStore.BuildMock());
        mockTransRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TransMastEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TransMastEntity>, CancellationToken>((entities, _) => backingTransStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = floors.Select(f => BuildCertificate(propertyId, new DateTime(2026, 6, 1), "Electricity Bill", propertyDetailsId: f.Id)).ToList();
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(() => certs.BuildMock());

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        var apply = async () => await service.ApplyAsync(propertyId, userId: 1);
        await apply.Should().NotThrowAsync();

        // Bill date (2026-06-01) falls within the current FY (2026), so the current-year row uses
        // PARTIAL_ELECTRIC_BILL, not the full code.
        backingPolicyStore.Where(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_ELECTRIC_BILL"] && p.IsActive)
            .GroupBy(p => (p.PropertyId, p.PolicyCodeId, p.TaxId))
            .Should().OnlyContain(g => g.Count() == 1);
        backingTransStore.Where(t => t.IsActive)
            .GroupBy(t => (t.PropertyId, t.FinanceYearId, t.TaxId))
            .Should().OnlyContain(g => g.Count() == 1);
    }

    // =========================================================================================
    // GO-LIVE BLOCKER: PTIS.TransMast's live unique index (UQ_TransMast_Property_Year_Tax) and
    // PTIS.PolicyTaxDetails' (UX_PolicyTaxDetails_Property_Year_PolicyCode_TaxId) are DBA-managed
    // via a separate SQL project (not EF migrations) and are NOT filtered on IsActive/
    // MarkedForDeletion -- an inactive (soft-deleted) row still physically occupies its unique key.
    // SaveTaxesAsync's "existing rows" lookup previously filtered to IsActive && !MarkedForDeletion,
    // so a row soft-deleted by a prior CleanupStaleCertificateTaxRowsAsync run (or an earlier
    // SaveTaxesAsync's own cleanup) was invisible to the next save, which then took the "insert new"
    // branch and collided with the still-present-but-inactive row on the physical unique constraint.
    // Fixed by loading existing rows by the unique key alone, regardless of active state, and always
    // reactivating.
    // =========================================================================================

    [Fact]
    public async Task RulesEngine_ExistingInactiveTransMastRow_ApplyAsyncReactivatesRatherThanInserting()
    {
        const int propertyId = 514;
        var repo = new Mock<IPropertyRepository>();

        var backingPolicyStore = new List<PolicyTaxDetailsEntity>(StandardNetTaxDetails(propertyId));
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => backingPolicyStore.BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => backingPolicyStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        // Pre-existing TransMast row for the EXACT unique key (PropertyId+FinanceYearId+TaxId) the
        // new computation will need, but already soft-deleted (IsActive=0, MarkedForDeletion=1) --
        // exactly the state a prior certificate delete would have left behind.
        var inactiveTransMast = new TransMastEntity
        {
            PropertyId = propertyId, FinanceYearId = 10, TaxId = 1, TaxAmount = 999m, CalculationType = "RV",
            IsActive = false, MarkedForDeletion = true, MarkedForDeletionDate = DateTime.Now.AddDays(-1)
        };
        var backingTransStore = new List<TransMastEntity> { inactiveTransMast };
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(() => backingTransStore.BuildMock());
        mockTransRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TransMastEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TransMastEntity>, CancellationToken>((entities, _) => backingTransStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Completion Certificate") };
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(certs.BuildMock());

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        var apply = async () => await service.ApplyAsync(propertyId, userId: 1);
        await apply.Should().NotThrowAsync("an inactive row occupying the unique key must be reactivated, never collide with a new insert");

        backingTransStore.Where(t => t.TaxId == 1)
            .Should().ContainSingle("the inactive row must be reactivated in place, not left behind alongside a new row")
            .Which.Should().BeSameAs(inactiveTransMast);
        inactiveTransMast.IsActive.Should().BeTrue();
        inactiveTransMast.MarkedForDeletion.Should().BeFalse();
        inactiveTransMast.MarkedForDeletionDate.Should().BeNull();
        inactiveTransMast.TaxAmount.Should().NotBe(999m, "the reactivated row must carry the newly computed amount");
    }

    [Fact]
    public async Task RulesEngine_ExistingInactivePolicyTaxDetailsRow_ApplyAsyncReactivatesRatherThanInserting()
    {
        const int propertyId = 515;
        var repo = new Mock<IPropertyRepository>();

        // Pre-existing PolicyTaxDetails row for the EXACT unique key
        // (PropertyId+PolicyCodeId+TaxId) the new computation will need, but already soft-deleted.
        var inactiveCcRow = new PolicyTaxDetailsEntity
        {
            PropertyId = propertyId, PolicyCodeId = PolicyCodeIds["PARTIAL_CC"], TaxId = 1,
            TaxAmount = 999m, IsActive = false, MarkedForDeletion = true, MarkedForDeletionDate = DateTime.Now.AddDays(-1)
        };
        var backingPolicyStore = new List<PolicyTaxDetailsEntity>(StandardNetTaxDetails(propertyId)) { inactiveCcRow };
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => backingPolicyStore.BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => backingPolicyStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());

        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Completion Certificate") };
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(certs.BuildMock());

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        var apply = async () => await service.ApplyAsync(propertyId, userId: 1);
        await apply.Should().NotThrowAsync("an inactive PolicyTaxDetails row occupying the unique key must be reactivated, never collide with a new insert");

        backingPolicyStore.Where(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"] && p.TaxId == 1)
            .Should().ContainSingle("the inactive row must be reactivated in place, not left behind alongside a new row")
            .Which.Should().BeSameAs(inactiveCcRow);
        inactiveCcRow.IsActive.Should().BeTrue();
        inactiveCcRow.MarkedForDeletion.Should().BeFalse();
        inactiveCcRow.MarkedForDeletionDate.Should().BeNull();
        inactiveCcRow.TaxAmount.Should().NotBe(999m, "the reactivated row must carry the newly computed amount");
    }

    [Fact]
    public async Task RulesEngine_CertificateAddThenDeleteThenAddAgain_ReactivatesWithoutDuplicateKeyException()
    {
        const int propertyId = 516;
        var repo = new Mock<IPropertyRepository>();

        var backingPolicyStore = new List<PolicyTaxDetailsEntity>(StandardNetTaxDetails(propertyId));
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => backingPolicyStore.BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => backingPolicyStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var backingTransStore = new List<TransMastEntity>();
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(() => backingTransStore.BuildMock());
        mockTransRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TransMastEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TransMastEntity>, CancellationToken>((entities, _) => backingTransStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Completion Certificate") };
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(() => certs.BuildMock());

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        // Add.
        await service.ApplyAsync(propertyId, userId: 1);
        backingPolicyStore.Should().Contain(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"] && p.IsActive);

        // Delete: certificate removed -> stale rows soft-deleted (IsActive=0, MarkedForDeletion=1).
        certs.Clear();
        await service.ApplyAsync(propertyId, userId: 1);
        backingPolicyStore.Where(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"])
            .Should().OnlyContain(p => p.MarkedForDeletion);

        // Add again: same certificate re-added -- must reactivate the soft-deleted row, not collide
        // with it on the unique key.
        certs.Add(BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Completion Certificate"));
        var applyThirdTime = async () => await service.ApplyAsync(propertyId, userId: 1);
        await applyThirdTime.Should().NotThrowAsync("re-adding after delete must reactivate the existing row, never throw a duplicate-key exception");

        backingPolicyStore.Where(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"])
            .GroupBy(p => (p.PropertyId, p.PolicyCodeId, p.TaxId))
            .Should().OnlyContain(g => g.Count() == 1, "no duplicate row should exist after the add-delete-add cycle");
        backingPolicyStore.Should().Contain(p => p.PolicyCodeId == PolicyCodeIds["PARTIAL_CC"] && p.IsActive,
            "Tax Details must show the CC row active again after the second add");
    }

    [Fact]
    public async Task RulesEngine_BulkMultiFloorCcSave_AfterOldInactiveTransMastRowsExist_NoDuplicateKeyException()
    {
        const int propertyId = 517;
        var floors = new List<PropertyDetailsEntity>
        {
            new() { Id = 9516, PropertyId = propertyId, BuiltupAreaSqMeter = 100 },
            new() { Id = 9517, PropertyId = propertyId, BuiltupAreaSqMeter = 100 },
            new() { Id = 9518, PropertyId = propertyId, BuiltupAreaSqMeter = 100 }
        };
        var repo = new Mock<IPropertyRepository>();
        repo.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(floors);

        var backingPolicyStore = new List<PolicyTaxDetailsEntity>(StandardNetTaxDetails(propertyId));
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => backingPolicyStore.BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => backingPolicyStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        // Old inactive TransMast rows already occupy the exact unique keys this bulk save will need
        // -- mirrors a property whose certificate tax was previously added then deleted (leaving
        // soft-deleted rows behind), and is now being re-saved with a bulk multi-floor CC.
        var oldInactiveGeneral = new TransMastEntity
        {
            PropertyId = propertyId, FinanceYearId = 10, TaxId = 1, TaxAmount = 999m, CalculationType = "RV",
            IsActive = false, MarkedForDeletion = true, MarkedForDeletionDate = DateTime.Now.AddDays(-2)
        };
        var oldInactiveComponent = new TransMastEntity
        {
            PropertyId = propertyId, FinanceYearId = 10, TaxId = 2, TaxAmount = 999m, CalculationType = "RV",
            IsActive = false, MarkedForDeletion = true, MarkedForDeletionDate = DateTime.Now.AddDays(-2)
        };
        var backingTransStore = new List<TransMastEntity> { oldInactiveGeneral, oldInactiveComponent };
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(() => backingTransStore.BuildMock());
        mockTransRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TransMastEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TransMastEntity>, CancellationToken>((entities, _) => backingTransStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = floors.Select(f => BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Completion Certificate", propertyDetailsId: f.Id)).ToList();
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(() => certs.BuildMock());

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        var apply = async () => await service.ApplyAsync(propertyId, userId: 1);
        await apply.Should().NotThrowAsync("old inactive TransMast rows must be reactivated, never block the bulk multi-floor save");

        backingTransStore.Where(t => t.IsActive)
            .GroupBy(t => (t.PropertyId, t.FinanceYearId, t.TaxId))
            .Should().OnlyContain(g => g.Count() == 1, "property-wise rows must be saved correctly with no duplicates");
        oldInactiveGeneral.IsActive.Should().BeTrue("the old row must be reactivated, not orphaned alongside a new row");
        oldInactiveComponent.IsActive.Should().BeTrue();
        backingPolicyStore.Where(p => p.PolicyCodeId == PolicyCodeIds["NETTAX"])
            .Should().OnlyContain(p => p.IsActive && !p.MarkedForDeletion, "NETTAX must remain untouched");
    }

    [Fact]
    public async Task RulesEngine_ExistingInactiveOcTransMastRow_ApplyAsyncReactivatesRatherThanInserting()
    {
        const int propertyId = 518;
        var repo = new Mock<IPropertyRepository>();

        var backingPolicyStore = new List<PolicyTaxDetailsEntity>(StandardNetTaxDetails(propertyId));
        var mockPolicyRepo = new Mock<IRepository<PolicyTaxDetailsEntity, int>>();
        mockPolicyRepo.Setup(r => r.GetQueryable()).Returns(() => backingPolicyStore.BuildMock());
        mockPolicyRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<PolicyTaxDetailsEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PolicyTaxDetailsEntity>, CancellationToken>((entities, _) => backingPolicyStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var inactiveOcTransMast = new TransMastEntity
        {
            PropertyId = propertyId, FinanceYearId = 10, TaxId = 1, TaxAmount = 999m, CalculationType = "RV",
            IsActive = false, MarkedForDeletion = true, MarkedForDeletionDate = DateTime.Now.AddDays(-1)
        };
        var backingTransStore = new List<TransMastEntity> { inactiveOcTransMast };
        var mockTransRepo = new Mock<IRepository<TransMastEntity, int>>();
        mockTransRepo.Setup(r => r.GetQueryable()).Returns(() => backingTransStore.BuildMock());
        mockTransRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<TransMastEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TransMastEntity>, CancellationToken>((entities, _) => backingTransStore.AddRange(entities))
            .Returns(Task.CompletedTask);

        var mockYearRepo = new Mock<IRepository<YearMasterEntity, int>>();
        mockYearRepo.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Year = 2026, Id = 10 } }.BuildMock());
        var mockFyProvider = new Mock<IFinanceYearProvider>();
        mockFyProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2026);
        var mockUow = new Mock<IUnitOfWork>();

        var certs = new List<PropertyCertificateEntity> { BuildCertificate(propertyId, new DateTime(2026, 4, 17), "Occupancy Certificate") };
        var mockCertRepo = new Mock<IRepository<PropertyCertificateEntity, int>>();
        mockCertRepo.Setup(r => r.GetQueryable()).Returns(certs.BuildMock());

        var guideline = DefaultGuideline() with { EnableRetrospectiveTax = false };
        var service = new OccupationTaxApplicationService(
            new OccupationTaxEngine(NullLogger<OccupationTaxEngine>.Instance),
            repo.Object, mockCertRepo.Object, mockPolicyRepo.Object, mockTransRepo.Object, mockYearRepo.Object,
            EmptyTaxPendingRepo(), EmptyTaxPendingRetroRepo(),
            PolicyCodeLookup().Object, mockFyProvider.Object, GuidelineService(guideline).Object,
            mockUow.Object, NullLogger<OccupationTaxApplicationService>.Instance);

        var apply = async () => await service.ApplyAsync(propertyId, userId: 1);
        await apply.Should().NotThrowAsync("an inactive OC TransMast row occupying the unique key must be reactivated, never collide with a new insert");

        backingTransStore.Where(t => t.TaxId == 1)
            .Should().ContainSingle("the inactive OC row must be reactivated in place, not left behind alongside a new row")
            .Which.Should().BeSameAs(inactiveOcTransMast);
        inactiveOcTransMast.IsActive.Should().BeTrue();
        inactiveOcTransMast.MarkedForDeletion.Should().BeFalse();
    }
}

