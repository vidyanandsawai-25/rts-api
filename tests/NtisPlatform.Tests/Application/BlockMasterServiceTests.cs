using AutoMapper;
using Moq;
using MockQueryable;
using NtisPlatform.Application.DTOs.Master.BlockMaster;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

public class BlockMasterServiceTests
{
    private readonly Mock<IRepository<BlockMasterEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly BlockMasterService _service;

    public BlockMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<BlockMasterEntity, int>>();
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

        _service = new BlockMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new BlockMasterEntity
        {
            Id = 1,
            WardId = 10,
            BlockNo = "BLK001",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<BlockMasterDtos>(It.IsAny<BlockMasterEntity>()))
            .Returns(new BlockMasterDtos
            {
                Id = 1,
                WardId = 10,
                BlockNo = "BLK001",
                IsActive = true
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.WardId);
        Assert.Equal("BLK001", result.BlockNo);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<BlockMasterDtos>(entity), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlockMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<BlockMasterDtos>(It.IsAny<BlockMasterEntity>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetByIdAsync_InvalidId_ReturnsNull(int invalidId)
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlockMasterEntity?)null);

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
        var entities = new List<BlockMasterEntity>
        {
            new()
            {
                Id = 1,
                WardId = 10,
                BlockNo = "BLK001",
                IsActive = true
            },
            new()
            {
                Id = 2,
                WardId = 10,
                BlockNo = "BLK002",
                IsActive = true
            },
            new()
            {
                Id = 3,
                WardId = 20,
                BlockNo = "BLK003",
                IsActive = false
            }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BlockMasterEntity, BlockMasterDtos>();
        },Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new BlockMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new BlockQueryParameters
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
        Assert.Contains(items, x => x.BlockNo == "BLK001" && x.WardId == 10);
        Assert.Contains(items, x => x.BlockNo == "BLK002" && x.WardId == 10);
        Assert.Contains(items, x => x.BlockNo == "BLK003" && x.WardId == 20);
    }

    [Fact]
    public async Task GetAllAsync_WithWardIdFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<BlockMasterEntity>
        {
            new() { Id = 1, WardId = 10, BlockNo = "BLK001", IsActive = true },
            new() { Id = 2, WardId = 20, BlockNo = "BLK002", IsActive = true },
            new() { Id = 3, WardId = 10, BlockNo = "BLK003", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BlockMasterEntity, BlockMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new BlockMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new BlockQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            WardId = 10
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal(10, item.WardId));
    }

    [Fact]
    public async Task GetAllAsync_WithBlockNoFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<BlockMasterEntity>
        {
            new() { Id = 1, WardId = 10, BlockNo = "BLK001", IsActive = true },
            new() { Id = 2, WardId = 20, BlockNo = "BLK002", IsActive = true },
            new() { Id = 3, WardId = 30, BlockNo = "BLK001-A", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BlockMasterEntity, BlockMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new BlockMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new BlockQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            BlockNo = "BLK001"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item =>
            Assert.Contains("BLK001", item.BlockNo, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAllAsync_WithIsActiveFilter_ReturnsOnlyActiveEntities()
    {
        // Arrange
        var entities = new List<BlockMasterEntity>
        {
            new() { Id = 1, WardId = 10, BlockNo = "BLK001", IsActive = true },
            new() { Id = 2, WardId = 20, BlockNo = "BLK002", IsActive = false },
            new() { Id = 3, WardId = 30, BlockNo = "BLK003", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BlockMasterEntity, BlockMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new BlockMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new BlockQueryParameters
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

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var entities = new List<BlockMasterEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BlockMasterEntity, BlockMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new BlockMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new BlockQueryParameters
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
            .Select(i => new BlockMasterEntity
            {
                Id = i,
                WardId = (i % 5) + 1,
                BlockNo = $"BLK{i:000}",
                IsActive = true
            })
            .ToList();

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BlockMasterEntity, BlockMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new BlockMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new BlockQueryParameters
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
        var entities = new List<BlockMasterEntity>
        {
            new() { Id = 1, WardId = 10, BlockNo = "BLK001", IsActive = true },
            new() { Id = 2, WardId = 20, BlockNo = "SPECIAL-002", IsActive = true },
            new() { Id = 3, WardId = 30, BlockNo = "BLK003", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BlockMasterEntity, BlockMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new BlockMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new BlockQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "SPECIAL"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateBlockMasterDto
        {
            WardId = 10,
            BlockNo = "BLK001",
            IsActive = true,
            CreatedBy = 1
        };

        var createdEntity = new BlockMasterEntity
        {
            Id = 1,
            WardId = 10,
            BlockNo = "BLK001",
            IsActive = true,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<BlockMasterEntity>(It.IsAny<CreateBlockMasterDto>()))
            .Returns(new BlockMasterEntity
            {
                WardId = createDto.WardId,
                BlockNo = createDto.BlockNo,
                IsActive = createDto.IsActive,
                CreatedBy = createDto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<BlockMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdEntity);

        _mockMapper
            .Setup(m => m.Map<BlockMasterDtos>(It.IsAny<BlockMasterEntity>()))
            .Returns(new BlockMasterDtos
            {
                Id = 1,
                WardId = 10,
                BlockNo = "BLK001",
                IsActive = true
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.WardId);
        Assert.Equal("BLK001", result.BlockNo);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(
            It.Is<BlockMasterEntity>(e => e.WardId == 10 && e.BlockNo == "BLK001"),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InactiveBlock_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateBlockMasterDto
        {
            WardId = 10,
            BlockNo = "OLD-BLOCK",
            IsActive = false,
            CreatedBy = 1
        };

        var createdEntity = new BlockMasterEntity
        {
            Id = 1,
            WardId = 10,
            BlockNo = "OLD-BLOCK",
            IsActive = false,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<BlockMasterEntity>(It.IsAny<CreateBlockMasterDto>()))
            .Returns(new BlockMasterEntity
            {
                WardId = createDto.WardId,
                BlockNo = createDto.BlockNo,
                IsActive = createDto.IsActive,
                CreatedBy = createDto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<BlockMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdEntity);

        _mockMapper
            .Setup(m => m.Map<BlockMasterDtos>(It.IsAny<BlockMasterEntity>()))
            .Returns(new BlockMasterDtos
            {
                Id = 1,
                WardId = 10,
                BlockNo = "OLD-BLOCK",
                IsActive = false
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("OLD-BLOCK", result.BlockNo);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_MultipleBlocksInSameWard_ReturnsCreatedDto()
    {
        // Arrange
        var createDto1 = new CreateBlockMasterDto
        {
            WardId = 10,
            BlockNo = "BLK001",
            IsActive = true,
            CreatedBy = 1
        };

        var createDto2 = new CreateBlockMasterDto
        {
            WardId = 10,
            BlockNo = "BLK002",
            IsActive = true,
            CreatedBy = 1
        };

        var id = 1;
        _mockMapper
            .Setup(m => m.Map<BlockMasterEntity>(It.IsAny<CreateBlockMasterDto>()))
            .Returns((CreateBlockMasterDto dto) => new BlockMasterEntity
            {
                WardId = dto.WardId,
                BlockNo = dto.BlockNo,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<BlockMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlockMasterEntity e, CancellationToken _) =>
            {
                e.Id = id++;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<BlockMasterDtos>(It.IsAny<BlockMasterEntity>()))
            .Returns((BlockMasterEntity e) => new BlockMasterDtos
            {
                Id = e.Id,
                WardId = e.WardId,
                BlockNo = e.BlockNo,
                IsActive = e.IsActive
            });

        // Act
        var result1 = await _service.CreateAsync(createDto1, CancellationToken.None);
        var result2 = await _service.CreateAsync(createDto2, CancellationToken.None);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(10, result1.WardId);
        Assert.Equal(10, result2.WardId);
        Assert.Equal("BLK001", result1.BlockNo);
        Assert.Equal("BLK002", result2.BlockNo);
        Assert.NotEqual(result1.Id, result2.Id);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidDto_ReturnsUpdatedDto()
    {
        // Arrange
        var existingEntity = new BlockMasterEntity
        {
            Id = 1,
            WardId = 10,
            BlockNo = "BLK001",
            IsActive = true
        };

        var updateDto = new UpdateBlockMasterDto
        {
            WardId = 20,
            BlockNo = "BLK001-UPDATED",
            IsActive = true,
            UpdatedBy = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateBlockMasterDto>(), It.IsAny<BlockMasterEntity>()))
            .Callback<UpdateBlockMasterDto, BlockMasterEntity>((dto, entity) =>
            {
                entity.WardId = dto.WardId;
                entity.BlockNo = dto.BlockNo;
                entity.IsActive = dto.IsActive;
                entity.UpdatedBy = dto.UpdatedBy;
            })
            .Returns((UpdateBlockMasterDto _, BlockMasterEntity e) => e);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<BlockMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map<BlockMasterDtos>(It.IsAny<BlockMasterEntity>()))
            .Returns((BlockMasterEntity e) => new BlockMasterDtos
            {
                Id = e.Id,
                WardId = e.WardId,
                BlockNo = e.BlockNo,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(20, result.WardId);
        Assert.Equal("BLK001-UPDATED", result.BlockNo);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<BlockMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateBlockMasterDto
        {
            WardId = 10,
            BlockNo = "BLK001",
            IsActive = true,
            UpdatedBy = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlockMasterEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<BlockMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateBlock_ReturnsUpdatedDto()
    {
        // Arrange
        var existingEntity = new BlockMasterEntity
        {
            Id = 1,
            WardId = 10,
            BlockNo = "BLK001",
            IsActive = true
        };

        var updateDto = new UpdateBlockMasterDto
        {
            WardId = 10,
            BlockNo = "BLK001",
            IsActive = false,
            UpdatedBy = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateBlockMasterDto>(), It.IsAny<BlockMasterEntity>()))
            .Callback<UpdateBlockMasterDto, BlockMasterEntity>((dto, entity) =>
            {
                entity.IsActive = dto.IsActive;
                entity.UpdatedBy = dto.UpdatedBy;
            })
            .Returns((UpdateBlockMasterDto _, BlockMasterEntity e) => e);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<BlockMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map<BlockMasterDtos>(It.IsAny<BlockMasterEntity>()))
            .Returns((BlockMasterEntity e) => new BlockMasterDtos
            {
                Id = e.Id,
                WardId = e.WardId,
                BlockNo = e.BlockNo,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsFalse()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlockMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(999, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
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
            .ReturnsAsync((BlockMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(invalidId, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Edge Cases and Business Logic Tests

    [Fact]
    public async Task CreateAsync_NullWardId_ReturnsCreatedDto()
    {
        // Arrange - Testing optional WardId
        var createDto = new CreateBlockMasterDto
        {
            WardId = 0,
            BlockNo = "BLK-ORPHAN",
            IsActive = true,
            CreatedBy = 1
        };

        var createdEntity = new BlockMasterEntity
        {
            Id = 1,
            WardId = 0,
            BlockNo = "BLK-ORPHAN",
            IsActive = true,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<BlockMasterEntity>(It.IsAny<CreateBlockMasterDto>()))
            .Returns(new BlockMasterEntity
            {
                WardId = createDto.WardId,
                BlockNo = createDto.BlockNo,
                IsActive = createDto.IsActive,
                CreatedBy = createDto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<BlockMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdEntity);

        _mockMapper
            .Setup(m => m.Map<BlockMasterDtos>(It.IsAny<BlockMasterEntity>()))
            .Returns(new BlockMasterDtos
            {
                Id = 1,
                WardId = 0,
                BlockNo = "BLK-ORPHAN",
                IsActive = true
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // Assert.Null(result.WardId); // Removed: xUnit2002 - WardId is int (value type), cannot be null
        Assert.Equal("BLK-ORPHAN", result.BlockNo);
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleFilters_ReturnsCorrectEntities()
    {
        // Arrange
        var entities = new List<BlockMasterEntity>
        {
            new() { Id = 1, WardId = 10, BlockNo = "BLK001", IsActive = true },
            new() { Id = 2, WardId = 10, BlockNo = "BLK002", IsActive = false },
            new() { Id = 3, WardId = 20, BlockNo = "BLK001", IsActive = true },
            new() { Id = 4, WardId = 10, BlockNo = "BLK003", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BlockMasterEntity, BlockMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new BlockMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new BlockQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            WardId = 10,
            IsActive = true
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item =>
        {
            Assert.Equal(10, item.WardId);
            Assert.True(item.IsActive);
        });
    }

    [Fact]
    public async Task GetAllAsync_LargeDataset_HandlesPerformanceGracefully()
    {
        // Arrange - Large dataset test
        var entities = Enumerable.Range(1, 1000)
            .Select(i => new BlockMasterEntity
            {
                Id = i,
                WardId = (i % 50) + 1,
                BlockNo = $"BLK{i:0000}",
                IsActive = i % 3 != 0 // Every 3rd block is inactive
            })
            .ToList();

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BlockMasterEntity, BlockMasterDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new BlockMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new BlockQueryParameters
        {
            PageNumber = 1,
            PageSize = 50
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1000, result.TotalCount);
        Assert.Equal(50, result.Items.Count());
        }
    }

    #endregion