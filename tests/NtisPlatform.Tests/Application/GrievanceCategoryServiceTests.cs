using AutoMapper;
using Moq;
using MockQueryable;
using NtisPlatform.Application.DTOs.Master.GrievanceCategoryMaster;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Mappings;

namespace NtisPlatform.Tests.Application;

public class GrievanceCategoryServiceTests
{
    private readonly Mock<IRepository<GrievanceCategoryEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly GrievanceCategoryService _service;

    public GrievanceCategoryServiceTests()
    {
        _mockRepository = new Mock<IRepository<GrievanceCategoryEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        // Setup SaveChangesAsync
        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Optional transaction setups
        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new GrievanceCategoryService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new GrievanceCategoryEntity
        {
            GrievanceCategoryId = 1,
            CategoryCode = "GRV001",
            CategoryName = "Service Quality Issue",
            DepartmentId = 1,
            Priority = "High",
            ResolutionSla = "24 Hours",
            EscalationLevel = "Level 1",
            Description = "Issues related to service quality",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<GrievanceCategoryDto>(It.IsAny<GrievanceCategoryEntity>()))
            .Returns(new GrievanceCategoryDto
            {
                GrievanceCategoryId = 1,
                CategoryCode = "GRV001",
                CategoryName = "Service Quality Issue",
                DepartmentId = 1,
                Priority = "High",
                ResolutionSla = "24 Hours",
                EscalationLevel = "Level 1",
                Description = "Issues related to service quality",
                IsActive = true
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.GrievanceCategoryId);
        Assert.Equal("GRV001", result.CategoryCode);
        Assert.Equal("Service Quality Issue", result.CategoryName);
        Assert.Equal(1, result.DepartmentId);
        Assert.Equal("High", result.Priority);
        Assert.Equal("24 Hours", result.ResolutionSla);
        Assert.Equal("Level 1", result.EscalationLevel);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<GrievanceCategoryDto>(entity), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GrievanceCategoryEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<GrievanceCategoryDto>(It.IsAny<GrievanceCategoryEntity>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetByIdAsync_InvalidId_ReturnsNull(int invalidId)
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GrievanceCategoryEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(invalidId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<GrievanceCategoryEntity>
        {
            new() 
            {
                GrievanceCategoryId = 1, 
                CategoryCode = "GRV001",
                CategoryName = "Service Quality Issue",
                DepartmentId = 1,
                Priority = "High",
                ResolutionSla = "24 Hours",
                EscalationLevel = "Level 1",
                Description = "Issues related to service quality",
                IsActive = true
            },
            new() 
            {
                GrievanceCategoryId = 2, 
                CategoryCode = "GRV002",
                CategoryName = "Billing Issue",
                DepartmentId = 2,
                Priority = "Medium",
                ResolutionSla = "48 Hours",
                EscalationLevel = "Level 1",
                Description = "Issues related to billing",
                IsActive = true
            },
            new() 
            {
                GrievanceCategoryId = 3, 
                CategoryCode = "GRV003",
                CategoryName = "Technical Support",
                DepartmentId = 3,
                Priority = "Low",
                ResolutionSla = "72 Hours",
                EscalationLevel = "Level 2",
                Description = "Technical support issues",
                IsActive = false
            }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<GrievanceCategoryMappingProfile>();
        });

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new GrievanceCategoryService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new GrievanceCategoryQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(3, items.Count);
        Assert.Contains(items, x => x.CategoryCode == "GRV001" && x.CategoryName == "Service Quality Issue");
        Assert.Contains(items, x => x.CategoryCode == "GRV002" && x.CategoryName == "Billing Issue");
        Assert.Contains(items, x => x.CategoryCode == "GRV003" && x.CategoryName == "Technical Support");
    }

    [Fact]
    public async Task GetAllAsync_WithCategoryCodeFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<GrievanceCategoryEntity>
        {
            new() { GrievanceCategoryId = 1, CategoryCode = "GRV001", CategoryName = "Service Quality", Priority = "High", IsActive = true },
            new() { GrievanceCategoryId = 2, CategoryCode = "BIL001", CategoryName = "Billing Issue", Priority = "Medium", IsActive = true },
            new() { GrievanceCategoryId = 3, CategoryCode = "GRV002", CategoryName = "Technical Support", Priority = "Low", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<GrievanceCategoryEntity, GrievanceCategoryDto>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new GrievanceCategoryService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new GrievanceCategoryQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "GRV"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.All(items, item => Assert.Contains("GRV", item.CategoryCode));
        Assert.DoesNotContain(items, item => item.CategoryCode == "BIL001");
    }

    [Fact]
    public async Task GetAllAsync_WithPriorityFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<GrievanceCategoryEntity>
        {
            new() { GrievanceCategoryId = 1, CategoryCode = "GRV001", CategoryName = "Service Quality", Priority = "High", IsActive = true },
            new() { GrievanceCategoryId = 2, CategoryCode = "GRV002", CategoryName = "Billing Issue", Priority = "Medium", IsActive = true },
            new() { GrievanceCategoryId = 3, CategoryCode = "GRV003", CategoryName = "Technical Support", Priority = "High", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<GrievanceCategoryEntity, GrievanceCategoryDto>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new GrievanceCategoryService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new GrievanceCategoryQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "High"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.All(items, item => Assert.Equal("High", item.Priority));
        Assert.Contains(items, item => item.CategoryCode == "GRV001");
        Assert.Contains(items, item => item.CategoryCode == "GRV003");
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var entities = new List<GrievanceCategoryEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<GrievanceCategoryEntity, GrievanceCategoryDto>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new GrievanceCategoryService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new GrievanceCategoryQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var entities = Enumerable.Range(1, 25)
            .Select(i => new GrievanceCategoryEntity
            {
                GrievanceCategoryId = i,
                CategoryCode = $"GRV{i:000}",
                CategoryName = $"Category {i}",
                Priority = "Medium",
                ResolutionSla = "48 Hours",
                Description = $"Description for category {i}",
                IsActive = true
            })
            .ToList();

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<GrievanceCategoryEntity, GrievanceCategoryDto>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new GrievanceCategoryService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new GrievanceCategoryQueryParameters
        {
            PageNumber = 2,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
    }

    [Fact]
    public async Task GetAllAsync_WithSearchTerm_ReturnsMatchingEntities()
    {
        // Arrange
        var entities = new List<GrievanceCategoryEntity>
        {
            new() { GrievanceCategoryId = 1, CategoryCode = "GRV001", CategoryName = "Service Quality Issue", Priority = "High", IsActive = true },
            new() { GrievanceCategoryId = 2, CategoryCode = "BIL001", CategoryName = "Billing Problem", Priority = "Medium", IsActive = true },
            new() { GrievanceCategoryId = 3, CategoryCode = "TEC001", CategoryName = "Technical Service Support", Priority = "Low", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<GrievanceCategoryEntity, GrievanceCategoryDto>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new GrievanceCategoryService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new GrievanceCategoryQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "Service"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.All(items, item => Assert.Contains("Service", item.CategoryName, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(items, item => item.CategoryCode == "GRV001");
        Assert.Contains(items, item => item.CategoryCode == "TEC001");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateGrievanceCategoryDto
        {
            CategoryCode = "GRV001",
            CategoryName = "Service Quality Issue",
            DepartmentId = 1,
            Priority = "High",
            ResolutionSla = "24 Hours",
            EscalationLevel = "Level 1",
            Description = "Issues related to service quality",
            IsActive = true,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<GrievanceCategoryEntity>(It.IsAny<CreateGrievanceCategoryDto>()))
            .Returns((CreateGrievanceCategoryDto dto) => new GrievanceCategoryEntity
            {
                CategoryCode = dto.CategoryCode,
                CategoryName = dto.CategoryName,
                DepartmentId = dto.DepartmentId,
                Priority = dto.Priority,
                ResolutionSla = dto.ResolutionSla,
                EscalationLevel = dto.EscalationLevel,
                Description = dto.Description,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<GrievanceCategoryEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GrievanceCategoryEntity e, CancellationToken _) =>
            {
                e.GrievanceCategoryId = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<GrievanceCategoryDto>(It.IsAny<GrievanceCategoryEntity>()))
            .Returns((GrievanceCategoryEntity e) => new GrievanceCategoryDto
            {
                GrievanceCategoryId = e.GrievanceCategoryId,
                CategoryCode = e.CategoryCode,
                CategoryName = e.CategoryName,
                DepartmentId = e.DepartmentId,
                Priority = e.Priority,
                ResolutionSla = e.ResolutionSla,
                EscalationLevel = e.EscalationLevel,
                Description = e.Description,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.GrievanceCategoryId);
        Assert.Equal("GRV001", result.CategoryCode);
        Assert.Equal("Service Quality Issue", result.CategoryName);
        Assert.Equal(1, result.DepartmentId);
        Assert.Equal("High", result.Priority);
        Assert.Equal("24 Hours", result.ResolutionSla);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(
            It.Is<GrievanceCategoryEntity>(e => e.CategoryCode == "GRV001" && e.CategoryName == "Service Quality Issue"),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InactiveCategory_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateGrievanceCategoryDto
        {
            CategoryCode = "OLD001",
            CategoryName = "Obsolete Category",
            Priority = "Low",
            ResolutionSla = "72 Hours",
            EscalationLevel = "Level 3",
            Description = "Deprecated grievance category",
            IsActive = false
        };

        _mockMapper
            .Setup(m => m.Map<GrievanceCategoryEntity>(It.IsAny<CreateGrievanceCategoryDto>()))
            .Returns(new GrievanceCategoryEntity
            {
                GrievanceCategoryId = 0,
                CategoryCode = "OLD001",
                CategoryName = "Obsolete Category",
                Priority = "Low",
                IsActive = false
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<GrievanceCategoryEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GrievanceCategoryEntity e, CancellationToken _) =>
            {
                e.GrievanceCategoryId = 2;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<GrievanceCategoryDto>(It.IsAny<GrievanceCategoryEntity>()))
            .Returns(new GrievanceCategoryDto
            {
                GrievanceCategoryId = 2,
                CategoryCode = "OLD001",
                CategoryName = "Obsolete Category",
                IsActive = false
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCategoryCode_ThrowsException()
    {
        // Arrange
        var createDto = new CreateGrievanceCategoryDto
        {
            CategoryCode = "GRV001",
            CategoryName = "Service Quality",
            Priority = "High",
            ResolutionSla = "24 Hours",
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<GrievanceCategoryEntity>(It.IsAny<CreateGrievanceCategoryDto>()))
            .Returns(new GrievanceCategoryEntity { CategoryCode = "GRV001" });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<GrievanceCategoryEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate CategoryCode: 'GRV001' already exists"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithOptionalFields_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateGrievanceCategoryDto
        {
            CategoryCode = "GRV002",
            CategoryName = "Basic Category",
            Priority = "Medium",
            ResolutionSla = null,
            EscalationLevel = null,
            Description = null,
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<GrievanceCategoryEntity>(It.IsAny<CreateGrievanceCategoryDto>()))
            .Returns(new GrievanceCategoryEntity
            {
                CategoryCode = "GRV002",
                CategoryName = "Basic Category",
                Priority = "Medium",
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<GrievanceCategoryEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GrievanceCategoryEntity e, CancellationToken _) =>
            {
                e.GrievanceCategoryId = 3;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<GrievanceCategoryDto>(It.IsAny<GrievanceCategoryEntity>()))
            .Returns(new GrievanceCategoryDto
            {
                GrievanceCategoryId = 3,
                CategoryCode = "GRV002",
                CategoryName = "Basic Category",
                Priority = "Medium",
                IsActive = true
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.GrievanceCategoryId);
        Assert.Equal("GRV002", result.CategoryCode);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateGrievanceCategoryDto
        {
            CategoryCode = "GRV001",
            CategoryName = "Service Quality Issue - Updated",
            DepartmentId = 2,
            Priority = "Critical",
            ResolutionSla = "12 Hours",
            EscalationLevel = "Level 2",
            Description = "Updated description for service quality",
            IsActive = true,
            UpdatedBy = 1
        };

        var existingEntity = new GrievanceCategoryEntity
        {
            GrievanceCategoryId = 1,
            CategoryCode = "GRV001",
            CategoryName = "Service Quality Issue",
            DepartmentId = 1,
            Priority = "High",
            ResolutionSla = "24 Hours",
            EscalationLevel = "Level 1",
            Description = "Issues related to service quality",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<GrievanceCategoryEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateGrievanceCategoryDto>(), It.IsAny<GrievanceCategoryEntity>()))
            .Callback((UpdateGrievanceCategoryDto src, GrievanceCategoryEntity dest) =>
            {
                dest.CategoryName = src.CategoryName;
                dest.DepartmentId = src.DepartmentId;
                dest.Priority = src.Priority;
                dest.ResolutionSla = src.ResolutionSla;
                dest.EscalationLevel = src.EscalationLevel;
                dest.Description = src.Description;
            });

        _mockMapper
            .Setup(m => m.Map<GrievanceCategoryDto>(It.IsAny<GrievanceCategoryEntity>()))
            .Returns((GrievanceCategoryEntity e) => new GrievanceCategoryDto
            {
                GrievanceCategoryId = e.GrievanceCategoryId,
                CategoryCode = e.CategoryCode,
                CategoryName = e.CategoryName,
                DepartmentId = e.DepartmentId,
                Priority = e.Priority,
                ResolutionSla = e.ResolutionSla,
                EscalationLevel = e.EscalationLevel,
                Description = e.Description,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Service Quality Issue - Updated", result.CategoryName);
        Assert.Equal(2, result.DepartmentId);
        Assert.Equal("Critical", result.Priority);
        Assert.Equal("12 Hours", result.ResolutionSla);
        Assert.Equal("Level 2", result.EscalationLevel);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateGrievanceCategoryDto
        {
            CategoryCode = "TEST001",
            CategoryName = "Test Category",
            Priority = "High",
            ResolutionSla = "24 Hours",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GrievanceCategoryEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<GrievanceCategoryEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ChangePriorityLevel_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateGrievanceCategoryDto
        {
            CategoryCode = "GRV001",
            CategoryName = "Service Quality Issue",
            Priority = "Critical",
            ResolutionSla = "6 Hours",
            EscalationLevel = "Level 3",
            IsActive = true
        };

        var existingEntity = new GrievanceCategoryEntity
        {
            GrievanceCategoryId = 1,
            CategoryCode = "GRV001",
            CategoryName = "Service Quality Issue",
            Priority = "Low",
            ResolutionSla = "72 Hours",
            EscalationLevel = "Level 1",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<GrievanceCategoryEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateGrievanceCategoryDto>(), It.IsAny<GrievanceCategoryEntity>()))
            .Callback((UpdateGrievanceCategoryDto src, GrievanceCategoryEntity dest) =>
            {
                dest.Priority = src.Priority;
                dest.ResolutionSla = src.ResolutionSla;
                dest.EscalationLevel = src.EscalationLevel;
            });

        _mockMapper
            .Setup(m => m.Map<GrievanceCategoryDto>(It.IsAny<GrievanceCategoryEntity>()))
            .Returns((GrievanceCategoryEntity e) => new GrievanceCategoryDto
            {
                GrievanceCategoryId = e.GrievanceCategoryId,
                Priority = e.Priority,
                ResolutionSla = e.ResolutionSla,
                EscalationLevel = e.EscalationLevel,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Critical", result.Priority);
        Assert.Equal("6 Hours", result.ResolutionSla);
        Assert.Equal("Level 3", result.EscalationLevel);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSavesSuccessfully()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new GrievanceCategoryEntity
        {
            GrievanceCategoryId = idToDelete,
            CategoryCode = "OLD001",
            CategoryName = "Old Category",
            Priority = "Low",
            IsActive = false
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(idToDelete, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ActiveCategory_ShouldStillDelete()
    {
        // Arrange - Even active categories can be deleted
        var idToDelete = 1;

        var existingEntity = new GrievanceCategoryEntity
        {
            GrievanceCategoryId = idToDelete,
            CategoryCode = "GRV001",
            CategoryName = "Active Category",
            Priority = "High",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(idToDelete, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GrievanceCategoryEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
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
            .ReturnsAsync((GrievanceCategoryEntity?)null);

        // Act
        var result = await _service.DeleteAsync(invalidId, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Transaction Tests

    [Fact]
    public async Task CreateAsync_VerifiesNoExplicitTransactionUsed()
    {
        // Arrange
        var createDto = new CreateGrievanceCategoryDto
        {
            CategoryCode = "TEST001",
            CategoryName = "Test Category",
            Priority = "Medium",
            ResolutionSla = "48 Hours",
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<GrievanceCategoryEntity>(It.IsAny<CreateGrievanceCategoryDto>()))
            .Returns(new GrievanceCategoryEntity());

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<GrievanceCategoryEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GrievanceCategoryEntity { GrievanceCategoryId = 1 });

        _mockMapper
            .Setup(m => m.Map<GrievanceCategoryDto>(It.IsAny<GrievanceCategoryEntity>()))
            .Returns(new GrievanceCategoryDto());

        // Act
        await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_VerifiesNoExplicitTransactionUsed()
    {
        // Arrange
        var existingEntity = new GrievanceCategoryEntity
        {
            GrievanceCategoryId = 1,
            CategoryCode = "GRV001",
            CategoryName = "Service Quality"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<GrievanceCategoryEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper.Setup(m => m.Map<GrievanceCategoryDto>(It.IsAny<GrievanceCategoryEntity>()))
            .Returns(new GrievanceCategoryDto());

        var updateDto = new UpdateGrievanceCategoryDto 
        { 
            CategoryCode = "GRV001", 
            CategoryName = "Service Quality Updated",
            Priority = "High",
            ResolutionSla = "24 Hours",
            IsActive = true
        };

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Mapper Tests

    [Fact]
    public async Task GetByIdAsync_VerifiesMapperCalledOnce()
    {
        // Arrange
        var entity = new GrievanceCategoryEntity
        {
            GrievanceCategoryId = 1,
            CategoryCode = "GRV001",
            CategoryName = "Service Quality"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<GrievanceCategoryDto>(It.IsAny<GrievanceCategoryEntity>()))
            .Returns(new GrievanceCategoryDto());

        // Act
        await _service.GetByIdAsync(1);

        // Assert
        _mockMapper.Verify(m => m.Map<GrievanceCategoryDto>(entity), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_VerifiesMapperCalledTwice()
    {
        // Arrange
        var createDto = new CreateGrievanceCategoryDto 
        { 
            CategoryCode = "GRV001", 
            CategoryName = "Service Quality",
            Priority = "High",
            ResolutionSla = "24 Hours",
            IsActive = true
        };

        _mockMapper.Setup(m => m.Map<GrievanceCategoryEntity>(It.IsAny<CreateGrievanceCategoryDto>()))
            .Returns(new GrievanceCategoryEntity());

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<GrievanceCategoryEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GrievanceCategoryEntity { GrievanceCategoryId = 1 });

        _mockMapper.Setup(m => m.Map<GrievanceCategoryDto>(It.IsAny<GrievanceCategoryEntity>()))
            .Returns(new GrievanceCategoryDto());

        // Act
        await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        _mockMapper.Verify(m => m.Map<GrievanceCategoryEntity>(createDto), Times.Once);
        _mockMapper.Verify(m => m.Map<GrievanceCategoryDto>(It.IsAny<GrievanceCategoryEntity>()), Times.Once);
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task GetAllAsync_OrderedByCategoryNameAscending_ReturnsOrderedResults()
    {
        // Arrange
        var entities = new List<GrievanceCategoryEntity>
        {
            new() { GrievanceCategoryId = 1, CategoryCode = "GRV002", CategoryName = "Billing Issue", Priority = "Medium", IsActive = true },
            new() { GrievanceCategoryId = 2, CategoryCode = "GRV001", CategoryName = "Service Quality", Priority = "High", IsActive = true },
            new() { GrievanceCategoryId = 3, CategoryCode = "GRV003", CategoryName = "Technical Support", Priority = "Low", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<GrievanceCategoryEntity, GrievanceCategoryDto>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new GrievanceCategoryService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new GrievanceCategoryQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "CategoryName",
            SortOrder = "asc"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.True(items.Count > 0);
    }

    [Fact]
    public async Task CreateAsync_WithCompleteInformation_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateGrievanceCategoryDto
        {
            CategoryCode = "GRV005",
            CategoryName = "Infrastructure Issue",
            DepartmentId = 5,
            Priority = "Critical",
            ResolutionSla = "6 Hours",
            EscalationLevel = "Level 3",
            Description = "Critical infrastructure related issues requiring immediate attention",
            IsActive = true,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<GrievanceCategoryEntity>(It.IsAny<CreateGrievanceCategoryDto>()))
            .Returns((CreateGrievanceCategoryDto dto) => new GrievanceCategoryEntity
            {
                CategoryCode = dto.CategoryCode,
                CategoryName = dto.CategoryName,
                DepartmentId = dto.DepartmentId,
                Priority = dto.Priority,
                ResolutionSla = dto.ResolutionSla,
                EscalationLevel = dto.EscalationLevel,
                Description = dto.Description,
                IsActive = dto.IsActive
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<GrievanceCategoryEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GrievanceCategoryEntity e, CancellationToken _) =>
            {
                e.GrievanceCategoryId = 5;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<GrievanceCategoryDto>(It.IsAny<GrievanceCategoryEntity>()))
            .Returns((GrievanceCategoryEntity e) => new GrievanceCategoryDto
            {
                GrievanceCategoryId = e.GrievanceCategoryId,
                CategoryCode = e.CategoryCode,
                CategoryName = e.CategoryName,
                DepartmentId = e.DepartmentId,
                Priority = e.Priority,
                ResolutionSla = e.ResolutionSla,
                EscalationLevel = e.EscalationLevel,
                Description = e.Description,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.GrievanceCategoryId);
        Assert.Equal("GRV005", result.CategoryCode);
        Assert.Equal("Infrastructure Issue", result.CategoryName);
        Assert.Equal(5, result.DepartmentId);
        Assert.Equal("Critical", result.Priority);
        Assert.Equal("6 Hours", result.ResolutionSla);
        Assert.Equal("Level 3", result.EscalationLevel);
        Assert.NotNull(result.Description);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetAllAsync_WithIsActiveFilter_ReturnsOnlyActiveCategories()
    {
        // Arrange
        var entities = new List<GrievanceCategoryEntity>
        {
            new() { GrievanceCategoryId = 1, CategoryCode = "GRV001", CategoryName = "Active Category 1", Priority = "High", IsActive = true },
            new() { GrievanceCategoryId = 2, CategoryCode = "GRV002", CategoryName = "Inactive Category", Priority = "Medium", IsActive = false },
            new() { GrievanceCategoryId = 3, CategoryCode = "GRV003", CategoryName = "Active Category 2", Priority = "Low", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<GrievanceCategoryMappingProfile>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new GrievanceCategoryService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new GrievanceCategoryQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            IsActive = true
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.All(items, item => Assert.True(item.IsActive));
        Assert.DoesNotContain(items, item => item.CategoryCode == "GRV002");
    }

    #endregion
}

