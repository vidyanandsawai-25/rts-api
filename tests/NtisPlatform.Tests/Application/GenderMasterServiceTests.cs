using AutoMapper;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Master.GenderMaster;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class GenderMasterServiceTests
{
    private readonly Mock<IRepository<GenderMasterEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly GenderMasterService _service;

    public GenderMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<GenderMasterEntity, int>>();
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

        _service = new GenderMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new GenderMasterEntity
        {
            Id = 1,
            GenderName = "Male",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<GenderMasterDtos>(It.IsAny<GenderMasterEntity>()))
            .Returns(new GenderMasterDtos
            {
                Id = 1,
                GenderName = "Male",
                IsActive = true
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Male", result.GenderName);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<GenderMasterDtos>(entity), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GenderMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<GenderMasterDtos>(It.IsAny<GenderMasterEntity>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetByIdAsync_InvalidId_ReturnsNull(int invalidId)
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GenderMasterEntity?)null);

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
        var entities = new List<GenderMasterEntity>
        {
            new() { Id = 1, GenderName = "Male", IsActive = true },
            new() { Id = 2, GenderName = "Female", IsActive = true },
            new() { Id = 3, GenderName = "Other", IsActive = false }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<GenderMasterEntity, GenderMasterDtos>();
        });

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new GenderMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new GenderQueryParameters
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
        Assert.Contains(items, x => x.GenderName == "Male");
        Assert.Contains(items, x => x.GenderName == "Female");
        Assert.Contains(items, x => x.GenderName == "Other");
    }

    [Fact]
    public async Task GetAllAsync_WithGenderNameFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<GenderMasterEntity>
        {
            new() { Id = 1, GenderName = "Male", IsActive = true },
            new() { Id = 2, GenderName = "Female", IsActive = true },
            new() { Id = 3, GenderName = "Transgender", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<GenderMasterEntity, GenderMasterDtos>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new GenderMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new GenderQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            GenderName = "Female"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item =>
             Assert.Equal("Female", item.GenderName, ignoreCase: true));
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var entities = new List<GenderMasterEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<GenderMasterEntity, GenderMasterDtos>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new GenderMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new GenderQueryParameters
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
            .Select(i => new GenderMasterEntity
            {
                Id = i,
                GenderName = $"Gender {i}",
                IsActive = true
            })
            .ToList();

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<GenderMasterEntity, GenderMasterDtos>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new GenderMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new GenderQueryParameters
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
        var entities = new List<GenderMasterEntity>
        {
            new() { Id = 1, GenderName = "Male", IsActive = true },
            new() { Id = 2, GenderName = "Female", IsActive = true },
            new() { Id = 3, GenderName = "Other", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<GenderMasterEntity, GenderMasterDtos>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new GenderMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new GenderQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "Female"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Female", result.Items.Single().GenderName);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateGenderMasterDto
        {
            GenderName = "Male",
            IsActive = true,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<GenderMasterEntity>(It.IsAny<CreateGenderMasterDto>()))
            .Returns((CreateGenderMasterDto dto) => new GenderMasterEntity
            {
                GenderName = dto.GenderName,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<GenderMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GenderMasterEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<GenderMasterDtos>(It.IsAny<GenderMasterEntity>()))
            .Returns((GenderMasterEntity e) => new GenderMasterDtos
            {
                Id = e.Id,
                GenderName = e.GenderName,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Male", result.GenderName);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(
            It.Is<GenderMasterEntity>(e => e.GenderName == "Male"),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InactiveGender_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateGenderMasterDto
        {
            GenderName = "Not Specified",
            IsActive = false
        };

        _mockMapper
            .Setup(m => m.Map<GenderMasterEntity>(It.IsAny<CreateGenderMasterDto>()))
            .Returns(new GenderMasterEntity
            {
                Id = 0,
                GenderName = "Not Specified",
                IsActive = false
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<GenderMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GenderMasterEntity e, CancellationToken _) =>
            {
                e.Id = 2;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<GenderMasterDtos>(It.IsAny<GenderMasterEntity>()))
            .Returns(new GenderMasterDtos
            {
                Id = 2,
                GenderName = "Not Specified",
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
    public async Task CreateAsync_DuplicateGenderName_ThrowsException()
    {
        // Arrange
        var createDto = new CreateGenderMasterDto
        {
            GenderName = "Male",
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<GenderMasterEntity>(It.IsAny<CreateGenderMasterDto>()))
            .Returns(new GenderMasterEntity { GenderName = "Male" });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<GenderMasterEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate GenderName: 'Male' already exists"));

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
        var updateDto = new UpdateGenderMasterDto
        {
            GenderName = "Male - Updated",
            IsActive = true,
            UpdatedBy = 1
        };

        var existingEntity = new GenderMasterEntity
        {
            Id = 1,
            GenderName = "Male",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<GenderMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateGenderMasterDto>(), It.IsAny<GenderMasterEntity>()))
            .Callback((UpdateGenderMasterDto src, GenderMasterEntity dest) =>
            {
                dest.GenderName = src.GenderName;
            });

        _mockMapper
            .Setup(m => m.Map<GenderMasterDtos>(It.IsAny<GenderMasterEntity>()))
            .Returns((GenderMasterEntity e) => new GenderMasterDtos
            {
                Id = e.Id,
                GenderName = e.GenderName,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Male - Updated", result.GenderName);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateGenderMasterDto
        {
            GenderName = "Test Gender",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GenderMasterEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<GenderMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSavesSuccessfully()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new GenderMasterEntity
        {
            Id = idToDelete,
            GenderName = "Old Gender",
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
    public async Task DeleteAsync_ActiveGender_ShouldStillDelete()
    {
        // Arrange - Even active genders can be deleted
        var idToDelete = 1;

        var existingEntity = new GenderMasterEntity
        {
            Id = idToDelete,
            GenderName = "Male",
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
            .ReturnsAsync((GenderMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Transaction Tests

    [Fact]
    public async Task CreateAsync_VerifiesNoExplicitTransactionUsed()
    {
        // Arrange
        var createDto = new CreateGenderMasterDto
        {
            GenderName = "Test Gender",
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<GenderMasterEntity>(It.IsAny<CreateGenderMasterDto>()))
            .Returns(new GenderMasterEntity());

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<GenderMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GenderMasterEntity { Id = 1 });

        _mockMapper
            .Setup(m => m.Map<GenderMasterDtos>(It.IsAny<GenderMasterEntity>()))
            .Returns(new GenderMasterDtos());

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
        var existingEntity = new GenderMasterEntity
        {
            Id = 1,
            GenderName = "Male"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<GenderMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper.Setup(m => m.Map<GenderMasterDtos>(It.IsAny<GenderMasterEntity>()))
            .Returns(new GenderMasterDtos());

        var updateDto = new UpdateGenderMasterDto
        {
            GenderName = "Male Updated"
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
        var entity = new GenderMasterEntity
        {
            Id = 1,
            GenderName = "Male"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<GenderMasterDtos>(It.IsAny<GenderMasterEntity>()))
            .Returns(new GenderMasterDtos());

        // Act
        await _service.GetByIdAsync(1);

        // Assert
        _mockMapper.Verify(m => m.Map<GenderMasterDtos>(entity), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_VerifiesMapperCalledTwice()
    {
        // Arrange
        var createDto = new CreateGenderMasterDto
        {
            GenderName = "Male"
        };

        _mockMapper.Setup(m => m.Map<GenderMasterEntity>(It.IsAny<CreateGenderMasterDto>()))
            .Returns(new GenderMasterEntity());

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<GenderMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GenderMasterEntity { Id = 1 });

        _mockMapper.Setup(m => m.Map<GenderMasterDtos>(It.IsAny<GenderMasterEntity>()))
            .Returns(new GenderMasterDtos());

        // Act
        await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        _mockMapper.Verify(m => m.Map<GenderMasterEntity>(createDto), Times.Once);
        _mockMapper.Verify(m => m.Map<GenderMasterDtos>(It.IsAny<GenderMasterEntity>()), Times.Once);
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task GetAllAsync_OrderedByGenderNameAscending_ReturnsOrderedResults()
    {
        // Arrange
        var entities = new List<GenderMasterEntity>
        {
            new() { Id = 1, GenderName = "Male", IsActive = true },
            new() { Id = 2, GenderName = "Female", IsActive = true },
            new() { Id = 3, GenderName = "Other", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<GenderMasterEntity, GenderMasterDtos>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new GenderMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new GenderQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "GenderName",
            SortOrder = "asc"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.True(items.Count > 0);
        var expectedOrder = items.OrderBy(item => item.GenderName).Select(item => item.GenderName).ToList();
        var actualOrder = items.Select(item => item.GenderName).ToList();
        Assert.Equal(expectedOrder, actualOrder);
    }

    [Fact]
    public async Task GetAllAsync_WithIsActiveFilter_ReturnsOnlyActiveGenders()
    {
        // Arrange
        var entities = new List<GenderMasterEntity>
        {
            new() { Id = 1, GenderName = "Male", IsActive = true },
            new() { Id = 2, GenderName = "Female", IsActive = true },
            new() { Id = 3, GenderName = "Other", IsActive = false }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<GenderMasterEntity, GenderMasterDtos>();
        });

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new GenderMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new GenderQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            IsActive = true
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Items, item => Assert.True(item.IsActive));
    }

    #endregion
}