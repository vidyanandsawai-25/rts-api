using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

public class ZoneServiceTests
{
    private readonly Mock<IRepository<ZoneEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly ZoneService _service;

    public ZoneServiceTests()
    {
        _mockRepository = new Mock<IRepository<ZoneEntity, int>>();
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

        _service = new ZoneService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new ZoneEntity
        {
            Id = 1,

            ZoneNo = "WKD",

            Description = "????",
            SequenceNo = 1,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 31,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 31
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<ZoneDto>(It.IsAny<ZoneEntity>()))
            .Returns((ZoneEntity e) => new ZoneDto
            {
                Id = e.Id,
                ZoneNo = e.ZoneNo,
                Description = e.Description,
                SequenceNo = e.SequenceNo,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate,
                UpdatedDate = e.UpdatedDate
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("WKD", result.ZoneNo);
        Assert.Equal("????", result.Description);
        Assert.Equal(1, result.SequenceNo);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ZoneEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<ZoneEntity>
        {
            new() { Id = 1, ZoneNo = "MSH", Description = "????", SequenceNo = 1, IsActive = true, CreatedBy = 31, CreatedDate = DateTime.Now, UpdatedBy = 31, UpdatedDate = DateTime.Now },
            new() { Id = 2, ZoneNo = "TRG", Description = "??????", SequenceNo = 2, IsActive = true, CreatedBy = 31, CreatedDate = DateTime.Now, UpdatedBy = 31, UpdatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ZoneEntity, ZoneDto>()
                .ForMember(dest => dest.ZoneNo, opt => opt.MapFrom(src => src.ZoneNo));
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new ZoneService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new ZoneQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And,
            SearchTerm = null!,
            SortBy = null!
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);

        Assert.Contains(items, x => x.Description == "????");
        Assert.Contains(items, x => x.Description == "??????");

        Assert.Contains(items, x => x.Id == 1);
        Assert.Contains(items, x => x.Id == 2);
        
        Assert.Contains(items, x => x.ZoneNo == "MSH");
        Assert.Contains(items, x => x.ZoneNo == "TRG");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateZoneDto
        {
            ZoneNo = "WKD",
            Description = "????",
            SequenceNo = 1,
            CreatedBy = 31
        };

        _mockMapper
            .Setup(m => m.Map<ZoneEntity>(It.IsAny<CreateZoneDto>()))
            .Returns((CreateZoneDto dto) => new ZoneEntity
            {
                Id = 1,

                ZoneNo = dto.ZoneNo,

                Description = dto.Description,
                SequenceNo = dto.SequenceNo,
                IsActive = true,
                CreatedDate = DateTime.Now,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<ZoneEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ZoneEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<ZoneDto>(It.IsAny<ZoneEntity>()))
            .Returns((ZoneEntity e) => new ZoneDto
            {

                ZoneNo = e.ZoneNo,
                Description = e.Description,

                Id = e.Id,

                SequenceNo = e.SequenceNo,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("WKD", result.ZoneNo);
        Assert.Equal("????", result.Description);
        Assert.Equal(1, result.SequenceNo);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ZoneEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateZoneDto
        {
            ZoneNo = "MSH",
            Description = "????",
            SequenceNo = 2,
            IsActive = true,
            UpdatedBy = 31
        };

        var existingEntity = new ZoneEntity
        {
            Id = 1,
            ZoneNo = "WKD",
            Description = "????",
            SequenceNo = 1,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 31,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 31
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ZoneEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateZoneDto>(), It.IsAny<ZoneEntity>()))
            .Callback((UpdateZoneDto src, ZoneEntity dest) =>
            {
                dest.ZoneNo = src.ZoneNo;
                dest.Description = src.Description;
                dest.SequenceNo = src.SequenceNo;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
            });

        _mockMapper
            .Setup(m => m.Map<ZoneDto>(It.IsAny<ZoneEntity>()))
            .Returns((ZoneEntity e) => new ZoneDto
            {
                Id = e.Id,
                ZoneNo = e.ZoneNo,
                Description = e.Description,
                SequenceNo = e.SequenceNo,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ZoneEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal("MSH", existingEntity.ZoneNo);
        Assert.Equal("????", existingEntity.Description);
        Assert.Equal(2, existingEntity.SequenceNo);
        Assert.True(existingEntity.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateZoneDto
        {
            ZoneNo = "WKD",
            Description = "????",
            SequenceNo = 1,
            IsActive = true,
            UpdatedBy = 31
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ZoneEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ZoneEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ZoneEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<ZoneEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new ZoneEntity
        {

            Id = 1,
            ZoneNo = "WKD",
            Description = "????",
            SequenceNo = 1,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 31,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 31
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<ZoneEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<ZoneEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #region Bulk Operations Tests

    [Fact]
    public async Task BulkCreateAsync_EmptyArray_ReturnsEmptyResult()
    {
        // Arrange
        var items = Array.Empty<CreateZoneDto>();

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Results);
        Assert.True(result.AllSucceeded);

        _mockRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<ZoneEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkCreateAsync_ValidItems_CreatesAllAndReturnsSuccessResult()
    {
        // Arrange
        var createDtos = new[]
        {
            new CreateZoneDto { ZoneNo = "Z1", Description = "Zone 1", SequenceNo = 1, IsActive = true },
            new CreateZoneDto { ZoneNo = "Z2", Description = "Zone 2", SequenceNo = 2, IsActive = true },
            new CreateZoneDto { ZoneNo = "Z3", Description = "Zone 3", SequenceNo = 3, IsActive = true }
        };

        _mockMapper
            .Setup(m => m.Map<ZoneEntity>(It.IsAny<CreateZoneDto>()))
            .Returns((CreateZoneDto dto) => new ZoneEntity
            {
                ZoneNo = dto.ZoneNo,
                Description = dto.Description,
                SequenceNo = dto.SequenceNo,
                IsActive = dto.IsActive
            });

        _mockRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<ZoneEntity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map<ZoneDto[]>(It.IsAny<ZoneEntity[]>()))
            .Returns((ZoneEntity[] entities) => entities.Select(e => new ZoneDto
            {
                Id = e.Id,
                ZoneNo = e.ZoneNo,
                Description = e.Description,
                SequenceNo = e.SequenceNo,
                IsActive = e.IsActive
            }).ToArray());

        _mockMapper
            .Setup(m => m.Map<List<ZoneDto>>(It.IsAny<List<ZoneEntity>>()))
            .Returns((List<ZoneEntity> entities) => entities.Select(e => new ZoneDto
            {
                Id = e.Id,
                ZoneNo = e.ZoneNo,
                Description = e.Description,
                SequenceNo = e.SequenceNo,
                IsActive = e.IsActive
            }).ToList());

        // Act
        var result = await _service.BulkCreateAsync(createDtos, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(3, result.Results.Count);
        Assert.True(result.AllSucceeded);
        Assert.False(result.HasFailures);
        Assert.Null(result.Errors);

        Assert.Contains(result.Results, r => r.ZoneNo == "Z1");
        Assert.Contains(result.Results, r => r.ZoneNo == "Z2");
        Assert.Contains(result.Results, r => r.ZoneNo == "Z3");

        _mockRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<ZoneEntity>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkUpdateAsync_EmptyArray_ReturnsEmptyResult()
    {
        // Arrange
        var items = Array.Empty<BulkUpdateItem<int, UpdateZoneDto>>();

        // Act
        var result = await _service.BulkUpdateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Results);
        Assert.True(result.AllSucceeded);

        _mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkUpdateAsync_AllExistingEntities_UpdatesAllSuccessfully()
    {
        // Arrange
        var updateItems = new[]
        {
            new BulkUpdateItem<int, UpdateZoneDto>(1, new UpdateZoneDto { ZoneNo = "Z1", Description = "Updated 1", SequenceNo = 10, IsActive = true }),
            new BulkUpdateItem<int, UpdateZoneDto>(2, new UpdateZoneDto { ZoneNo = "Z2", Description = "Updated 2", SequenceNo = 20, IsActive = true })
        };

        var existingEntities = new Dictionary<int, ZoneEntity>
        {
            { 1, new ZoneEntity { Id = 1, ZoneNo = "Z1", Description = "Old 1", SequenceNo = 1, IsActive = true } },
            { 2, new ZoneEntity { Id = 2, ZoneNo = "Z2", Description = "Old 2", SequenceNo = 2, IsActive = true } }
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => existingEntities.GetValueOrDefault(id));

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ZoneEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateZoneDto>(), It.IsAny<ZoneEntity>()))
            .Callback((UpdateZoneDto src, ZoneEntity dest) =>
            {
                dest.ZoneNo = src.ZoneNo;
                dest.Description = src.Description;
                dest.SequenceNo = src.SequenceNo;
                dest.IsActive = src.IsActive;
            });

        _mockMapper
            .Setup(m => m.Map<List<ZoneDto>>(It.IsAny<List<ZoneEntity>>()))
            .Returns((List<ZoneEntity> entities) => entities.Select(e => new ZoneDto
            {
                Id = e.Id,
                ZoneNo = e.ZoneNo,
                Description = e.Description,
                SequenceNo = e.SequenceNo,
                IsActive = e.IsActive
            }).ToList());

        // Act
        var result = await _service.BulkUpdateAsync(updateItems, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(2, result.Results.Count);
        Assert.True(result.AllSucceeded);
        Assert.False(result.HasFailures);
        Assert.Null(result.Errors);

        Assert.Contains(result.Results, r => r.Description == "Updated 1");
        Assert.Contains(result.Results, r => r.Description == "Updated 2");

        _mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ZoneEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkUpdateAsync_MixedExistingAndNonExisting_ReturnsPartialSuccess()
    {
        // Arrange
        var updateItems = new[]
        {
            new BulkUpdateItem<int, UpdateZoneDto>(1, new UpdateZoneDto { ZoneNo = "Z1", Description = "Updated 1", SequenceNo = 10, IsActive = true }),
            new BulkUpdateItem<int, UpdateZoneDto>(9999, new UpdateZoneDto { ZoneNo = "ZX", Description = "Not Found", SequenceNo = 99, IsActive = true }),
            new BulkUpdateItem<int, UpdateZoneDto>(2, new UpdateZoneDto { ZoneNo = "Z2", Description = "Updated 2", SequenceNo = 20, IsActive = true })
        };

        var existingEntities = new Dictionary<int, ZoneEntity>
        {
            { 1, new ZoneEntity { Id = 1, ZoneNo = "Z1", Description = "Old 1", SequenceNo = 1, IsActive = true } },
            { 2, new ZoneEntity { Id = 2, ZoneNo = "Z2", Description = "Old 2", SequenceNo = 2, IsActive = true } }
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => existingEntities.GetValueOrDefault(id));

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ZoneEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateZoneDto>(), It.IsAny<ZoneEntity>()))
            .Callback((UpdateZoneDto src, ZoneEntity dest) =>
            {
                dest.Description = src.Description;
            });

        _mockMapper
            .Setup(m => m.Map<List<ZoneDto>>(It.IsAny<List<ZoneEntity>>()))
            .Returns((List<ZoneEntity> entities) => entities.Select(e => new ZoneDto
            {
                Id = e.Id,
                ZoneNo = e.ZoneNo,
                Description = e.Description,
                SequenceNo = e.SequenceNo,
                IsActive = e.IsActive
            }).ToList());

        // Act
        var result = await _service.BulkUpdateAsync(updateItems, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(2, result.Results.Count);
        Assert.False(result.AllSucceeded);
        Assert.True(result.HasFailures);
        Assert.NotNull(result.Errors);
        Assert.Single(result.Errors);
        Assert.Contains("9999", result.Errors[0]);

        _mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ZoneEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkDeleteAsync_EmptyArray_ReturnsEmptyResult()
    {
        // Arrange
        var ids = Array.Empty<int>();

        // Act
        var result = await _service.BulkDeleteAsync(ids, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Results);
        Assert.True(result.AllSucceeded);

        _mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkDeleteAsync_AllExistingEntities_DeletesAllSuccessfully()
    {
        // Arrange
        var idsToDelete = new[] { 1, 2, 3 };

        var existingEntities = new Dictionary<int, ZoneEntity>
        {
            { 1, new ZoneEntity { Id = 1, ZoneNo = "Z1", Description = "Zone 1" } },
            { 2, new ZoneEntity { Id = 2, ZoneNo = "Z2", Description = "Zone 2" } },
            { 3, new ZoneEntity { Id = 3, ZoneNo = "Z3", Description = "Zone 3" } }
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => existingEntities.GetValueOrDefault(id));

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<ZoneEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkDeleteAsync(idsToDelete, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Equal(3, result.Results.Count);
        Assert.True(result.AllSucceeded);
        Assert.False(result.HasFailures);
        Assert.Null(result.Errors);

        Assert.Contains(1, result.Results);
        Assert.Contains(2, result.Results);
        Assert.Contains(3, result.Results);

        _mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<ZoneEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkDeleteAsync_MixedExistingAndNonExisting_ReturnsPartialSuccess()
    {
        // Arrange
        var idsToDelete = new[] { 1, 9999, 2 };

        var existingEntities = new Dictionary<int, ZoneEntity>
        {
            { 1, new ZoneEntity { Id = 1, ZoneNo = "Z1", Description = "Zone 1" } },
            { 2, new ZoneEntity { Id = 2, ZoneNo = "Z2", Description = "Zone 2" } }
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => existingEntities.GetValueOrDefault(id));

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<ZoneEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkDeleteAsync(idsToDelete, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(2, result.Results.Count);
        Assert.False(result.AllSucceeded);
        Assert.True(result.HasFailures);
        Assert.NotNull(result.Errors);
        Assert.Single(result.Errors);
        Assert.Contains("9999", result.Errors[0]);

        Assert.Contains(1, result.Results);
        Assert.Contains(2, result.Results);
        Assert.DoesNotContain(9999, result.Results);

        _mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<ZoneEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
