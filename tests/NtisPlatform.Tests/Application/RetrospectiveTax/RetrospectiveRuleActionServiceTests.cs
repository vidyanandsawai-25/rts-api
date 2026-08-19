using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleAction;
using NtisPlatform.Application.Services.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.RetrospectiveTax;

public class RetrospectiveRuleActionServiceTests
{
    private readonly Mock<IRepository<RetrospectiveRuleActionEntity, int>> _mockRepository;
    private readonly Mock<IRepository<EvidenceTypeMasterEntity, int>> _mockEvidenceTypeRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly RetrospectiveRuleActionService _service;

    public RetrospectiveRuleActionServiceTests()
    {
        _mockRepository = new Mock<IRepository<RetrospectiveRuleActionEntity, int>>();
        _mockEvidenceTypeRepository = new Mock<IRepository<EvidenceTypeMasterEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new RetrospectiveRuleActionService(
            _mockRepository.Object,
            _mockEvidenceTypeRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new RetrospectiveRuleActionEntity
        {
            Id = 1,
            RuleId = 10,
            TaxStartMode = "EVIDENCE_DATE",
            StartEvidenceTypeId = 2,
            OffsetMonths = 6,
            RetrospectiveLimitType = "MAXIMUM_YEARS",
            MaximumYears = 5,
            CutoffDate = new DateTime(2024, 1, 1),
            TaxCalculationMode = "SPLIT",
            TaxMultiplier = 1.00m,
            SplitStartEvidenceTypeId = 3,
            SplitEndEvidenceTypeId = 4,
            SplitMultiplier = 1.5m,
            AfterSplitMultiplier = 1.0m,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1,
            Rule = new RetrospectiveRuleMasterEntity { Id = 10, RuleCode = "R1", RuleName = "Rule 1" },
            StartEvidenceType = new EvidenceTypeMasterEntity { Id = 2, EvidenceCode = "OC", EvidenceName = "Occupancy Certificate" },
            SplitStartEvidenceType = new EvidenceTypeMasterEntity { Id = 3, EvidenceCode = "CC", EvidenceName = "Completion Certificate" },
            SplitEndEvidenceType = new EvidenceTypeMasterEntity { Id = 4, EvidenceCode = "EL", EvidenceName = "Electricity" }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<RetrospectiveRuleActionDto>(It.IsAny<RetrospectiveRuleActionEntity>()))
            .Returns((RetrospectiveRuleActionEntity e) => new RetrospectiveRuleActionDto
            {
                Id = e.Id,
                RuleId = e.RuleId,
                TaxStartMode = e.TaxStartMode,
                StartEvidenceTypeId = e.StartEvidenceTypeId,
                OffsetMonths = e.OffsetMonths,
                RetrospectiveLimitType = e.RetrospectiveLimitType,
                MaximumYears = e.MaximumYears,
                CutoffDate = e.CutoffDate,
                TaxCalculationMode = e.TaxCalculationMode,
                TaxMultiplier = e.TaxMultiplier,
                SplitStartEvidenceTypeId = e.SplitStartEvidenceTypeId,
                SplitEndEvidenceTypeId = e.SplitEndEvidenceTypeId,
                SplitMultiplier = e.SplitMultiplier,
                AfterSplitMultiplier = e.AfterSplitMultiplier,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.RuleId);
        Assert.Equal("EVIDENCE_DATE", result.TaxStartMode);
        Assert.Equal(2, result.StartEvidenceTypeId);
        Assert.True(result.IsActive);
        Assert.Equal(6, result.OffsetMonths);
        Assert.Equal(1.5m, result.SplitMultiplier);
        Assert.Equal(1.0m, result.AfterSplitMultiplier);
        Assert.NotNull(entity.Rule);
        Assert.NotNull(entity.StartEvidenceType);
        Assert.NotNull(entity.SplitStartEvidenceType);
        Assert.NotNull(entity.SplitEndEvidenceType);
        Assert.Equal("R1", entity.Rule!.RuleCode);
        Assert.Equal("OC", entity.StartEvidenceType!.EvidenceCode);
        Assert.Equal("CC", entity.SplitStartEvidenceType!.EvidenceCode);
        Assert.Equal("EL", entity.SplitEndEvidenceType!.EvidenceCode);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleActionEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<RetrospectiveRuleActionEntity>
        {
            new()
            {
                Id = 1,
                RuleId = 10,
                TaxStartMode = "EVIDENCE_DATE",
                RetrospectiveLimitType = "MAXIMUM_YEARS",
                TaxCalculationMode = "SINGLE",
                TaxMultiplier = 1.00m,
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now
            },
            new()
            {
                Id = 2,
                RuleId = 11,
                TaxStartMode = "FIXED_CUTOFF",
                RetrospectiveLimitType = "NONE",
                TaxCalculationMode = "SPLIT",
                TaxMultiplier = 1.50m,
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now
            }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<RetrospectiveRuleActionEntity, RetrospectiveRuleActionDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new RetrospectiveRuleActionService(
            _mockRepository.Object,
            _mockEvidenceTypeRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new RetrospectiveRuleActionQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.RuleId == 10);
        Assert.Contains(items, x => x.RuleId == 11);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateRetrospectiveRuleActionDto
        {
            RuleId = 10,
            TaxStartMode = "EVIDENCE_DATE",
            StartEvidenceTypeId = 2,
            OffsetMonths = 6,
            RetrospectiveLimitType = "MAXIMUM_YEARS",
            MaximumYears = 5,
            CutoffDate = new DateTime(2024, 1, 1),
            TaxCalculationMode = "SPLIT",
            TaxMultiplier = 1.00m,
            SplitStartEvidenceTypeId = 3,
            SplitEndEvidenceTypeId = 4,
            SplitMultiplier = 1.5m,
            AfterSplitMultiplier = 1.0m,
            CreatedBy = 1,
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<RetrospectiveRuleActionEntity>(It.IsAny<CreateRetrospectiveRuleActionDto>()))
            .Returns((CreateRetrospectiveRuleActionDto dto) => new RetrospectiveRuleActionEntity
            {
                Id = 1,
                RuleId = dto.RuleId,
                TaxStartMode = dto.TaxStartMode,
                StartEvidenceTypeId = dto.StartEvidenceTypeId,
                OffsetMonths = dto.OffsetMonths,
                RetrospectiveLimitType = dto.RetrospectiveLimitType,
                MaximumYears = dto.MaximumYears,
                CutoffDate = dto.CutoffDate,
                TaxCalculationMode = dto.TaxCalculationMode,
                TaxMultiplier = dto.TaxMultiplier,
                SplitStartEvidenceTypeId = dto.SplitStartEvidenceTypeId,
                SplitEndEvidenceTypeId = dto.SplitEndEvidenceTypeId,
                SplitMultiplier = dto.SplitMultiplier,
                AfterSplitMultiplier = dto.AfterSplitMultiplier,
                IsActive = true,
                CreatedDate = DateTime.Now,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveRuleActionEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleActionEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<RetrospectiveRuleActionDto>(It.IsAny<RetrospectiveRuleActionEntity>()))
            .Returns((RetrospectiveRuleActionEntity e) => new RetrospectiveRuleActionDto
            {
                Id = e.Id,
                RuleId = e.RuleId,
                TaxStartMode = e.TaxStartMode,
                StartEvidenceTypeId = e.StartEvidenceTypeId,
                RetrospectiveLimitType = e.RetrospectiveLimitType,
                MaximumYears = e.MaximumYears,
                TaxCalculationMode = e.TaxCalculationMode,
                TaxMultiplier = e.TaxMultiplier,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.RuleId);
        Assert.Equal("EVIDENCE_DATE", result.TaxStartMode);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveRuleActionEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateRetrospectiveRuleActionDto
        {
            RuleId = 10,
            TaxStartMode = "FIXED_CUTOFF",
            StartEvidenceTypeId = 2,
            OffsetMonths = 6,
            CutoffDate = new DateTime(2024, 1, 1),
            RetrospectiveLimitType = "FIXED_CUTOFF_DATE",
            MaximumYears = 5,
            TaxCalculationMode = "SPLIT",
            TaxMultiplier = 2.00m,
            SplitStartEvidenceTypeId = 3,
            SplitEndEvidenceTypeId = 4,
            SplitMultiplier = 1.5m,
            AfterSplitMultiplier = 1.0m,
            IsActive = true,
            UpdatedBy = 2
        };

        var existingEntity = new RetrospectiveRuleActionEntity
        {
            Id = 1,
            RuleId = 10,
            TaxStartMode = "EVIDENCE_DATE",
            StartEvidenceTypeId = 2,
            RetrospectiveLimitType = "MAXIMUM_YEARS",
            MaximumYears = 5,
            TaxCalculationMode = "SINGLE",
            TaxMultiplier = 1.00m,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleActionEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRetrospectiveRuleActionDto>(), It.IsAny<RetrospectiveRuleActionEntity>()))
            .Callback((UpdateRetrospectiveRuleActionDto src, RetrospectiveRuleActionEntity dest) =>
            {
                dest.RuleId = src.RuleId;
                dest.TaxStartMode = src.TaxStartMode;
                dest.StartEvidenceTypeId = src.StartEvidenceTypeId;
                dest.OffsetMonths = src.OffsetMonths;
                dest.CutoffDate = src.CutoffDate;
                dest.RetrospectiveLimitType = src.RetrospectiveLimitType;
                dest.MaximumYears = src.MaximumYears;
                dest.TaxCalculationMode = src.TaxCalculationMode;
                dest.TaxMultiplier = src.TaxMultiplier;
                dest.SplitStartEvidenceTypeId = src.SplitStartEvidenceTypeId;
                dest.SplitEndEvidenceTypeId = src.SplitEndEvidenceTypeId;
                dest.SplitMultiplier = src.SplitMultiplier;
                dest.AfterSplitMultiplier = src.AfterSplitMultiplier;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
                dest.UpdatedDate = DateTime.Now;
            });

        _mockMapper
            .Setup(m => m.Map<RetrospectiveRuleActionDto>(It.IsAny<RetrospectiveRuleActionEntity>()))
            .Returns((RetrospectiveRuleActionEntity e) => new RetrospectiveRuleActionDto
            {
                Id = e.Id,
                RuleId = e.RuleId,
                TaxStartMode = e.TaxStartMode,
                CutoffDate = e.CutoffDate,
                RetrospectiveLimitType = e.RetrospectiveLimitType,
                TaxCalculationMode = e.TaxCalculationMode,
                TaxMultiplier = e.TaxMultiplier,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleActionEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal("FIXED_CUTOFF", existingEntity.TaxStartMode);
        Assert.Equal("FIXED_CUTOFF_DATE", existingEntity.RetrospectiveLimitType);
        Assert.Equal(2, existingEntity.StartEvidenceTypeId);
        Assert.Equal(6, existingEntity.OffsetMonths);
        Assert.Equal(5, existingEntity.MaximumYears);
        Assert.Equal(3, existingEntity.SplitStartEvidenceTypeId);
        Assert.Equal(4, existingEntity.SplitEndEvidenceTypeId);
        Assert.Equal(1.5m, existingEntity.SplitMultiplier);
        Assert.Equal(1.0m, existingEntity.AfterSplitMultiplier);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateRetrospectiveRuleActionDto
        {
            RuleId = 10,
            TaxStartMode = "EVIDENCE_DATE",
            RetrospectiveLimitType = "MAXIMUM_YEARS",
            TaxCalculationMode = "SINGLE",
            TaxMultiplier = 1.00m,
            IsActive = true,
            UpdatedBy = 2
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleActionEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleActionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        var idToDelete = 1;
        var existingEntity = new RetrospectiveRuleActionEntity
        {
            Id = 1,
            RuleId = 10,
            TaxStartMode = "EVIDENCE_DATE",
            RetrospectiveLimitType = "MAXIMUM_YEARS",
            TaxCalculationMode = "SINGLE",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<RetrospectiveRuleActionEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectiveRuleActionEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleActionEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectiveRuleActionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetUseDateOptionsAsync_ReturnsOneOptionPerActiveEvidenceType_PlusSyntheticCutoffDate()
    {
        // Arrange
        var evidenceTypes = new List<EvidenceTypeMasterEntity>
        {
            new()
            {
                Id = 1,
                EvidenceCode = "OC",
                EvidenceName = "OC",
                DisplayOrder = 1,
                IsActive = true
            },
            new()
            {
                Id = 2,
                EvidenceCode = "CC",
                EvidenceName = "CC",
                DisplayOrder = 2,
                IsActive = true
            },
            new()
            {
                Id = 3,
                EvidenceCode = "CHANGE_DETECTION",
                EvidenceName = "Change Detection",
                DisplayOrder = 3,
                IsActive = true
            },
            new()
            {
                Id = 4,
                EvidenceCode = "CONSTRUCTION_YEAR",
                EvidenceName = "Construction Year",
                DisplayOrder = 4,
                IsActive = true
            },
            new()
            {
                Id = 5,
                EvidenceCode = "ELECTRICITY",
                EvidenceName = "Electricity",
                DisplayOrder = 5,
                IsActive = false // inactive - must be excluded
            }
        };

        var mockQuery = evidenceTypes.BuildMock();
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        // Act
        var result = await _service.GetUseDateOptionsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Count); // 4 active evidence types + 1 synthetic cutoff-date entry

        // Ordered by DisplayOrder, with correct labels (including overrides)
        Assert.Equal(1, result[0].EvidenceTypeId);
        Assert.Equal("OC date", result[0].Label);
        Assert.False(result[0].IsCutoffDate);

        Assert.Equal(2, result[1].EvidenceTypeId);
        Assert.Equal("CC date", result[1].Label);
        Assert.False(result[1].IsCutoffDate);

        Assert.Equal(3, result[2].EvidenceTypeId);
        Assert.Equal("Change detection date", result[2].Label);
        Assert.False(result[2].IsCutoffDate);

        Assert.Equal(4, result[3].EvidenceTypeId);
        Assert.Equal("Construction date/year", result[3].Label);
        Assert.False(result[3].IsCutoffDate);

        // Synthetic "Cutoff date" entry appended last
        var lastOption = result[^1];
        Assert.Null(lastOption.EvidenceTypeId);
        Assert.Equal("Cutoff date", lastOption.Label);
        Assert.True(lastOption.IsCutoffDate);
    }
}
