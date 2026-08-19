using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RuleLibrary;
using NtisPlatform.Application.Services.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.RetrospectiveTax;

public class RuleLibraryServiceTests
{
    private readonly Mock<IRepository<RetrospectiveRuleMasterEntity, int>> _mockRuleRepository;
    private readonly Mock<IRepository<RetrospectiveRuleActionEntity, int>> _mockActionRepository;
    private readonly Mock<IRepository<RetrospectivePenaltyRuleEntity, int>> _mockPenaltyRepository;
    private readonly Mock<IRepository<EvidenceTypeMasterEntity, int>> _mockEvidenceTypeRepository;
    private readonly Mock<IRepository<RetrospectiveTaxPolicyEntity, int>> _mockTaxPolicyRepository;
    private readonly RuleLibraryService _service;

    public RuleLibraryServiceTests()
    {
        _mockRuleRepository = new Mock<IRepository<RetrospectiveRuleMasterEntity, int>>();
        _mockActionRepository = new Mock<IRepository<RetrospectiveRuleActionEntity, int>>();
        _mockPenaltyRepository = new Mock<IRepository<RetrospectivePenaltyRuleEntity, int>>();
        _mockEvidenceTypeRepository = new Mock<IRepository<EvidenceTypeMasterEntity, int>>();
        _mockTaxPolicyRepository = new Mock<IRepository<RetrospectiveTaxPolicyEntity, int>>();

        _service = new RuleLibraryService(
            _mockRuleRepository.Object,
            _mockActionRepository.Object,
            _mockPenaltyRepository.Object,
            _mockEvidenceTypeRepository.Object,
            _mockTaxPolicyRepository.Object);
    }

    private static List<EvidenceTypeMasterEntity> DefaultEvidenceTypes() => new()
    {
        new() { Id = 1, EvidenceCode = "OC", EvidenceName = "OC", IsActive = true },
        new() { Id = 2, EvidenceCode = "CC", EvidenceName = "CC", IsActive = true },
        new() { Id = 3, EvidenceCode = "ELECTRICITY", EvidenceName = "Electricity", IsActive = true }
    };

    private RuleLibraryQueryParameters DefaultQuery() => new()
    {
        PageNumber = 1,
        PageSize = 10,
        FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
    };

    [Fact]
    public async Task GetLibraryAsync_NoActionOrPenaltyOrPolicy_ReturnsRowsWithNullSections()
    {
        var rules = new List<RetrospectiveRuleMasterEntity>
        {
            new() { Id = 1, RuleCode = "FUR-01", RuleName = "Rule One", RuleStatus = "Active", AuthorizationStatus = "AUTHORIZED", RuleDescription = "Only OC available.", IsActive = true },
            new() { Id = 2, RuleCode = "FUR-02", RuleName = "Rule Two", RuleStatus = "Review", AuthorizationStatus = "UNAUTHORIZED", IsActive = true },
            new() { Id = 3, RuleCode = "FUR-03", RuleName = "Rule Three", RuleStatus = "NeedsClarification", AuthorizationStatus = "UNDETERMINED", IsActive = true },
            new() { Id = 4, RuleCode = "FUR-04", RuleName = "Rule Four", RuleStatus = "Draft", AuthorizationStatus = null, IsActive = true }
        };

        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(rules.BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity>().BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity>().BuildMock());
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(DefaultEvidenceTypes().BuildMock());
        _mockTaxPolicyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity>().BuildMock());

        var result = await _service.GetLibraryAsync(DefaultQuery(), CancellationToken.None);

        Assert.Null(result.CommonTaxation);
        Assert.Equal(4, result.Rules.TotalCount);

        var rows = result.Rules.Items.ToList();

        var authorizedRow = rows.Single(r => r.RuleCode == "FUR-01");
        Assert.Equal("Authorized: OC or CC available", authorizedRow.ConditionTag);
        Assert.Equal("Only OC available.", authorizedRow.ConditionDescription);
        Assert.Null(authorizedRow.StartLogicSummary);
        Assert.Null(authorizedRow.StartLogicBoundary);
        Assert.Null(authorizedRow.TaxMultiplierNote);
        Assert.Equal("Not applicable - OC/CC available", authorizedRow.PenaltySummary);

        var unauthorizedRow = rows.Single(r => r.RuleCode == "FUR-02");
        Assert.Equal("Unauthorized: OC & CC unavailable", unauthorizedRow.ConditionTag);
        Assert.Equal("Do not apply penalty", unauthorizedRow.PenaltySummary);

        var undeterminedRow = rows.Single(r => r.RuleCode == "FUR-03");
        Assert.Equal("Undetermined: rule condition incomplete", undeterminedRow.ConditionTag);

        var nullStatusRow = rows.Single(r => r.RuleCode == "FUR-04");
        Assert.Null(nullStatusRow.ConditionTag);
    }

    [Fact]
    public async Task GetLibraryAsync_ActiveTaxPolicy_ReturnsCommonTaxationWithLabels()
    {
        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleMasterEntity>().BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity>().BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity>().BuildMock());
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(new List<EvidenceTypeMasterEntity>().BuildMock());

        var policy = new RetrospectiveTaxPolicyEntity
        {
            Id = 1,
            TaxPolicyCode = "DEFAULT",
            RateMode = "CURRENT_YEAR_FOR_ALL_YEARS",
            PercentageMode = "HISTORIC_YEAR_WISE",
            IsActive = true
        };
        _mockTaxPolicyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity> { policy }.BuildMock());

        var result = await _service.GetLibraryAsync(DefaultQuery(), CancellationToken.None);

        Assert.NotNull(result.CommonTaxation);
        Assert.Equal("CURRENT_YEAR_FOR_ALL_YEARS", result.CommonTaxation!.RateModeCode);
        Assert.Equal("Current-year rate for all years", result.CommonTaxation.RateModeLabel);
        Assert.Equal("HISTORIC_YEAR_WISE", result.CommonTaxation.PercentageModeCode);
        Assert.Equal("Historical year-wise percentage", result.CommonTaxation.PercentageModeLabel);
    }

    [Theory]
    [InlineData("EVIDENCE_DATE", "From OC date")]
    [InlineData("FY_START", "1 April aligned to OC date")]
    [InlineData("NEXT_FINANCIAL_YEAR", "Next FY after OC date")]
    [InlineData("CONSTRUCTION_YEAR", "Construction year/date")]
    [InlineData("CONSTRUCTION_OR_CAP", "Later of construction date or rolling cap")]
    public async Task GetLibraryAsync_StartLogicSummary_MatchesTaxStartMode(string taxStartMode, string expectedSummary)
    {
        var rule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "R-01", RuleName = "R", RuleStatus = "Active", IsActive = true, LegalCapYears = 6 };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 1,
            RuleId = 1,
            TaxStartMode = taxStartMode,
            StartEvidenceTypeId = 1,
            RetrospectiveLimitType = "NONE",
            TaxCalculationMode = "SINGLE",
            TaxMultiplier = 1.00m,
            IsActive = true
        };

        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleMasterEntity> { rule }.BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity> { action }.BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity>().BuildMock());
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(DefaultEvidenceTypes().BuildMock());
        _mockTaxPolicyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity>().BuildMock());

        var result = await _service.GetLibraryAsync(DefaultQuery(), CancellationToken.None);

        var row = result.Rules.Items.Single();
        Assert.Equal(expectedSummary, row.StartLogicSummary);
    }

    [Fact]
    public async Task GetLibraryAsync_MonthsAfter_IncludesOffsetAndEvidenceName()
    {
        var rule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "FUR-04", RuleName = "R", RuleStatus = "Review", IsActive = true, LegalCapYears = 6 };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 1,
            RuleId = 1,
            TaxStartMode = "MONTHS_AFTER",
            StartEvidenceTypeId = 3,
            OffsetMonths = 6,
            RetrospectiveLimitType = "NONE",
            TaxCalculationMode = "SINGLE",
            TaxMultiplier = 1.00m,
            IsActive = true
        };

        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleMasterEntity> { rule }.BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity> { action }.BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity>().BuildMock());
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(DefaultEvidenceTypes().BuildMock());
        _mockTaxPolicyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity>().BuildMock());

        var result = await _service.GetLibraryAsync(DefaultQuery(), CancellationToken.None);

        Assert.Equal("6 months after Electricity date", result.Rules.Items.Single().StartLogicSummary);
    }

    [Fact]
    public async Task GetLibraryAsync_MaxLookBackDate_UsesRuleLegalCapYears()
    {
        var rule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "FUR-01", RuleName = "R", RuleStatus = "Active", IsActive = true, LegalCapYears = 6 };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 1,
            RuleId = 1,
            TaxStartMode = "MAX_LOOK_BACK_DATE",
            RetrospectiveLimitType = "NONE",
            TaxCalculationMode = "SINGLE",
            TaxMultiplier = 1.00m,
            IsActive = true
        };

        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleMasterEntity> { rule }.BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity> { action }.BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity>().BuildMock());
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(DefaultEvidenceTypes().BuildMock());
        _mockTaxPolicyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity>().BuildMock());

        var result = await _service.GetLibraryAsync(DefaultQuery(), CancellationToken.None);

        Assert.Equal("Rolling 6-year boundary", result.Rules.Items.Single().StartLogicSummary);
    }

    [Fact]
    public async Task GetLibraryAsync_FixedCutoff_IncludesFormattedDate()
    {
        var rule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "FUR-03", RuleName = "R", RuleStatus = "Review", IsActive = true };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 1,
            RuleId = 1,
            TaxStartMode = "FIXED_CUTOFF",
            CutoffDate = new DateTime(2024, 9, 1),
            RetrospectiveLimitType = "NONE",
            TaxCalculationMode = "SINGLE",
            TaxMultiplier = 1.00m,
            IsActive = true
        };

        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleMasterEntity> { rule }.BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity> { action }.BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity>().BuildMock());
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(DefaultEvidenceTypes().BuildMock());
        _mockTaxPolicyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity>().BuildMock());

        var result = await _service.GetLibraryAsync(DefaultQuery(), CancellationToken.None);

        Assert.Equal("Fixed cutoff 01 Sep 2024", result.Rules.Items.Single().StartLogicSummary);
    }

    [Fact]
    public async Task GetLibraryAsync_MaximumYearsLimit_ReturnsBoundaryText()
    {
        var rule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "FUR-01", RuleName = "R", RuleStatus = "Active", IsActive = true };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 1,
            RuleId = 1,
            TaxStartMode = "EVIDENCE_DATE",
            RetrospectiveLimitType = "MAXIMUM_YEARS",
            MaximumYears = 6,
            TaxCalculationMode = "SINGLE",
            TaxMultiplier = 1.00m,
            IsActive = true
        };

        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleMasterEntity> { rule }.BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity> { action }.BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity>().BuildMock());
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(DefaultEvidenceTypes().BuildMock());
        _mockTaxPolicyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity>().BuildMock());

        var result = await _service.GetLibraryAsync(DefaultQuery(), CancellationToken.None);

        Assert.Equal("Boundary: 6 years", result.Rules.Items.Single().StartLogicBoundary);
    }

    [Fact]
    public async Task GetLibraryAsync_FixedCutoffDateLimit_ReturnsBoundaryDate()
    {
        var rule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "THA-05", RuleName = "R", RuleStatus = "Review", IsActive = true };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 1,
            RuleId = 1,
            TaxStartMode = "EVIDENCE_DATE",
            RetrospectiveLimitType = "FIXED_CUTOFF_DATE",
            CutoffDate = new DateTime(2016, 4, 1),
            TaxCalculationMode = "SINGLE",
            TaxMultiplier = 1.00m,
            IsActive = true
        };

        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleMasterEntity> { rule }.BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity> { action }.BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity>().BuildMock());
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(DefaultEvidenceTypes().BuildMock());
        _mockTaxPolicyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity>().BuildMock());

        var result = await _service.GetLibraryAsync(DefaultQuery(), CancellationToken.None);

        Assert.Equal("Boundary: 2016-04-01", result.Rules.Items.Single().StartLogicBoundary);
    }

    [Fact]
    public async Task GetLibraryAsync_SplitTaxCalculation_ReturnsSplitMultiplierNote()
    {
        var rule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "THA-09", RuleName = "R", RuleStatus = "Active", IsActive = true };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 1,
            RuleId = 1,
            TaxStartMode = "EVIDENCE_DATE",
            RetrospectiveLimitType = "NONE",
            TaxCalculationMode = "SPLIT",
            TaxMultiplier = 1.00m,
            SplitStartEvidenceTypeId = 2,
            SplitEndEvidenceTypeId = 1,
            SplitMultiplier = 1.5m,
            AfterSplitMultiplier = 1.0m,
            IsActive = true
        };

        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleMasterEntity> { rule }.BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity> { action }.BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity>().BuildMock());
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(DefaultEvidenceTypes().BuildMock());
        _mockTaxPolicyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity>().BuildMock());

        var result = await _service.GetLibraryAsync(DefaultQuery(), CancellationToken.None);

        Assert.Equal("1.5x from CC date to OC date, then 1.0x", result.Rules.Items.Single().TaxMultiplierNote);
    }

    [Fact]
    public async Task GetLibraryAsync_SingleModeNonUnitMultiplier_ReturnsMultiplierNote()
    {
        var rule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "THA-03", RuleName = "R", RuleStatus = "Active", IsActive = true };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 1,
            RuleId = 1,
            TaxStartMode = "EVIDENCE_DATE",
            RetrospectiveLimitType = "NONE",
            TaxCalculationMode = "SINGLE",
            TaxMultiplier = 1.5m,
            IsActive = true
        };

        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleMasterEntity> { rule }.BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity> { action }.BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity>().BuildMock());
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(DefaultEvidenceTypes().BuildMock());
        _mockTaxPolicyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity>().BuildMock());

        var result = await _service.GetLibraryAsync(DefaultQuery(), CancellationToken.None);

        Assert.Equal("Retrospective tax x 1.5", result.Rules.Items.Single().TaxMultiplierNote);
    }

    [Fact]
    public async Task GetLibraryAsync_SingleModeUnitMultiplier_ReturnsNullNote()
    {
        var rule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "THA-01", RuleName = "R", RuleStatus = "Active", IsActive = true };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 1,
            RuleId = 1,
            TaxStartMode = "EVIDENCE_DATE",
            RetrospectiveLimitType = "NONE",
            TaxCalculationMode = "SINGLE",
            TaxMultiplier = 1.00m,
            IsActive = true
        };

        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleMasterEntity> { rule }.BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity> { action }.BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity>().BuildMock());
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(DefaultEvidenceTypes().BuildMock());
        _mockTaxPolicyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity>().BuildMock());

        var result = await _service.GetLibraryAsync(DefaultQuery(), CancellationToken.None);

        Assert.Null(result.Rules.Items.Single().TaxMultiplierNote);
    }

    [Fact]
    public async Task GetLibraryAsync_PenaltyActUnlawful_ReturnsActPenaltySummary()
    {
        var rule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "FUR-03", RuleName = "R", RuleStatus = "Review", AuthorizationStatus = "UNAUTHORIZED", IsActive = true };
        var penalty = new RetrospectivePenaltyRuleEntity { Id = 1, RuleId = 1, PenaltyMode = "ACT_UNLAWFUL", IsActive = true };

        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleMasterEntity> { rule }.BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity>().BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity> { penalty }.BuildMock());
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(DefaultEvidenceTypes().BuildMock());
        _mockTaxPolicyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity>().BuildMock());

        var result = await _service.GetLibraryAsync(DefaultQuery(), CancellationToken.None);

        Assert.Equal("Apply penalty as per the Act", result.Rules.Items.Single().PenaltySummary);
    }

    [Fact]
    public async Task GetLibraryAsync_PenaltyDateValidationOnOrAfter_ReturnsFormattedDateSummary()
    {
        var rule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "PCM-04", RuleName = "R", RuleStatus = "NeedsClarification", AuthorizationStatus = "UNAUTHORIZED", IsActive = true };
        var penalty = new RetrospectivePenaltyRuleEntity
        {
            Id = 1,
            RuleId = 1,
            PenaltyMode = "DATE_VALIDATION",
            PenaltyDateSourceType = "EVIDENCE_DATE",
            PenaltyDateEvidenceTypeId = 3,
            PenaltyDateCondition = "ON_OR_AFTER",
            CompareDate = new DateTime(2026, 3, 3),
            IsActive = true
        };

        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleMasterEntity> { rule }.BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity>().BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity> { penalty }.BuildMock());
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(DefaultEvidenceTypes().BuildMock());
        _mockTaxPolicyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity>().BuildMock());

        var result = await _service.GetLibraryAsync(DefaultQuery(), CancellationToken.None);

        Assert.Equal("Apply when Electricity date is on or after 03 Mar 2026", result.Rules.Items.Single().PenaltySummary);
    }

    [Fact]
    public async Task GetLibraryAsync_PenaltyDateValidationBetween_ReturnsRangeSummary()
    {
        var rule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "PCM-05", RuleName = "R", RuleStatus = "Review", AuthorizationStatus = "UNAUTHORIZED", IsActive = true };
        var penalty = new RetrospectivePenaltyRuleEntity
        {
            Id = 1,
            RuleId = 1,
            PenaltyMode = "DATE_VALIDATION",
            PenaltyDateSourceType = "FIXED_DATE",
            PenaltyDateCondition = "BETWEEN",
            CompareDate = new DateTime(2016, 4, 1),
            CompareDateTo = new DateTime(2024, 9, 1),
            IsActive = true
        };

        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleMasterEntity> { rule }.BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity>().BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity> { penalty }.BuildMock());
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(DefaultEvidenceTypes().BuildMock());
        _mockTaxPolicyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity>().BuildMock());

        var result = await _service.GetLibraryAsync(DefaultQuery(), CancellationToken.None);

        Assert.Equal("Apply when Fixed date is between 01 Apr 2016 and 01 Sep 2024", result.Rules.Items.Single().PenaltySummary);
    }

    [Fact]
    public async Task GetLibraryAsync_PenaltyDateValidationAssessmentDateBefore_ReturnsFormattedSummary()
    {
        // Covers two BuildPenaltySummary branches that no other test exercises:
        // PenaltyDateSourceType = ASSESSMENT_DATE (only EVIDENCE_DATE/FIXED_DATE were tested)
        // and PenaltyDateCondition = BEFORE (only ON_OR_AFTER/BETWEEN were tested).
        var rule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "PCM-06", RuleName = "R", RuleStatus = "Review", AuthorizationStatus = "UNAUTHORIZED", IsActive = true };
        var penalty = new RetrospectivePenaltyRuleEntity
        {
            Id = 1,
            RuleId = 1,
            PenaltyMode = "DATE_VALIDATION",
            PenaltyDateSourceType = "ASSESSMENT_DATE",
            PenaltyDateCondition = "BEFORE",
            CompareDate = new DateTime(2020, 1, 1),
            IsActive = true
        };

        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleMasterEntity> { rule }.BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity>().BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity> { penalty }.BuildMock());
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(DefaultEvidenceTypes().BuildMock());
        _mockTaxPolicyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity>().BuildMock());

        var result = await _service.GetLibraryAsync(DefaultQuery(), CancellationToken.None);

        Assert.Equal("Apply when Assessment date is before 01 Jan 2020", result.Rules.Items.Single().PenaltySummary);
    }

    [Theory]
    [InlineData("AFTER", "after")]
    [InlineData("ON_OR_BEFORE", "on or before")]
    [InlineData("SOME_UNMAPPED_CONDITION", "on")]
    public async Task GetLibraryAsync_PenaltyDateCondition_MatchesConditionLabel(string condition, string expectedLabel)
    {
        var rule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "PCM-07", RuleName = "R", RuleStatus = "Review", AuthorizationStatus = "UNAUTHORIZED", IsActive = true };
        var penalty = new RetrospectivePenaltyRuleEntity
        {
            Id = 1,
            RuleId = 1,
            PenaltyMode = "DATE_VALIDATION",
            PenaltyDateSourceType = "FIXED_DATE",
            PenaltyDateCondition = condition,
            CompareDate = new DateTime(2020, 1, 1),
            IsActive = true
        };

        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleMasterEntity> { rule }.BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity>().BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity> { penalty }.BuildMock());
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(DefaultEvidenceTypes().BuildMock());
        _mockTaxPolicyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity>().BuildMock());

        var result = await _service.GetLibraryAsync(DefaultQuery(), CancellationToken.None);

        Assert.Equal($"Apply when Fixed date is {expectedLabel} 01 Jan 2020", result.Rules.Items.Single().PenaltySummary);
    }

    [Fact]
    public async Task GetLibraryAsync_PenaltyDateSourceType_UnmappedValue_FallsBackToGenericDateLabel()
    {
        var rule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "PCM-08", RuleName = "R", RuleStatus = "Review", AuthorizationStatus = "UNAUTHORIZED", IsActive = true };
        var penalty = new RetrospectivePenaltyRuleEntity
        {
            Id = 1,
            RuleId = 1,
            PenaltyMode = "DATE_VALIDATION",
            PenaltyDateSourceType = "SOME_UNMAPPED_SOURCE",
            PenaltyDateCondition = "ON_OR_AFTER",
            CompareDate = new DateTime(2020, 1, 1),
            IsActive = true
        };

        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleMasterEntity> { rule }.BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity>().BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity> { penalty }.BuildMock());
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(DefaultEvidenceTypes().BuildMock());
        _mockTaxPolicyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity>().BuildMock());

        var result = await _service.GetLibraryAsync(DefaultQuery(), CancellationToken.None);

        Assert.Equal("Apply when date is on or after 01 Jan 2020", result.Rules.Items.Single().PenaltySummary);
    }

    [Fact]
    public async Task GetLibraryAsync_PenaltyModeUnmapped_ReturnsDoNotApplyPenalty()
    {
        var rule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "PCM-09", RuleName = "R", RuleStatus = "Review", AuthorizationStatus = "UNAUTHORIZED", IsActive = true };
        var penalty = new RetrospectivePenaltyRuleEntity { Id = 1, RuleId = 1, PenaltyMode = "SOME_UNMAPPED_MODE", IsActive = true };

        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleMasterEntity> { rule }.BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity>().BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity> { penalty }.BuildMock());
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(DefaultEvidenceTypes().BuildMock());
        _mockTaxPolicyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity>().BuildMock());

        var result = await _service.GetLibraryAsync(DefaultQuery(), CancellationToken.None);

        Assert.Equal("Do not apply penalty", result.Rules.Items.Single().PenaltySummary);
    }

    [Fact]
    public async Task GetLibraryAsync_TaxStartModeUnmapped_ReturnsNullStartLogicSummary()
    {
        var rule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "R-99", RuleName = "R", RuleStatus = "Active", IsActive = true, LegalCapYears = 6 };
        var action = new RetrospectiveRuleActionEntity
        {
            Id = 1,
            RuleId = 1,
            TaxStartMode = "SOME_UNMAPPED_MODE",
            RetrospectiveLimitType = "NONE",
            TaxCalculationMode = "SINGLE",
            TaxMultiplier = 1.00m,
            IsActive = true
        };

        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleMasterEntity> { rule }.BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity> { action }.BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity>().BuildMock());
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(DefaultEvidenceTypes().BuildMock());
        _mockTaxPolicyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity>().BuildMock());

        var result = await _service.GetLibraryAsync(DefaultQuery(), CancellationToken.None);

        Assert.Null(result.Rules.Items.Single().StartLogicSummary);
    }

    [Fact]
    public async Task GetLibraryAsync_RuleStatusFilter_NarrowsResults()
    {
        var rules = new List<RetrospectiveRuleMasterEntity>
        {
            new() { Id = 1, RuleCode = "R1", RuleName = "R1", RuleStatus = "Active", IsActive = true },
            new() { Id = 2, RuleCode = "R2", RuleName = "R2", RuleStatus = "Review", IsActive = true }
        };

        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(rules.BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity>().BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity>().BuildMock());
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(new List<EvidenceTypeMasterEntity>().BuildMock());
        _mockTaxPolicyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity>().BuildMock());

        var query = DefaultQuery();
        query.RuleStatus = "Active";
        query.RuleCode = "R1";
        query.RuleName = "R1";

        var result = await _service.GetLibraryAsync(query, CancellationToken.None);

        Assert.Equal(1, result.Rules.TotalCount);
        Assert.Equal("R1", result.Rules.Items.Single().RuleCode);
        Assert.Equal("R1", query.RuleCode);
        Assert.Equal("R1", query.RuleName);
    }

    [Fact]
    public async Task GetLibraryAsync_Paging_ReflectsPageNumberAndSize()
    {
        var rules = Enumerable.Range(1, 5)
            .Select(i => new RetrospectiveRuleMasterEntity { Id = i, RuleCode = $"R{i}", RuleName = $"R{i}", RuleStatus = "Active", IsActive = true })
            .ToList();

        _mockRuleRepository.Setup(r => r.GetQueryable()).Returns(rules.BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity>().BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity>().BuildMock());
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(new List<EvidenceTypeMasterEntity>().BuildMock());
        _mockTaxPolicyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveTaxPolicyEntity>().BuildMock());

        var query = new RuleLibraryQueryParameters { PageNumber = 2, PageSize = 2, FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And };

        var result = await _service.GetLibraryAsync(query, CancellationToken.None);

        Assert.Equal(5, result.Rules.TotalCount);
        Assert.Equal(2, result.Rules.PageNumber);
        Assert.Equal(2, result.Rules.PageSize);
        Assert.Equal(2, result.Rules.Items.Count());
    }
}
