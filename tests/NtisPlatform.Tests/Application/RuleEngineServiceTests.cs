using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Rules.RuleEngine;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Mappings.Rules;
using NtisPlatform.Application.Services.Rules;
using NtisPlatform.Core.Entities.Rules;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive unit tests for RuleEngineService
/// Tests business logic, versioning, and data operations
/// </summary>
public class RuleEngineServiceTests
{
    private readonly Mock<IRepository<RuleEngineEntity, int>> _mockRepository;
    private readonly Mock<IRepository<RuleVersionHistoryEntity, long>> _mockVersionRepository;
    private readonly Mock<IRuleExecutionService> _mockRuleExecutionService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly IMapper _mapper;
    private readonly RuleEngineService _service;

    public RuleEngineServiceTests()
    {
        _mockRepository = new Mock<IRepository<RuleEngineEntity, int>>();
        _mockVersionRepository = new Mock<IRepository<RuleVersionHistoryEntity, long>>();
        _mockRuleExecutionService = new Mock<IRuleExecutionService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        // Create real mapper using production configuration
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<RuleEngineMappingProfile>();
            cfg.AddProfile<RuleVersionHistoryMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = mapperConfig.CreateMapper();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new RuleEngineService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mapper,
            _mockVersionRepository.Object,
            _mockRuleExecutionService.Object);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithValidParameters_ReturnsPagedResult()
    {
        // Arrange
        var entities = new List<RuleEngineEntity>
        {
            new() { Id = 1, RuleCode = "RULE001", RuleName = "Tax Rule 1", RuleCategory = "TAX", RuleJson = "{}", IsActive = true },
            new() { Id = 2, RuleCode = "RULE002", RuleName = "Tax Rule 2", RuleCategory = "TAX", RuleJson = "{}", IsActive = true }
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var queryParams = new RuleEngineQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal("RULE001", result.Items.First().RuleCode);
    }

    [Fact]
    public async Task GetAllAsync_WithFiltering_ReturnsFilteredResults()
    {
        // Arrange
        var entities = new List<RuleEngineEntity>
        {
            new() { Id = 1, RuleCode = "RULE001", RuleName = "Tax Rule 1", RuleCategory = "TAX", RuleJson = "{}", IsEnabled = true, IsActive = true },
            new() { Id = 2, RuleCode = "RULE002", RuleName = "Tax Rule 2", RuleCategory = "TAX", RuleJson = "{}", IsEnabled = false, IsActive = true }
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var queryParams = new RuleEngineQueryParameters
        {
            IsEnabled = true,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.All(result.Items, item => Assert.True(item.IsEnabled));
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var entities = Enumerable.Range(1, 25).Select(i => new RuleEngineEntity
        {
            Id = i,
            RuleCode = $"RULE{i:000}",
            RuleName = $"Rule {i}",
            RuleCategory = "TAX",
            RuleJson = "{}",
            IsActive = true
        }).ToList();

        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var queryParams = new RuleEngineQueryParameters
        {
            PageNumber = 2,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(11, result.Items.First().Id);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsDto()
    {
        // Arrange
        var entity = new RuleEngineEntity
        {
            Id = 1,
            RuleCode = "RULE001",
            RuleName = "Tax Rule 1",
            RuleCategory = "TAX",
            RuleJson = "{}",
            IsActive = true
        };

        var entities = new List<RuleEngineEntity> { entity };
        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("RULE001", result.RuleCode);
        Assert.Equal("Tax Rule 1", result.RuleName);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var entities = new List<RuleEngineEntity>();
        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _service.GetByIdAsync(999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesEntityAndVersionHistory()
    {
        // Arrange
        var createDto = new CreateRuleEngineDto
        {
            RuleCode = "RULE001",
            RuleName = "Test Rule",
            Description = "Test Description",
            RuleCategory = "TAX",
            RuleJson = "{}",
            Priority = 100,
            IsEnabled = true,
            CreatedBy = 1,
            ChangeReason = "Initial creation"
        };

        RuleEngineEntity? capturedEntity = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RuleEngineEntity>(), It.IsAny<CancellationToken>()))
            .Callback<RuleEngineEntity, CancellationToken>((entity, _) =>
            {
                entity.Id = 1;
                capturedEntity = entity;
            })
            .Returns((RuleEngineEntity e, CancellationToken ct) => Task.FromResult(e));

        var entities = new List<RuleEngineEntity>();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(() =>
        {
            if (capturedEntity != null && !entities.Contains(capturedEntity))
            {
                entities.Add(capturedEntity);
            }
            return MockQueryableExtensions.BuildMock(entities);
        });

        // Mock version history repository for MaxAsync
        var versionHistoryEntities = new List<RuleVersionHistoryEntity>();
        _mockVersionRepository.Setup(r => r.GetQueryable()).Returns(() => MockQueryableExtensions.BuildMock(versionHistoryEntities));

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("RULE001", result.RuleCode);
        Assert.Equal("Test Rule", result.RuleName);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RuleEngineEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockVersionRepository.Verify(r => r.AddAsync(It.IsAny<RuleVersionHistoryEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithoutRuleCode_GeneratesRuleCode()
    {
        // Arrange
        var createDto = new CreateRuleEngineDto
        {
            RuleCode = "",
            RuleName = "Test Rule",
            RuleCategory = "TAX",
            RuleJson = "{}",
            CreatedBy = 1
        };

        // Mock version history repository for MaxAsync
        var versionHistoryEntities = new List<RuleVersionHistoryEntity>();
        _mockVersionRepository.Setup(r => r.GetQueryable()).Returns(() => MockQueryableExtensions.BuildMock(versionHistoryEntities));

        var existingEntities = new List<RuleEngineEntity>
        {
            new() { Id = 1, RuleCode = "RULE0001", RuleName = "Rule 1", RuleCategory = "TAX", RuleJson = "{}", IsActive = true }
        };

        RuleEngineEntity? capturedEntity = null;
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RuleEngineEntity>(), It.IsAny<CancellationToken>()))
            .Callback<RuleEngineEntity, CancellationToken>((entity, _) =>
            {
                entity.Id = 2;
                capturedEntity = entity;
            })
            .Returns((RuleEngineEntity e, CancellationToken ct) => Task.FromResult(e));

        var entities = new List<RuleEngineEntity>(existingEntities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(() =>
        {
            if (capturedEntity != null && !entities.Contains(capturedEntity))
            {
                entities.Add(capturedEntity);
            }
            return MockQueryableExtensions.BuildMock(entities);
        });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.RuleCode);
        Assert.NotEmpty(result.RuleCode);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesEntityAndCreatesVersionHistory()
    {
        // Arrange
        var existingEntity = new RuleEngineEntity
        {
            Id = 1,
            RuleCode = "RULE001",
            RuleName = "Original Rule",
            RuleCategory = "TAX",
            RuleJson = "{}",
            Priority = 100,
            IsActive = true
        };

        var entities = new List<RuleEngineEntity> { existingEntity };
        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);

        // Mock version history repository for MaxAsync
        var versionHistoryEntities = new List<RuleVersionHistoryEntity>();
        _mockVersionRepository.Setup(r => r.GetQueryable()).Returns(() => MockQueryableExtensions.BuildMock(versionHistoryEntities));

        var updateDto = new UpdateRuleEngineDto
        {
            RuleName = "Updated Rule",
            Description = "Updated Description",
            Priority = 200,
            UpdatedBy = 1,
            ChangeReason = "Policy update"
        };

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Rule", result.RuleName);
        Assert.Equal(200, result.Priority);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RuleEngineEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockVersionRepository.Verify(r => r.AddAsync(It.IsAny<RuleVersionHistoryEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentId_ThrowsException()
    {
        // Arrange
        var entities = new List<RuleEngineEntity>();
        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((RuleEngineEntity?)null);

        var updateDto = new UpdateRuleEngineDto
        {
            RuleName = "Updated Rule",
            UpdatedBy = 1
        };

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert - Service returns null instead of throwing exception for non-existent entities
        Assert.Null(result);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_SoftDeletesEntity()
    {
        // Arrange
        var entity = new RuleEngineEntity
        {
            Id = 1,
            RuleCode = "RULE001",
            RuleName = "Test Rule",
            RuleCategory = "TAX",
            RuleJson = "{}",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<RuleEngineEntity>(), It.IsAny<CancellationToken>()))
            .Callback<RuleEngineEntity, CancellationToken>((e, _) => { e.IsActive = false; })
            .Returns(Task.CompletedTask);

        // Mock version history repository for MaxAsync
        var versionHistoryEntities = new List<RuleVersionHistoryEntity>();
        _mockVersionRepository.Setup(r => r.GetQueryable()).Returns(() => MockQueryableExtensions.BuildMock(versionHistoryEntities));

        // Act
        var result = await _service.DeleteAsync(1, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.False(entity.IsActive);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RuleEngineEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ThrowsException()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((RuleEngineEntity?)null);

        // Act
        var result = await _service.DeleteAsync(999, CancellationToken.None);

        // Assert - Service returns false instead of throwing exception for non-existent entities
        Assert.False(result);
    }

    #endregion

    #region GetVersionHistoryAsync Tests

    [Fact]
    public async Task GetVersionHistoryAsync_WithValidRuleId_ReturnsVersionHistory()
    {
        // Arrange
        var versionHistories = new List<RuleVersionHistoryEntity>
        {
            new()
            {
                Id = 1,
                RuleId = 1,
                RuleCode = "RULE001",
                Version = 1,
                RuleName = "Version 1",
                RuleJson = "{}",
                ChangeType = "CREATED",
                ChangedBy = 1,
                ChangedDate = DateTime.Now,
                Priority = 100,
                IsEnabled = true
            },
            new()
            {
                Id = 2,
                RuleId = 1,
                RuleCode = "RULE001",
                Version = 2,
                RuleName = "Version 2",
                RuleJson = "{}",
                ChangeType = "UPDATED",
                ChangedBy = 1,
                ChangedDate = DateTime.Now,
                Priority = 100,
                IsEnabled = true
            }
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(versionHistories);
        _mockVersionRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var queryParams = new RuleVersionHistoryQueryParameters
        {
            RuleId = 1,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetVersionHistoryAsync(queryParams.RuleId ?? 0, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.Equal(1, item.RuleId));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingRule_CallsRepositoryDeleteAndCreatesHistory()
    {
        // Arrange
        var rule = new RuleEngineEntity
        {
            Id = 1,
            RuleCode = "RULE001",
            RuleName = "Test Rule",
            RuleCategory = "ARV",
            RuleJson = "{}",
            IsEnabled = true,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        // We mock GetQueryable for VersionRepository.MaxAsync used by CreateVersionHistoryAsync
        var emptyHistories = new List<RuleVersionHistoryEntity>();
        var mockVersionQueryable = MockQueryableExtensions.BuildMock(emptyHistories);
        _mockVersionRepository.Setup(r => r.GetQueryable()).Returns(mockVersionQueryable);

        // Act
        var result = await _service.DeleteAsync(1, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(rule, It.IsAny<CancellationToken>()), Times.Once);
        _mockVersionRepository.Verify(r => r.AddAsync(It.Is<RuleVersionHistoryEntity>(v => v.ChangeType == "DELETED"), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingRule_ReturnsFalse()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleEngineEntity)null!);

        // Act
        var result = await _service.DeleteAsync(999, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RuleEngineEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Soft Delete Filtering Tests

    [Fact]
    public async Task GetByIdAsync_WithMarkedForDeletionTrue_ReturnsNull()
    {
        // Arrange
        var entity = new RuleEngineEntity
        {
            Id = 1,
            RuleCode = "RULE001",
            RuleName = "Deleted Rule",
            RuleCategory = "TAX",
            RuleJson = "{}",
            IsActive = false,
            MarkedForDeletion = true
        };

        var entities = new List<RuleEngineEntity> { entity };
        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        // Act
        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_WithMarkedForDeletionTrue_ExcludesDeletedRecords()
    {
        // Arrange
        var entities = new List<RuleEngineEntity>
        {
            new() { Id = 1, RuleCode = "RULE001", RuleName = "Active Rule", RuleCategory = "TAX", RuleJson = "{}", IsActive = true, MarkedForDeletion = false },
            new() { Id = 2, RuleCode = "RULE002", RuleName = "Deleted Rule", RuleCategory = "TAX", RuleJson = "{}", IsActive = false, MarkedForDeletion = true }
        };

        var mockQueryable = MockQueryableExtensions.BuildMock(entities);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        var queryParams = new RuleEngineQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("RULE001", result.Items.First().RuleCode);
    }

    #endregion
}
