using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleSummary;
using NtisPlatform.Application.Services.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.RetrospectiveTax;

public class RetrospectiveRuleSummaryServiceTests
{
    private readonly Mock<IRepository<RetrospectiveRuleSummaryEntity, int>> _mockRepository;
    private readonly Mock<IRepository<RetrospectiveRuleMasterEntity, int>> _mockRuleRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly RetrospectiveRuleSummaryService _service;

    public RetrospectiveRuleSummaryServiceTests()
    {
        _mockRepository = new Mock<IRepository<RetrospectiveRuleSummaryEntity, int>>();
        _mockRuleRepository = new Mock<IRepository<RetrospectiveRuleMasterEntity, int>>();
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

        _service = new RetrospectiveRuleSummaryService(
            _mockRepository.Object,
            _mockRuleRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new RetrospectiveRuleSummaryEntity
        {
            Id = 1,
            RuleId = 10,
            WhenSummary = "Electricity, Change Detection, Construction Year available; OC, CC unavailable",
            TaxSummary = "Start from Later of construction date or rolling cap; not before 01 Apr 2016; tax x 1.",
            PenaltySummary = "Do not apply penalty",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1,
            Rule = new RetrospectiveRuleMasterEntity { Id = 10, RuleCode = "R1", RuleName = "Rule 1" }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<RetrospectiveRuleSummaryDto>(It.IsAny<RetrospectiveRuleSummaryEntity>()))
            .Returns((RetrospectiveRuleSummaryEntity e) => new RetrospectiveRuleSummaryDto
            {
                Id = e.Id,
                RuleId = e.RuleId,
                WhenSummary = e.WhenSummary,
                TaxSummary = e.TaxSummary,
                PenaltySummary = e.PenaltySummary,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.RuleId);
        Assert.Equal("Do not apply penalty", result.PenaltySummary);
        Assert.True(result.IsActive);
        Assert.NotNull(entity.Rule);
        Assert.Equal("R1", entity.Rule!.RuleCode);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleSummaryEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<RetrospectiveRuleSummaryEntity>
        {
            new() { Id = 1, RuleId = 10, WhenSummary = "When-1", TaxSummary = "Tax-1", PenaltySummary = "Penalty-1", IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now },
            new() { Id = 2, RuleId = 11, WhenSummary = "When-2", TaxSummary = "Tax-2", PenaltySummary = "Penalty-2", IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<RetrospectiveRuleSummaryEntity, RetrospectiveRuleSummaryDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new RetrospectiveRuleSummaryService(
            _mockRepository.Object,
            _mockRuleRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new RetrospectiveRuleSummaryQueryParameters
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
        Assert.Contains(items, x => x.WhenSummary == "When-1");
        Assert.Contains(items, x => x.WhenSummary == "When-2");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateRetrospectiveRuleSummaryDto
        {
            RuleId = 10,
            WhenSummary = "When summary",
            TaxSummary = "Tax summary",
            PenaltySummary = "Penalty summary",
            CreatedBy = 1,
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<RetrospectiveRuleSummaryEntity>(It.IsAny<CreateRetrospectiveRuleSummaryDto>()))
            .Returns((CreateRetrospectiveRuleSummaryDto dto) => new RetrospectiveRuleSummaryEntity
            {
                Id = 1,
                RuleId = dto.RuleId,
                WhenSummary = dto.WhenSummary,
                TaxSummary = dto.TaxSummary,
                PenaltySummary = dto.PenaltySummary,
                IsActive = true,
                CreatedDate = DateTime.Now,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveRuleSummaryEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleSummaryEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<RetrospectiveRuleSummaryDto>(It.IsAny<RetrospectiveRuleSummaryEntity>()))
            .Returns((RetrospectiveRuleSummaryEntity e) => new RetrospectiveRuleSummaryDto
            {
                Id = e.Id,
                RuleId = e.RuleId,
                WhenSummary = e.WhenSummary,
                TaxSummary = e.TaxSummary,
                PenaltySummary = e.PenaltySummary,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.RuleId);
        Assert.Equal("When summary", result.WhenSummary);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveRuleSummaryEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateRetrospectiveRuleSummaryDto
        {
            RuleId = 10,
            WhenSummary = "When summary updated",
            TaxSummary = "Tax summary updated",
            PenaltySummary = "Penalty summary updated",
            IsActive = true
        };

        var existingEntity = new RetrospectiveRuleSummaryEntity
        {
            Id = 1,
            RuleId = 10,
            WhenSummary = "When summary",
            TaxSummary = "Tax summary",
            PenaltySummary = "Penalty summary",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleSummaryEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRetrospectiveRuleSummaryDto>(), It.IsAny<RetrospectiveRuleSummaryEntity>()))
            .Callback((UpdateRetrospectiveRuleSummaryDto src, RetrospectiveRuleSummaryEntity dest) =>
            {
                dest.RuleId = src.RuleId;
                dest.WhenSummary = src.WhenSummary;
                dest.TaxSummary = src.TaxSummary;
                dest.PenaltySummary = src.PenaltySummary;
                dest.IsActive = src.IsActive;
            });

        _mockMapper
            .Setup(m => m.Map<RetrospectiveRuleSummaryDto>(It.IsAny<RetrospectiveRuleSummaryEntity>()))
            .Returns((RetrospectiveRuleSummaryEntity e) => new RetrospectiveRuleSummaryDto
            {
                Id = e.Id,
                RuleId = e.RuleId,
                WhenSummary = e.WhenSummary,
                TaxSummary = e.TaxSummary,
                PenaltySummary = e.PenaltySummary,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleSummaryEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal("When summary updated", existingEntity.WhenSummary);
        Assert.Equal("Tax summary updated", existingEntity.TaxSummary);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateRetrospectiveRuleSummaryDto
        {
            RuleId = 10,
            WhenSummary = "When summary",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleSummaryEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleSummaryEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        var idToDelete = 1;
        var existingEntity = new RetrospectiveRuleSummaryEntity
        {
            Id = 1,
            RuleId = 10,
            WhenSummary = "When summary",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<RetrospectiveRuleSummaryEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectiveRuleSummaryEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleSummaryEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectiveRuleSummaryEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetForRuleAsync_RuleNotFound_ReturnsNull()
    {
        // Arrange
        _mockRuleRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleMasterEntity?)null);

        // Act
        var result = await _service.GetForRuleAsync(999, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRuleRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.GetQueryable(), Times.Never);
    }

    [Fact]
    public async Task GetForRuleAsync_RuleFoundButNoSummaryRow_ReturnsViewDtoWithNullSummaryFields()
    {
        // Arrange
        var rule = new RetrospectiveRuleMasterEntity
        {
            Id = 10,
            RuleCode = "THA-01",
            RuleName = "Thatched roof rule"
        };

        _mockRuleRepository
            .Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        // No summary rows at all for RuleId 10 (or any other rule).
        var entities = new List<RetrospectiveRuleSummaryEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        // Act
        var result = await _service.GetForRuleAsync(10, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.RuleId);
        Assert.Equal("THA-01", result.RuleCode);
        Assert.Null(result.WhenSummary);
        Assert.Null(result.TaxSummary);
        Assert.Null(result.PenaltySummary);
        Assert.Null(result.SummaryGeneratedDate);
    }

    [Fact]
    public async Task GetForRuleAsync_RuleFoundWithSummary_ReturnsPopulatedViewDto_MostRecentWins()
    {
        // Arrange
        var rule = new RetrospectiveRuleMasterEntity
        {
            Id = 10,
            RuleCode = "THA-01",
            RuleName = "Thatched roof rule"
        };

        _mockRuleRepository
            .Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var olderSummary = new RetrospectiveRuleSummaryEntity
        {
            Id = 1,
            RuleId = 10,
            WhenSummary = "Old when summary",
            TaxSummary = "Old tax summary",
            PenaltySummary = "Old penalty summary",
            IsActive = true,
            CreatedDate = new DateTime(2025, 1, 1)
        };

        var newerSummary = new RetrospectiveRuleSummaryEntity
        {
            Id = 2,
            RuleId = 10,
            WhenSummary = "Electricity, Change Detection, Construction Year available; OC, CC unavailable",
            TaxSummary = "Start from Later of construction date or rolling cap; not before 01 Apr 2016; tax x 1.",
            PenaltySummary = "Do not apply penalty",
            IsActive = true,
            CreatedDate = new DateTime(2026, 6, 1)
        };

        // A row for a different rule should never be picked up.
        var otherRuleSummary = new RetrospectiveRuleSummaryEntity
        {
            Id = 3,
            RuleId = 11,
            WhenSummary = "Other rule summary",
            IsActive = true,
            CreatedDate = new DateTime(2026, 7, 1)
        };

        var entities = new List<RetrospectiveRuleSummaryEntity> { olderSummary, newerSummary, otherRuleSummary };
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        // Act
        var result = await _service.GetForRuleAsync(10, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.RuleId);
        Assert.Equal("THA-01", result.RuleCode);
        Assert.Equal("Electricity, Change Detection, Construction Year available; OC, CC unavailable", result.WhenSummary);
        Assert.Equal("Start from Later of construction date or rolling cap; not before 01 Apr 2016; tax x 1.", result.TaxSummary);
        Assert.Equal("Do not apply penalty", result.PenaltySummary);
        Assert.Equal(newerSummary.CreatedDate, result.SummaryGeneratedDate);
    }
}
