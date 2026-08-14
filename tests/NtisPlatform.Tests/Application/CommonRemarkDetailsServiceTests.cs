using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Master.CommonRemarkDetails;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

public class CommonRemarkDetailsServiceTests
{
    private readonly Mock<IRepository<CommonRemarkDetailsEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly CommonRemarkDetailsService _service;

    public CommonRemarkDetailsServiceTests()
    {
        _mockRepository = new Mock<IRepository<CommonRemarkDetailsEntity, int>>();
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

        _service = new CommonRemarkDetailsService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new CommonRemarkDetailsEntity
        {
            Id = 1,
            RemarkTypeId = 1,
            Remark = "Test Remark",
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<CommonRemarkDetailsDtos>(It.IsAny<CommonRemarkDetailsEntity>()))
            .Returns(new CommonRemarkDetailsDtos
            {
                Id = 1,
                RemarkTypeId = 1,
                Remark = "Test Remark",
                IsActive = true
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(1, result.RemarkTypeId);
        Assert.Equal("Test Remark", result.Remark);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<CommonRemarkDetailsDtos>(entity), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommonRemarkDetailsEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<CommonRemarkDetailsDtos>(It.IsAny<CommonRemarkDetailsEntity>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetByIdAsync_InvalidId_ReturnsNull(int invalidId)
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommonRemarkDetailsEntity?)null);

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
        var entities = new List<CommonRemarkDetailsEntity>
        {
            new() { Id = 1, RemarkTypeId = 1, Remark = "Remark 1", IsActive = true },
            new() { Id = 2, RemarkTypeId = 1, Remark = "Remark 2", IsActive = true },
            new() { Id = 3, RemarkTypeId = 2, Remark = "Remark 3", IsActive = false }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CommonRemarkDetailsEntity, CommonRemarkDetailsDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new CommonRemarkDetailsService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new CommonRemarkDetailsQueryParameters
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
        Assert.Contains(items, x => x.Remark == "Remark 1");
        Assert.Contains(items, x => x.Remark == "Remark 2");
        Assert.Contains(items, x => x.Remark == "Remark 3");
    }

    [Fact]
    public async Task GetAllAsync_WithRemarkTypeIdFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<CommonRemarkDetailsEntity>
        {
            new() { Id = 1, RemarkTypeId = 1, Remark = "Remark Type 1 - Item 1", IsActive = true },
            new() { Id = 2, RemarkTypeId = 1, Remark = "Remark Type 1 - Item 2", IsActive = true },
            new() { Id = 3, RemarkTypeId = 2, Remark = "Remark Type 2 - Item 1", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CommonRemarkDetailsEntity, CommonRemarkDetailsDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new CommonRemarkDetailsService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new CommonRemarkDetailsQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            RemarkTypeId = 1
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 2);
        Assert.All(result.Items, item => Assert.Equal(1, item.RemarkTypeId));
    }

    [Fact]
    public async Task GetAllAsync_WithRemarkTextFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<CommonRemarkDetailsEntity>
        {
            new() { Id = 1, RemarkTypeId = 1, Remark = "Payment delayed", IsActive = true },
            new() { Id = 2, RemarkTypeId = 1, Remark = "Payment completed", IsActive = true },
            new() { Id = 3, RemarkTypeId = 2, Remark = "Document submitted", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CommonRemarkDetailsEntity, CommonRemarkDetailsDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new CommonRemarkDetailsService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new CommonRemarkDetailsQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            Remark = "Payment"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 2);
        Assert.All(result.Items, item => Assert.Contains("Payment", item.Remark, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var entities = new List<CommonRemarkDetailsEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CommonRemarkDetailsEntity, CommonRemarkDetailsDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new CommonRemarkDetailsService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new CommonRemarkDetailsQueryParameters
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
            .Select(i => new CommonRemarkDetailsEntity
            {
                Id = i,
                RemarkTypeId = 1,
                Remark = $"Remark {i}",
                IsActive = true
            })
            .ToList();

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CommonRemarkDetailsEntity, CommonRemarkDetailsDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new CommonRemarkDetailsService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new CommonRemarkDetailsQueryParameters
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
        var entities = new List<CommonRemarkDetailsEntity>
        {
            new() { Id = 1, RemarkTypeId = 1, Remark = "Payment completed", IsActive = true },
            new() { Id = 2, RemarkTypeId = 1, Remark = "Document verified", IsActive = true },
            new() { Id = 3, RemarkTypeId = 2, Remark = "Payment pending", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CommonRemarkDetailsEntity, CommonRemarkDetailsDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new CommonRemarkDetailsService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new CommonRemarkDetailsQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "Payment"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Contains("Payment", item.Remark));
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateCommonRemarkDetailsDto
        {
            RemarkTypeId = 1,
            Remark = "New test remark",
            IsActive = true,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<CommonRemarkDetailsEntity>(It.IsAny<CreateCommonRemarkDetailsDto>()))
            .Returns((CreateCommonRemarkDetailsDto dto) => new CommonRemarkDetailsEntity
            {
                RemarkTypeId = dto.RemarkTypeId,
                Remark = dto.Remark,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<CommonRemarkDetailsEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommonRemarkDetailsEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<CommonRemarkDetailsDtos>(It.IsAny<CommonRemarkDetailsEntity>()))
            .Returns((CommonRemarkDetailsEntity e) => new CommonRemarkDetailsDtos
            {
                Id = e.Id,
                RemarkTypeId = e.RemarkTypeId,
                Remark = e.Remark,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(1, result.RemarkTypeId);
        Assert.Equal("New test remark", result.Remark);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(
            It.Is<CommonRemarkDetailsEntity>(e => e.Remark == "New test remark"),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InactiveRemark_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateCommonRemarkDetailsDto
        {
            RemarkTypeId = 2,
            Remark = "Inactive remark",
            IsActive = false
        };

        _mockMapper
            .Setup(m => m.Map<CommonRemarkDetailsEntity>(It.IsAny<CreateCommonRemarkDetailsDto>()))
            .Returns(new CommonRemarkDetailsEntity
            {
                Id = 0,
                RemarkTypeId = 2,
                Remark = "Inactive remark",
                IsActive = false
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<CommonRemarkDetailsEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommonRemarkDetailsEntity e, CancellationToken _) =>
            {
                e.Id = 2;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<CommonRemarkDetailsDtos>(It.IsAny<CommonRemarkDetailsEntity>()))
            .Returns(new CommonRemarkDetailsDtos
            {
                Id = 2,
                RemarkTypeId = 2,
                Remark = "Inactive remark",
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
    public async Task CreateAsync_LongRemarkText_CreatesSuccessfully()
    {
        // Arrange
        var longText = new string('A', 300); // Max length
        var createDto = new CreateCommonRemarkDetailsDto
        {
            RemarkTypeId = 1,
            Remark = longText,
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<CommonRemarkDetailsEntity>(It.IsAny<CreateCommonRemarkDetailsDto>()))
            .Returns(new CommonRemarkDetailsEntity
            {
                RemarkTypeId = 1,
                Remark = longText,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<CommonRemarkDetailsEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommonRemarkDetailsEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<CommonRemarkDetailsDtos>(It.IsAny<CommonRemarkDetailsEntity>()))
            .Returns(new CommonRemarkDetailsDtos
            {
                Id = 1,
                RemarkTypeId = 1,
                Remark = longText,
                IsActive = true
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(300, result.Remark.Length);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateCommonRemarkDetailsDto
        {
            RemarkTypeId = 1,
            Remark = "Updated remark text",
            IsActive = true,
            UpdatedBy = 1
        };

        var existingEntity = new CommonRemarkDetailsEntity
        {
            Id = 1,
            RemarkTypeId = 1,
            Remark = "Old remark text",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<CommonRemarkDetailsEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateCommonRemarkDetailsDto>(), It.IsAny<CommonRemarkDetailsEntity>()))
            .Callback((UpdateCommonRemarkDetailsDto src, CommonRemarkDetailsEntity dest) =>
            {
                dest.Remark = src.Remark;
                dest.RemarkTypeId = src.RemarkTypeId;
            });

        _mockMapper
            .Setup(m => m.Map<CommonRemarkDetailsDtos>(It.IsAny<CommonRemarkDetailsEntity>()))
            .Returns((CommonRemarkDetailsEntity e) => new CommonRemarkDetailsDtos
            {
                Id = e.Id,
                RemarkTypeId = e.RemarkTypeId,
                Remark = e.Remark,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated remark text", result.Remark);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ChangeRemarkTypeId_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateCommonRemarkDetailsDto
        {
            RemarkTypeId = 2,
            Remark = "Test remark",
            IsActive = true,
            UpdatedBy = 1
        };

        var existingEntity = new CommonRemarkDetailsEntity
        {
            Id = 1,
            RemarkTypeId = 1,
            Remark = "Test remark",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<CommonRemarkDetailsEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateCommonRemarkDetailsDto>(), It.IsAny<CommonRemarkDetailsEntity>()))
            .Callback((UpdateCommonRemarkDetailsDto src, CommonRemarkDetailsEntity dest) =>
            {
                dest.RemarkTypeId = src.RemarkTypeId;
            });

        _mockMapper
            .Setup(m => m.Map<CommonRemarkDetailsDtos>(It.IsAny<CommonRemarkDetailsEntity>()))
            .Returns((CommonRemarkDetailsEntity e) => new CommonRemarkDetailsDtos
            {
                Id = e.Id,
                RemarkTypeId = e.RemarkTypeId,
                Remark = e.Remark,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.RemarkTypeId);
        _mockRepository.Verify(r => r.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateCommonRemarkDetailsDto
        {
            RemarkTypeId = 1,
            Remark = "Test remark",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommonRemarkDetailsEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<CommonRemarkDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSavesSuccessfully()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new CommonRemarkDetailsEntity
        {
            Id = idToDelete,
            RemarkTypeId = 1,
            Remark = "Old remark",
            IsActive = false
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<CommonRemarkDetailsEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<CommonRemarkDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ActiveRemark_ShouldStillDelete()
    {
        // Arrange - Even active remarks can be deleted
        var idToDelete = 1;

        var existingEntity = new CommonRemarkDetailsEntity
        {
            Id = idToDelete,
            RemarkTypeId = 1,
            Remark = "Active remark",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<CommonRemarkDetailsEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<CommonRemarkDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommonRemarkDetailsEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<CommonRemarkDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Transaction Tests

    [Fact]
    public async Task CreateAsync_VerifiesNoExplicitTransactionUsed()
    {
        // Arrange
        var createDto = new CreateCommonRemarkDetailsDto
        {
            RemarkTypeId = 1,
            Remark = "Test remark",
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<CommonRemarkDetailsEntity>(It.IsAny<CreateCommonRemarkDetailsDto>()))
            .Returns(new CommonRemarkDetailsEntity());

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<CommonRemarkDetailsEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommonRemarkDetailsEntity { Id = 1 });

        _mockMapper
            .Setup(m => m.Map<CommonRemarkDetailsDtos>(It.IsAny<CommonRemarkDetailsEntity>()))
            .Returns(new CommonRemarkDetailsDtos());

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
        var existingEntity = new CommonRemarkDetailsEntity
        {
            Id = 1,
            RemarkTypeId = 1,
            Remark = "Old text",
            IsActive = true
        };

        var updateDto = new UpdateCommonRemarkDetailsDto
        {
            RemarkTypeId = 1,
            Remark = "New text",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<CommonRemarkDetailsEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateCommonRemarkDetailsDto>(), It.IsAny<CommonRemarkDetailsEntity>()));

        _mockMapper
            .Setup(m => m.Map<CommonRemarkDetailsDtos>(It.IsAny<CommonRemarkDetailsEntity>()))
            .Returns(new CommonRemarkDetailsDtos());

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_VerifiesNoExplicitTransactionUsed()
    {
        // Arrange
        var existingEntity = new CommonRemarkDetailsEntity
        {
            Id = 1,
            RemarkTypeId = 1,
            Remark = "Test",
            IsActive = false
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<CommonRemarkDetailsEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(1, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CreateAsync_WithSpecialCharactersInRemarkText_CreatesSuccessfully()
    {
        // Arrange
        var specialText = "Remark with special chars: @#$%^&*()[]{}|\\;:'\"<>?/";
        var createDto = new CreateCommonRemarkDetailsDto
        {
            RemarkTypeId = 1,
            Remark = specialText,
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<CommonRemarkDetailsEntity>(It.IsAny<CreateCommonRemarkDetailsDto>()))
            .Returns(new CommonRemarkDetailsEntity
            {
                RemarkTypeId = 1,
                Remark = specialText,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<CommonRemarkDetailsEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommonRemarkDetailsEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<CommonRemarkDetailsDtos>(It.IsAny<CommonRemarkDetailsEntity>()))
            .Returns(new CommonRemarkDetailsDtos
            {
                Id = 1,
                RemarkTypeId = 1,
                Remark = specialText,
                IsActive = true
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(specialText, result.Remark);
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleFilters_ReturnsFilteredResults()
    {
        // Arrange
        var entities = new List<CommonRemarkDetailsEntity>
        {
            new() { Id = 1, RemarkTypeId = 1, Remark = "Payment completed", IsActive = true },
            new() { Id = 2, RemarkTypeId = 1, Remark = "Payment pending", IsActive = true },
            new() { Id = 3, RemarkTypeId = 2, Remark = "Document verified", IsActive = true },
            new() { Id = 4, RemarkTypeId = 1, Remark = "Payment failed", IsActive = false }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CommonRemarkDetailsEntity, CommonRemarkDetailsDtos>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new CommonRemarkDetailsService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new CommonRemarkDetailsQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            RemarkTypeId = 1,
            Remark = "Payment"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 2);
        Assert.All(result.Items, item =>
        {
            Assert.Equal(1, item.RemarkTypeId);
            Assert.Contains("Payment", item.Remark);
        });
    }

    #endregion
}
