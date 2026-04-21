using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class RetentionFactWiseServiceTests
{
    private readonly Mock<IRepository<RetentionFactWiseEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly RetentionFactWiseService _service;

    public RetentionFactWiseServiceTests()
    {
        _mockRepository = new Mock<IRepository<RetentionFactWiseEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new RetentionFactWiseService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new RetentionFactWiseEntity
        {
            Id = 1,
            FromFactor = 10,
            ToFactor = 20,
            FactorValue = 1.5,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 1
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<RetentionFactWiseDto>(It.IsAny<RetentionFactWiseEntity>()))
            .Returns(new RetentionFactWiseDto
            {
                Id = 1,
                FromFactor = 10,
                ToFactor = 20,
                FactorValue = 1.5,
                IsActive = true,
                CreatedDate = entity.CreatedDate ?? DateTime.Now,
                UpdatedDate = entity.UpdatedDate
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.FromFactor);
        Assert.Equal(20, result.ToFactor);
        Assert.Equal(1.5, result.FactorValue);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetentionFactWiseEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<RetentionFactWiseEntity>
        {
            new() { Id = 1, FromFactor = 10, ToFactor = 20, FactorValue = 1.5, IsActive = true, CreatedDate = DateTime.Now },
            new() { Id = 2, FromFactor = 21, ToFactor = 30, FactorValue = 2.0, IsActive = false, CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<RetentionFactWiseEntity, RetentionFactWiseDto>();
        });

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new RetentionFactWiseService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new RetentionFactWiseQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            // Add other query parameters if needed
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.Id == 1);
        Assert.Contains(items, x => x.Id == 2);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateRetentionFactWiseDto
        {
            FromFactor = 10,
            ToFactor = 20,
            FactorValue = 1.5,
            IsActive = true,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<RetentionFactWiseEntity>(It.IsAny<CreateRetentionFactWiseDto>()))
            .Returns((CreateRetentionFactWiseDto dto) => new RetentionFactWiseEntity
            {
                Id = 1,
                FromFactor = dto.FromFactor,
                ToFactor = dto.ToFactor,
                FactorValue = dto.FactorValue,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy ?? 0,
                CreatedDate = DateTime.Now
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RetentionFactWiseEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetentionFactWiseEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<RetentionFactWiseDto>(It.IsAny<RetentionFactWiseEntity>()))
            .Returns((RetentionFactWiseEntity e) => new RetentionFactWiseDto
            {
                Id = e.Id,
                FromFactor = e.FromFactor,
                ToFactor = e.ToFactor,
                FactorValue = e.FactorValue,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate ?? DateTime.Now,
                UpdatedDate = e.UpdatedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.FromFactor);
        Assert.Equal(20, result.ToFactor);
        Assert.Equal(1.5, result.FactorValue);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RetentionFactWiseEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateRetentionFactWiseDto
        {
            FromFactor = 15,
            ToFactor = 25,
            FactorValue = 2.5,
            IsActive = false,
            UpdatedBy = 2
        };

        var existingEntity = new RetentionFactWiseEntity
        {
            Id = 1,
            FromFactor = 10,
            ToFactor = 20,
            FactorValue = 1.5,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RetentionFactWiseEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRetentionFactWiseDto>(), It.IsAny<RetentionFactWiseEntity>()))
            .Callback((UpdateRetentionFactWiseDto src, RetentionFactWiseEntity dest) =>
            {
                dest.FromFactor = src.FromFactor;
                dest.ToFactor = src.ToFactor;
                dest.FactorValue = src.FactorValue;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy ?? 0;
                dest.UpdatedDate = DateTime.Now;
            });

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetentionFactWiseEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(15, existingEntity.FromFactor);
        Assert.Equal(25, existingEntity.ToFactor);
        Assert.Equal(2.5, existingEntity.FactorValue);
        Assert.False(existingEntity.IsActive);
        Assert.Equal(2, existingEntity.UpdatedBy);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
    {
        // Arrange
        var updateDto = new UpdateRetentionFactWiseDto
        {
            FromFactor = 15,
            ToFactor = 25,
            FactorValue = 2.5,
            IsActive = false,
            UpdatedBy = 2
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetentionFactWiseEntity?)null);

        // Act
        await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetentionFactWiseEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetentionFactWiseEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new RetentionFactWiseEntity
        {
            Id = idToDelete,
            FromFactor = 10,
            ToFactor = 20,
            FactorValue = 1.5,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
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
}
