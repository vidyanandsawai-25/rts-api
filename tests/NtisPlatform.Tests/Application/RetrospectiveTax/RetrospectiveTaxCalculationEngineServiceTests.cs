using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.RateableValue;
using NtisPlatform.Application.DTOs.RetrospectiveTax;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services.RetrospectiveTax;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.RetrospectiveTax;

public class RetrospectiveTaxCalculationEngineServiceTests
{
    private const int OcEvidenceTypeId = 1;
    private const int CcEvidenceTypeId = 2;
    private const int ElectricityEvidenceTypeId = 3;
    private const int ChangeDetectionEvidenceTypeId = 4;

    private readonly Mock<IRepository<RetrospectiveRuleMasterEntity, int>> _mockRuleRepository = new();
    private readonly Mock<IRepository<RetrospectiveRuleEvidenceConditionEntity, int>> _mockEvidenceConditionRepository = new();
    private readonly Mock<IRepository<RetrospectiveRuleDateConditionEntity, int>> _mockDateConditionRepository = new();
    private readonly Mock<IRepository<RetrospectiveRuleActionEntity, int>> _mockActionRepository = new();
    private readonly Mock<IRepository<RetrospectivePenaltyRuleEntity, int>> _mockPenaltyRepository = new();
    private readonly Mock<IRepository<EvidenceTypeMasterEntity, int>> _mockEvidenceTypeRepository = new();
    private readonly Mock<IRepository<PropertyCertificateEntity, int>> _mockCertificateRepository = new();
    private readonly Mock<IRepository<PropertyCertificateTypeMasterEntity, int>> _mockCertificateTypeRepository = new();
    private readonly Mock<IRepository<PropertyDetailsEntity, int>> _mockPropertyDetailsRepository = new();
    private readonly Mock<IRepository<PropertyEntity, int>> _mockPropertyRepository = new();
    private readonly Mock<IRepository<WingDetailsMastEntity, int>> _mockWingDetailsMastRepository = new();
    private readonly Mock<IRepository<SocietyDetailsEntity, int>> _mockSocietyRepository = new();
    private readonly Mock<IRepository<RetrospectiveTaxCalculationEntity, int>> _mockCalculationRepository = new();
    private readonly Mock<IRepository<RetrospectiveTaxCalculationDetailEntity, int>> _mockCalculationDetailRepository = new();
    private readonly Mock<IRepository<PolicyTaxDetailsEntity, int>> _mockPolicyTaxDetailsRepository = new();
    private readonly Mock<IRepository<TransMastEntity, int>> _mockTransMastRepository = new();
    private readonly Mock<IRepository<YearMasterEntity, int>> _mockYearRepository = new();
    private readonly Mock<IRateableValueService> _mockRateableValueService = new();
    private readonly Mock<IFinanceYearProvider> _mockFinanceYearProvider = new();
    private readonly Mock<IPolicyCodeLookupService> _mockPolicyCodeLookup = new();
    private readonly Mock<ITaxApplicabilityService> _mockTaxApplicabilityService = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly RetrospectiveTaxCalculationEngineService _service;

    public RetrospectiveTaxCalculationEngineServiceTests()
    {
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(new List<EvidenceTypeMasterEntity>
        {
            new() { Id = OcEvidenceTypeId, EvidenceCode = "OC", IsActive = true },
            new() { Id = CcEvidenceTypeId, EvidenceCode = "CC", IsActive = true },
            new() { Id = ElectricityEvidenceTypeId, EvidenceCode = "ELECTRICITY", IsActive = true },
            new() { Id = ChangeDetectionEvidenceTypeId, EvidenceCode = "CHANGE_DETECTION", IsActive = true },
        }.BuildMock());

        _mockPropertyDetailsRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyDetailsEntity>().BuildMock());
        _mockPropertyRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity>().BuildMock());
        _mockWingDetailsMastRepository.Setup(r => r.GetQueryable()).Returns(new List<WingDetailsMastEntity>().BuildMock());
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(new List<SocietyDetailsEntity>().BuildMock());

        _mockCertificateTypeRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateTypeMasterEntity>
        {
            new() { Id = 10, CertificateTypeCode = "OC" },
            new() { Id = 11, CertificateTypeCode = "CC" },
            new() { Id = 12, CertificateTypeCode = "ELECTRIC_BILL" },
            new() { Id = 13, CertificateTypeCode = "CHANGE_DETECTION" },
        }.BuildMock());

        _mockCalculationRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveTaxCalculationEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveTaxCalculationEntity e, CancellationToken _) => { e.Id = 501; return e; });
        _mockCalculationDetailRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveTaxCalculationDetailEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveTaxCalculationDetailEntity e, CancellationToken _) => e);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _mockFinanceYearProvider.Setup(p => p.GetCurrentFinanceYear()).Returns(2025);

        // Empty by default: no existing NETTAX rows / no YearMaster row means
        // SyncPolicyTaxAndTransMastAsync's write branch never fires, so most tests (which only
        // assert on RetrospectiveTaxCalculation/Detail, not PolicyTaxDetails/TransMast) are
        // unaffected. Tests that DO exercise persistence override these individually.
        _mockPolicyTaxDetailsRepository.Setup(r => r.GetQueryable()).Returns(new List<PolicyTaxDetailsEntity>().BuildMock());
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity>().BuildMock());
        _mockYearRepository.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity>().BuildMock());
        _mockPolicyCodeLookup.Setup(s => s.GetIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(99);
        _mockPolicyCodeLookup.Setup(s => s.GetIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>
            {
                ["OC"] = 101, ["PARTIAL_OC"] = 102, ["CC"] = 103, ["PARTIAL_CC"] = 104,
                ["ELECTRIC_BILL"] = 105, ["PARTIAL_ELECTRIC_BILL"] = 106
            });
        // Empty by default: OLD_ARREARS is optional (not seeded in every environment), so its
        // absence must not fail the recalculation -- see DeactivateOldArrearsAsync. Tests that DO
        // exercise the OLD_ARREARS-replaced-by-retro path override this individually.
        _mockPolicyCodeLookup.Setup(s => s.GetExistingIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());
        _mockTaxApplicabilityService.Setup(s => s.GetExemptedTaxIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int>());

        _service = new RetrospectiveTaxCalculationEngineService(
            _mockRuleRepository.Object,
            _mockEvidenceConditionRepository.Object,
            _mockDateConditionRepository.Object,
            _mockActionRepository.Object,
            _mockPenaltyRepository.Object,
            _mockEvidenceTypeRepository.Object,
            _mockCertificateRepository.Object,
            _mockCertificateTypeRepository.Object,
            _mockPropertyDetailsRepository.Object,
            _mockPropertyRepository.Object,
            _mockWingDetailsMastRepository.Object,
            _mockSocietyRepository.Object,
            _mockCalculationRepository.Object,
            _mockCalculationDetailRepository.Object,
            _mockPolicyTaxDetailsRepository.Object,
            _mockTransMastRepository.Object,
            _mockYearRepository.Object,
            _mockRateableValueService.Object,
            _mockFinanceYearProvider.Object,
            _mockPolicyCodeLookup.Object,
            _mockTaxApplicabilityService.Object,
            _mockUnitOfWork.Object);
    }

    private static readonly Dictionary<string, int> CertificateTypeIdByCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["OC"] = 10,
        ["CC"] = 11,
        ["ELECTRIC_BILL"] = 12,
        ["CHANGE_DETECTION"] = 13,
    };

    private static PropertyCertificateEntity Certificate(int propertyId, string typeCode, DateTime issueDate) =>
        PropertyCertificateEntity.Create(propertyId, certificateTypeId: CertificateTypeIdByCode[typeCode], certificateNo: "X", issueDate: issueDate);

    private void SetupRule(
        RetrospectiveRuleMasterEntity rule,
        List<RetrospectiveRuleEvidenceConditionEntity> evidenceConditions,
        RetrospectiveRuleActionEntity action,
        RetrospectiveRuleDateConditionEntity? dateCondition = null,
        RetrospectivePenaltyRuleEntity? penalty = null)
    {
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleMasterEntity> { rule }.BuildMock());
        _mockEvidenceConditionRepository.Setup(r => r.GetQueryable()).Returns(evidenceConditions.BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity> { action }.BuildMock());
        _mockDateConditionRepository.Setup(r => r.GetQueryable()).Returns(
            (dateCondition is null ? new List<RetrospectiveRuleDateConditionEntity>() : new List<RetrospectiveRuleDateConditionEntity> { dateCondition }).BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(
            (penalty is null ? new List<RetrospectivePenaltyRuleEntity>() : new List<RetrospectivePenaltyRuleEntity> { penalty }).BuildMock());
    }

    /// <summary>
    /// Registers TWO competing rules at once (SetupRule only supports one), for tests proving the
    /// EVIDENCE_GAP_WITHIN_PERIOD comparator picks between them by the CC/OC gap.
    /// </summary>
    private void SetupRules(
        List<(RetrospectiveRuleMasterEntity Rule, List<RetrospectiveRuleEvidenceConditionEntity> EvidenceConditions, RetrospectiveRuleActionEntity Action, RetrospectiveRuleDateConditionEntity DateCondition)> rules)
    {
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(rules.Select(r => r.Rule).ToList().BuildMock());
        _mockEvidenceConditionRepository.Setup(r => r.GetQueryable()).Returns(rules.SelectMany(r => r.EvidenceConditions).ToList().BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(rules.Select(r => r.Action).ToList().BuildMock());
        _mockDateConditionRepository.Setup(r => r.GetQueryable()).Returns(rules.Select(r => r.DateCondition).ToList().BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity>().BuildMock());
    }

    [Theory]
    [InlineData("2023-01-01", "2023-05-01", "GAP-WITHIN")] // ~4 month gap -> within 6 months -> OC alone
    [InlineData("2020-05-01", "2023-06-01", "GAP-EXCEEDS")] // multi-year gap -> exceeds 6 months -> CC-then-OC merge
    public async Task CalculateAndSaveAsync_EvidenceGapWithinPeriod_SelectsRuleByCcToOcGap(string ccDate, string ocDate, string expectedRuleCode)
    {
        const int propertyId = 400;
        var evidenceConditions = new List<RetrospectiveRuleEvidenceConditionEntity>
        {
            new() { RuleId = 20, EvidenceTypeId = CcEvidenceTypeId, EvidenceState = "AVAILABLE", IsActive = true },
            new() { RuleId = 20, EvidenceTypeId = OcEvidenceTypeId, EvidenceState = "AVAILABLE", IsActive = true },
            new() { RuleId = 21, EvidenceTypeId = CcEvidenceTypeId, EvidenceState = "AVAILABLE", IsActive = true },
            new() { RuleId = 21, EvidenceTypeId = OcEvidenceTypeId, EvidenceState = "AVAILABLE", IsActive = true },
        };

        var withinRule = new RetrospectiveRuleMasterEntity { Id = 20, RuleCode = "GAP-WITHIN", RuleName = "Gap within 6 months", PriorityNo = 1, IsFallbackRule = false, RuleStatus = "Active", IsActive = true };
        var withinAction = new RetrospectiveRuleActionEntity
        {
            Id = 20, RuleId = 20, TaxStartMode = "EVIDENCE_DATE", StartEvidenceTypeId = OcEvidenceTypeId,
            RetrospectiveLimitType = "NONE", TaxCalculationMode = "SINGLE", TaxMultiplier = 1.00m, RateMode = "YEAR_WISE", IsActive = true
        };
        var withinDateCondition = new RetrospectiveRuleDateConditionEntity
        {
            RuleId = 20, ComparatorCode = "EVIDENCE_GAP_WITHIN_PERIOD", LeftEvidenceTypeId = CcEvidenceTypeId, RightEvidenceTypeId = OcEvidenceTypeId,
            CompareOperator = "WITHIN_YEARS", CompareYears = 6, CompareGapUnit = "MONTHS", IsActive = true
        };

        var exceedsRule = new RetrospectiveRuleMasterEntity { Id = 21, RuleCode = "GAP-EXCEEDS", RuleName = "Gap exceeds 6 months", PriorityNo = 2, IsFallbackRule = false, RuleStatus = "Active", IsActive = true };
        var exceedsAction = new RetrospectiveRuleActionEntity
        {
            Id = 21, RuleId = 21, TaxStartMode = "EVIDENCE_DATE", StartEvidenceTypeId = CcEvidenceTypeId,
            RetrospectiveLimitType = "NONE", TaxCalculationMode = "CC_THEN_OC_MERGE", TaxMultiplier = 1.00m,
            SplitEndEvidenceTypeId = OcEvidenceTypeId, SplitMultiplier = 1.50m, AfterSplitMultiplier = 1.00m, RateMode = "YEAR_WISE", IsActive = true
        };
        var exceedsDateCondition = new RetrospectiveRuleDateConditionEntity
        {
            RuleId = 21, ComparatorCode = "EVIDENCE_GAP_WITHIN_PERIOD", LeftEvidenceTypeId = CcEvidenceTypeId, RightEvidenceTypeId = OcEvidenceTypeId,
            CompareOperator = "OLDER_THAN_YEARS", CompareYears = 6, CompareGapUnit = "MONTHS", IsActive = true
        };

        SetupRules(new()
        {
            (withinRule, evidenceConditions.Where(c => c.RuleId == 20).ToList(), withinAction, withinDateCondition),
            (exceedsRule, evidenceConditions.Where(c => c.RuleId == 21).ToList(), exceedsAction, exceedsDateCondition),
        });

        _mockCertificateRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>
        {
            Certificate(propertyId, "CC", DateTime.Parse(ccDate)),
            Certificate(propertyId, "OC", DateTime.Parse(ocDate))
        }.BuildMock());

        _mockRateableValueService
            .Setup(s => s.PreviewTotalTaxAsync(propertyId, It.IsAny<int>()))
            .ReturnsAsync((int _, int year) => new RetrospectiveRatePreviewDto { PropertyId = propertyId, FinanceYear = year, TotalTaxAmount = 1000m });

        var result = await _service.CalculateAndSaveAsync(propertyId, calculatedBy: 9, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(expectedRuleCode, result!.AppliedRuleCode);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_OcOnlyRule_MatchesAndComputesYearWiseRetroTax()
    {
        const int propertyId = 100;
        var rule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "THA-01", RuleName = "OC only", PriorityNo = 1, IsFallbackRule = false, RuleStatus = "Active", IsActive = true };
        var evidenceConditions = new List<RetrospectiveRuleEvidenceConditionEntity>
        {
            new() { RuleId = 1, EvidenceTypeId = OcEvidenceTypeId, EvidenceState = "AVAILABLE", IsActive = true }
        };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 1, RuleId = 1, TaxStartMode = "EVIDENCE_DATE", StartEvidenceTypeId = OcEvidenceTypeId,
            RetrospectiveLimitType = "FIXED_CUTOFF_DATE", CutoffDate = new DateTime(2016, 4, 1),
            TaxCalculationMode = "SINGLE", TaxMultiplier = 1.00m, RateMode = "YEAR_WISE", IsActive = true
        };
        SetupRule(rule, evidenceConditions, action);

        _mockCertificateRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>
        {
            Certificate(propertyId, "OC", new DateTime(2019, 6, 15))
        }.BuildMock());

        _mockRateableValueService
            .Setup(s => s.PreviewTotalTaxAsync(propertyId, It.IsAny<int>()))
            .ReturnsAsync((int _, int year) => new RetrospectiveRatePreviewDto { PropertyId = propertyId, FinanceYear = year, TotalTaxAmount = 1000m + year });

        var result = await _service.CalculateAndSaveAsync(propertyId, calculatedBy: 9, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("THA-01", result!.AppliedRuleCode);
        Assert.Equal(new DateTime(2019, 6, 15), result.ChargeableStartDate);
        Assert.Equal(7, result.RetroYearCount); // FY2019 .. FY2025 inclusive
        Assert.Equal(7, result.YearWiseBreakdown.Count);
        Assert.All(result.YearWiseBreakdown, y => Assert.Equal("YEAR_WISE", y.RateMode));
        // YEAR_WISE must price each year with that year's own rate, not the current year's.
        _mockRateableValueService.Verify(s => s.PreviewTotalTaxAsync(propertyId, 2019), Times.Once);
        _mockRateableValueService.Verify(s => s.PreviewTotalTaxAsync(propertyId, 2025), Times.Once);
        _mockCalculationDetailRepository.Verify(
            r => r.AddAsync(It.IsAny<RetrospectiveTaxCalculationDetailEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(7));
    }

    [Fact]
    public async Task CalculateAndSaveAsync_NoDirectCertificate_FallsBackToWingScopedCertificate()
    {
        // A unit with no Property-level OC certificate of its own is still covered by a
        // Wing-scoped OC certificate (created via the "Add Certificate Record" Wing-level flow --
        // EntityType 'W', PropertyId null) applying to its whole wing.
        const int propertyId = 100;
        const int wingDetailId = 20;

        var rule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "THA-01", RuleName = "OC only", PriorityNo = 1, IsFallbackRule = false, RuleStatus = "Active", IsActive = true };
        var evidenceConditions = new List<RetrospectiveRuleEvidenceConditionEntity>
        {
            new() { RuleId = 1, EvidenceTypeId = OcEvidenceTypeId, EvidenceState = "AVAILABLE", IsActive = true }
        };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 1, RuleId = 1, TaxStartMode = "EVIDENCE_DATE", StartEvidenceTypeId = OcEvidenceTypeId,
            RetrospectiveLimitType = "FIXED_CUTOFF_DATE", CutoffDate = new DateTime(2016, 4, 1),
            TaxCalculationMode = "SINGLE", TaxMultiplier = 1.00m, RateMode = "YEAR_WISE", IsActive = true
        };
        SetupRule(rule, evidenceConditions, action);

        // No certificate carries this unit's own PropertyId -- only a Wing-scoped row for its wing.
        var wingCert = PropertyCertificateEntity.Create(
            propertyId: null, certificateTypeId: CertificateTypeIdByCode["OC"], certificateNo: "WING-OC",
            issueDate: new DateTime(2019, 6, 15), entityType: "W", societyDetailId: 5, wingDetailId: wingDetailId);
        _mockCertificateRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity> { wingCert }.BuildMock());

        _mockPropertyRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity>
        {
            new() { Id = propertyId, WingDetailId = wingDetailId, IsActive = true }
        }.BuildMock());

        _mockRateableValueService
            .Setup(s => s.PreviewTotalTaxAsync(propertyId, It.IsAny<int>()))
            .ReturnsAsync((int _, int year) => new RetrospectiveRatePreviewDto { PropertyId = propertyId, FinanceYear = year, TotalTaxAmount = 1000m + year });

        var result = await _service.CalculateAndSaveAsync(propertyId, calculatedBy: 9, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("THA-01", result!.AppliedRuleCode);
        Assert.Equal(new DateTime(2019, 6, 15), result.ChargeableStartDate);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_NoDirectOrWingCertificate_FallsBackToSocietyScopedCertificate()
    {
        // A unit with neither a Property-level nor a Wing-level OC certificate is still covered by
        // a Society-scoped OC certificate (EntityType 'S', PropertyId null) applying to every unit
        // in the society, resolved via the unit's wing -> society chain.
        const int propertyId = 100;
        const int wingDetailId = 20;
        const int societyDetailId = 5;

        var rule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "THA-01", RuleName = "OC only", PriorityNo = 1, IsFallbackRule = false, RuleStatus = "Active", IsActive = true };
        var evidenceConditions = new List<RetrospectiveRuleEvidenceConditionEntity>
        {
            new() { RuleId = 1, EvidenceTypeId = OcEvidenceTypeId, EvidenceState = "AVAILABLE", IsActive = true }
        };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 1, RuleId = 1, TaxStartMode = "EVIDENCE_DATE", StartEvidenceTypeId = OcEvidenceTypeId,
            RetrospectiveLimitType = "FIXED_CUTOFF_DATE", CutoffDate = new DateTime(2016, 4, 1),
            TaxCalculationMode = "SINGLE", TaxMultiplier = 1.00m, RateMode = "YEAR_WISE", IsActive = true
        };
        SetupRule(rule, evidenceConditions, action);

        var societyCert = PropertyCertificateEntity.Create(
            propertyId: null, certificateTypeId: CertificateTypeIdByCode["OC"], certificateNo: "SOCIETY-OC",
            issueDate: new DateTime(2019, 6, 15), entityType: "S", societyDetailId: societyDetailId);
        _mockCertificateRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity> { societyCert }.BuildMock());

        _mockPropertyRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity>
        {
            new() { Id = propertyId, WingDetailId = wingDetailId, IsActive = true }
        }.BuildMock());
        _mockWingDetailsMastRepository.Setup(r => r.GetQueryable()).Returns(new List<WingDetailsMastEntity>
        {
            new() { Id = wingDetailId, SocietyDetailsMastId = societyDetailId, IsActive = true }
        }.BuildMock());

        _mockRateableValueService
            .Setup(s => s.PreviewTotalTaxAsync(propertyId, It.IsAny<int>()))
            .ReturnsAsync((int _, int year) => new RetrospectiveRatePreviewDto { PropertyId = propertyId, FinanceYear = year, TotalTaxAmount = 1000m + year });

        var result = await _service.CalculateAndSaveAsync(propertyId, calculatedBy: 9, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("THA-01", result!.AppliedRuleCode);
        Assert.Equal(new DateTime(2019, 6, 15), result.ChargeableStartDate);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_MultiYearRetro_PersistsTransMastForEveryHistoricalYear()
    {
        // TransMast is the single source of tax demand across every year (PropertyId +
        // FinanceYearId + CalculationType + TaxId), not just the current one -- a 3-year retro
        // span (OC dated exactly at FY start, so no proration) must write one TransMast row per
        // year, not just for the current finance year.
        const int propertyId = 600;
        var rule = new RetrospectiveRuleMasterEntity { Id = 40, RuleCode = "THA-40", RuleName = "OC only, multi-year", PriorityNo = 1, IsFallbackRule = false, RuleStatus = "Active", IsActive = true };
        var evidenceConditions = new List<RetrospectiveRuleEvidenceConditionEntity>
        {
            new() { RuleId = 40, EvidenceTypeId = OcEvidenceTypeId, EvidenceState = "AVAILABLE", IsActive = true }
        };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 40, RuleId = 40, TaxStartMode = "EVIDENCE_DATE", StartEvidenceTypeId = OcEvidenceTypeId,
            RetrospectiveLimitType = "NONE", TaxCalculationMode = "SINGLE", TaxMultiplier = 1.00m, RateMode = "YEAR_WISE", IsActive = true
        };
        SetupRule(rule, evidenceConditions, action);

        _mockCertificateRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>
        {
            Certificate(propertyId, "OC", new DateTime(2023, 4, 1)) // exact FY2023-24 start -- no proration
        }.BuildMock());

        _mockRateableValueService
            .Setup(s => s.PreviewTotalTaxAsync(propertyId, It.IsAny<int>()))
            .ReturnsAsync((int _, int year) => new RetrospectiveRatePreviewDto { PropertyId = propertyId, FinanceYear = year, TotalTaxAmount = 1000m });

        var netTaxRow = new PolicyTaxDetailsEntity { PropertyId = propertyId, PolicyCodeId = 99, TaxId = 1, TaxAmount = 1000m, IsActive = true };
        _mockPolicyTaxDetailsRepository.Setup(r => r.GetQueryable()).Returns(new List<PolicyTaxDetailsEntity> { netTaxRow }.BuildMock());

        _mockYearRepository.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity>
        {
            new() { Id = 2023, Year = 2023 },
            new() { Id = 2024, Year = 2024 },
            new() { Id = 2025, Year = 2025 },
        }.BuildMock());

        var addedTransMasts = new List<TransMastEntity>();
        _mockTransMastRepository
            .Setup(r => r.AddAsync(It.IsAny<TransMastEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransMastEntity e, CancellationToken _) => { addedTransMasts.Add(e); return e; });

        var result = await _service.CalculateAndSaveAsync(propertyId, calculatedBy: 9, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result!.YearWiseBreakdown.Count); // FY2023-24, 2024-25, 2025-26(current)

        Assert.Equal(3, addedTransMasts.Count);
        Assert.Equal(new[] { 2023, 2024, 2025 }, addedTransMasts.Select(tm => tm.FinanceYearId).OrderBy(id => id).ToArray());
        Assert.All(addedTransMasts, tm => Assert.Equal(1000m, tm.TaxAmount));
        Assert.All(addedTransMasts, tm => Assert.Equal(101, tm.PolicyCodeId)); // OC full code, not silently defaulted to NETTAX (1)
    }

    [Fact]
    public async Task CalculateAndSaveAsync_ActiveOldArrearsForRecalculatedYear_IsDeactivatedNotDeleted()
    {
        // Spec: "If an old year already has an active OLD_ARREARS demand and the same year is
        // recalculated through OC/CC..., the old arrears row must not remain as a second active
        // demand." Only IsActive flips to false -- MarkedForDeletion stays false, so the row
        // remains available through audit/history.
        const int propertyId = 601;
        const int oldArrearsPolicyCodeId = 999;

        var arrearsRule = new RetrospectiveRuleMasterEntity { Id = 41, RuleCode = "THA-41", RuleName = "OC only, arrears replaced", PriorityNo = 1, IsFallbackRule = false, RuleStatus = "Active", IsActive = true };
        var arrearsEvidenceConditions = new List<RetrospectiveRuleEvidenceConditionEntity>
        {
            new() { RuleId = 41, EvidenceTypeId = OcEvidenceTypeId, EvidenceState = "AVAILABLE", IsActive = true }
        };
        var arrearsAction = new RetrospectiveRuleActionEntity
        {
            Id = 41, RuleId = 41, TaxStartMode = "EVIDENCE_DATE", StartEvidenceTypeId = OcEvidenceTypeId,
            RetrospectiveLimitType = "NONE", TaxCalculationMode = "SINGLE", TaxMultiplier = 1.00m, RateMode = "YEAR_WISE", IsActive = true
        };
        SetupRule(arrearsRule, arrearsEvidenceConditions, arrearsAction);

        _mockCertificateRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>
        {
            Certificate(propertyId, "OC", new DateTime(2023, 4, 1))
        }.BuildMock());

        _mockRateableValueService
            .Setup(s => s.PreviewTotalTaxAsync(propertyId, It.IsAny<int>()))
            .ReturnsAsync((int _, int year) => new RetrospectiveRatePreviewDto { PropertyId = propertyId, FinanceYear = year, TotalTaxAmount = 1000m });

        var arrearsNetTaxRow = new PolicyTaxDetailsEntity { PropertyId = propertyId, PolicyCodeId = 99, TaxId = 1, TaxAmount = 1000m, IsActive = true };
        _mockPolicyTaxDetailsRepository.Setup(r => r.GetQueryable()).Returns(new List<PolicyTaxDetailsEntity> { arrearsNetTaxRow }.BuildMock());

        _mockYearRepository.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity>
        {
            new() { Id = 2023, Year = 2023 },
            new() { Id = 2024, Year = 2024 },
            new() { Id = 2025, Year = 2025 },
        }.BuildMock());

        // An existing active OLD_ARREARS row for FY2023-24, TaxId 1 -- the exact key this run
        // recalculates via OC.
        var oldArrearsRow = new TransMastEntity
        {
            PropertyId = propertyId, FinanceYearId = 2023, CalculationType = "RV", TaxId = 1,
            PolicyCodeId = oldArrearsPolicyCodeId, TaxAmount = 20000m, IsActive = true
        };
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity> { oldArrearsRow }.BuildMock());

        _mockPolicyCodeLookup.Setup(s => s.GetExistingIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int> { [PolicyCodes.OldArrears] = oldArrearsPolicyCodeId });

        var result = await _service.CalculateAndSaveAsync(propertyId, calculatedBy: 9, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(oldArrearsRow.IsActive);
        Assert.False(oldArrearsRow.MarkedForDeletion);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_EarliestYearStartsMidYear_ProratesOnlyThatYearByDayCount()
    {
        // FY2019-20 (01-Apr-2019..31-Mar-2020) is the earliest chargeable year but the certificate
        // is dated 15-Jun-2019, so only that one year should be billed for the 291 days from
        // 15-Jun-2019 to 31-Mar-2020 inclusive; FY2020-21 onward must be billed in full.
        const int propertyId = 101;
        var rule = new RetrospectiveRuleMasterEntity { Id = 3, RuleCode = "THA-03", RuleName = "OC only, mid-year", PriorityNo = 1, IsFallbackRule = false, RuleStatus = "Active", IsActive = true };
        var evidenceConditions = new List<RetrospectiveRuleEvidenceConditionEntity>
        {
            new() { RuleId = 3, EvidenceTypeId = OcEvidenceTypeId, EvidenceState = "AVAILABLE", IsActive = true }
        };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 3, RuleId = 3, TaxStartMode = "EVIDENCE_DATE", StartEvidenceTypeId = OcEvidenceTypeId,
            RetrospectiveLimitType = "NONE",
            TaxCalculationMode = "SINGLE", TaxMultiplier = 1.00m, RateMode = "CURRENT_YEAR", IsActive = true
        };
        SetupRule(rule, evidenceConditions, action);

        _mockCertificateRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>
        {
            Certificate(propertyId, "OC", new DateTime(2019, 6, 15))
        }.BuildMock());

        _mockRateableValueService
            .Setup(s => s.PreviewTotalTaxAsync(propertyId, 2025))
            .ReturnsAsync(new RetrospectiveRatePreviewDto { PropertyId = propertyId, FinanceYear = 2025, TotalTaxAmount = 3650m });

        var result = await _service.CalculateAndSaveAsync(propertyId, calculatedBy: 9, CancellationToken.None);

        Assert.NotNull(result);
        var earliestYear = result!.YearWiseBreakdown.Single(y => y.FinancialYear == "2019-20");
        Assert.Equal(new DateTime(2019, 6, 15), earliestYear.FromDate);
        Assert.Equal(new DateTime(2020, 3, 31), earliestYear.ToDate);
        // 291 days (15-Jun-2019..31-Mar-2020 inclusive) / 365 * 3650 = 2910.00
        Assert.Equal(2910.00m, earliestYear.BaseTaxAmount);

        var laterYear = result.YearWiseBreakdown.Single(y => y.FinancialYear == "2020-21");
        Assert.Equal(new DateTime(2020, 4, 1), laterYear.FromDate);
        Assert.Equal(3650m, laterYear.BaseTaxAmount);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_FullLeapFinanceYearThatIsNotTheEarliestYear_IsNotProrated()
    {
        // Regression guard: FY2023-24 (01-Apr-2023..31-Mar-2024) actually spans 366 days because it
        // contains 29-Feb-2024, but when it is NOT the earliest chargeable year it must still be
        // billed at the full, unprorated rate -- proration only ever applies to the single earliest
        // (genuinely partial) chargeable year, never to a later year just because that year happens
        // to contain a leap day.
        const int propertyId = 102;
        var rule = new RetrospectiveRuleMasterEntity { Id = 4, RuleCode = "THA-04", RuleName = "OC only, spans a leap FY", PriorityNo = 1, IsFallbackRule = false, RuleStatus = "Active", IsActive = true };
        var evidenceConditions = new List<RetrospectiveRuleEvidenceConditionEntity>
        {
            new() { RuleId = 4, EvidenceTypeId = OcEvidenceTypeId, EvidenceState = "AVAILABLE", IsActive = true }
        };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 4, RuleId = 4, TaxStartMode = "EVIDENCE_DATE", StartEvidenceTypeId = OcEvidenceTypeId,
            RetrospectiveLimitType = "NONE",
            TaxCalculationMode = "SINGLE", TaxMultiplier = 1.00m, RateMode = "CURRENT_YEAR", IsActive = true
        };
        SetupRule(rule, evidenceConditions, action);

        // Onset is exactly 01-Apr-2023 (the FY's own start), so FY2023-24 is a FULL year -- it just
        // happens to be a leap finance year, not a partial one.
        _mockCertificateRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>
        {
            Certificate(propertyId, "OC", new DateTime(2023, 4, 1))
        }.BuildMock());

        _mockRateableValueService
            .Setup(s => s.PreviewTotalTaxAsync(propertyId, 2025))
            .ReturnsAsync(new RetrospectiveRatePreviewDto { PropertyId = propertyId, FinanceYear = 2025, TotalTaxAmount = 1000m });

        var result = await _service.CalculateAndSaveAsync(propertyId, calculatedBy: 9, CancellationToken.None);

        Assert.NotNull(result);
        var leapYear = result!.YearWiseBreakdown.Single(y => y.FinancialYear == "2023-24");
        Assert.Equal(new DateTime(2023, 4, 1), leapYear.FromDate);
        Assert.Equal(1000m, leapYear.BaseTaxAmount);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_CurrentYearRateMode_PricesEveryYearWithCurrentYearRateOnly()
    {
        const int propertyId = 200;
        var rule = new RetrospectiveRuleMasterEntity { Id = 2, RuleCode = "PCM-02", RuleName = "OC within 6y", PriorityNo = 1, IsFallbackRule = false, RuleStatus = "Active", IsActive = true };
        var evidenceConditions = new List<RetrospectiveRuleEvidenceConditionEntity>
        {
            new() { RuleId = 2, EvidenceTypeId = OcEvidenceTypeId, EvidenceState = "AVAILABLE", IsActive = true }
        };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 2, RuleId = 2, TaxStartMode = "EVIDENCE_DATE", StartEvidenceTypeId = OcEvidenceTypeId,
            RetrospectiveLimitType = "MAXIMUM_YEARS", MaximumYears = 6,
            TaxCalculationMode = "SINGLE", TaxMultiplier = 1.00m, RateMode = "CURRENT_YEAR", IsActive = true
        };
        SetupRule(rule, evidenceConditions, action);

        _mockCertificateRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>
        {
            Certificate(propertyId, "OC", new DateTime(2023, 5, 1))
        }.BuildMock());

        _mockRateableValueService
            .Setup(s => s.PreviewTotalTaxAsync(propertyId, 2025))
            .ReturnsAsync(new RetrospectiveRatePreviewDto { PropertyId = propertyId, FinanceYear = 2025, TotalTaxAmount = 5000m });

        var result = await _service.CalculateAndSaveAsync(propertyId, calculatedBy: 9, CancellationToken.None);

        Assert.NotNull(result);
        // The earliest chargeable year (2023-24) starts mid-year (01-May, not 01-Apr) and is
        // day-prorated: 336 of 365 days × 5000 = 4602.74. Every later year runs in full.
        var earliestYear = result!.YearWiseBreakdown.Single(y => y.FinancialYear == "2023-24");
        Assert.Equal(4602.74m, earliestYear.BaseTaxAmount);
        Assert.Equal(new DateTime(2023, 5, 1), earliestYear.FromDate);
        Assert.All(result.YearWiseBreakdown.Where(y => y.FinancialYear != "2023-24"), y => Assert.Equal(5000m, y.BaseTaxAmount));
        // Only the current finance year is ever priced — never a historical one.
        _mockRateableValueService.Verify(s => s.PreviewTotalTaxAsync(propertyId, 2025), Times.Once);
        _mockRateableValueService.Verify(s => s.PreviewTotalTaxAsync(propertyId, It.Is<int>(y => y != 2025)), Times.Never);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_SplitMultiplier_UsesSplitBeforeOcAndAfterSplitFromOc()
    {
        const int propertyId = 300;
        var rule = new RetrospectiveRuleMasterEntity { Id = 9, RuleCode = "THA-09", RuleName = "CC then OC split", PriorityNo = 1, IsFallbackRule = false, RuleStatus = "Active", IsActive = true };
        var evidenceConditions = new List<RetrospectiveRuleEvidenceConditionEntity>
        {
            new() { RuleId = 9, EvidenceTypeId = CcEvidenceTypeId, EvidenceState = "AVAILABLE", IsActive = true }
        };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 9, RuleId = 9, TaxStartMode = "EVIDENCE_DATE", StartEvidenceTypeId = CcEvidenceTypeId,
            RetrospectiveLimitType = "FIXED_CUTOFF_DATE", CutoffDate = new DateTime(2016, 4, 1),
            TaxCalculationMode = "SPLIT", TaxMultiplier = 1.00m,
            SplitStartEvidenceTypeId = CcEvidenceTypeId, SplitEndEvidenceTypeId = OcEvidenceTypeId,
            SplitMultiplier = 1.50m, AfterSplitMultiplier = 1.00m, RateMode = "YEAR_WISE", IsActive = true
        };
        SetupRule(rule, evidenceConditions, action);

        _mockCertificateRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>
        {
            Certificate(propertyId, "CC", new DateTime(2020, 5, 1)),
            Certificate(propertyId, "OC", new DateTime(2023, 6, 1))
        }.BuildMock());

        _mockRateableValueService
            .Setup(s => s.PreviewTotalTaxAsync(propertyId, It.IsAny<int>()))
            .ReturnsAsync((int _, int year) => new RetrospectiveRatePreviewDto { PropertyId = propertyId, FinanceYear = year, TotalTaxAmount = 1000m });

        var result = await _service.CalculateAndSaveAsync(propertyId, calculatedBy: 9, CancellationToken.None);

        Assert.NotNull(result);
        var fy2020 = result!.YearWiseBreakdown.Single(y => y.FinancialYear == "2020-21");
        // FY2023-24 (From=2023-04-01) starts before the 2023-06-01 OC date -> whole year still split-priced.
        var fy2023 = result.YearWiseBreakdown.Single(y => y.FinancialYear == "2023-24");
        // FY2024-25 (From=2024-04-01) starts after the OC date -> after-split multiplier applies.
        var fy2024 = result.YearWiseBreakdown.Single(y => y.FinancialYear == "2024-25");
        Assert.Equal(1.50m, fy2020.TaxMultiplier);
        Assert.Equal(1.50m, fy2023.TaxMultiplier);
        Assert.Equal(1.00m, fy2024.TaxMultiplier);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_CcThenOcMerge_SplitsBoundaryYearByDayAndAppliesCcBeforeOcAfter()
    {
        // Same dates as the SPLIT-mode test above (CC 01-May-2020, OC 01-Jun-2023), but under
        // CC_THEN_OC_MERGE: unlike SPLIT (which bills FY2023-24 in full at the pre-OC multiplier),
        // the merge splits that one boundary year by day between CC's and OC's portions.
        const int propertyId = 301;
        var rule = new RetrospectiveRuleMasterEntity { Id = 10, RuleCode = "THA-10", RuleName = "CC then OC merge", PriorityNo = 1, IsFallbackRule = false, RuleStatus = "Active", IsActive = true };
        var evidenceConditions = new List<RetrospectiveRuleEvidenceConditionEntity>
        {
            new() { RuleId = 10, EvidenceTypeId = CcEvidenceTypeId, EvidenceState = "AVAILABLE", IsActive = true }
        };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 10, RuleId = 10, TaxStartMode = "EVIDENCE_DATE", StartEvidenceTypeId = CcEvidenceTypeId,
            RetrospectiveLimitType = "FIXED_CUTOFF_DATE", CutoffDate = new DateTime(2016, 4, 1),
            TaxCalculationMode = "CC_THEN_OC_MERGE", TaxMultiplier = 1.00m,
            SplitEndEvidenceTypeId = OcEvidenceTypeId,
            SplitMultiplier = 1.50m, AfterSplitMultiplier = 1.00m, RateMode = "YEAR_WISE", IsActive = true
        };
        SetupRule(rule, evidenceConditions, action);

        _mockCertificateRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>
        {
            Certificate(propertyId, "CC", new DateTime(2020, 5, 1)),
            Certificate(propertyId, "OC", new DateTime(2023, 6, 1))
        }.BuildMock());

        _mockRateableValueService
            .Setup(s => s.PreviewTotalTaxAsync(propertyId, It.IsAny<int>()))
            .ReturnsAsync((int _, int year) => new RetrospectiveRatePreviewDto { PropertyId = propertyId, FinanceYear = year, TotalTaxAmount = 1000m });

        var result = await _service.CalculateAndSaveAsync(propertyId, calculatedBy: 9, CancellationToken.None);

        Assert.NotNull(result);

        // Earliest chargeable year: partial from CC's own date (335 of 365 days), fully at 1.5x.
        var fy2020 = result!.YearWiseBreakdown.Single(y => y.FinancialYear == "2020-21");
        Assert.Equal(new DateTime(2020, 5, 1), fy2020.FromDate);
        Assert.Equal(1.50m, fy2020.TaxMultiplier);
        Assert.Equal(917.81m, fy2020.BaseTaxAmount);

        // A full year strictly before OC's onset year -- billed in full at 1.5x.
        var fy2022 = result.YearWiseBreakdown.Single(y => y.FinancialYear == "2022-23");
        Assert.Equal(1.50m, fy2022.TaxMultiplier);
        Assert.Equal(1000m, fy2022.BaseTaxAmount);

        // The OC-onset year (2023-24) must split into exactly two rows: CC's portion (01-Apr..31-May)
        // at 1.5x, OC's portion (01-Jun..31-Mar) at 1.0x.
        var fy2023Rows = result.YearWiseBreakdown.Where(y => y.FinancialYear == "2023-24").OrderBy(y => y.FromDate).ToList();
        Assert.Equal(2, fy2023Rows.Count);
        Assert.Equal(new DateTime(2023, 4, 1), fy2023Rows[0].FromDate);
        Assert.Equal(new DateTime(2023, 5, 31), fy2023Rows[0].ToDate);
        Assert.Equal(1.50m, fy2023Rows[0].TaxMultiplier);
        Assert.Equal(167.12m, fy2023Rows[0].BaseTaxAmount);
        Assert.Equal(new DateTime(2023, 6, 1), fy2023Rows[1].FromDate);
        Assert.Equal(new DateTime(2024, 3, 31), fy2023Rows[1].ToDate);
        Assert.Equal(1.00m, fy2023Rows[1].TaxMultiplier);
        Assert.Equal(835.62m, fy2023Rows[1].BaseTaxAmount);

        // A full year strictly after OC's onset year -- billed in full at 1.0x.
        var fy2024 = result.YearWiseBreakdown.Single(y => y.FinancialYear == "2024-25");
        Assert.Equal(1.00m, fy2024.TaxMultiplier);
        Assert.Equal(1000m, fy2024.BaseTaxAmount);

        // RetroYearCount counts distinct financial years (6: 2020-21..2025-26), not raw rows (7).
        Assert.Equal(6, result.RetroYearCount);
        Assert.Equal(7, result.YearWiseBreakdown.Count);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_CcThenOcMerge_SameFinanceYear_SplitsFromCcsOwnDateNotFyStart()
    {
        // CC and OC both land in the SAME finance year (2023-24), and it is also the earliest
        // chargeable year -- the CC portion must start from CC's own date (01-May), not that
        // year's 01-Apr FY-start, since nothing was chargeable before CC's own evidence date.
        const int propertyId = 302;
        var rule = new RetrospectiveRuleMasterEntity { Id = 11, RuleCode = "THA-11", RuleName = "CC then OC merge, same FY", PriorityNo = 1, IsFallbackRule = false, RuleStatus = "Active", IsActive = true };
        var evidenceConditions = new List<RetrospectiveRuleEvidenceConditionEntity>
        {
            new() { RuleId = 11, EvidenceTypeId = CcEvidenceTypeId, EvidenceState = "AVAILABLE", IsActive = true }
        };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 11, RuleId = 11, TaxStartMode = "EVIDENCE_DATE", StartEvidenceTypeId = CcEvidenceTypeId,
            RetrospectiveLimitType = "FIXED_CUTOFF_DATE", CutoffDate = new DateTime(2016, 4, 1),
            TaxCalculationMode = "CC_THEN_OC_MERGE", TaxMultiplier = 1.00m,
            SplitEndEvidenceTypeId = OcEvidenceTypeId,
            SplitMultiplier = 1.50m, AfterSplitMultiplier = 1.00m, RateMode = "YEAR_WISE", IsActive = true
        };
        SetupRule(rule, evidenceConditions, action);

        _mockCertificateRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>
        {
            Certificate(propertyId, "CC", new DateTime(2023, 5, 1)),
            Certificate(propertyId, "OC", new DateTime(2023, 11, 1))
        }.BuildMock());

        _mockRateableValueService
            .Setup(s => s.PreviewTotalTaxAsync(propertyId, It.IsAny<int>()))
            .ReturnsAsync((int _, int year) => new RetrospectiveRatePreviewDto { PropertyId = propertyId, FinanceYear = year, TotalTaxAmount = 1000m });

        var result = await _service.CalculateAndSaveAsync(propertyId, calculatedBy: 9, CancellationToken.None);

        Assert.NotNull(result);
        var fy2023Rows = result!.YearWiseBreakdown.Where(y => y.FinancialYear == "2023-24").OrderBy(y => y.FromDate).ToList();
        Assert.Equal(2, fy2023Rows.Count);
        Assert.Equal(new DateTime(2023, 5, 1), fy2023Rows[0].FromDate);
        Assert.Equal(new DateTime(2023, 10, 31), fy2023Rows[0].ToDate);
        Assert.Equal(1.50m, fy2023Rows[0].TaxMultiplier);
        Assert.Equal(504.11m, fy2023Rows[0].BaseTaxAmount);
        Assert.Equal(new DateTime(2023, 11, 1), fy2023Rows[1].FromDate);
        Assert.Equal(new DateTime(2024, 3, 31), fy2023Rows[1].ToDate);
        Assert.Equal(1.00m, fy2023Rows[1].TaxMultiplier);
        Assert.Equal(416.44m, fy2023Rows[1].BaseTaxAmount);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_NoEvidenceAndNoFallbackRule_ReturnsNull()
    {
        const int propertyId = 400;
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleMasterEntity>().BuildMock());
        _mockEvidenceConditionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleEvidenceConditionEntity>().BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity>().BuildMock());
        _mockDateConditionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleDateConditionEntity>().BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity>().BuildMock());
        _mockCertificateRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>().BuildMock());

        var result = await _service.CalculateAndSaveAsync(propertyId, calculatedBy: 9, CancellationToken.None);

        Assert.Null(result);
        _mockCalculationRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveTaxCalculationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_NoEvidence_FallsBackToFallbackRule()
    {
        const int propertyId = 500;
        var fallbackRule = new RetrospectiveRuleMasterEntity { Id = 8, RuleCode = "THA-08", RuleName = "No evidence at all", PriorityNo = 99, IsFallbackRule = true, RuleStatus = "Active", IsActive = true };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 8, RuleId = 8, TaxStartMode = "CONSTRUCTION_YEAR",
            RetrospectiveLimitType = "NONE", TaxCalculationMode = "SINGLE", TaxMultiplier = 1.00m, RateMode = "YEAR_WISE", IsActive = true
        };
        SetupRule(fallbackRule, new List<RetrospectiveRuleEvidenceConditionEntity>(), action);

        _mockCertificateRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>().BuildMock());
        _mockPropertyDetailsRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyDetailsEntity>
        {
            new() { PropertyId = propertyId, ConstructionYear = "2018", MarkedForDeletion = false }
        }.BuildMock());

        _mockRateableValueService
            .Setup(s => s.PreviewTotalTaxAsync(propertyId, It.IsAny<int>()))
            .ReturnsAsync((int _, int year) => new RetrospectiveRatePreviewDto { PropertyId = propertyId, FinanceYear = year, TotalTaxAmount = 100m });

        var result = await _service.CalculateAndSaveAsync(propertyId, calculatedBy: 9, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("THA-08", result!.AppliedRuleCode);
        Assert.Equal(new DateTime(2018, 4, 1), result.ChargeableStartDate);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_RuleRequiresConstructionYearAvailable_MatchesWhenConstructionYearIsSet()
    {
        // Regression test: evidence conditions requiring "Construction year AVAILABLE" (THA-08,
        // PCM-06, FUR-03's fallback rules) must actually be able to match — CONSTRUCTION_YEAR has
        // no certificate of its own, so it must be resolved from PropertyDetails.ConstructionYear.
        const int propertyId = 700;
        var rule = new RetrospectiveRuleMasterEntity { Id = 8, RuleCode = "THA-08", RuleName = "Construction year fallback", PriorityNo = 80, IsFallbackRule = true, RuleStatus = "Active", IsActive = true };
        var evidenceConditions = new List<RetrospectiveRuleEvidenceConditionEntity>
        {
            new() { RuleId = 8, EvidenceTypeId = 5, EvidenceState = "AVAILABLE", IsActive = true }, // CONSTRUCTION_YEAR
            new() { RuleId = 8, EvidenceTypeId = OcEvidenceTypeId, EvidenceState = "UNAVAILABLE", IsActive = true },
            new() { RuleId = 8, EvidenceTypeId = CcEvidenceTypeId, EvidenceState = "UNAVAILABLE", IsActive = true },
            new() { RuleId = 8, EvidenceTypeId = ElectricityEvidenceTypeId, EvidenceState = "UNAVAILABLE", IsActive = true },
            new() { RuleId = 8, EvidenceTypeId = ChangeDetectionEvidenceTypeId, EvidenceState = "UNAVAILABLE", IsActive = true },
        };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 8, RuleId = 8, TaxStartMode = "CONSTRUCTION_YEAR",
            RetrospectiveLimitType = "FIXED_CUTOFF_DATE", CutoffDate = new DateTime(2016, 4, 1),
            TaxCalculationMode = "SINGLE", TaxMultiplier = 1.00m, RateMode = "YEAR_WISE", IsActive = true
        };
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(new List<EvidenceTypeMasterEntity>
        {
            new() { Id = OcEvidenceTypeId, EvidenceCode = "OC", IsActive = true },
            new() { Id = CcEvidenceTypeId, EvidenceCode = "CC", IsActive = true },
            new() { Id = ElectricityEvidenceTypeId, EvidenceCode = "ELECTRICITY", IsActive = true },
            new() { Id = ChangeDetectionEvidenceTypeId, EvidenceCode = "CHANGE_DETECTION", IsActive = true },
            new() { Id = 5, EvidenceCode = "CONSTRUCTION_YEAR", IsActive = true },
        }.BuildMock());
        SetupRule(rule, evidenceConditions, action);

        _mockCertificateRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>().BuildMock());
        _mockPropertyDetailsRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyDetailsEntity>
        {
            new() { PropertyId = propertyId, ConstructionYear = "2015", MarkedForDeletion = false }
        }.BuildMock());

        _mockRateableValueService
            .Setup(s => s.PreviewTotalTaxAsync(propertyId, It.IsAny<int>()))
            .ReturnsAsync((int _, int year) => new RetrospectiveRatePreviewDto { PropertyId = propertyId, FinanceYear = year, TotalTaxAmount = 100m });

        var result = await _service.CalculateAndSaveAsync(propertyId, calculatedBy: 9, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("THA-08", result!.AppliedRuleCode);
        // Construction year 2015 -> FY_start(2015) = 2015-04-01, floored at cutoff 2016-04-01 -> MAX wins.
        Assert.Equal(new DateTime(2016, 4, 1), result.ChargeableStartDate);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_DateValidationPenalty_AppliesPercentWhenConditionMet()
    {
        const int propertyId = 600;
        var rule = new RetrospectiveRuleMasterEntity { Id = 4, RuleCode = "PCM-04", RuleName = "Electricity only", PriorityNo = 1, IsFallbackRule = false, RuleStatus = "Active", IsActive = true };
        var evidenceConditions = new List<RetrospectiveRuleEvidenceConditionEntity>
        {
            new() { RuleId = 4, EvidenceTypeId = ElectricityEvidenceTypeId, EvidenceState = "AVAILABLE", IsActive = true }
        };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 4, RuleId = 4, TaxStartMode = "FY_START", StartEvidenceTypeId = ElectricityEvidenceTypeId,
            RetrospectiveLimitType = "MAXIMUM_YEARS", MaximumYears = 6,
            TaxCalculationMode = "SINGLE", TaxMultiplier = 1.00m, RateMode = "CURRENT_YEAR", IsActive = true
        };
        var penalty = new RetrospectivePenaltyRuleEntity
        {
            Id = 4, RuleId = 4, IsPenaltyApplicable = true, PenaltyMode = "DATE_VALIDATION",
            PenaltyPercent = 10m, PenaltyDateSourceType = "EVIDENCE_DATE", PenaltyDateEvidenceTypeId = ElectricityEvidenceTypeId,
            PenaltyDateCondition = "AFTER", CompareDate = new DateTime(2023, 3, 3), IsActive = true
        };
        SetupRule(rule, evidenceConditions, action, penalty: penalty);

        _mockCertificateRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>
        {
            Certificate(propertyId, "ELECTRIC_BILL", new DateTime(2024, 1, 1))
        }.BuildMock());

        _mockRateableValueService
            .Setup(s => s.PreviewTotalTaxAsync(propertyId, 2025))
            .ReturnsAsync(new RetrospectiveRatePreviewDto { PropertyId = propertyId, FinanceYear = 2025, TotalTaxAmount = 1000m });

        var result = await _service.CalculateAndSaveAsync(propertyId, calculatedBy: 9, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.RequiresManualReview);
        Assert.All(result.YearWiseBreakdown, y =>
        {
            Assert.Equal(10m, y.PenaltyPercent);
            Assert.Equal(100m, y.PenaltyAmount); // 10% of 1000
        });
    }

    [Fact]
    public async Task CalculateAndSaveAsync_OcRuleForCurrentYear_PersistsPolicyTaxDetailsAndTransMastSplitAcrossNettaxTaxIds()
    {
        const int propertyId = 500;
        var rule = new RetrospectiveRuleMasterEntity { Id = 30, RuleCode = "THA-30", RuleName = "OC only, current year", PriorityNo = 1, IsFallbackRule = false, RuleStatus = "Active", IsActive = true };
        var evidenceConditions = new List<RetrospectiveRuleEvidenceConditionEntity>
        {
            new() { RuleId = 30, EvidenceTypeId = OcEvidenceTypeId, EvidenceState = "AVAILABLE", IsActive = true }
        };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 30, RuleId = 30, TaxStartMode = "EVIDENCE_DATE", StartEvidenceTypeId = OcEvidenceTypeId,
            RetrospectiveLimitType = "NONE", TaxCalculationMode = "SINGLE", TaxMultiplier = 1.00m, RateMode = "YEAR_WISE", IsActive = true
        };
        SetupRule(rule, evidenceConditions, action);

        // OC dated exactly at this finance year's start -- a full, unprorated current year.
        _mockCertificateRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>
        {
            Certificate(propertyId, "OC", new DateTime(2025, 4, 1))
        }.BuildMock());

        _mockRateableValueService
            .Setup(s => s.PreviewTotalTaxAsync(propertyId, 2025))
            .ReturnsAsync(new RetrospectiveRatePreviewDto { PropertyId = propertyId, FinanceYear = 2025, TotalTaxAmount = 1000m });

        // Property's existing NETTAX rows -- 75%/25% weight split across two TaxIds.
        _mockPolicyTaxDetailsRepository.Setup(r => r.GetQueryable()).Returns(new List<PolicyTaxDetailsEntity>
        {
            new() { PropertyId = propertyId, PolicyCodeId = 99, TaxId = 1, TaxAmount = 750m, IsActive = true },
            new() { PropertyId = propertyId, PolicyCodeId = 99, TaxId = 2, TaxAmount = 250m, IsActive = true },
        }.BuildMock());

        var yearMaster = new YearMasterEntity { Id = 777, Year = 2025 };
        _mockYearRepository.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { yearMaster }.BuildMock());

        var addedPolicyTaxDetails = new List<PolicyTaxDetailsEntity>();
        _mockPolicyTaxDetailsRepository
            .Setup(r => r.AddAsync(It.IsAny<PolicyTaxDetailsEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PolicyTaxDetailsEntity e, CancellationToken _) => { addedPolicyTaxDetails.Add(e); return e; });
        var addedTransMasts = new List<TransMastEntity>();
        _mockTransMastRepository
            .Setup(r => r.AddAsync(It.IsAny<TransMastEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransMastEntity e, CancellationToken _) => { addedTransMasts.Add(e); return e; });

        var result = await _service.CalculateAndSaveAsync(propertyId, calculatedBy: 9, CancellationToken.None);

        Assert.NotNull(result);

        Assert.Equal(2, addedPolicyTaxDetails.Count);
        var pt1 = addedPolicyTaxDetails.Single(p => p.TaxId == 1);
        var pt2 = addedPolicyTaxDetails.Single(p => p.TaxId == 2);
        Assert.Equal(101, pt1.PolicyCodeId); // "OC" full code (not partial -- OC dated exactly at FY start)
        Assert.Equal(101, pt2.PolicyCodeId);
        Assert.Equal(750m, pt1.TaxAmount); // 1000 total * 75% weight
        Assert.Equal(250m, pt2.TaxAmount); // 1000 total * 25% weight

        Assert.Equal(2, addedTransMasts.Count);
        Assert.All(addedTransMasts, tm =>
        {
            Assert.Equal(777, tm.FinanceYearId);
            Assert.Equal("RV", tm.CalculationType);
        });
        Assert.Equal(750m, addedTransMasts.Single(tm => tm.TaxId == 1).TaxAmount);
        Assert.Equal(250m, addedTransMasts.Single(tm => tm.TaxId == 2).TaxAmount);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_OcRuleForCurrentYear_DeactivatesNetTaxTransMastRowAtSameKey()
    {
        const int propertyId = 501;
        var rule = new RetrospectiveRuleMasterEntity { Id = 31, RuleCode = "THA-31", RuleName = "OC only, current year", PriorityNo = 1, IsFallbackRule = false, RuleStatus = "Active", IsActive = true };
        var evidenceConditions = new List<RetrospectiveRuleEvidenceConditionEntity>
        {
            new() { RuleId = 31, EvidenceTypeId = OcEvidenceTypeId, EvidenceState = "AVAILABLE", IsActive = true }
        };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 31, RuleId = 31, TaxStartMode = "EVIDENCE_DATE", StartEvidenceTypeId = OcEvidenceTypeId,
            RetrospectiveLimitType = "NONE", TaxCalculationMode = "SINGLE", TaxMultiplier = 1.00m, RateMode = "YEAR_WISE", IsActive = true
        };
        SetupRule(rule, evidenceConditions, action);

        _mockCertificateRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>
        {
            Certificate(propertyId, "OC", new DateTime(2025, 4, 1))
        }.BuildMock());

        _mockRateableValueService
            .Setup(s => s.PreviewTotalTaxAsync(propertyId, 2025))
            .ReturnsAsync(new RetrospectiveRatePreviewDto { PropertyId = propertyId, FinanceYear = 2025, TotalTaxAmount = 1000m });

        _mockPolicyTaxDetailsRepository.Setup(r => r.GetQueryable()).Returns(new List<PolicyTaxDetailsEntity>
        {
            new() { PropertyId = propertyId, PolicyCodeId = 99, TaxId = 1, TaxAmount = 1000m, IsActive = true },
        }.BuildMock());

        var yearMaster = new YearMasterEntity { Id = 778, Year = 2025 };
        _mockYearRepository.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { yearMaster }.BuildMock());

        // An existing, currently-active NETTAX TransMast row at the exact (FinanceYearId, TaxId)
        // key the certificate's OC row is about to occupy -- TransMast is the single-demand ledger,
        // so this must be removed (IsActive=false, MarkedForDeletion=true), not left active
        // alongside the new OC row.
        var existingNetTaxTransMast = new TransMastEntity
        {
            PropertyId = propertyId, FinanceYearId = 778, TaxId = 1, PolicyCodeId = 99,
            CalculationType = "RV", TaxAmount = 1000m, IsActive = true, MarkedForDeletion = false
        };
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity> { existingNetTaxTransMast }.BuildMock());
        _mockTransMastRepository
            .Setup(r => r.AddAsync(It.IsAny<TransMastEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransMastEntity e, CancellationToken _) => e);

        var result = await _service.CalculateAndSaveAsync(propertyId, calculatedBy: 9, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(existingNetTaxTransMast.IsActive);
        Assert.True(existingNetTaxTransMast.MarkedForDeletion);
        Assert.NotNull(existingNetTaxTransMast.MarkedForDeletionDate);
        _mockTransMastRepository.Verify(r => r.UpdateAsync(existingNetTaxTransMast, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_NoExistingNetTax_CalculatesRvFirstThenWritesCertificateDemand()
    {
        const int propertyId = 502;
        var rule = new RetrospectiveRuleMasterEntity { Id = 32, RuleCode = "THA-32", RuleName = "OC only, current year", PriorityNo = 1, IsFallbackRule = false, RuleStatus = "Active", IsActive = true };
        var evidenceConditions = new List<RetrospectiveRuleEvidenceConditionEntity>
        {
            new() { RuleId = 32, EvidenceTypeId = OcEvidenceTypeId, EvidenceState = "AVAILABLE", IsActive = true }
        };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 32, RuleId = 32, TaxStartMode = "EVIDENCE_DATE", StartEvidenceTypeId = OcEvidenceTypeId,
            RetrospectiveLimitType = "NONE", TaxCalculationMode = "SINGLE", TaxMultiplier = 1.00m, RateMode = "YEAR_WISE", IsActive = true
        };
        SetupRule(rule, evidenceConditions, action);

        _mockCertificateRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>
        {
            Certificate(propertyId, "OC", new DateTime(2025, 4, 1))
        }.BuildMock());

        _mockRateableValueService
            .Setup(s => s.PreviewTotalTaxAsync(propertyId, 2025))
            .ReturnsAsync(new RetrospectiveRatePreviewDto { PropertyId = propertyId, FinanceYear = 2025, TotalTaxAmount = 1000m });

        // NETTAX rows only appear once CalculateAndSaveAsync has "run" -- simulates a property whose
        // Rateable Value was never calculated before its certificate was applied.
        var netTaxRows = new List<PolicyTaxDetailsEntity>();
        _mockPolicyTaxDetailsRepository.Setup(r => r.GetQueryable()).Returns(() => netTaxRows.BuildMock());
        _mockRateableValueService
            .Setup(s => s.CalculateAndSaveAsync(propertyId, true))
            .ReturnsAsync(new RateableValueResponseDto { PropertyId = propertyId })
            .Callback(() => netTaxRows.Add(new PolicyTaxDetailsEntity { PropertyId = propertyId, PolicyCodeId = 99, TaxId = 1, TaxAmount = 1000m, IsActive = true }));

        var yearMaster = new YearMasterEntity { Id = 779, Year = 2025 };
        _mockYearRepository.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { yearMaster }.BuildMock());

        var addedPolicyTaxDetails = new List<PolicyTaxDetailsEntity>();
        _mockPolicyTaxDetailsRepository
            .Setup(r => r.AddAsync(It.IsAny<PolicyTaxDetailsEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PolicyTaxDetailsEntity e, CancellationToken _) => { addedPolicyTaxDetails.Add(e); return e; });
        _mockTransMastRepository
            .Setup(r => r.AddAsync(It.IsAny<TransMastEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransMastEntity e, CancellationToken _) => e);

        var result = await _service.CalculateAndSaveAsync(propertyId, calculatedBy: 9, CancellationToken.None);

        Assert.NotNull(result);
        _mockRateableValueService.Verify(s => s.CalculateAndSaveAsync(propertyId, true), Times.Once);
        Assert.Single(addedPolicyTaxDetails);
        Assert.Equal(101, addedPolicyTaxDetails[0].PolicyCodeId);
        Assert.Equal(1000m, addedPolicyTaxDetails[0].TaxAmount);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_NoRuleMatches_DeactivatesStalePolicyTaxDetailsAndTransMast()
    {
        const int propertyId = 501;
        // No rules registered at all -- FindMatchingRuleAsync returns null.
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleMasterEntity>().BuildMock());
        _mockEvidenceConditionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleEvidenceConditionEntity>().BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity>().BuildMock());
        _mockDateConditionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleDateConditionEntity>().BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity>().BuildMock());
        _mockCertificateRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>().BuildMock());

        // A stale row left over from a prior successful run under the OC-family policy code.
        var staleRow = new PolicyTaxDetailsEntity { PropertyId = propertyId, PolicyCodeId = 101, TaxId = 1, TaxAmount = 500m, IsActive = true };
        _mockPolicyTaxDetailsRepository.Setup(r => r.GetQueryable()).Returns(new List<PolicyTaxDetailsEntity> { staleRow }.BuildMock());
        var staleTransMast = new TransMastEntity { PropertyId = propertyId, FinanceYearId = 777, CalculationType = "RV", TaxId = 1, PolicyCodeId = 101, TaxAmount = 500m, IsActive = true };
        _mockTransMastRepository.Setup(r => r.GetQueryable()).Returns(new List<TransMastEntity> { staleTransMast }.BuildMock());
        _mockYearRepository.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Id = 777, Year = 2025 } }.BuildMock());

        var result = await _service.CalculateAndSaveAsync(propertyId, calculatedBy: 9, CancellationToken.None);

        Assert.Null(result);
        Assert.False(staleRow.IsActive);
        Assert.True(staleRow.MarkedForDeletion);
        Assert.False(staleTransMast.IsActive);
        Assert.True(staleTransMast.MarkedForDeletion);
    }

    [Fact]
    public async Task CalculateAndSaveAsync_FamilySwitchesFromCcToOc_DeactivatesStaleCcRowInsteadOfLeavingBothActive()
    {
        // Regression guard for a real production bug in the retired engine: PolicyTaxDetails'
        // unique index is on (PropertyId, PolicyCodeId, TaxId), not (PropertyId, TaxId) -- so a
        // property whose governing family changed (e.g. CC certificate replaced by OC) could end up
        // with TWO active rows for the same TaxId under two different PolicyCodeIds if the old
        // family's row was never explicitly deactivated. This run's OC rule must both write the new
        // OC row AND deactivate the stale CC row for the same TaxId.
        const int propertyId = 502;
        var rule = new RetrospectiveRuleMasterEntity { Id = 31, RuleCode = "THA-31", RuleName = "OC only, family switch", PriorityNo = 1, IsFallbackRule = false, RuleStatus = "Active", IsActive = true };
        var evidenceConditions = new List<RetrospectiveRuleEvidenceConditionEntity>
        {
            new() { RuleId = 31, EvidenceTypeId = OcEvidenceTypeId, EvidenceState = "AVAILABLE", IsActive = true }
        };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 31, RuleId = 31, TaxStartMode = "EVIDENCE_DATE", StartEvidenceTypeId = OcEvidenceTypeId,
            RetrospectiveLimitType = "NONE", TaxCalculationMode = "SINGLE", TaxMultiplier = 1.00m, RateMode = "YEAR_WISE", IsActive = true
        };
        SetupRule(rule, evidenceConditions, action);

        _mockCertificateRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCertificateEntity>
        {
            Certificate(propertyId, "OC", new DateTime(2025, 4, 1))
        }.BuildMock());

        _mockRateableValueService
            .Setup(s => s.PreviewTotalTaxAsync(propertyId, 2025))
            .ReturnsAsync(new RetrospectiveRatePreviewDto { PropertyId = propertyId, FinanceYear = 2025, TotalTaxAmount = 1000m });

        // NETTAX row this run needs to split across.
        var nettaxRow = new PolicyTaxDetailsEntity { PropertyId = propertyId, PolicyCodeId = 99, TaxId = 1, TaxAmount = 1000m, IsActive = true };
        // Stale row left over from a PRIOR run under the CC family (PolicyCodeId 103), same TaxId.
        var staleCcRow = new PolicyTaxDetailsEntity { PropertyId = propertyId, PolicyCodeId = 103, TaxId = 1, TaxAmount = 1000m, IsActive = true };
        _mockPolicyTaxDetailsRepository.Setup(r => r.GetQueryable()).Returns(new List<PolicyTaxDetailsEntity> { nettaxRow, staleCcRow }.BuildMock());
        _mockYearRepository.Setup(r => r.GetQueryable()).Returns(new List<YearMasterEntity> { new() { Id = 777, Year = 2025 } }.BuildMock());

        var addedPolicyTaxDetails = new List<PolicyTaxDetailsEntity>();
        _mockPolicyTaxDetailsRepository
            .Setup(r => r.AddAsync(It.IsAny<PolicyTaxDetailsEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PolicyTaxDetailsEntity e, CancellationToken _) => { addedPolicyTaxDetails.Add(e); return e; });

        var result = await _service.CalculateAndSaveAsync(propertyId, calculatedBy: 9, CancellationToken.None);

        Assert.NotNull(result);

        // New OC row was added for TaxId 1.
        var newOcRow = Assert.Single(addedPolicyTaxDetails);
        Assert.Equal(101, newOcRow.PolicyCodeId); // OC full code
        Assert.Equal(1, newOcRow.TaxId);
        Assert.Equal(1000m, newOcRow.TaxAmount);

        // The stale CC row for the SAME TaxId is deactivated, not left active alongside the new one.
        Assert.False(staleCcRow.IsActive);
        Assert.True(staleCcRow.MarkedForDeletion);
    }
}
