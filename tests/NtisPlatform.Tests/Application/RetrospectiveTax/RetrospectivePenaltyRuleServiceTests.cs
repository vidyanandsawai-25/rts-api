using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectivePenaltyRule;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Services.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.RetrospectiveTax;

public class RetrospectivePenaltyRuleServiceTests
{
    private readonly Mock<IRepository<RetrospectivePenaltyRuleEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly RetrospectivePenaltyRuleService _service;

    public RetrospectivePenaltyRuleServiceTests()
    {
        _mockRepository = new Mock<IRepository<RetrospectivePenaltyRuleEntity, int>>();
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

        _service = new RetrospectivePenaltyRuleService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new RetrospectivePenaltyRuleEntity
        {
            Id = 1,
            RuleId = 10,
            IsPenaltyApplicable = true,
            PenaltyMode = "DATE_VALIDATION",
            PenaltyPercent = 12.5m,
            PenaltyDateSourceType = "EVIDENCE_DATE",
            PenaltyDateEvidenceTypeId = 2,
            PenaltyDateCondition = "ON_OR_AFTER",
            CompareDate = new DateTime(2026, 1, 1),
            CompareDateTo = new DateTime(2026, 6, 1),
            ElseAction = "MANUAL_REVIEW",
            RequiresManualReview = true,
            Remarks = "Needs review",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1,
            UpdatedDate = null,
            UpdatedBy = null,
            Rule = new RetrospectiveRuleMasterEntity { Id = 10, RuleCode = "R1", RuleName = "Rule 1" },
            PenaltyDateEvidenceType = new EvidenceTypeMasterEntity { Id = 2, EvidenceCode = "OC", EvidenceName = "Occupancy Certificate" }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<RetrospectivePenaltyRuleDto>(It.IsAny<RetrospectivePenaltyRuleEntity>()))
            .Returns((RetrospectivePenaltyRuleEntity e) => new RetrospectivePenaltyRuleDto
            {
                Id = e.Id,
                RuleId = e.RuleId,
                IsPenaltyApplicable = e.IsPenaltyApplicable,
                PenaltyMode = e.PenaltyMode,
                PenaltyPercent = e.PenaltyPercent,
                PenaltyDateSourceType = e.PenaltyDateSourceType,
                PenaltyDateEvidenceTypeId = e.PenaltyDateEvidenceTypeId,
                PenaltyDateCondition = e.PenaltyDateCondition,
                CompareDate = e.CompareDate,
                CompareDateTo = e.CompareDateTo,
                ElseAction = e.ElseAction,
                RequiresManualReview = e.RequiresManualReview,
                Remarks = e.Remarks,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate,
                UpdatedDate = e.UpdatedDate
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.RuleId);
        Assert.True(result.IsPenaltyApplicable);
        Assert.Equal("DATE_VALIDATION", result.PenaltyMode);
        Assert.Equal(12.5m, result.PenaltyPercent);
        Assert.True(result.IsActive);
        Assert.True(result.RequiresManualReview);
        Assert.Equal("Needs review", result.Remarks);
        Assert.NotNull(entity.Rule);
        Assert.NotNull(entity.PenaltyDateEvidenceType);
        Assert.Equal("R1", entity.Rule!.RuleCode);
        Assert.Equal("OC", entity.PenaltyDateEvidenceType!.EvidenceCode);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectivePenaltyRuleEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<RetrospectivePenaltyRuleEntity>
        {
            new()
            {
                Id = 1,
                RuleId = 10,
                IsPenaltyApplicable = true,
                PenaltyMode = "ACT_UNLAWFUL",
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now
            },
            new()
            {
                Id = 2,
                RuleId = 11,
                IsPenaltyApplicable = false,
                PenaltyMode = "NONE",
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now
            }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<RetrospectivePenaltyRuleEntity, RetrospectivePenaltyRuleDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new RetrospectivePenaltyRuleService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new RetrospectivePenaltyRuleQueryParameters
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
        var createDto = new CreateRetrospectivePenaltyRuleDto
        {
            RuleId = 10,
            IsPenaltyApplicable = true,
            PenaltyMode = "DATE_VALIDATION",
            PenaltyPercent = 5m,
            PenaltyDateSourceType = "EVIDENCE_DATE",
            PenaltyDateEvidenceTypeId = 2,
            PenaltyDateCondition = "ON_OR_AFTER",
            CompareDate = new DateTime(2026, 1, 1),
            CompareDateTo = new DateTime(2026, 6, 1),
            ElseAction = "MANUAL_REVIEW",
            RequiresManualReview = true,
            Remarks = "Needs review",
            CreatedBy = 1,
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<RetrospectivePenaltyRuleEntity>(It.IsAny<CreateRetrospectivePenaltyRuleDto>()))
            .Returns((CreateRetrospectivePenaltyRuleDto dto) => new RetrospectivePenaltyRuleEntity
            {
                Id = 1,
                RuleId = dto.RuleId,
                IsPenaltyApplicable = dto.IsPenaltyApplicable,
                PenaltyMode = dto.PenaltyMode,
                PenaltyPercent = dto.PenaltyPercent,
                PenaltyDateSourceType = dto.PenaltyDateSourceType,
                PenaltyDateEvidenceTypeId = dto.PenaltyDateEvidenceTypeId,
                PenaltyDateCondition = dto.PenaltyDateCondition,
                CompareDate = dto.CompareDate,
                CompareDateTo = dto.CompareDateTo,
                ElseAction = dto.ElseAction,
                RequiresManualReview = dto.RequiresManualReview,
                Remarks = dto.Remarks,
                IsActive = true,
                CreatedDate = DateTime.Now,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectivePenaltyRuleEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectivePenaltyRuleEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<RetrospectivePenaltyRuleDto>(It.IsAny<RetrospectivePenaltyRuleEntity>()))
            .Returns((RetrospectivePenaltyRuleEntity e) => new RetrospectivePenaltyRuleDto
            {
                Id = e.Id,
                RuleId = e.RuleId,
                IsPenaltyApplicable = e.IsPenaltyApplicable,
                PenaltyMode = e.PenaltyMode,
                PenaltyPercent = e.PenaltyPercent,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.RuleId);
        Assert.True(result.IsPenaltyApplicable);
        Assert.Equal("DATE_VALIDATION", result.PenaltyMode);
        Assert.Equal(5m, result.PenaltyPercent);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(
            It.Is<RetrospectivePenaltyRuleEntity>(e =>
                e.PenaltyDateSourceType == "EVIDENCE_DATE" &&
                e.PenaltyDateEvidenceTypeId == 2 &&
                e.PenaltyDateCondition == "ON_OR_AFTER" &&
                e.CompareDateTo == new DateTime(2026, 6, 1) &&
                e.ElseAction == "MANUAL_REVIEW" &&
                e.RequiresManualReview == true &&
                e.Remarks == "Needs review"),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateRetrospectivePenaltyRuleDto
        {
            RuleId = 10,
            IsPenaltyApplicable = true,
            PenaltyMode = "DATE_VALIDATION",
            PenaltyPercent = 7.5m,
            PenaltyDateSourceType = "FIXED_DATE",
            PenaltyDateEvidenceTypeId = 2,
            PenaltyDateCondition = "ON_OR_AFTER",
            CompareDate = new DateTime(2026, 1, 1),
            CompareDateTo = new DateTime(2026, 6, 1),
            ElseAction = "MANUAL_REVIEW",
            RequiresManualReview = true,
            Remarks = "Needs review",
            IsActive = true,
            UpdatedBy = 2
        };

        var existingEntity = new RetrospectivePenaltyRuleEntity
        {
            Id = 1,
            RuleId = 10,
            IsPenaltyApplicable = false,
            PenaltyMode = "NONE",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RetrospectivePenaltyRuleEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRetrospectivePenaltyRuleDto>(), It.IsAny<RetrospectivePenaltyRuleEntity>()))
            .Callback((UpdateRetrospectivePenaltyRuleDto src, RetrospectivePenaltyRuleEntity dest) =>
            {
                dest.RuleId = src.RuleId;
                dest.IsPenaltyApplicable = src.IsPenaltyApplicable;
                dest.PenaltyMode = src.PenaltyMode;
                dest.PenaltyPercent = src.PenaltyPercent;
                dest.PenaltyDateSourceType = src.PenaltyDateSourceType;
                dest.PenaltyDateEvidenceTypeId = src.PenaltyDateEvidenceTypeId;
                dest.PenaltyDateCondition = src.PenaltyDateCondition;
                dest.CompareDate = src.CompareDate;
                dest.CompareDateTo = src.CompareDateTo;
                dest.ElseAction = src.ElseAction;
                dest.RequiresManualReview = src.RequiresManualReview;
                dest.Remarks = src.Remarks;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
                dest.UpdatedDate = DateTime.Now;
            });

        _mockMapper
            .Setup(m => m.Map<RetrospectivePenaltyRuleDto>(It.IsAny<RetrospectivePenaltyRuleEntity>()))
            .Returns((RetrospectivePenaltyRuleEntity e) => new RetrospectivePenaltyRuleDto
            {
                Id = e.Id,
                RuleId = e.RuleId,
                IsPenaltyApplicable = e.IsPenaltyApplicable,
                PenaltyMode = e.PenaltyMode,
                PenaltyDateSourceType = e.PenaltyDateSourceType,
                PenaltyDateCondition = e.PenaltyDateCondition,
                CompareDate = e.CompareDate,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectivePenaltyRuleEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal("DATE_VALIDATION", existingEntity.PenaltyMode);
        Assert.Equal("FIXED_DATE", existingEntity.PenaltyDateSourceType);
        Assert.True(existingEntity.IsPenaltyApplicable);
        Assert.Equal(7.5m, existingEntity.PenaltyPercent);
        Assert.Equal(2, existingEntity.PenaltyDateEvidenceTypeId);
        Assert.Equal(new DateTime(2026, 6, 1), existingEntity.CompareDateTo);
        Assert.Equal("MANUAL_REVIEW", existingEntity.ElseAction);
        Assert.True(existingEntity.RequiresManualReview);
        Assert.Equal("Needs review", existingEntity.Remarks);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateRetrospectivePenaltyRuleDto
        {
            RuleId = 10,
            IsPenaltyApplicable = true,
            PenaltyMode = "ACT_UNLAWFUL",
            IsActive = true,
            UpdatedBy = 2
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectivePenaltyRuleEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectivePenaltyRuleEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        var idToDelete = 1;
        var existingEntity = new RetrospectivePenaltyRuleEntity
        {
            Id = 1,
            RuleId = 10,
            PenaltyMode = "NONE",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<RetrospectivePenaltyRuleEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectivePenaltyRuleEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectivePenaltyRuleEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectivePenaltyRuleEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
