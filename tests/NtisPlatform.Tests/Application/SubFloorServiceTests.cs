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

    public class SubFloorServiceTests
{
    private readonly Mock<IRepository<SubFloorEntity, string>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly SubFloorService _service;

    public SubFloorServiceTests()
    {
        _mockRepository = new Mock<IRepository<SubFloorEntity, string>>();
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

        _service = new SubFloorService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new SubFloorEntity
        {
            SubFloorId = "1",
            SubFloorDescription = "1 st",
            SubFloorDescriptionEnglish = "1 st",
            SubFloorPercentage = 2,
            CreatedDate = DateTime.Now,
            CreatedBy = 31,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 31,
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync("1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<SubFloorDto>(It.IsAny<SubFloorEntity>()))
            .Returns(new SubFloorDto
            {
                SubFloorId = "1",
                SubFloorDescription = "1 st",
                SubFloorDescriptionEnglish = "1 st",
                SubFloorPercentage = 2,
                IsActive = true
            });

        // Act
        var result = await _service.GetByIdAsync("1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1", result.SubFloorId);
        Assert.Equal("1 st", result.SubFloorDescription);
        Assert.Equal("1 st", result.SubFloorDescriptionEnglish);
        Assert.Equal(2, result.SubFloorPercentage);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync("9999", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubFloorEntity?)null);

        // Act
        var result = await _service.GetByIdAsync("9999");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<SubFloorEntity>
        {
            new() { SubFloorId = "1", SubFloorDescription = "Test1", SubFloorDescriptionEnglish = "Desc1", SubFloorPercentage=1, CreatedBy=31, CreatedDate = DateTime.Now,IsActive=true },
            new() { SubFloorId = "2", SubFloorDescription = "Test2", SubFloorDescriptionEnglish = "Desc2", SubFloorPercentage=2, CreatedBy=31, CreatedDate = DateTime.Now,IsActive=true },
        };

        var mockQuery = entities.BuildMock(); // async IQueryable
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SubFloorEntity, SubFloorDto>();
        });

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new SubFloorService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new SubFloorQueryParameters
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
        Assert.Contains(items, x => x.SubFloorId == "1");
        Assert.Contains(items, x => x.SubFloorId == "2");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateSubFloorDto
        {
            SubFloorId = "1",
            SubFloorDescription = "New Description",
            SubFloorDescriptionEnglish = "New English Description",
            SubFloorPercentage = 1
        };

        _mockMapper
            .Setup(m => m.Map<SubFloorEntity>(It.IsAny<CreateSubFloorDto>()))
            .Returns((CreateSubFloorDto dto) => new SubFloorEntity
            {
                SubFloorId = dto.SubFloorId,
                SubFloorDescription = dto.SubFloorDescription,
                SubFloorDescriptionEnglish = dto.SubFloorDescriptionEnglish,
                SubFloorPercentage = dto.SubFloorPercentage,
                CreatedBy = 31,
                CreatedDate = DateTime.Now
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<SubFloorEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubFloorEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<SubFloorDto>(It.IsAny<SubFloorEntity>()))
            .Returns((SubFloorEntity e) => new SubFloorDto
            {
                SubFloorId = e.SubFloorId,
                SubFloorDescription = e.SubFloorDescription,
                SubFloorDescriptionEnglish = e.SubFloorDescriptionEnglish,
                SubFloorPercentage = e.SubFloorPercentage
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1", result.SubFloorId);
        Assert.Equal("New Description", result.SubFloorDescription);
        Assert.Equal("New English Description", result.SubFloorDescriptionEnglish);
        Assert.Equal(1, result.SubFloorPercentage);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<SubFloorEntity>(), It.IsAny<CancellationToken>()), Times.Once);

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
        var updateDto = new UpdateSubFloorDto
        {
            SubFloorId = "1",
            SubFloorDescription = "New Description",
            SubFloorDescriptionEnglish = "New English Description",
            SubFloorPercentage = 1,
            IsActive = true
        };

        var existingEntity = new SubFloorEntity
        {
            SubFloorId = "1",
            SubFloorDescription = "Old Description",
            SubFloorDescriptionEnglish = "Old English Description",
            SubFloorPercentage = 1,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync("1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<SubFloorEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateSubFloorDto>(), It.IsAny<SubFloorEntity>()))
            .Callback((UpdateSubFloorDto src, SubFloorEntity dest) =>
            {
                dest.SubFloorDescription = src.SubFloorDescription;
                dest.SubFloorDescriptionEnglish = src.SubFloorDescriptionEnglish;
            });

        // Act
        await _service.UpdateAsync("1", updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync("1", It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<SubFloorEntity>(), It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal("New Description", existingEntity.SubFloorDescription);
        Assert.Equal("New English Description", existingEntity.SubFloorDescriptionEnglish);
        Assert.Equal("1", existingEntity.SubFloorId);
        Assert.Equal(1, existingEntity.SubFloorPercentage);
        Assert.True(existingEntity.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
    {
        // Arrange
        var updateDto = new UpdateSubFloorDto
        {
            SubFloorId = "1",
            SubFloorDescription = "Description",
            SubFloorDescriptionEnglish = "English Description",
            SubFloorPercentage = 1,
            IsActive = true,
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync("9999", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubFloorEntity?)null);

        // Act
        await _service.UpdateAsync("9999", updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<SubFloorEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        var idToDelete = "9999";

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubFloorEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        var idToDelete = "A";

        var existingEntity = new SubFloorEntity
        {
            SubFloorId = "1",
            SubFloorDescription = "Old Description",
            SubFloorDescriptionEnglish = "Old English Description",
            SubFloorPercentage = 1,
            IsActive = true,
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
