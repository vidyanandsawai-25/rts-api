using AutoMapper;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Master.CommonRemarkTypeMaster;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class CommonRemarkTypeMasterServiceTests
{
    private readonly Mock<IRepository<CommonRemarkTypeMasterEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly CommonRemarkTypeMasterService _service;

    public CommonRemarkTypeMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<CommonRemarkTypeMasterEntity, int>>();
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

        _service = new CommonRemarkTypeMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new CommonRemarkTypeMasterEntity
        {
            Id = 1,
            RemarkTypeName = "General Remark",
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<CommonRemarkTypeMasterDtos>(It.IsAny<CommonRemarkTypeMasterEntity>()))
            .Returns(new CommonRemarkTypeMasterDtos
            {
                Id = 1,
                RemarkTypeName = "General Remark",
                IsActive = true
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("General Remark", result.RemarkTypeName);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<CommonRemarkTypeMasterDtos>(entity), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommonRemarkTypeMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<CommonRemarkTypeMasterDtos>(It.IsAny<CommonRemarkTypeMasterEntity>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetByIdAsync_InvalidId_ReturnsNull(int invalidId)
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommonRemarkTypeMasterEntity?)null);

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
        var entities = new List<CommonRemarkTypeMasterEntity>
        {
            new() { Id = 1, RemarkTypeName = "General Remark", IsActive = true },
            new() { Id = 2, RemarkTypeName = "Technical Remark", IsActive = true },
            new() { Id = 3, RemarkTypeName = "Administrative Remark", IsActive = false }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CommonRemarkTypeMasterEntity, CommonRemarkTypeMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new CommonRemarkTypeMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new CommonRemarkTypeQueryParameters
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
        Assert.Contains(items, x => x.RemarkTypeName == "General Remark");
        Assert.Contains(items, x => x.RemarkTypeName == "Technical Remark");
        Assert.Contains(items, x => x.RemarkTypeName == "Administrative Remark");
    }

    [Fact]
    public async Task GetAllAsync_WithRemarkTypeNameFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<CommonRemarkTypeMasterEntity>
        {
            new() { Id = 1, RemarkTypeName = "General Remark", IsActive = true },
            new() { Id = 2, RemarkTypeName = "Technical Remark", IsActive = true },
            new() { Id = 3, RemarkTypeName = "Administrative Remark", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CommonRemarkTypeMasterEntity, CommonRemarkTypeMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new CommonRemarkTypeMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new CommonRemarkTypeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            RemarkTypeName = "Technical"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item =>
             Assert.Contains("Technical", item.RemarkTypeName, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var entities = new List<CommonRemarkTypeMasterEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CommonRemarkTypeMasterEntity, CommonRemarkTypeMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new CommonRemarkTypeMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new CommonRemarkTypeQueryParameters
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
            .Select(i => new CommonRemarkTypeMasterEntity
            {
                Id = i,
                RemarkTypeName = $"Remark Type {i}",
                IsActive = true
            })
            .ToList();

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CommonRemarkTypeMasterEntity, CommonRemarkTypeMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new CommonRemarkTypeMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new CommonRemarkTypeQueryParameters
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
        var entities = new List<CommonRemarkTypeMasterEntity>
        {
            new() { Id = 1, RemarkTypeName = "General Remark", IsActive = true },
            new() { Id = 2, RemarkTypeName = "Technical Remark", IsActive = true },
            new() { Id = 3, RemarkTypeName = "Administrative Remark", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CommonRemarkTypeMasterEntity, CommonRemarkTypeMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new CommonRemarkTypeMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new CommonRemarkTypeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "Technical"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Technical Remark", result.Items.Single().RemarkTypeName);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateCommonRemarkTypeMasterDto
        {
            RemarkTypeName = "General Remark",
            IsActive = true,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<CommonRemarkTypeMasterEntity>(It.IsAny<CreateCommonRemarkTypeMasterDto>()))
            .Returns((CreateCommonRemarkTypeMasterDto dto) => new CommonRemarkTypeMasterEntity
            {
                RemarkTypeName = dto.RemarkTypeName,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<CommonRemarkTypeMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommonRemarkTypeMasterEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<CommonRemarkTypeMasterDtos>(It.IsAny<CommonRemarkTypeMasterEntity>()))
            .Returns((CommonRemarkTypeMasterEntity e) => new CommonRemarkTypeMasterDtos
            {
                Id = e.Id,
                RemarkTypeName = e.RemarkTypeName,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("General Remark", result.RemarkTypeName);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(
            It.Is<CommonRemarkTypeMasterEntity>(e => e.RemarkTypeName == "General Remark"),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InactiveRemarkType_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateCommonRemarkTypeMasterDto
        {
            RemarkTypeName = "Inactive Remark Type",
            IsActive = false
        };

        _mockMapper
            .Setup(m => m.Map<CommonRemarkTypeMasterEntity>(It.IsAny<CreateCommonRemarkTypeMasterDto>()))
            .Returns(new CommonRemarkTypeMasterEntity
            {
                Id = 0,
                RemarkTypeName = "Inactive Remark Type",
                IsActive = false
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<CommonRemarkTypeMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommonRemarkTypeMasterEntity e, CancellationToken _) =>
            {
                e.Id = 2;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<CommonRemarkTypeMasterDtos>(It.IsAny<CommonRemarkTypeMasterEntity>()))
            .Returns(new CommonRemarkTypeMasterDtos
            {
                Id = 2,
                RemarkTypeName = "Inactive Remark Type",
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
    public async Task CreateAsync_DuplicateRemarkTypeName_ThrowsException()
    {
        // Arrange
        var createDto = new CreateCommonRemarkTypeMasterDto
        {
            RemarkTypeName = "General Remark",
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<CommonRemarkTypeMasterEntity>(It.IsAny<CreateCommonRemarkTypeMasterDto>()))
            .Returns(new CommonRemarkTypeMasterEntity { RemarkTypeName = "General Remark" });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<CommonRemarkTypeMasterEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate RemarkTypeName: 'General Remark' already exists"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateCommonRemarkTypeMasterDto
        {
            RemarkTypeName = "General Remark - Updated",
            IsActive = true,
            UpdatedBy = 1
        };

        var existingEntity = new CommonRemarkTypeMasterEntity
        {
            Id = 1,
            RemarkTypeName = "General Remark",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<CommonRemarkTypeMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateCommonRemarkTypeMasterDto>(), It.IsAny<CommonRemarkTypeMasterEntity>()))
            .Callback((UpdateCommonRemarkTypeMasterDto src, CommonRemarkTypeMasterEntity dest) =>
            {
                dest.RemarkTypeName = src.RemarkTypeName;
            });

        _mockMapper
            .Setup(m => m.Map<CommonRemarkTypeMasterDtos>(It.IsAny<CommonRemarkTypeMasterEntity>()))
            .Returns((CommonRemarkTypeMasterEntity e) => new CommonRemarkTypeMasterDtos
            {
                Id = e.Id,
                RemarkTypeName = e.RemarkTypeName,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("General Remark - Updated", result.RemarkTypeName);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateCommonRemarkTypeMasterDto
        {
            RemarkTypeName = "Test Remark Type",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommonRemarkTypeMasterEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<CommonRemarkTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSavesSuccessfully()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new CommonRemarkTypeMasterEntity
        {
            Id = idToDelete,
            RemarkTypeName = "Old Remark Type",
            IsActive = false
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<CommonRemarkTypeMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<CommonRemarkTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ActiveRemarkType_ShouldStillDelete()
    {
        // Arrange - Even active remark types can be deleted
        var idToDelete = 1;

        var existingEntity = new CommonRemarkTypeMasterEntity
        {
            Id = idToDelete,
            RemarkTypeName = "General Remark",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<CommonRemarkTypeMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<CommonRemarkTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommonRemarkTypeMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<CommonRemarkTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Transaction Tests

    [Fact]
    public async Task CreateAsync_VerifiesNoExplicitTransactionUsed()
    {
        // Arrange
        var createDto = new CreateCommonRemarkTypeMasterDto
        {
            RemarkTypeName = "Test Remark Type",
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<CommonRemarkTypeMasterEntity>(It.IsAny<CreateCommonRemarkTypeMasterDto>()))
            .Returns(new CommonRemarkTypeMasterEntity { RemarkTypeName = "Test Remark Type" });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<CommonRemarkTypeMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommonRemarkTypeMasterEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<CommonRemarkTypeMasterDtos>(It.IsAny<CommonRemarkTypeMasterEntity>()))
            .Returns(new CommonRemarkTypeMasterDtos { Id = 1, RemarkTypeName = "Test Remark Type" });

        // Act
        await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert - Verify NO explicit transaction management in simple CRUD
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_VerifiesNoExplicitTransactionUsed()
    {
        // Arrange
        var updateDto = new UpdateCommonRemarkTypeMasterDto
        {
            RemarkTypeName = "Updated Remark Type",
            IsActive = true
        };

        var existingEntity = new CommonRemarkTypeMasterEntity
        {
            Id = 1,
            RemarkTypeName = "Original Remark Type"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<CommonRemarkTypeMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateCommonRemarkTypeMasterDto>(), It.IsAny<CommonRemarkTypeMasterEntity>()))
            .Callback((UpdateCommonRemarkTypeMasterDto src, CommonRemarkTypeMasterEntity dest) =>
            {
                dest.RemarkTypeName = src.RemarkTypeName;
            });

        _mockMapper
            .Setup(m => m.Map<CommonRemarkTypeMasterDtos>(It.IsAny<CommonRemarkTypeMasterEntity>()))
            .Returns(new CommonRemarkTypeMasterDtos { Id = 1, RemarkTypeName = "Updated Remark Type" });

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_VerifiesNoExplicitTransactionUsed()
    {
        // Arrange
        var existingEntity = new CommonRemarkTypeMasterEntity
        {
            Id = 1,
            RemarkTypeName = "To Delete"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<CommonRemarkTypeMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(1, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task CreateAsync_WithLongRemarkTypeName_CreatesSuccessfully()
    {
        // Arrange - RemarkTypeName max length is 100
        var longName = new string('A', 100);
        var createDto = new CreateCommonRemarkTypeMasterDto
        {
            RemarkTypeName = longName,
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<CommonRemarkTypeMasterEntity>(It.IsAny<CreateCommonRemarkTypeMasterDto>()))
            .Returns(new CommonRemarkTypeMasterEntity { RemarkTypeName = longName });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<CommonRemarkTypeMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommonRemarkTypeMasterEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<CommonRemarkTypeMasterDtos>(It.IsAny<CommonRemarkTypeMasterEntity>()))
            .Returns(new CommonRemarkTypeMasterDtos { Id = 1, RemarkTypeName = longName });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(longName, result.RemarkTypeName);
        Assert.Equal(100, result.RemarkTypeName.Length);
    }

    [Fact]
    public async Task GetAllAsync_WithIsActiveFilter_ReturnsOnlyActiveEntities()
    {
        // Arrange
        var entities = new List<CommonRemarkTypeMasterEntity>
        {
            new() { Id = 1, RemarkTypeName = "Active Remark 1", IsActive = true },
            new() { Id = 2, RemarkTypeName = "Inactive Remark", IsActive = false },
            new() { Id = 3, RemarkTypeName = "Active Remark 2", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CommonRemarkTypeMasterEntity, CommonRemarkTypeMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new CommonRemarkTypeMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new CommonRemarkTypeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            IsActive = true
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.True(item.IsActive));
    }

    #endregion
}
