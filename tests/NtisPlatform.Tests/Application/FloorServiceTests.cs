using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Tests.Application;

public class FloorServiceTests
{
    private readonly Mock<IRepository<FloorEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly FloorService _service;

    public FloorServiceTests()
    {
        _mockRepository = new Mock<IRepository<FloorEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

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

        _service = new FloorService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
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
        });

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new FloorService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

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
            Id=1,
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
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);

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

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

