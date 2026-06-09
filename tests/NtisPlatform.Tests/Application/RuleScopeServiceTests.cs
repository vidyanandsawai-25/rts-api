using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Master.RuleScopeMaster;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Rules;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive unit tests for RuleScopeService
/// Tests business logic, data operations, and service layer functionality
/// </summary>
public class RuleScopeServiceTests
{
    private readonly Mock<IRepository<RuleScopeEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly RuleScopeService _service;

    public RuleScopeServiceTests()
    {
        _mockRepository = new Mock<IRepository<RuleScopeEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new RuleScopeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    /// <summary>
    /// Creates a real IMapper using the production RuleScopeMappingProfile.
    /// This ensures tests validate the actual mapping configuration.
    /// </summary>
    private static IMapper CreateRealMapper()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<RuleScopeMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        // Assert configuration is valid - catches mapping errors at test time
        mapperConfig.AssertConfigurationIsValid();
        return mapperConfig.CreateMapper();
    }

    #region Mapping Profile Tests

    [Fact]
    public void RuleScopeMappingProfile_ConfigurationIsValid()
    {
        // Arrange & Act
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<RuleScopeMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        // Assert - This will throw if configuration is invalid
        mapperConfig.AssertConfigurationIsValid();
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new RuleScopeEntity
        {
            Id = 1,
            RuleScope = "Tax Rules",
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now,
            UpdatedBy = 1,
            UpdatedDate = DateTime.Now
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<RuleScopeDto>(It.IsAny<RuleScopeEntity>()))
            .Returns(new RuleScopeDto
            {
                Id = 1,
                RuleScope = "Tax Rules",
                IsActive = true,
                CreatedDate = entity.CreatedDate,
                UpdatedDate = entity.UpdatedDate
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Tax Rules", result.RuleScope);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<RuleScopeDto>(entity), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleScopeEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<RuleScopeDto>(It.IsAny<RuleScopeEntity>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetByIdAsync_InvalidId_ReturnsNull(int invalidId)
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleScopeEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(invalidId);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<RuleScopeEntity>
        {
            new() { Id = 1, RuleScope = "Tax Rules", IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now },
            new() { Id = 2, RuleScope = "Discount Rules", IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        // Use real mapper with production profile
        var mapper = CreateRealMapper();

        var service = new RuleScopeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new RuleScopeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);

        Assert.Contains(items, x => x.RuleScope == "Tax Rules");
        Assert.Contains(items, x => x.RuleScope == "Discount Rules");
        Assert.Contains(items, x => x.Id == 1);
        Assert.Contains(items, x => x.Id == 2);
    }

    [Fact]
    public async Task GetAllAsync_WithRuleScopeFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<RuleScopeEntity>
        {
            new() { Id = 1, RuleScope = "Tax Rules", IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now },
            new() { Id = 2, RuleScope = "Discount Rules", IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now },
            new() { Id = 3, RuleScope = "Tax Calculation", IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        // Use real mapper with production profile
        var mapper = CreateRealMapper();

        var service = new RuleScopeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new RuleScopeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            RuleScope = "Tax",
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        var items = result.Items.ToList();
        Assert.All(items, item => Assert.Contains("Tax", item.RuleScope));
    }

    [Fact]
    public async Task GetAllAsync_WithIsActiveFilter_ReturnsOnlyActiveEntities()
    {
        // Arrange
        var entities = new List<RuleScopeEntity>
        {
            new() { Id = 1, RuleScope = "Active Scope", IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now },
            new() { Id = 2, RuleScope = "Inactive Scope", IsActive = false, CreatedBy = 1, CreatedDate = DateTime.Now },
            new() { Id = 3, RuleScope = "Another Active", IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        // Use real mapper with production profile
        var mapper = CreateRealMapper();

        var service = new RuleScopeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new RuleScopeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            IsActive = true,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.True(item.IsActive));
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var entities = new List<RuleScopeEntity>();
        for (int i = 1; i <= 15; i++)
        {
            entities.Add(new RuleScopeEntity
            {
                Id = i,
                RuleScope = $"Scope {i}",
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now
            });
        }

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        // Use real mapper with production profile
        var mapper = CreateRealMapper();

        var service = new RuleScopeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new RuleScopeQueryParameters
        {
            PageNumber = 2,
            PageSize = 5,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(5, result.Items.Count());
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var entities = new List<RuleScopeEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        // Use real mapper with production profile
        var mapper = CreateRealMapper();

        var service = new RuleScopeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new RuleScopeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }



    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateRuleScopeDto
        {
            RuleScope = "New Tax Scope",
            IsActive = true,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<RuleScopeEntity>(It.IsAny<CreateRuleScopeDto>()))
            .Returns((CreateRuleScopeDto dto) => new RuleScopeEntity
            {
                RuleScope = dto.RuleScope,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy,
                CreatedDate = DateTime.Now
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RuleScopeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleScopeEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                e.CreatedDate = DateTime.Now;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<RuleScopeDto>(It.IsAny<RuleScopeEntity>()))
            .Returns((RuleScopeEntity e) => new RuleScopeDto
            {
                Id = e.Id,
                RuleScope = e.RuleScope,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("New Tax Scope", result.RuleScope);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(
            It.Is<RuleScopeEntity>(e => e.RuleScope == "New Tax Scope" && e.IsActive),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithInactiveStatus_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateRuleScopeDto
        {
            RuleScope = "Inactive Scope",
            IsActive = false,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<RuleScopeEntity>(It.IsAny<CreateRuleScopeDto>()))
            .Returns((CreateRuleScopeDto dto) => new RuleScopeEntity
            {
                RuleScope = dto.RuleScope,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RuleScopeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleScopeEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<RuleScopeDto>(It.IsAny<RuleScopeEntity>()))
            .Returns((RuleScopeEntity e) => new RuleScopeDto
            {
                Id = e.Id,
                RuleScope = e.RuleScope,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        Assert.Equal("Inactive Scope", result.RuleScope);
    }

    [Fact]
    public async Task CreateAsync_WithMinimalData_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateRuleScopeDto
        {
            RuleScope = "Minimal Scope",
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<RuleScopeEntity>(It.IsAny<CreateRuleScopeDto>()))
            .Returns((CreateRuleScopeDto dto) => new RuleScopeEntity
            {
                RuleScope = dto.RuleScope,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RuleScopeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleScopeEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<RuleScopeDto>(It.IsAny<RuleScopeEntity>()))
            .Returns((RuleScopeEntity e) => new RuleScopeDto
            {
                Id = e.Id,
                RuleScope = e.RuleScope
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Minimal Scope", result.RuleScope);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateRuleScopeDto
        {
            RuleScope = "Updated Scope",
            IsActive = true,
            UpdatedBy = 1
        };

        var existingEntity = new RuleScopeEntity
        {
            Id = 1,
            RuleScope = "Original Scope",
            IsActive = false,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RuleScopeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRuleScopeDto>(), It.IsAny<RuleScopeEntity>()))
            .Callback((UpdateRuleScopeDto src, RuleScopeEntity dest) =>
            {
                dest.RuleScope = src.RuleScope;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
                dest.UpdatedDate = DateTime.Now;
            });

        _mockMapper
            .Setup(m => m.Map<RuleScopeDto>(It.IsAny<RuleScopeEntity>()))
            .Returns((RuleScopeEntity e) => new RuleScopeDto
            {
                Id = e.Id,
                RuleScope = e.RuleScope,
                IsActive = e.IsActive,
                UpdatedDate = e.UpdatedDate
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Scope", existingEntity.RuleScope);
        Assert.True(existingEntity.IsActive);
        Assert.Equal(1, existingEntity.UpdatedBy);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RuleScopeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateRuleScopeDto
        {
            RuleScope = "Updated Scope",
            UpdatedBy = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleScopeEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RuleScopeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateRuleScope_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateRuleScopeDto
        {
            RuleScope = "Test Scope",
            IsActive = false,
            UpdatedBy = 1
        };

        var existingEntity = new RuleScopeEntity
        {
            Id = 1,
            RuleScope = "Test Scope",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RuleScopeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRuleScopeDto>(), It.IsAny<RuleScopeEntity>()))
            .Callback((UpdateRuleScopeDto src, RuleScopeEntity dest) =>
            {
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
            });

        _mockMapper
            .Setup(m => m.Map<RuleScopeDto>(It.IsAny<RuleScopeEntity>()))
            .Returns((RuleScopeEntity e) => new RuleScopeDto
            {
                Id = e.Id,
                RuleScope = e.RuleScope,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(existingEntity.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_OnlyChangesRuleScope_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateRuleScopeDto
        {
            RuleScope = "Changed Scope Name",
            IsActive = true,
            UpdatedBy = 1
        };

        var existingEntity = new RuleScopeEntity
        {
            Id = 1,
            RuleScope = "Original Name",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RuleScopeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRuleScopeDto>(), It.IsAny<RuleScopeEntity>()))
            .Callback((UpdateRuleScopeDto src, RuleScopeEntity dest) =>
            {
                dest.RuleScope = src.RuleScope;
                dest.UpdatedBy = src.UpdatedBy;
            });

        _mockMapper
            .Setup(m => m.Map<RuleScopeDto>(It.IsAny<RuleScopeEntity>()))
            .Returns((RuleScopeEntity e) => new RuleScopeDto
            {
                Id = e.Id,
                RuleScope = e.RuleScope,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Changed Scope Name", existingEntity.RuleScope);
        Assert.True(existingEntity.IsActive);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSavesSuccessfully()
    {
        // Arrange
        var existingEntity = new RuleScopeEntity
        {
            Id = 1,
            RuleScope = "To Delete",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<RuleScopeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(1, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleScopeEntity?)null);

        // Act
        var result = await _service.DeleteAsync(999, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RuleScopeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task DeleteAsync_InvalidId_ReturnsFalse(int invalidId)
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleScopeEntity?)null);

        // Act
        var result = await _service.DeleteAsync(invalidId, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RuleScopeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Cases and Additional Tests

    [Fact]
    public async Task GetAllAsync_WithComplexFiltering_ReturnsCorrectResults()
    {
        // Arrange
        var entities = new List<RuleScopeEntity>
        {
            new() { Id = 1, RuleScope = "Tax Rules Active", IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now },
            new() { Id = 2, RuleScope = "Tax Rules Inactive", IsActive = false, CreatedBy = 1, CreatedDate = DateTime.Now },
            new() { Id = 3, RuleScope = "Discount Rules", IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        // Use real mapper with production profile
        var mapper = CreateRealMapper();

        var service = new RuleScopeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new RuleScopeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            RuleScope = "Tax",
            IsActive = true,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Contains("Tax", result.Items.First().RuleScope);
        Assert.True(result.Items.First().IsActive);
    }

    [Fact]
    public async Task CreateAsync_CallsRepositoryWithCorrectEntity()
    {
        // Arrange
        var createDto = new CreateRuleScopeDto
        {
            RuleScope = "Test Scope",
            IsActive = true,
            CreatedBy = 5
        };

        RuleScopeEntity? capturedEntity = null;

        _mockMapper
            .Setup(m => m.Map<RuleScopeEntity>(It.IsAny<CreateRuleScopeDto>()))
            .Returns((CreateRuleScopeDto dto) => new RuleScopeEntity
            {
                RuleScope = dto.RuleScope,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RuleScopeEntity>(), It.IsAny<CancellationToken>()))
            .Callback<RuleScopeEntity, CancellationToken>((e, ct) => capturedEntity = e)
            .ReturnsAsync((RuleScopeEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<RuleScopeDto>(It.IsAny<RuleScopeEntity>()))
            .Returns((RuleScopeEntity e) => new RuleScopeDto { Id = e.Id, RuleScope = e.RuleScope });

        // Act
        await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedEntity);
        Assert.Equal("Test Scope", capturedEntity.RuleScope);
        Assert.True(capturedEntity.IsActive);
        Assert.Equal(5, capturedEntity.CreatedBy);
    }

    [Fact]
    public async Task UpdateAsync_CallsMapperWithCorrectParameters()
    {
        // Arrange
        var updateDto = new UpdateRuleScopeDto
        {
            RuleScope = "Updated",
            UpdatedBy = 3
        };

        var existingEntity = new RuleScopeEntity
        {
            Id = 1,
            RuleScope = "Original",
            CreatedBy = 1,
            CreatedDate = DateTime.Now
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RuleScopeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        UpdateRuleScopeDto? capturedDto = null;
        RuleScopeEntity? capturedEntity = null;

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRuleScopeDto>(), It.IsAny<RuleScopeEntity>()))
            .Callback<UpdateRuleScopeDto, RuleScopeEntity>((dto, entity) =>
            {
                capturedDto = dto;
                capturedEntity = entity;
                entity.RuleScope = dto.RuleScope;
                entity.UpdatedBy = dto.UpdatedBy;
            });

        _mockMapper
            .Setup(m => m.Map<RuleScopeDto>(It.IsAny<RuleScopeEntity>()))
            .Returns((RuleScopeEntity e) => new RuleScopeDto { Id = e.Id });

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedDto);
        Assert.NotNull(capturedEntity);
        Assert.Equal(updateDto, capturedDto);
        Assert.Equal(existingEntity, capturedEntity);
    }

    [Fact]
    public async Task GetAllAsync_VerifiesRepositoryGetQueryableIsCalled()
    {
        // Arrange
        var entities = new List<RuleScopeEntity>
        {
            new() { Id = 1, RuleScope = "Test", IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        // Use real mapper with production profile
        var mapper = CreateRealMapper();

        var service = new RuleScopeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new RuleScopeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
        };

        // Act
        await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetQueryable(), Times.Once);
    }

    #endregion
}
