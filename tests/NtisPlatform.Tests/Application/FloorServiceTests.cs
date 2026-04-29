using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using ValidationResult = NtisPlatform.Application.Models.ValidationResult;

namespace NtisPlatform.Tests.Application;

public class FloorServiceTests
{
    private readonly Mock<IRepository<FloorEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly FloorService _service;

    public FloorServiceTests()
    {
        _mockRepository = new Mock<IRepository<FloorEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();

        // Service is calling SaveChangesAsync (NOT transactions), so setup SaveChangesAsync.
        // If your SaveChangesAsync returns Task (not Task<int>), change ReturnsAsync(1) to Returns(Task.CompletedTask).
        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Optional: keep these setups if your interface has them (harmless even if not called)
        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new FloorService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new FloorEntity
        {
            Id = 1,
            FloorCode = "1",
            Description = "1 st",
            MaxFloorNo = 2,
            SequenceNo = 1,
            CreatedDate = DateTime.Now,
            CreatedBy = 31,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 31,
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<FloorDto>(It.IsAny<FloorEntity>()))
            .Returns(new FloorDto
            {
                Id = 1,
                FloorCode = "1",
                Description = "1 st",
                MaxFloorNo = 2,
                SequenceNo = 1,
                IsActive = true
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1", result.FloorCode);
        Assert.Equal("1 st", result.Description);
        Assert.Equal(2, result.MaxFloorNo);
        Assert.Equal(1, result.SequenceNo);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FloorEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(9999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<FloorEntity>
        {
            new() { Id = 1,  FloorCode = "1",Description = "Test1", MaxFloorNo=1,  SequenceNo=1, CreatedBy=31, CreatedDate = DateTime.Now,IsActive=true },
            new() { Id = 2,  FloorCode = "2",Description = "Test2", MaxFloorNo=2,  SequenceNo=2, CreatedBy=31, CreatedDate = DateTime.Now ,IsActive=true},
        };

        var mockQuery = entities.BuildMock(); // async IQueryable
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FloorEntity, FloorDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new FloorService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var qp = new FloorQueryParameters
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
        Assert.Contains(items, x => x.FloorCode == "1");
        Assert.Contains(items, x => x.FloorCode == "2");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateFloorDto
        {
            FloorCode = "1",
            Description = "New Description",
            MaxFloorNo = 1,
            SequenceNo = 1,
            IsActive = true,
        };

        _mockMapper
            .Setup(m => m.Map<FloorEntity>(It.IsAny<CreateFloorDto>()))
            .Returns((CreateFloorDto dto) => new FloorEntity
            {
                FloorCode = dto.FloorCode,
                Description = dto.Description,
                MaxFloorNo = dto.MaxFloorNo,
                SequenceNo = dto.SequenceNo,
                CreatedBy = 31,
                CreatedDate = DateTime.Now,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FloorEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<FloorDto>(It.IsAny<FloorEntity>()))
            .Returns((FloorEntity e) => new FloorDto
            {
                FloorCode = e.FloorCode,
                Description = e.Description,
                MaxFloorNo = e.MaxFloorNo,
                SequenceNo = e.SequenceNo,
                IsActive = e.IsActive,
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1", result.FloorCode);
        Assert.Equal("New Description", result.Description);
        Assert.Equal(1, result.MaxFloorNo);
        Assert.Equal(1, result.SequenceNo);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()), Times.Once);

        // Service calls SaveChangesAsync (based on your test output)
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Not called by service (based on your test output)
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateFloorDto
        {
            FloorCode = "1",
            Description = "New Description",
            MaxFloorNo = 1,
            SequenceNo = 1,
            IsActive = true,
        };

        var existingEntity = new FloorEntity
        {
            Id = 1,
            FloorCode = "1",
            Description = "Old Description",
            MaxFloorNo = 1,
            SequenceNo = 1,
            IsActive = true,
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateFloorDto>(), It.IsAny<FloorEntity>()))
            .Callback((UpdateFloorDto src, FloorEntity dest) =>
            {
                dest.Description = src.Description;
            });

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal("New Description", existingEntity.Description);
        Assert.Equal("1", existingEntity.FloorCode);
        Assert.Equal(1, existingEntity.MaxFloorNo);
        Assert.Equal(1, existingEntity.SequenceNo);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
    {
        // Arrange
        var updateDto = new UpdateFloorDto
        {
            FloorCode = "1",
            Description = "Description",
            MaxFloorNo = 1,
            SequenceNo = 1,
            IsActive = true,
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FloorEntity?)null);

        // Act
        await _service.UpdateAsync(9999, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        int idToDelete = 9999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FloorEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()), Times.Never);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        int idToDelete = 1;

        var existingEntity = new FloorEntity
        {
            Id = idToDelete,
            FloorCode = "1",
            Description = "Old Description",
            MaxFloorNo = 1,
            SequenceNo = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<FloorEntity>(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #region Bulk Operations Tests

    [Fact]
    public async Task BulkCreateAsync_EmptyArray_ReturnsEmptyResult()
    {
        // Arrange
        var items = Array.Empty<CreateFloorDto>();

        // Act
        var result = await _service.BulkCreateAsync(items, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Results);
        Assert.True(result.AllSucceeded);

        _mockRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<FloorEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkCreateAsync_ValidItems_CreatesAllAndReturnsSuccessResult()
    {
        // Arrange
        var createDtos = new[]
        {
            new CreateFloorDto { FloorCode = "F1", Description = "Floor 1", MaxFloorNo = 1, SequenceNo = 1, IsActive = true },
            new CreateFloorDto { FloorCode = "F2", Description = "Floor 2", MaxFloorNo = 2, SequenceNo = 2, IsActive = true },
            new CreateFloorDto { FloorCode = "F3", Description = "Floor 3", MaxFloorNo = 3, SequenceNo = 3, IsActive = true }
        };

        _mockMapper
            .Setup(m => m.Map<FloorEntity>(It.IsAny<CreateFloorDto>()))
            .Returns((CreateFloorDto dto) => new FloorEntity
            {
                FloorCode = dto.FloorCode,
                Description = dto.Description,
                MaxFloorNo = dto.MaxFloorNo,
                SequenceNo = dto.SequenceNo,
                IsActive = dto.IsActive
            });

        _mockRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<FloorEntity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map<FloorDto[]>(It.IsAny<FloorEntity[]>()))
            .Returns((FloorEntity[] entities) => entities.Select(e => new FloorDto
            {
                Id = e.Id,
                FloorCode = e.FloorCode,
                Description = e.Description,
                MaxFloorNo = e.MaxFloorNo,
                SequenceNo = e.SequenceNo,
                IsActive = e.IsActive
            }).ToArray());

        _mockMapper
            .Setup(m => m.Map<List<FloorDto>>(It.IsAny<List<FloorEntity>>()))
            .Returns((List<FloorEntity> entities) => entities.Select(e => new FloorDto
            {
                Id = e.Id,
                FloorCode = e.FloorCode,
                Description = e.Description,
                MaxFloorNo = e.MaxFloorNo,
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

        Assert.Contains(result.Results, r => r.FloorCode == "F1");
        Assert.Contains(result.Results, r => r.FloorCode == "F2");
        Assert.Contains(result.Results, r => r.FloorCode == "F3");

        _mockRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<FloorEntity>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkUpdateAsync_EmptyArray_ReturnsEmptyResult()
    {
        // Arrange
        var items = Array.Empty<BulkUpdateItem<int, UpdateFloorDto>>();

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
            new BulkUpdateItem<int, UpdateFloorDto>(1, new UpdateFloorDto { FloorCode = "F1", Description = "Updated 1", MaxFloorNo = 10, SequenceNo = 1, IsActive = true }),
            new BulkUpdateItem<int, UpdateFloorDto>(2, new UpdateFloorDto { FloorCode = "F2", Description = "Updated 2", MaxFloorNo = 20, SequenceNo = 2, IsActive = true })
        };

        var existingEntities = new Dictionary<int, FloorEntity>
        {
            { 1, new FloorEntity { Id = 1, FloorCode = "F1", Description = "Old 1", MaxFloorNo = 1, SequenceNo = 1, IsActive = true } },
            { 2, new FloorEntity { Id = 2, FloorCode = "F2", Description = "Old 2", MaxFloorNo = 2, SequenceNo = 2, IsActive = true } }
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => existingEntities.GetValueOrDefault(id));

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateFloorDto>(), It.IsAny<FloorEntity>()))
            .Callback((UpdateFloorDto src, FloorEntity dest) =>
            {
                dest.FloorCode = src.FloorCode;
                dest.Description = src.Description;
                dest.MaxFloorNo = src.MaxFloorNo;
                dest.SequenceNo = src.SequenceNo;
                dest.IsActive = src.IsActive;
            });

        _mockMapper
            .Setup(m => m.Map<List<FloorDto>>(It.IsAny<List<FloorEntity>>()))
            .Returns((List<FloorEntity> entities) => entities.Select(e => new FloorDto
            {
                Id = e.Id,
                FloorCode = e.FloorCode,
                Description = e.Description,
                MaxFloorNo = e.MaxFloorNo,
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
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkUpdateAsync_MixedExistingAndNonExisting_ReturnsPartialSuccess()
    {
        // Arrange
        var updateItems = new[]
        {
            new BulkUpdateItem<int, UpdateFloorDto>(1, new UpdateFloorDto { FloorCode = "F1", Description = "Updated 1", MaxFloorNo = 10, SequenceNo = 1, IsActive = true }),
            new BulkUpdateItem<int, UpdateFloorDto>(9999, new UpdateFloorDto { FloorCode = "FX", Description = "Not Found", MaxFloorNo = 99, SequenceNo = 99, IsActive = true }),
            new BulkUpdateItem<int, UpdateFloorDto>(2, new UpdateFloorDto { FloorCode = "F2", Description = "Updated 2", MaxFloorNo = 20, SequenceNo = 2, IsActive = true })
        };

        var existingEntities = new Dictionary<int, FloorEntity>
        {
            { 1, new FloorEntity { Id = 1, FloorCode = "F1", Description = "Old 1", MaxFloorNo = 1, SequenceNo = 1, IsActive = true } },
            { 2, new FloorEntity { Id = 2, FloorCode = "F2", Description = "Old 2", MaxFloorNo = 2, SequenceNo = 2, IsActive = true } }
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => existingEntities.GetValueOrDefault(id));

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateFloorDto>(), It.IsAny<FloorEntity>()))
            .Callback((UpdateFloorDto src, FloorEntity dest) =>
            {
                dest.Description = src.Description;
            });

        _mockMapper
            .Setup(m => m.Map<List<FloorDto>>(It.IsAny<List<FloorEntity>>()))
            .Returns((List<FloorEntity> entities) => entities.Select(e => new FloorDto
            {
                Id = e.Id,
                FloorCode = e.FloorCode,
                Description = e.Description,
                MaxFloorNo = e.MaxFloorNo,
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
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkUpdateAsync_AllNonExisting_ReturnsAllFailures()
    {
        // Arrange
        var updateItems = new[]
        {
            new BulkUpdateItem<int, UpdateFloorDto>(9998, new UpdateFloorDto { FloorCode = "FX", Description = "Not Found 1", MaxFloorNo = 1, SequenceNo = 1, IsActive = true }),
            new BulkUpdateItem<int, UpdateFloorDto>(9999, new UpdateFloorDto { FloorCode = "FY", Description = "Not Found 2", MaxFloorNo = 2, SequenceNo = 2, IsActive = true })
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FloorEntity?)null);

        _mockMapper
            .Setup(m => m.Map<List<FloorDto>>(It.IsAny<List<FloorEntity>>()))
            .Returns((List<FloorEntity> entities) => entities.Select(e => new FloorDto()).ToList());

        // Act
        var result = await _service.BulkUpdateAsync(updateItems, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(2, result.FailedCount);
        Assert.Empty(result.Results);
        Assert.False(result.AllSucceeded);
        Assert.True(result.HasFailures);
        Assert.NotNull(result.Errors);
        Assert.Equal(2, result.Errors.Count);

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()), Times.Never);
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

        var existingEntities = new Dictionary<int, FloorEntity>
        {
            { 1, new FloorEntity { Id = 1, FloorCode = "F1", Description = "Floor 1" } },
            { 2, new FloorEntity { Id = 2, FloorCode = "F2", Description = "Floor 2" } },
            { 3, new FloorEntity { Id = 3, FloorCode = "F3", Description = "Floor 3" } }
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => existingEntities.GetValueOrDefault(id));

        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<FloorEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()))
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
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkDeleteAsync_MixedExistingAndNonExisting_ReturnsPartialSuccess()
    {
        // Arrange
        var idsToDelete = new[] { 1, 9999, 2 };

        var existingEntities = new Dictionary<int, FloorEntity>
        {
            { 1, new FloorEntity { Id = 1, FloorCode = "F1", Description = "Floor 1" } },
            { 2, new FloorEntity { Id = 2, FloorCode = "F2", Description = "Floor 2" } }
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => existingEntities.GetValueOrDefault(id));

        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<FloorEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()))
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
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkDeleteAsync_AllNonExisting_ReturnsAllFailures()
    {
        // Arrange
        var idsToDelete = new[] { 9998, 9999 };

        _mockRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FloorEntity?)null);

        // Act
        var result = await _service.BulkDeleteAsync(idsToDelete, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(2, result.FailedCount);
        Assert.Empty(result.Results);
        Assert.False(result.AllSucceeded);
        Assert.True(result.HasFailures);
        Assert.NotNull(result.Errors);
        Assert.Equal(2, result.Errors.Count);

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateFromRange_CallsServiceAndReturnsOkObjectResult()
    {
        // Arrange
        var mockService = new Mock<IFloorService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<FloorController>>();
        var controller = new FloorController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var request = new RangeCreateRequest<CreateFloorDto>
        {
            RangeFrom = "1",
            RangeTo = "3",
            Template = new CreateFloorDto { FloorCode = "F", Description = "Floor", MaxFloorNo = 1, SequenceNo = 1, IsActive = true }
        };
        var rangeResult = new RangeResult<FloorDto>(3, 1, null);

        // Mock both overloads
        mockService
            .Setup(s => s.CreateFromRangeAsync(
                It.IsAny<RangeCreateRequest<CreateFloorDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rangeResult);

        mockService
            .Setup(s => s.CreateFromRangeAsync(
                It.IsAny<RangeCreateRequest<CreateFloorDto>>(),
                It.IsAny<Func<CreateFloorDto, string, int, CreateFloorDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rangeResult);

        // Act
        var result = await controller.CreateFromRange(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<NtisPlatform.Application.Models.ApiResponse<RangeResult<FloorDto>>>(okResult.Value);
        Assert.Same(rangeResult, apiResponse.Items);

        mockService.Verify(s => s.CreateFromRangeAsync(
            It.IsAny<RangeCreateRequest<CreateFloorDto>>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    #endregion

    #region Reference Validation Tests

    [Fact]
    public async Task UpdateAsync_DeactivateWithReferences_ThrowsValidationException()
    {
        // Arrange
        var updateDto = new UpdateFloorDto
        {
            FloorCode = "1",
            Description = "Floor 1",
            MaxFloorNo = 1,
            SequenceNo = 1,
            IsActive = false
        };

        var existingEntity = new FloorEntity
        {
            Id = 1,
            FloorCode = "1",
            Description = "Floor 1",
            MaxFloorNo = 1,
            SequenceNo = 1,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateFloorDto>(), It.IsAny<FloorEntity>()))
            .Callback((UpdateFloorDto src, FloorEntity dest) =>
            {
                dest.IsActive = src.IsActive;
                dest.Description = src.Description;
            });

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<FloorEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot deactivate Floor. It is referenced by other records."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, updateDto, CancellationToken.None));

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateWithoutReferences_Succeeds()
    {
        // Arrange
        var updateDto = new UpdateFloorDto
        {
            FloorCode = "1",
            Description = "Floor 1",
            MaxFloorNo = 1,
            SequenceNo = 1,
            IsActive = false
        };

        var existingEntity = new FloorEntity
        {
            Id = 1,
            FloorCode = "1",
            Description = "Floor 1",
            MaxFloorNo = 1,
            SequenceNo = 1,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateFloorDto>(), It.IsAny<FloorEntity>()))
            .Callback((UpdateFloorDto src, FloorEntity dest) =>
            {
                dest.IsActive = src.IsActive;
                dest.Description = src.Description;
            });

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<FloorEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.False(existingEntity.IsActive);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ThrowsValidationException()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new FloorEntity
        {
            Id = idToDelete,
            FloorCode = "1",
            Description = "Floor 1",
            MaxFloorNo = 1,
            SequenceNo = 1,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<FloorEntity>(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot delete Floor. It is referenced by other records."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(idToDelete, CancellationToken.None));

        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithoutReferences_Succeeds()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new FloorEntity
        {
            Id = idToDelete,
            FloorCode = "1",
            Description = "Floor 1",
            MaxFloorNo = 1,
            SequenceNo = 1,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<FloorEntity>(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<FloorEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}