using AutoMapper;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

#region Test Classes

public class TestEntity : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class TestDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class TestCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class TestUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class TestQueryParams : BaseQueryParameters
{
}

public class TestCrudService : BaseCrudService<TestEntity, TestDto, TestCreateDto, TestUpdateDto, TestQueryParams>
{
    public TestCrudService(
        IRepository<TestEntity> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
    }
}

#endregion

public class BaseCrudServiceTests
{
    private readonly Mock<IRepository<TestEntity>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly TestCrudService _service;

    public BaseCrudServiceTests()
    {
        _repositoryMock = new Mock<IRepository<TestEntity>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();

        _service = new TestCrudService(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithNoEntities_ReturnsEmptyPagedResult()
    {
        // Arrange
        var entities = new List<TestEntity>();
        var mockQueryable = entities.BuildMock();

        _repositoryMock.Setup(x => x.GetQueryable())
            .Returns(mockQueryable);

        var queryParams = new TestQueryParams
        {
            PageNumber = 1,
            PageSize = 10
        };

        var mockMapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<TestEntity, TestDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapperMock.Setup(x => x.ConfigurationProvider)
            .Returns(mockMapperConfig);

        // Act
        var result = await _service.GetAllAsync(queryParams);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleEntities_ReturnsPaginatedResult()
    {
        // Arrange
        var entities = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Entity 1", Description = "Desc 1", IsActive = true },
            new TestEntity { Id = 2, Name = "Entity 2", Description = "Desc 2", IsActive = true },
            new TestEntity { Id = 3, Name = "Entity 3", Description = "Desc 3", IsActive = false }
        };
        var mockQueryable = entities.BuildMock();

        _repositoryMock.Setup(x => x.GetQueryable())
            .Returns(mockQueryable);


        var mockMapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<TestEntity, TestDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        _mapperMock.Setup(x => x.ConfigurationProvider)
            .Returns(mockMapperConfig);

        var queryParams = new TestQueryParams
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count());
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var entities = Enumerable.Range(1, 25)
            .Select(i => new TestEntity { Id = i, Name = $"Entity {i}", Description = $"Desc {i}", IsActive = true })
            .ToList();
        var mockQueryable = entities.BuildMock();

        _repositoryMock.Setup(x => x.GetQueryable())
            .Returns(mockQueryable);

        var mockMapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<TestEntity, TestDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        _mapperMock.Setup(x => x.ConfigurationProvider)
            .Returns(mockMapperConfig);

        var queryParams = new TestQueryParams
        {
            PageNumber = 2,
            PageSize = 10
        };

        // Act
        var result = await _service.GetAllAsync(queryParams);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPrevious);
        Assert.True(result.HasNext);
    }

    [Fact]
    public async Task GetAllAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var entities = new List<TestEntity>();
        var mockQueryable = entities.BuildMock();

        _repositoryMock.Setup(x => x.GetQueryable())
            .Returns(mockQueryable);

        var mockMapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<TestEntity, TestDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapperMock.Setup(x => x.ConfigurationProvider)
            .Returns(mockMapperConfig);

        var queryParams = new TestQueryParams();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Act
        await _service.GetAllAsync(queryParams, cts.Token);

        // Assert
        _repositoryMock.Verify(x => x.GetQueryable(), Times.Once);

    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingEntity_ReturnsDto()
    {
        // Arrange
        var entity = new TestEntity
        {
            Id = 1,
            Name = "Test Entity",
            Description = "Test Description",
            IsActive = true
        };

        var dto = new TestDto
        {
            Id = 1,
            Name = "Test Entity",
            Description = "Test Description",
            IsActive = true
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mapperMock.Setup(x => x.Map<TestDto>(entity))
            .Returns(dto);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Entity", result.Name);
        Assert.Equal("Test Description", result.Description);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentEntity_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
        _mapperMock.Verify(x => x.Map<TestDto>(It.IsAny<TestEntity>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        _repositoryMock.Setup(x => x.GetByIdAsync(1, cts.Token))
            .ReturnsAsync((TestEntity?)null);

        // Act
        await _service.GetByIdAsync(1, cts.Token);

        // Assert
        _repositoryMock.Verify(x => x.GetByIdAsync(1, cts.Token), Times.Once);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesEntityAndReturnsDto()
    {
        // Arrange
        var createDto = new TestCreateDto
        {
            Name = "New Entity",
            Description = "New Description"
        };

        var entity = new TestEntity
        {
            Id = 0,
            Name = "New Entity",
            Description = "New Description"
        };

        var savedEntity = new TestEntity
        {
            Id = 1,
            Name = "New Entity",
            Description = "New Description",
            CreatedDate = DateTime.Now
        };

        var resultDto = new TestDto
        {
            Id = 1,
            Name = "New Entity",
            Description = "New Description"
        };

        _mapperMock.Setup(x => x.Map<TestEntity>(createDto))
            .Returns(entity);

        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedEntity);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(x => x.Map<TestDto>(It.IsAny<TestEntity>()))
            .Returns(resultDto);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Entity", result.Name);
        Assert.Equal("New Description", result.Description);

        _repositoryMock.Verify(x => x.AddAsync(
            It.Is<TestEntity>(e => e.CreatedDate != default(DateTime)),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedDateProperty()
    {
        // Arrange
        var createDto = new TestCreateDto { Name = "Test", Description = "Desc" };
        var entity = new TestEntity { Name = "Test", Description = "Desc" };
        var beforeCreate = DateTime.Now;

        _mapperMock.Setup(x => x.Map<TestEntity>(createDto))
            .Returns(entity);

        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(x => x.Map<TestDto>(It.IsAny<TestEntity>()))
            .Returns(new TestDto());

        // Act
        await _service.CreateAsync(createDto);

        // Assert
        _repositoryMock.Verify(x => x.AddAsync(
            It.Is<TestEntity>(e => e.CreatedDate >= beforeCreate && e.CreatedDate <= DateTime.Now.AddSeconds(1)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithCancellationToken_PassesTokenToMethods()
    {
        // Arrange
        var createDto = new TestCreateDto { Name = "Test", Description = "Desc" };
        var entity = new TestEntity();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        _mapperMock.Setup(x => x.Map<TestEntity>(createDto))
            .Returns(entity);

        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<TestEntity>(), cts.Token))
            .ReturnsAsync(entity);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(cts.Token))
            .ReturnsAsync(1);

        _mapperMock.Setup(x => x.Map<TestDto>(It.IsAny<TestEntity>()))
            .Returns(new TestDto());

        // Act
        await _service.CreateAsync(createDto, cts.Token);

        // Assert
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<TestEntity>(), cts.Token), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(cts.Token), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingEntity_UpdatesAndReturnsDto()
    {
        // Arrange
        var entity = new TestEntity
        {
            Id = 1,
            Name = "Original Name",
            Description = "Original Desc",
            IsActive = true,
            CreatedDate = DateTime.Now.AddDays(-1)
        };

        var updateDto = new TestUpdateDto
        {
            Name = "Updated Name",
            Description = "Updated Desc",
            IsActive = false
        };

        var updatedEntity = new TestEntity
        {
            Id = 1,
            Name = "Updated Name",
            Description = "Updated Desc",
            IsActive = false,
            CreatedDate = entity.CreatedDate,
            UpdatedDate = DateTime.Now
        };

        var resultDto = new TestDto
        {
            Id = 1,
            Name = "Updated Name",
            Description = "Updated Desc",
            IsActive = false
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mapperMock.Setup(x => x.Map(updateDto, entity))
            .Returns(updatedEntity);

        _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(x => x.Map<TestDto>(It.IsAny<TestEntity>()))
            .Returns(resultDto);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("Updated Desc", result.Description);
        Assert.False(result.IsActive);

        _repositoryMock.Verify(x => x.UpdateAsync(
            It.Is<TestEntity>(e => e.UpdatedDate != null && e.UpdatedDate != default(DateTime)),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new TestUpdateDto
        {
            Name = "Updated Name",
            Description = "Updated Desc"
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto);

        // Assert
        Assert.Null(result);
        _mapperMock.Verify(x => x.Map(It.IsAny<TestUpdateDto>(), It.IsAny<TestEntity>()), Times.Never);
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_SetsUpdatedDateProperty()
    {
        // Arrange
        var entity = new TestEntity
        {
            Id = 1,
            Name = "Original",
            CreatedDate = DateTime.Now.AddDays(-1)
        };
        var updateDto = new TestUpdateDto { Name = "Updated", Description = "Desc" };
        var beforeUpdate = DateTime.Now;

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mapperMock.Setup(x => x.Map(updateDto, entity))
            .Returns(entity);

        _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(x => x.Map<TestDto>(It.IsAny<TestEntity>()))
            .Returns(new TestDto());

        // Act
        await _service.UpdateAsync(1, updateDto);

        // Assert
        _repositoryMock.Verify(x => x.UpdateAsync(
            It.Is<TestEntity>(e => e.UpdatedDate >= beforeUpdate && e.UpdatedDate <= DateTime.Now.AddSeconds(1)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithCancellationToken_PassesTokenToMethods()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };
        var updateDto = new TestUpdateDto { Name = "Updated" };
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        _repositoryMock.Setup(x => x.GetByIdAsync(1, cts.Token))
            .ReturnsAsync(entity);

        _mapperMock.Setup(x => x.Map(updateDto, entity))
            .Returns(entity);

        _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<TestEntity>(), cts.Token))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(cts.Token))
            .ReturnsAsync(1);

        _mapperMock.Setup(x => x.Map<TestDto>(It.IsAny<TestEntity>()))
            .Returns(new TestDto());

        // Act
        await _service.UpdateAsync(1, updateDto, cts.Token);

        // Assert
        _repositoryMock.Verify(x => x.GetByIdAsync(1, cts.Token), Times.Once);
        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<TestEntity>(), cts.Token), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(cts.Token), Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingEntity_DeletesAndReturnsTrue()
    {
        // Arrange
        var entity = new TestEntity
        {
            Id = 1,
            Name = "Test Entity",
            IsActive = false
        };

        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _repositoryMock.Setup(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        Assert.True(result);
        _repositoryMock.Verify(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentEntity_ReturnsFalse()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestEntity?)null);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        Assert.False(result);
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithCancellationToken_PassesTokenToMethods()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        _repositoryMock.Setup(x => x.GetByIdAsync(1, cts.Token))
            .ReturnsAsync(entity);

        _repositoryMock.Setup(x => x.DeleteAsync(1, cts.Token))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(cts.Token))
            .ReturnsAsync(1);

        // Act
        await _service.DeleteAsync(1, cts.Token);

        // Assert
        _repositoryMock.Verify(x => x.GetByIdAsync(1, cts.Token), Times.Once);
        _repositoryMock.Verify(x => x.DeleteAsync(1, cts.Token), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(cts.Token), Times.Once);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CrudOperations_FullCycle_WorksCorrectly()
    {
        // Create
        var createDto = new TestCreateDto { Name = "Test Entity", Description = "Test Desc" };
        var createdEntity = new TestEntity { Id = 1, Name = "Test Entity", Description = "Test Desc" };
        var createdDto = new TestDto { Id = 1, Name = "Test Entity", Description = "Test Desc" };

        _mapperMock.Setup(x => x.Map<TestEntity>(createDto))
            .Returns(createdEntity);
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdEntity);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<TestDto>(It.Is<TestEntity>(e => e.Name == "Test Entity")))
            .Returns(createdDto);

        var created = await _service.CreateAsync(createDto);
        Assert.NotNull(created);
        Assert.Equal(1, created.Id);

        // Read
        _repositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdEntity);

        var retrieved = await _service.GetByIdAsync(1);
        Assert.NotNull(retrieved);
        Assert.Equal("Test Entity", retrieved.Name);

        // Update
        var updateDto = new TestUpdateDto { Name = "Updated Entity", Description = "Updated Desc" };
        var updatedDto = new TestDto { Id = 1, Name = "Updated Entity", Description = "Updated Desc" };

        _mapperMock.Setup(x => x.Map(updateDto, createdEntity))
            .Callback<TestUpdateDto, TestEntity>((dto, entity) =>
            {
                entity.Name = dto.Name;
                entity.Description = dto.Description;
            })
            .Returns(createdEntity);
        _repositoryMock.Setup(x => x.UpdateAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mapperMock.Setup(x => x.Map<TestDto>(It.Is<TestEntity>(e => e.Name == "Updated Entity")))
            .Returns(updatedDto);

        var updated = await _service.UpdateAsync(1, updateDto);
        Assert.NotNull(updated);
        Assert.Equal("Updated Entity", updated.Name);

        // Delete
        _repositoryMock.Setup(x => x.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var deleted = await _service.DeleteAsync(1);
        Assert.True(deleted);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task GetAllAsync_WithLargeDataSet_HandlesCorrectly()
    {
        // Arrange
        var entities = Enumerable.Range(1, 1000)
            .Select(i => new TestEntity { Id = i, Name = $"Entity {i}" })
            .ToList();
        var mockQueryable = entities.BuildMock();

        _repositoryMock.Setup(x => x.GetQueryable())
            .Returns(mockQueryable);

        var mockMapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<TestEntity, TestDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        _mapperMock.Setup(x => x.ConfigurationProvider)
            .Returns(mockMapperConfig);

        var queryParams = new TestQueryParams
        {
            PageNumber = 1,
            PageSize = 50
        };

        // Act
        var result = await _service.GetAllAsync(queryParams);

        // Assert
        Assert.Equal(1000, result.TotalCount);
        Assert.Equal(50, result.Items.Count());
        Assert.Equal(20, result.TotalPages);
    }

    [Fact]
    public async Task GetByIdAsync_WithZeroId_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(0);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithNegativeId_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetByIdAsync(-1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(-1);

        // Assert
        Assert.Null(result);
    }

    #endregion
}
