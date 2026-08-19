using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleMaster;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Services.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;
using Xunit;
using ValidationResult = NtisPlatform.Application.Models.ValidationResult;

namespace NtisPlatform.Tests.Application.RetrospectiveTax;

public class RetrospectiveRuleMasterServiceTests
{
    private readonly Mock<IRepository<RetrospectiveRuleMasterEntity, int>> _mockRepository;
    private readonly Mock<IRepository<RetrospectiveRuleAuditLogEntity, int>> _mockAuditLogRepository;
    private readonly Mock<IRetrospectiveRuleEvidenceConditionService> _mockEvidenceConditionService;
    private readonly Mock<IRepository<RetrospectiveRuleDateConditionEntity, int>> _mockDateConditionRepository;
    private readonly Mock<IRepository<RetrospectiveRuleActionEntity, int>> _mockActionRepository;
    private readonly Mock<IRepository<RetrospectivePenaltyRuleEntity, int>> _mockPenaltyRepository;
    private readonly Mock<IRetrospectiveRuleSummaryService> _mockRuleSummaryService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly RetrospectiveRuleMasterService _service;

    public RetrospectiveRuleMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<RetrospectiveRuleMasterEntity, int>>();
        _mockAuditLogRepository = new Mock<IRepository<RetrospectiveRuleAuditLogEntity, int>>();
        _mockEvidenceConditionService = new Mock<IRetrospectiveRuleEvidenceConditionService>();
        _mockDateConditionRepository = new Mock<IRepository<RetrospectiveRuleDateConditionEntity, int>>();
        _mockActionRepository = new Mock<IRepository<RetrospectiveRuleActionEntity, int>>();
        _mockPenaltyRepository = new Mock<IRepository<RetrospectivePenaltyRuleEntity, int>>();
        _mockRuleSummaryService = new Mock<IRetrospectiveRuleSummaryService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new RetrospectiveRuleMasterService(
            _mockRepository.Object,
            _mockAuditLogRepository.Object,
            _mockEvidenceConditionService.Object,
            _mockDateConditionRepository.Object,
            _mockActionRepository.Object,
            _mockPenaltyRepository.Object,
            _mockRuleSummaryService.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object);
    }

    #region Standard CRUD

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "THA-01", RuleName = "Rule One", RuleStatus = "Draft" };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<RetrospectiveRuleMasterDto>(It.IsAny<RetrospectiveRuleMasterEntity>()))
            .Returns((RetrospectiveRuleMasterEntity e) => new RetrospectiveRuleMasterDto { Id = e.Id, RuleCode = e.RuleCode, RuleName = e.RuleName, RuleStatus = e.RuleStatus });

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("THA-01", result.RuleCode);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleMasterEntity?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        var entities = new List<RetrospectiveRuleMasterEntity>
        {
            new() { Id = 1, RuleCode = "THA-01", RuleName = "Rule One", RuleStatus = "Active" },
            new() { Id = 2, RuleCode = "THA-02", RuleName = "Rule Two", RuleStatus = "Draft" }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<RetrospectiveRuleMasterEntity, RetrospectiveRuleMasterDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new RetrospectiveRuleMasterService(
            _mockRepository.Object,
            _mockAuditLogRepository.Object,
            _mockEvidenceConditionService.Object,
            _mockDateConditionRepository.Object,
            _mockActionRepository.Object,
            _mockPenaltyRepository.Object,
            _mockRuleSummaryService.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var qp = new RetrospectiveRuleMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
        };

        var result = await service.GetAllAsync(qp, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.RuleCode == "THA-01");
        Assert.Contains(items, x => x.RuleCode == "THA-02");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var createDto = new CreateRetrospectiveRuleMasterDto { RuleCode = "THA-03", RuleName = "Rule Three" };

        _mockMapper
            .Setup(m => m.Map<RetrospectiveRuleMasterEntity>(It.IsAny<CreateRetrospectiveRuleMasterDto>()))
            .Returns((CreateRetrospectiveRuleMasterDto dto) => new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = dto.RuleCode, RuleName = dto.RuleName });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveRuleMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleMasterEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<RetrospectiveRuleMasterDto>(It.IsAny<RetrospectiveRuleMasterEntity>()))
            .Returns((RetrospectiveRuleMasterEntity e) => new RetrospectiveRuleMasterDto { Id = e.Id, RuleCode = e.RuleCode, RuleName = e.RuleName });

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("THA-03", result.RuleCode);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveRuleMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var updateDto = new UpdateRetrospectiveRuleMasterDto
        {
            RuleCode = "THA-01",
            RuleName = "Updated Name",
            RuleDescription = "Updated description",
            PriorityNo = 2,
            MatchType = "EXACT_EVIDENCE_MATCH",
            IsFallbackRule = true,
            RuleStatus = "Review",
            AuthorizationStatus = "AUTHORIZED",
            LegalCapEnabled = true,
            LegalCapYears = 8,
            NoticeDays = 30,
            VersionNo = "v2",
            ResolutionRef = "RES-100",
            EffectiveFrom = new DateTime(2024, 4, 1),
            EffectiveTo = new DateTime(2025, 3, 31),
            Remarks = "Updated remarks",
            IsActive = true,
            UpdatedBy = 2
        };
        var existingEntity = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "THA-01", RuleName = "Old Name", IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRetrospectiveRuleMasterDto>(), It.IsAny<RetrospectiveRuleMasterEntity>()))
            .Callback((UpdateRetrospectiveRuleMasterDto src, RetrospectiveRuleMasterEntity dest) =>
            {
                dest.RuleCode = src.RuleCode;
                dest.RuleName = src.RuleName;
                dest.RuleDescription = src.RuleDescription;
                dest.PriorityNo = src.PriorityNo;
                dest.MatchType = src.MatchType;
                dest.IsFallbackRule = src.IsFallbackRule;
                dest.RuleStatus = src.RuleStatus;
                dest.AuthorizationStatus = src.AuthorizationStatus;
                dest.LegalCapEnabled = src.LegalCapEnabled;
                dest.LegalCapYears = src.LegalCapYears;
                dest.NoticeDays = src.NoticeDays;
                dest.VersionNo = src.VersionNo;
                dest.ResolutionRef = src.ResolutionRef;
                dest.EffectiveFrom = src.EffectiveFrom;
                dest.EffectiveTo = src.EffectiveTo;
                dest.Remarks = src.Remarks;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
            });

        _mockMapper
            .Setup(m => m.Map<RetrospectiveRuleMasterDto>(It.IsAny<RetrospectiveRuleMasterEntity>()))
            .Returns((RetrospectiveRuleMasterEntity e) => new RetrospectiveRuleMasterDto { Id = e.Id, RuleName = e.RuleName });

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Updated Name", existingEntity.RuleName);
        Assert.Equal("Updated description", existingEntity.RuleDescription);
        Assert.Equal(2, existingEntity.PriorityNo);
        Assert.Equal("EXACT_EVIDENCE_MATCH", existingEntity.MatchType);
        Assert.True(existingEntity.IsFallbackRule);
        Assert.Equal("Review", existingEntity.RuleStatus);
        Assert.Equal("AUTHORIZED", existingEntity.AuthorizationStatus);
        Assert.True(existingEntity.LegalCapEnabled);
        Assert.Equal(8, existingEntity.LegalCapYears);
        Assert.Equal(30, existingEntity.NoticeDays);
        Assert.Equal("v2", existingEntity.VersionNo);
        Assert.Equal("RES-100", existingEntity.ResolutionRef);
        Assert.Equal(new DateTime(2024, 4, 1), existingEntity.EffectiveFrom);
        Assert.Equal(new DateTime(2025, 3, 31), existingEntity.EffectiveTo);
        Assert.Equal("Updated remarks", existingEntity.Remarks);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        var updateDto = new UpdateRetrospectiveRuleMasterDto { RuleCode = "THA-01", RuleName = "X" };

        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((RetrospectiveRuleMasterEntity?)null);

        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateWithReferences_ThrowsValidationException()
    {
        var updateDto = new UpdateRetrospectiveRuleMasterDto { RuleCode = "THA-01", RuleName = "X", IsActive = false };
        var existingEntity = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "THA-01", RuleName = "X", IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRetrospectiveRuleMasterDto>(), It.IsAny<RetrospectiveRuleMasterEntity>()))
            .Callback((UpdateRetrospectiveRuleMasterDto src, RetrospectiveRuleMasterEntity dest) => dest.IsActive = src.IsActive);

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<RetrospectiveRuleMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Rule is referenced elsewhere."));

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, updateDto, CancellationToken.None));
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        var existingEntity = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "THA-01" };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mockReferenceValidator.Setup(v => v.ValidateReferencesAsync<RetrospectiveRuleMasterEntity>(1, It.IsAny<CancellationToken>())).ReturnsAsync(ValidationResult.Success());
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<RetrospectiveRuleMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(1, CancellationToken.None);

        Assert.True(result);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((RetrospectiveRuleMasterEntity?)null);

        var result = await _service.DeleteAsync(999, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ThrowsValidationException()
    {
        var existingEntity = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "THA-01" };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<RetrospectiveRuleMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Rule is referenced elsewhere."));

        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(1, CancellationToken.None));
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectiveRuleMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region CreateFromRangeAsync

    [Fact]
    public async Task CreateFromRangeAsync_GeneratesEntitiesWithRuleCodeFromRange()
    {
        var request = new NtisPlatform.Application.DTOs.Range.RangeCreateRequest<CreateRetrospectiveRuleMasterDto>
        {
            RangeFrom = "1",
            RangeTo = "2",
            Template = new CreateRetrospectiveRuleMasterDto { RuleName = "Generated Rule", MatchType = "CONDITION_BASED", RuleStatus = "Draft" }
        };

        // The transformer builds a CreateDto per range value; BaseCommonCrudService.CreateFromRangeAsync
        // maps each one to an entity individually before batching them for AddRangeAsync.
        _mockMapper
            .Setup(m => m.Map<RetrospectiveRuleMasterEntity>(It.IsAny<CreateRetrospectiveRuleMasterDto>()))
            .Returns((CreateRetrospectiveRuleMasterDto dto) => new RetrospectiveRuleMasterEntity { RuleCode = dto.RuleCode, RuleName = dto.RuleName });

        _mockMapper
            .Setup(m => m.Map<List<RetrospectiveRuleMasterDto>>(It.IsAny<List<RetrospectiveRuleMasterEntity>>()))
            .Returns((List<RetrospectiveRuleMasterEntity> entities) => entities.Select(e => new RetrospectiveRuleMasterDto { Id = e.Id, RuleCode = e.RuleCode }).ToList());

        _mockRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<RetrospectiveRuleMasterEntity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.CreateFromRangeAsync(request, CancellationToken.None);

        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        _mockRepository.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<RetrospectiveRuleMasterEntity>>(list => list.Select(e => e.RuleCode).SequenceEqual(new[] { "1", "2" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region PublishAsync

    [Fact]
    public async Task PublishAsync_RuleNotFound_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((RetrospectiveRuleMasterEntity?)null);

        var result = await _service.PublishAsync(999, new PublishRetrospectiveRuleDto { PublishedBy = 1 }, CancellationToken.None);

        Assert.Null(result);
        _mockAuditLogRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveRuleAuditLogEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_AlreadyActive_ThrowsValidationException()
    {
        var entity = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "THA-01", RuleStatus = "Active" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        await Assert.ThrowsAsync<ValidationException>(() => _service.PublishAsync(1, new PublishRetrospectiveRuleDto { PublishedBy = 1 }, CancellationToken.None));

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockAuditLogRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveRuleAuditLogEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_DraftRule_PublishesAndWritesAuditLog()
    {
        var entity = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "THA-01", RuleStatus = "Draft" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockAuditLogRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveRuleAuditLogEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleAuditLogEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<RetrospectiveRuleMasterDto>(It.IsAny<RetrospectiveRuleMasterEntity>()))
            .Returns((RetrospectiveRuleMasterEntity e) => new RetrospectiveRuleMasterDto { Id = e.Id, RuleStatus = e.RuleStatus });

        var request = new PublishRetrospectiveRuleDto { PublishedBy = 7, Remarks = "Approved" };
        var result = await _service.PublishAsync(1, request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Active", entity.RuleStatus);
        Assert.Equal(7, entity.UpdatedBy);

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockAuditLogRepository.Verify(r => r.AddAsync(
            It.Is<RetrospectiveRuleAuditLogEntity>(a => a.RuleId == 1 && a.ActionType == "PUBLISH" && a.OldValue == "Draft" && a.NewValue == "Active" && a.Remarks == "Approved"),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetDetailAsync

    [Fact]
    public async Task GetDetailAsync_RuleNotFound_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((RetrospectiveRuleMasterEntity?)null);

        var result = await _service.GetDetailAsync(999, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDetailAsync_NoChildSectionsConfigured_ReturnsNullSections()
    {
        var entity = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "THA-01", RuleName = "Rule One" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<RetrospectiveRuleMasterDto>(It.IsAny<RetrospectiveRuleMasterEntity>()))
            .Returns((RetrospectiveRuleMasterEntity e) => new RetrospectiveRuleMasterDto { Id = e.Id, RuleCode = e.RuleCode });

        _mockEvidenceConditionService
            .Setup(s => s.GetEvidenceStateForRuleAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition.RetrospectiveRuleEvidenceConditionStateDto>());

        _mockDateConditionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleDateConditionEntity>().BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity>().BuildMock());
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity>().BuildMock());

        _mockRuleSummaryService
            .Setup(s => s.GetForRuleAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleSummary.RetrospectiveRuleSummaryViewDto { RuleId = 1, RuleCode = "THA-01" });

        var result = await _service.GetDetailAsync(1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("THA-01", result.Rule.RuleCode);
        Assert.Empty(result.EvidenceConditions);
        Assert.Null(result.DateCondition);
        Assert.Null(result.Action);
        Assert.Null(result.PenaltyRule);
        Assert.NotNull(result.Summary);
    }

    [Fact]
    public async Task GetDetailAsync_AllChildSectionsConfigured_ReturnsPopulatedSections()
    {
        var entity = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "THA-01", RuleName = "Rule One" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<RetrospectiveRuleMasterDto>(It.IsAny<RetrospectiveRuleMasterEntity>()))
            .Returns((RetrospectiveRuleMasterEntity e) => new RetrospectiveRuleMasterDto { Id = e.Id, RuleCode = e.RuleCode });

        _mockEvidenceConditionService
            .Setup(s => s.GetEvidenceStateForRuleAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition.RetrospectiveRuleEvidenceConditionStateDto>
            {
                new() { EvidenceTypeId = 1, EvidenceCode = "OC", SelectedState = "AVAILABLE" }
            });

        var dateCondition = new RetrospectiveRuleDateConditionEntity { Id = 1, RuleId = 1, ComparatorCode = "NONE", IsActive = true };
        _mockDateConditionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleDateConditionEntity> { dateCondition }.BuildMock());
        _mockMapper.Setup(m => m.Map<NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleDateCondition.RetrospectiveRuleDateConditionDto>(It.IsAny<RetrospectiveRuleDateConditionEntity>()))
            .Returns(new NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleDateCondition.RetrospectiveRuleDateConditionDto { Id = 1, RuleId = 1, ComparatorCode = "NONE" });

        var action = new RetrospectiveRuleActionEntity { Id = 1, RuleId = 1, TaxStartMode = "EVIDENCE_DATE", RetrospectiveLimitType = "NONE", TaxCalculationMode = "SINGLE", IsActive = true };
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity> { action }.BuildMock());
        _mockMapper.Setup(m => m.Map<NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleAction.RetrospectiveRuleActionDto>(It.IsAny<RetrospectiveRuleActionEntity>()))
            .Returns(new NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleAction.RetrospectiveRuleActionDto { Id = 1, RuleId = 1, TaxStartMode = "EVIDENCE_DATE" });

        var penalty = new RetrospectivePenaltyRuleEntity { Id = 1, RuleId = 1, PenaltyMode = "NONE", IsActive = true };
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity> { penalty }.BuildMock());
        _mockMapper.Setup(m => m.Map<NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectivePenaltyRule.RetrospectivePenaltyRuleDto>(It.IsAny<RetrospectivePenaltyRuleEntity>()))
            .Returns(new NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectivePenaltyRule.RetrospectivePenaltyRuleDto { Id = 1, RuleId = 1, PenaltyMode = "NONE" });

        _mockRuleSummaryService
            .Setup(s => s.GetForRuleAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleSummary.RetrospectiveRuleSummaryViewDto { RuleId = 1, RuleCode = "THA-01", WhenSummary = "OC available" });

        var result = await _service.GetDetailAsync(1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.EvidenceConditions);
        Assert.NotNull(result.DateCondition);
        Assert.NotNull(result.Action);
        Assert.NotNull(result.PenaltyRule);
        Assert.Equal("OC available", result.Summary?.WhenSummary);
    }

    #endregion

    #region SaveAsync

    private void SetupGetDetailMocksForSave(int ruleId)
    {
        _mockMapper.Setup(m => m.Map<RetrospectiveRuleMasterDto>(It.IsAny<RetrospectiveRuleMasterEntity>()))
            .Returns((RetrospectiveRuleMasterEntity e) => new RetrospectiveRuleMasterDto { Id = e.Id, RuleCode = e.RuleCode, RuleName = e.RuleName, RuleStatus = e.RuleStatus });

        _mockEvidenceConditionService
            .Setup(s => s.GetEvidenceStateForRuleAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition.RetrospectiveRuleEvidenceConditionStateDto>());

        _mockRuleSummaryService
            .Setup(s => s.GetForRuleAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleSummary.RetrospectiveRuleSummaryViewDto?)null);
    }

    private static SaveRetrospectiveRuleDto BuildSaveRequest(int? id = null) => new()
    {
        Id = id,
        RuleCode = "THA-05",
        RuleName = "New Rule",
        PriorityNo = 1,
        MatchType = "CONDITION_BASED",
        AvailableEvidenceTypeIds = new List<int> { 1 },
        UnavailableEvidenceTypeIds = new List<int> { 2 },
        DateCondition = new NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleMaster.SaveRetrospectiveRuleDateConditionDto
        {
            ComparatorCode = "OC_OLDER_THAN_ALLOWED_PERIOD"
        },
        Action = new NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleMaster.SaveRetrospectiveRuleActionDto
        {
            TaxStartMode = "EVIDENCE_DATE",
            RetrospectiveLimitType = "MAXIMUM_YEARS",
            MaximumYears = 6,
            TaxCalculationMode = "SINGLE",
            TaxMultiplier = 2m
        },
        PenaltyRule = new NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleMaster.SaveRetrospectivePenaltyRuleDto
        {
            IsPenaltyApplicable = true,
            PenaltyMode = "ACT_UNLAWFUL"
        },
        UpdatedBy = 9
    };

    [Fact]
    public async Task SaveAsync_NewRule_CreatesRuleAndAllChildSections()
    {
        var request = BuildSaveRequest();

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveRuleMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleMasterEntity e, CancellationToken _) => { e.Id = 5; return e; });

        // GetDetailAsync (called at the end of SaveAsync) re-fetches the rule by its newly-assigned Id.
        _mockRepository
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RetrospectiveRuleMasterEntity { Id = 5, RuleCode = "THA-05", RuleName = "New Rule", RuleStatus = "Draft" });

        _mockEvidenceConditionService
            .Setup(s => s.SetEvidenceStateForRuleAsync(5, It.IsAny<NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition.SetRetrospectiveRuleEvidenceConditionStateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition.RetrospectiveRuleEvidenceConditionStateDto>());

        _mockDateConditionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleDateConditionEntity>().BuildMock());
        _mockDateConditionRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveRuleDateConditionEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleDateConditionEntity e, CancellationToken _) => e);

        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity>().BuildMock());
        _mockActionRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveRuleActionEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleActionEntity e, CancellationToken _) => e);

        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity>().BuildMock());
        _mockPenaltyRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectivePenaltyRuleEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectivePenaltyRuleEntity e, CancellationToken _) => e);

        SetupGetDetailMocksForSave(5);

        var result = await _service.SaveAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("THA-05", result!.Rule.RuleCode);

        _mockRepository.Verify(r => r.AddAsync(
            It.Is<RetrospectiveRuleMasterEntity>(e => e.RuleCode == "THA-05" && e.RuleStatus == "Draft" && e.CreatedBy == 9),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockEvidenceConditionService.Verify(s => s.SetEvidenceStateForRuleAsync(
            5,
            It.Is<NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition.SetRetrospectiveRuleEvidenceConditionStateDto>(
                d => d.AvailableEvidenceTypeIds.SequenceEqual(new[] { 1 }) && d.UnavailableEvidenceTypeIds.SequenceEqual(new[] { 2 }) && d.UpdatedBy == 9),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockDateConditionRepository.Verify(r => r.AddAsync(
            It.Is<RetrospectiveRuleDateConditionEntity>(e => e.RuleId == 5 && e.ComparatorCode == "OC_OLDER_THAN_ALLOWED_PERIOD" && e.CreatedBy == 9),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockActionRepository.Verify(r => r.AddAsync(
            It.Is<RetrospectiveRuleActionEntity>(e => e.RuleId == 5 && e.TaxStartMode == "EVIDENCE_DATE" && e.TaxMultiplier == 2m && e.CreatedBy == 9),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockPenaltyRepository.Verify(r => r.AddAsync(
            It.Is<RetrospectivePenaltyRuleEntity>(e => e.RuleId == 5 && e.PenaltyMode == "ACT_UNLAWFUL" && e.IsPenaltyApplicable && e.CreatedBy == 9),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ExistingIdNotFound_ReturnsNull()
    {
        var request = BuildSaveRequest(999);
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((RetrospectiveRuleMasterEntity?)null);

        var result = await _service.SaveAsync(request, CancellationToken.None);

        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockEvidenceConditionService.Verify(
            s => s.SetEvidenceStateForRuleAsync(It.IsAny<int>(), It.IsAny<NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition.SetRetrospectiveRuleEvidenceConditionStateDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SaveAsync_ExistingRule_UpdatesHeaderAndAllExistingSections()
    {
        var request = BuildSaveRequest(1);
        var existingRule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "THA-01", RuleName = "Old Name", RuleStatus = "Draft", IsActive = true };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingRule);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _mockEvidenceConditionService
            .Setup(s => s.SetEvidenceStateForRuleAsync(1, It.IsAny<NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition.SetRetrospectiveRuleEvidenceConditionStateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition.RetrospectiveRuleEvidenceConditionStateDto>());

        var existingDateCondition = new RetrospectiveRuleDateConditionEntity { Id = 10, RuleId = 1, ComparatorCode = "NONE", IsActive = true };
        _mockDateConditionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleDateConditionEntity> { existingDateCondition }.BuildMock());
        _mockDateConditionRepository.Setup(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleDateConditionEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var existingAction = new RetrospectiveRuleActionEntity { Id = 11, RuleId = 1, TaxStartMode = "FY_START", RetrospectiveLimitType = "NONE", TaxCalculationMode = "SINGLE", IsActive = true };
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity> { existingAction }.BuildMock());
        _mockActionRepository.Setup(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleActionEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var existingPenalty = new RetrospectivePenaltyRuleEntity { Id = 12, RuleId = 1, PenaltyMode = "NONE", IsActive = true };
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity> { existingPenalty }.BuildMock());
        _mockPenaltyRepository.Setup(r => r.UpdateAsync(It.IsAny<RetrospectivePenaltyRuleEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        SetupGetDetailMocksForSave(1);

        var result = await _service.SaveAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("THA-05", existingRule.RuleCode);
        Assert.Equal("Draft", existingRule.RuleStatus);
        Assert.Equal(9, existingRule.UpdatedBy);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveRuleMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal("OC_OLDER_THAN_ALLOWED_PERIOD", existingDateCondition.ComparatorCode);
        _mockDateConditionRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveRuleDateConditionEntity>(), It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal("EVIDENCE_DATE", existingAction.TaxStartMode);
        Assert.Equal(2m, existingAction.TaxMultiplier);
        _mockActionRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveRuleActionEntity>(), It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal("ACT_UNLAWFUL", existingPenalty.PenaltyMode);
        Assert.True(existingPenalty.IsPenaltyApplicable);
        _mockPenaltyRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectivePenaltyRuleEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_NullOptionalSections_WithExistingRows_DeactivatesThem()
    {
        var request = BuildSaveRequest(1);
        request.DateCondition = null;
        request.PenaltyRule = null;

        var existingRule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "THA-01", RuleStatus = "Draft", IsActive = true };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingRule);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _mockEvidenceConditionService
            .Setup(s => s.SetEvidenceStateForRuleAsync(1, It.IsAny<NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition.SetRetrospectiveRuleEvidenceConditionStateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition.RetrospectiveRuleEvidenceConditionStateDto>());

        var existingDateCondition = new RetrospectiveRuleDateConditionEntity { Id = 10, RuleId = 1, ComparatorCode = "NONE", IsActive = true };
        _mockDateConditionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleDateConditionEntity> { existingDateCondition }.BuildMock());
        _mockDateConditionRepository.Setup(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleDateConditionEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity>().BuildMock());
        _mockActionRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveRuleActionEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleActionEntity e, CancellationToken _) => e);

        var existingPenalty = new RetrospectivePenaltyRuleEntity { Id = 12, RuleId = 1, PenaltyMode = "ACT_UNLAWFUL", IsPenaltyApplicable = true, IsActive = true };
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity> { existingPenalty }.BuildMock());
        _mockPenaltyRepository.Setup(r => r.UpdateAsync(It.IsAny<RetrospectivePenaltyRuleEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        SetupGetDetailMocksForSave(1);

        var result = await _service.SaveAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(existingDateCondition.IsActive);
        Assert.Equal(9, existingDateCondition.UpdatedBy);
        Assert.False(existingPenalty.IsActive);
        Assert.Equal(9, existingPenalty.UpdatedBy);
        _mockDateConditionRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveRuleDateConditionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockPenaltyRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectivePenaltyRuleEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_NullOptionalSections_NoExistingRows_NoOp()
    {
        var request = BuildSaveRequest(1);
        request.DateCondition = null;
        request.PenaltyRule = null;

        var existingRule = new RetrospectiveRuleMasterEntity { Id = 1, RuleCode = "THA-01", RuleStatus = "Draft", IsActive = true };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingRule);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _mockEvidenceConditionService
            .Setup(s => s.SetEvidenceStateForRuleAsync(1, It.IsAny<NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition.SetRetrospectiveRuleEvidenceConditionStateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition.RetrospectiveRuleEvidenceConditionStateDto>());

        _mockDateConditionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleDateConditionEntity>().BuildMock());
        _mockActionRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectiveRuleActionEntity>().BuildMock());
        _mockActionRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveRuleActionEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleActionEntity e, CancellationToken _) => e);
        _mockPenaltyRepository.Setup(r => r.GetQueryable()).Returns(new List<RetrospectivePenaltyRuleEntity>().BuildMock());

        SetupGetDetailMocksForSave(1);

        var result = await _service.SaveAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        _mockDateConditionRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveRuleDateConditionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockDateConditionRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleDateConditionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockPenaltyRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectivePenaltyRuleEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockPenaltyRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectivePenaltyRuleEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
