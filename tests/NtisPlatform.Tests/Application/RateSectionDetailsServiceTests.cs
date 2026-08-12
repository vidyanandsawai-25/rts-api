using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

public class RateSectionDetailsServiceTests
{
    private readonly Mock<IRepository<RateSectionDetailsEntity, int>> _mockRepository;
    private readonly Mock<IRepository<WardEntity>> _mockWardRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly RateSectionDetailsService _service;

    public RateSectionDetailsServiceTests()
    {
        _mockRepository = new Mock<IRepository<RateSectionDetailsEntity, int>>();
        _mockWardRepository = new Mock<IRepository<WardEntity>>();
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

        _service = new RateSectionDetailsService(_mockRepository.Object, _mockWardRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new RateSectionDetailsEntity
        {
            Id = 1,
            RateSectionId = 10,
            WardId = 5,
            Ward = new WardEntity { Id = 5, WardNo = "W001" },
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 31,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 31
        };

        var entities = new List<RateSectionDetailsEntity> { entity };
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        _mockWardRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WardEntity { Id = 5, WardNo = "W001" });

        _mockMapper.Setup(m => m.Map<RateSectionDetailsDto>(It.IsAny<RateSectionDetailsEntity>()))
            .Returns((RateSectionDetailsEntity e) => new RateSectionDetailsDto
            {
                Id = e.Id,
                RateSectionId = e.RateSectionId,
                WardId = e.WardId,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate,
                UpdatedDate = e.UpdatedDate
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.RateSectionId);
        Assert.Equal(5, result.WardId);
        Assert.Equal("W001", result.WardNo);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        var entities = new List<RateSectionDetailsEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<RateSectionDetailsEntity>
        {
            new()
            {
                Id = 1,
                RateSectionId = 10,
                WardId = 5,
                Ward = new WardEntity { Id = 5, WardNo = "W001" },
                IsActive = true,
                CreatedBy = 31,
                CreatedDate = DateTime.Now,
                UpdatedBy = 31,
                UpdatedDate = DateTime.Now
            },
            new()
            {
                Id = 2,
                RateSectionId = 20,
                WardId = 6,
                Ward = new WardEntity { Id = 6, WardNo = "W002" },
                IsActive = true,
                CreatedBy = 31,
                CreatedDate = DateTime.Now,
                UpdatedBy = 31,
                UpdatedDate = DateTime.Now
            }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var wards = new List<WardEntity>
        {
            new() { Id = 5, WardNo = "W001" },
            new() { Id = 6, WardNo = "W002" }
        };
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(wards.BuildMock());

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<RateSectionDetailsMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new RateSectionDetailsService(
            _mockRepository.Object,
            _mockWardRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new RateSectionDetailsQueryParameters
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
        Assert.Contains(items, x => x.RateSectionId == 10);
        Assert.Contains(items, x => x.RateSectionId == 20);
        Assert.Contains(items, x => x.WardNo == "W001");
        Assert.Contains(items, x => x.WardNo == "W002");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateRateSectionDetailsDto
        {
            RateSectionId = 10,
            WardId = 5,
            CreatedBy = 31
        };

        _mockMapper
            .Setup(m => m.Map<RateSectionDetailsEntity>(It.IsAny<CreateRateSectionDetailsDto>()))
            .Returns((CreateRateSectionDetailsDto dto) => new RateSectionDetailsEntity
            {
                Id = 1,
                RateSectionId = dto.RateSectionId,
                WardId = dto.WardId,
                IsActive = true,
                CreatedDate = DateTime.Now,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RateSectionDetailsEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RateSectionDetailsEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<RateSectionDetailsDto>(It.IsAny<RateSectionDetailsEntity>()))
            .Returns((RateSectionDetailsEntity e) => new RateSectionDetailsDto
            {
                Id = e.Id,
                RateSectionId = e.RateSectionId,
                WardId = e.WardId,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.RateSectionId);
        Assert.Equal(5, result.WardId);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RateSectionDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateRateSectionDetailsDto
        {
            RateSectionId = 20,
            WardId = 6,
            IsActive = true,
            UpdatedBy = 31
        };

        var existingEntity = new RateSectionDetailsEntity
        {
            Id = 1,
            RateSectionId = 10,
            WardId = 5,
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
            .Setup(r => r.UpdateAsync(It.IsAny<RateSectionDetailsEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRateSectionDetailsDto>(), It.IsAny<RateSectionDetailsEntity>()))
            .Callback((UpdateRateSectionDetailsDto src, RateSectionDetailsEntity dest) =>
            {
                dest.RateSectionId = src.RateSectionId;
                dest.WardId = src.WardId;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
            });

        _mockMapper
            .Setup(m => m.Map<RateSectionDetailsDto>(It.IsAny<RateSectionDetailsEntity>()))
            .Returns((RateSectionDetailsEntity e) => new RateSectionDetailsDto
            {
                Id = e.Id,
                RateSectionId = e.RateSectionId,
                WardId = e.WardId,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RateSectionDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal(20, existingEntity.RateSectionId);
        Assert.Equal(6, existingEntity.WardId);
        Assert.True(existingEntity.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateRateSectionDetailsDto
        {
            RateSectionId = 20,
            WardId = 6,
            IsActive = true,
            UpdatedBy = 31
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RateSectionDetailsEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RateSectionDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        int idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RateSectionDetailsEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        int idToDelete = 1;

        var existingEntity = new RateSectionDetailsEntity
        {
            Id = idToDelete,
            RateSectionId = 10,
            WardId = 5,
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
            .Setup(r => r.DeleteAsync(It.IsAny<RateSectionDetailsEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RateSectionDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
