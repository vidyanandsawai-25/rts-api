using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class PolicyConfigurationServiceTests
{
    private readonly Mock<IRepository<PolicyConfigurationEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork>  _mockUnitOfWork;
    private readonly Mock<IMapper>      _mockMapper;
    private readonly Mock<ILogger<PolicyConfigurationService>> _mockLogger;
    private readonly PolicyConfigurationService _service;

    public PolicyConfigurationServiceTests()
    {
        _mockRepository = new Mock<IRepository<PolicyConfigurationEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper     = new Mock<IMapper>();
        _mockLogger     = new Mock<ILogger<PolicyConfigurationService>>();

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _service = new PolicyConfigurationService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }

    // ──────────────────────── Helpers ────────────────────────────────────────

    private static IMapper RealMapper() =>
        new MapperConfiguration(cfg =>
            cfg.CreateMap<PolicyConfigurationEntity, PolicyConfigurationDto>(),
            NullLoggerFactory.Instance)
        .CreateMapper();

    private static ILogger<PolicyConfigurationService> NullLogger() =>
        NullLoggerFactory.Instance.CreateLogger<PolicyConfigurationService>();

    private static PolicyConfigurationEntity SampleEntity(int id = 1) => new()
    {
        Id           = id,
        PolicyCode   = $"POL-{id:000}",
        Category     = "Tax",
        DisplayName  = $"Policy {id}",
        DataType     = "BIT",
        PolicyValue  = "1",
        DefaultValue = "0",
        Unit         = "INR",
        IsActive     = true,
        CreatedBy    = 1,
        CreatedDate  = DateTime.Now
    };

    // ──────────────────────── GetByIdAsync ────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = SampleEntity(1);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<PolicyConfigurationDto>(entity))
                   .Returns(new PolicyConfigurationDto
                   {
                       Id          = 1,
                       PolicyCode  = "POL-001",
                       Category    = "Tax",
                       DisplayName = "Policy 1",
                       DataType    = "BIT",
                       PolicyValue = "1",
                       IsActive    = true
                   });

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
        Assert.Equal("POL-001", result.PolicyCode);
        Assert.Equal("Tax", result.Category);
        Assert.Equal("BIT", result.DataType);
        Assert.Equal("1", result.PolicyValue);
        _mockMapper.Verify(m => m.Map<PolicyConfigurationDto>(entity), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((PolicyConfigurationEntity?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
        _mockMapper.Verify(m => m.Map<PolicyConfigurationDto>(It.IsAny<PolicyConfigurationEntity>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByIdAsync_InvalidId_ReturnsNull(int invalidId)
    {
        _mockRepository.Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((PolicyConfigurationEntity?)null);

        var result = await _service.GetByIdAsync(invalidId);

        Assert.Null(result);
    }

    // ──────────────────────── GetAllAsync ─────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        var entities = new List<PolicyConfigurationEntity>
        {
            new() { Id = 1, PolicyCode = "POL-001", Category = "Tax",     DisplayName = "Tax Policy",     DataType = "BIT",     IsActive = true },
            new() { Id = 2, PolicyCode = "POL-002", Category = "Finance", DisplayName = "Finance Policy", DataType = "DECIMAL", IsActive = true },
            new() { Id = 3, PolicyCode = "POL-003", Category = "Tax",     DisplayName = "Tax Policy 2",   DataType = "INT",     IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());
        var service = new PolicyConfigurationService(_mockRepository.Object, _mockUnitOfWork.Object, RealMapper(), NullLogger());

        var result = await service.GetAllAsync(new PolicyConfigurationQueryParameters { PageNumber = 1, PageSize = 10 });

        Assert.Equal(3, result.TotalCount);
        Assert.Contains(result.Items, x => x.PolicyCode == "POL-001");
        Assert.Contains(result.Items, x => x.PolicyCode == "POL-002");
    }

    [Fact]
    public async Task GetAllAsync_FilterByCategory_ReturnsMatchingItems()
    {
        var entities = new List<PolicyConfigurationEntity>
        {
            new() { Id = 1, PolicyCode = "POL-001", Category = "Tax",     DisplayName = "T1", DataType = "BIT",     IsActive = true },
            new() { Id = 2, PolicyCode = "POL-002", Category = "Finance", DisplayName = "F1", DataType = "DECIMAL", IsActive = true },
            new() { Id = 3, PolicyCode = "POL-003", Category = "Tax",     DisplayName = "T2", DataType = "INT",     IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());
        var service = new PolicyConfigurationService(_mockRepository.Object, _mockUnitOfWork.Object, RealMapper(), NullLogger());

        var result = await service.GetAllAsync(new PolicyConfigurationQueryParameters { PageNumber = 1, PageSize = 10, Category = "Tax" });

        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item => Assert.Contains("Tax", item.Category, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAllAsync_FilterByPolicyCode_ReturnsMatchingItems()
    {
        var entities = new List<PolicyConfigurationEntity>
        {
            new() { Id = 1, PolicyCode = "POL-001", Category = "Tax", DisplayName = "P1", DataType = "BIT",     IsActive = true },
            new() { Id = 2, PolicyCode = "POL-002", Category = "Tax", DisplayName = "P2", DataType = "DECIMAL", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());
        var service = new PolicyConfigurationService(_mockRepository.Object, _mockUnitOfWork.Object, RealMapper(), NullLogger());

        var result = await service.GetAllAsync(new PolicyConfigurationQueryParameters { PageNumber = 1, PageSize = 10, PolicyCode = "POL-001" });

        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item => Assert.Contains("POL-001", item.PolicyCode, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAllAsync_FilterByDataType_ReturnsMatchingItems()
    {
        var entities = new List<PolicyConfigurationEntity>
        {
            new() { Id = 1, PolicyCode = "POL-001", Category = "Tax", DisplayName = "P1", DataType = "BIT",     IsActive = true },
            new() { Id = 2, PolicyCode = "POL-002", Category = "Tax", DisplayName = "P2", DataType = "DECIMAL", IsActive = true },
            new() { Id = 3, PolicyCode = "POL-003", Category = "Tax", DisplayName = "P3", DataType = "BIT",     IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());
        var service = new PolicyConfigurationService(_mockRepository.Object, _mockUnitOfWork.Object, RealMapper(), NullLogger());

        var result = await service.GetAllAsync(new PolicyConfigurationQueryParameters { PageNumber = 1, PageSize = 10, DataType = "BIT" });

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal("BIT", item.DataType, ignoreCase: true));
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmpty()
    {
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<PolicyConfigurationEntity>().BuildMock());
        var service = new PolicyConfigurationService(_mockRepository.Object, _mockUnitOfWork.Object, RealMapper(), NullLogger());

        var result = await service.GetAllAsync(new PolicyConfigurationQueryParameters { PageNumber = 1, PageSize = 10 });

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllAsync_Pagination_ReturnsCorrectPage()
    {
        var entities = Enumerable.Range(1, 25)
            .Select(i => new PolicyConfigurationEntity
            {
                Id          = i,
                PolicyCode  = $"POL-{i:000}",
                Category    = "Tax",
                DisplayName = $"Policy {i}",
                DataType    = "BIT",
                IsActive    = true
            }).ToList();

        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());
        var service = new PolicyConfigurationService(_mockRepository.Object, _mockUnitOfWork.Object, RealMapper(), NullLogger());

        var result = await service.GetAllAsync(new PolicyConfigurationQueryParameters { PageNumber = 2, PageSize = 10 });

        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
    }

    [Fact]
    public async Task GetAllAsync_SearchTerm_ReturnsMatchingEntities()
    {
        var entities = new List<PolicyConfigurationEntity>
        {
            new() { Id = 1, PolicyCode = "POL-001", Category = "Tax",     DisplayName = "Tax Calculation", DataType = "DECIMAL", IsActive = true },
            new() { Id = 2, PolicyCode = "POL-002", Category = "Penalty", DisplayName = "Penalty Rate",    DataType = "INT",     IsActive = true },
            new() { Id = 3, PolicyCode = "POL-003", Category = "Discount",DisplayName = "Discount Rate",   DataType = "DECIMAL", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());
        var service = new PolicyConfigurationService(_mockRepository.Object, _mockUnitOfWork.Object, RealMapper(), NullLogger());

        var result = await service.GetAllAsync(
            new PolicyConfigurationQueryParameters { PageNumber = 1, PageSize = 10, SearchTerm = "Penalty" });

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.All(result.Items, item =>
            Assert.True(
                item.Category.Contains("Penalty", StringComparison.OrdinalIgnoreCase) ||
                item.DisplayName.Contains("Penalty", StringComparison.OrdinalIgnoreCase) ||
                item.PolicyCode.Contains("Penalty", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task GetAllAsync_SortByCategory_ReturnsSortedResults()
    {
        var entities = new List<PolicyConfigurationEntity>
        {
            new() { Id = 1, PolicyCode = "P1", Category = "Tax",     DisplayName = "T", DataType = "BIT", IsActive = true },
            new() { Id = 2, PolicyCode = "P2", Category = "Discount",DisplayName = "D", DataType = "BIT", IsActive = true },
            new() { Id = 3, PolicyCode = "P3", Category = "Penalty", DisplayName = "P", DataType = "BIT", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());
        var service = new PolicyConfigurationService(_mockRepository.Object, _mockUnitOfWork.Object, RealMapper(), NullLogger());

        var result = await service.GetAllAsync(
            new PolicyConfigurationQueryParameters { PageNumber = 1, PageSize = 10, SortBy = "Category", SortOrder = "asc" });

        var categories = result.Items.Select(x => x.Category).ToList();
        Assert.Equal(categories.OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList(), categories);
    }

    // ──────────────────────── CreateAsync ─────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var createDto = new CreatePolicyConfigurationDto
        {
            PolicyCode   = "POL-NEW",
            Category     = "Tax",
            DisplayName  = "New Policy",
            DataType     = "BIT",
            PolicyValue  = "1",
            DefaultValue = "0",
            Unit         = "INR",
            CreatedBy    = 1,
            IsActive     = true
        };

        _mockMapper.Setup(m => m.Map<PolicyConfigurationEntity>(createDto))
                   .Returns(new PolicyConfigurationEntity
                   {
                       PolicyCode  = "POL-NEW",
                       Category    = "Tax",
                       DisplayName = "New Policy",
                       DataType    = "BIT",
                       PolicyValue = "1",
                       Unit        = "INR",
                       IsActive    = true,
                       CreatedBy   = 1
                   });

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<PolicyConfigurationEntity>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync((PolicyConfigurationEntity e, CancellationToken _) => { e.Id = 10; return e; });

        _mockMapper.Setup(m => m.Map<PolicyConfigurationDto>(It.IsAny<PolicyConfigurationEntity>()))
                   .Returns(new PolicyConfigurationDto
                   {
                       Id          = 10,
                       PolicyCode  = "POL-NEW",
                       Category    = "Tax",
                       DisplayName = "New Policy",
                       DataType    = "BIT",
                       PolicyValue = "1",
                       Unit        = "INR",
                       IsActive    = true
                   });

        var result = await _service.CreateAsync(createDto);

        Assert.NotNull(result);
        Assert.Equal(10, result!.Id);
        Assert.Equal("POL-NEW", result.PolicyCode);
        Assert.Equal("1", result.PolicyValue);
        Assert.Equal("INR", result.Unit);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithEffectiveDates_MapsCorrectly()
    {
        var from = new DateTime(2025, 1, 1);
        var to   = new DateTime(2025, 12, 31);
        var createDto = new CreatePolicyConfigurationDto
        {
            PolicyCode    = "POL-DATE",
            Category      = "Tax",
            DisplayName   = "Date Policy",
            DataType      = "DATE",
            EffectiveFrom = from,
            EffectiveTo   = to,
            IsActive      = true,
            CreatedBy     = 1
        };

        _mockMapper.Setup(m => m.Map<PolicyConfigurationEntity>(It.IsAny<CreatePolicyConfigurationDto>()))
                   .Returns(new PolicyConfigurationEntity { EffectiveFrom = from, EffectiveTo = to });

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<PolicyConfigurationEntity>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync((PolicyConfigurationEntity e, CancellationToken _) => { e.Id = 1; return e; });

        _mockMapper.Setup(m => m.Map<PolicyConfigurationDto>(It.IsAny<PolicyConfigurationEntity>()))
                   .Returns(new PolicyConfigurationDto { Id = 1, EffectiveFrom = from, EffectiveTo = to });

        var result = await _service.CreateAsync(createDto);

        Assert.NotNull(result);
        Assert.Equal(from, result!.EffectiveFrom);
        Assert.Equal(to,   result.EffectiveTo);
    }

    [Fact]
    public async Task CreateAsync_InactivePolicy_CreatesSuccessfully()
    {
        var createDto = new CreatePolicyConfigurationDto
        {
            PolicyCode  = "POL-INV",
            Category    = "Tax",
            DisplayName = "Inactive Policy",
            DataType    = "BIT",
            IsActive    = false,
            CreatedBy   = 1
        };

        _mockMapper.Setup(m => m.Map<PolicyConfigurationEntity>(It.IsAny<CreatePolicyConfigurationDto>()))
                   .Returns(new PolicyConfigurationEntity { IsActive = false });

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<PolicyConfigurationEntity>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync((PolicyConfigurationEntity e, CancellationToken _) => { e.Id = 2; return e; });

        _mockMapper.Setup(m => m.Map<PolicyConfigurationDto>(It.IsAny<PolicyConfigurationEntity>()))
                   .Returns(new PolicyConfigurationDto { Id = 2, IsActive = false });

        var result = await _service.CreateAsync(createDto);

        Assert.NotNull(result);
        Assert.False(result!.IsActive);
    }

    // ──────────────────────── UpdateAsync ─────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var existing = SampleEntity(1);
        var updateDto = new UpdatePolicyConfigurationDto
        {
            PolicyCode   = "POL-001",
            Category     = "Tax",
            DisplayName  = "Updated Policy",
            DataType     = "INT",
            PolicyValue  = "42",
            DefaultValue = "0",
            Unit         = "INR",
            IsActive     = true,
            UpdatedBy    = 2
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<PolicyConfigurationEntity>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        _mockMapper.Setup(m => m.Map(It.IsAny<UpdatePolicyConfigurationDto>(), It.IsAny<PolicyConfigurationEntity>()))
                   .Callback((UpdatePolicyConfigurationDto src, PolicyConfigurationEntity dest) =>
                   {
                       dest.DisplayName  = src.DisplayName;
                       dest.PolicyValue  = src.PolicyValue;
                       dest.Unit         = src.Unit;
                       dest.UpdatedBy    = src.UpdatedBy;
                       dest.UpdatedDate  = DateTime.Now;
                   });

        _mockMapper.Setup(m => m.Map<PolicyConfigurationDto>(It.IsAny<PolicyConfigurationEntity>()))
                   .Returns((PolicyConfigurationEntity e) => new PolicyConfigurationDto
                   {
                       Id          = e.Id,
                       DisplayName = e.DisplayName,
                       PolicyValue = e.PolicyValue,
                       Unit        = e.Unit,
                       UpdatedDate = e.UpdatedDate
                   });

        var result = await _service.UpdateAsync(1, updateDto);

        Assert.NotNull(result);
        Assert.Equal("Updated Policy", result!.DisplayName);
        Assert.Equal("42", result.PolicyValue);
        Assert.Equal("INR", result.Unit);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((PolicyConfigurationEntity?)null);

        var result = await _service.UpdateAsync(999, new UpdatePolicyConfigurationDto
        {
            PolicyCode = "X", Category = "X", DisplayName = "X", DataType = "BIT"
        });

        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PolicyConfigurationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DeactivatePolicy_UpdatesIsActive()
    {
        var existing = SampleEntity(1);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<PolicyConfigurationEntity>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        _mockMapper.Setup(m => m.Map(It.IsAny<UpdatePolicyConfigurationDto>(), It.IsAny<PolicyConfigurationEntity>()))
                   .Callback((UpdatePolicyConfigurationDto src, PolicyConfigurationEntity dest) =>
                       dest.IsActive = src.IsActive);

        _mockMapper.Setup(m => m.Map<PolicyConfigurationDto>(It.IsAny<PolicyConfigurationEntity>()))
                   .Returns((PolicyConfigurationEntity e) => new PolicyConfigurationDto { IsActive = e.IsActive });

        var result = await _service.UpdateAsync(1, new UpdatePolicyConfigurationDto
        {
            PolicyCode = "POL-001", Category = "Tax", DisplayName = "P", DataType = "BIT", IsActive = false
        });

        Assert.NotNull(result);
        Assert.False(result!.IsActive);
    }

    // ──────────────────────── DeleteAsync ─────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingEntity_ReturnsTrueAndSaves()
    {
        var existing = SampleEntity(1);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PolicyConfigurationEntity>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(1);

        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PolicyConfigurationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((PolicyConfigurationEntity?)null);

        var result = await _service.DeleteAsync(999);

        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PolicyConfigurationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ──────────────────────── Transaction / Mapper ─────────────────────────────

    [Fact]
    public async Task CreateAsync_UsesNoExplicitTransaction()
    {
        _mockMapper.Setup(m => m.Map<PolicyConfigurationEntity>(It.IsAny<CreatePolicyConfigurationDto>()))
                   .Returns(new PolicyConfigurationEntity());
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<PolicyConfigurationEntity>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new PolicyConfigurationEntity { Id = 1 });
        _mockMapper.Setup(m => m.Map<PolicyConfigurationDto>(It.IsAny<PolicyConfigurationEntity>()))
                   .Returns(new PolicyConfigurationDto());

        await _service.CreateAsync(new CreatePolicyConfigurationDto
        {
            PolicyCode = "P", Category = "C", DisplayName = "D", DataType = "BIT", IsActive = true, CreatedBy = 1
        });

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_InvokesMapperOnce()
    {
        var entity = SampleEntity(1);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<PolicyConfigurationDto>(entity)).Returns(new PolicyConfigurationDto());

        await _service.GetByIdAsync(1);

        _mockMapper.Verify(m => m.Map<PolicyConfigurationDto>(entity), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvokesMapperTwice()
    {
        _mockMapper.Setup(m => m.Map<PolicyConfigurationEntity>(It.IsAny<CreatePolicyConfigurationDto>()))
                   .Returns(new PolicyConfigurationEntity());
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<PolicyConfigurationEntity>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new PolicyConfigurationEntity { Id = 1 });
        _mockMapper.Setup(m => m.Map<PolicyConfigurationDto>(It.IsAny<PolicyConfigurationEntity>()))
                   .Returns(new PolicyConfigurationDto());

        var dto = new CreatePolicyConfigurationDto
        {
            PolicyCode = "P", Category = "C", DisplayName = "D", DataType = "BIT", IsActive = true, CreatedBy = 1
        };
        await _service.CreateAsync(dto);

        _mockMapper.Verify(m => m.Map<PolicyConfigurationEntity>(dto), Times.Once);
        _mockMapper.Verify(m => m.Map<PolicyConfigurationDto>(It.IsAny<PolicyConfigurationEntity>()), Times.Once);
    }

    // ──────────────────────── GetPolicyValueAsync ─────────────────────────────

    [Fact]
    public async Task GetPolicyValueAsync_ExistingActivePolicy_ReturnsPolicyValue()
    {
        // Arrange
        var entities = new List<PolicyConfigurationEntity>
        {
            new() { Id = 1, PolicyCode = "RateableValueAreaType", PolicyValue = "BuiltupArea", IsActive = true }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());

        // Act
        var result = await _service.GetPolicyValueAsync("RateableValueAreaType", "CarpetArea");

        // Assert
        Assert.Equal("BuiltupArea", result);
    }

    [Fact]
    public async Task GetPolicyValueAsync_NonExistingPolicy_ReturnsDefaultValue()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<PolicyConfigurationEntity>().BuildMock());

        // Act
        var result = await _service.GetPolicyValueAsync("NonExistentPolicy", "DefaultValue");

        // Assert
        Assert.Equal("DefaultValue", result);
    }

    [Fact]
    public async Task GetPolicyValueAsync_InactivePolicy_ReturnsDefaultValue()
    {
        // Arrange
        var entities = new List<PolicyConfigurationEntity>
        {
            new() { Id = 1, PolicyCode = "RateableValueAreaType", PolicyValue = "BuiltupArea", IsActive = false }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());

        // Act
        var result = await _service.GetPolicyValueAsync("RateableValueAreaType", "CarpetArea");

        // Assert
        Assert.Equal("CarpetArea", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetPolicyValueAsync_NullOrEmptyPolicyValue_ReturnsDefaultValue(string policyValue)
    {
        // Arrange
        var entities = new List<PolicyConfigurationEntity>
        {
            new() { Id = 1, PolicyCode = "RateableValueAreaType", PolicyValue = policyValue, IsActive = true }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());

        // Act
        var result = await _service.GetPolicyValueAsync("RateableValueAreaType", "CarpetArea");

        // Assert
        Assert.Equal("CarpetArea", result);
    }

    // ──────────────────────── GetPolicyValuesAsync ─────────────────────────────

    [Fact]
    public async Task GetPolicyValuesAsync_AllPoliciesExist_ReturnsAllValues()
    {
        // Arrange
        var entities = new List<PolicyConfigurationEntity>
        {
            new() { Id = 1, PolicyCode = "RateableValueAreaType", PolicyValue = "BuiltupArea", IsActive = true },
            new() { Id = 2, PolicyCode = "RateMasterAreaUnit", PolicyValue = "SqFeet", IsActive = true },
            new() { Id = 3, PolicyCode = "RateMonthlyOrYearly", PolicyValue = "Monthly", IsActive = true }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());

        var policyDefaults = new Dictionary<string, string>
        {
            { "RateableValueAreaType", "CarpetArea" },
            { "RateMasterAreaUnit", "SqMeter" },
            { "RateMonthlyOrYearly", "Yearly" }
        };

        // Act
        var result = await _service.GetPolicyValuesAsync(policyDefaults);

        // Assert
        Assert.Equal("BuiltupArea", result["RateableValueAreaType"]);
        Assert.Equal("SqFeet", result["RateMasterAreaUnit"]);
        Assert.Equal("Monthly", result["RateMonthlyOrYearly"]);
    }

    [Fact]
    public async Task GetPolicyValuesAsync_SomePoliciesMissing_ReturnsDefaultsForMissing()
    {
        // Arrange
        var entities = new List<PolicyConfigurationEntity>
        {
            new() { Id = 1, PolicyCode = "RateableValueAreaType", PolicyValue = "BuiltupArea", IsActive = true }
            // Missing: RateMasterAreaUnit, RateMonthlyOrYearly
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());

        var policyDefaults = new Dictionary<string, string>
        {
            { "RateableValueAreaType", "CarpetArea" },
            { "RateMasterAreaUnit", "SqMeter" },
            { "RateMonthlyOrYearly", "Yearly" }
        };

        // Act
        var result = await _service.GetPolicyValuesAsync(policyDefaults);

        // Assert
        Assert.Equal("BuiltupArea", result["RateableValueAreaType"]); // From DB
        Assert.Equal("SqMeter", result["RateMasterAreaUnit"]); // Default
        Assert.Equal("Yearly", result["RateMonthlyOrYearly"]); // Default
    }

    [Fact]
    public async Task GetPolicyValuesAsync_SomePoliciesInactive_ReturnsDefaultsForInactive()
    {
        // Arrange
        var entities = new List<PolicyConfigurationEntity>
        {
            new() { Id = 1, PolicyCode = "RateableValueAreaType", PolicyValue = "BuiltupArea", IsActive = true },
            new() { Id = 2, PolicyCode = "RateMasterAreaUnit", PolicyValue = "SqFeet", IsActive = false }, // Inactive
            new() { Id = 3, PolicyCode = "RateMonthlyOrYearly", PolicyValue = "Monthly", IsActive = true }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());

        var policyDefaults = new Dictionary<string, string>
        {
            { "RateableValueAreaType", "CarpetArea" },
            { "RateMasterAreaUnit", "SqMeter" },
            { "RateMonthlyOrYearly", "Yearly" }
        };

        // Act
        var result = await _service.GetPolicyValuesAsync(policyDefaults);

        // Assert
        Assert.Equal("BuiltupArea", result["RateableValueAreaType"]); // From DB
        Assert.Equal("SqMeter", result["RateMasterAreaUnit"]); // Default (inactive)
        Assert.Equal("Monthly", result["RateMonthlyOrYearly"]); // From DB
    }

    [Fact]
    public async Task GetPolicyValuesAsync_EmptyDictionary_ReturnsEmptyDictionary()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<PolicyConfigurationEntity>().BuildMock());

        // Act
        var result = await _service.GetPolicyValuesAsync(new Dictionary<string, string>());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPolicyValuesAsync_NullOrEmptyPolicyValues_ReturnsDefaults()
    {
        // Arrange
        var entities = new List<PolicyConfigurationEntity>
        {
            new() { Id = 1, PolicyCode = "RateableValueAreaType", PolicyValue = null, IsActive = true },
            new() { Id = 2, PolicyCode = "RateMasterAreaUnit", PolicyValue = "", IsActive = true },
            new() { Id = 3, PolicyCode = "RateMonthlyOrYearly", PolicyValue = "   ", IsActive = true }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());

        var policyDefaults = new Dictionary<string, string>
        {
            { "RateableValueAreaType", "CarpetArea" },
            { "RateMasterAreaUnit", "SqMeter" },
            { "RateMonthlyOrYearly", "Yearly" }
        };

        // Act
        var result = await _service.GetPolicyValuesAsync(policyDefaults);

        // Assert - All should return defaults due to null/empty values
        Assert.Equal("CarpetArea", result["RateableValueAreaType"]);
        Assert.Equal("SqMeter", result["RateMasterAreaUnit"]);
        Assert.Equal("Yearly", result["RateMonthlyOrYearly"]);
    }
}
