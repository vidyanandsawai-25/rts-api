using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
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
            ZoneId = 1,
            ZoneNo = "WKD",
            Description = "वाकड",
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
                ZoneId = e.ZoneId,
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
        Assert.Equal(1, result.ZoneId);
        Assert.Equal("WKD", result.ZoneNo);
        Assert.Equal("वाकड", result.Description);
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
            new() { ZoneId = 1, ZoneNo = "MSH", Description = "मोशी", SequenceNo = 1, IsActive = true, CreatedBy = 31, CreatedDate = DateTime.Now, UpdatedBy = 31, UpdatedDate = DateTime.Now },
            new() { ZoneId = 2, ZoneNo = "TRG", Description = "थेरगाव", SequenceNo = 2, IsActive = true, CreatedBy = 31, CreatedDate = DateTime.Now, UpdatedBy = 31, UpdatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ZoneEntity, ZoneDto>();
        });

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
        Assert.Contains(items, x => x.ZoneId == 1);
        Assert.Contains(items, x => x.ZoneId == 2);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateZoneDto
        {
            ZoneNo = "WKD",
            Description = "वाकड",
            SequenceNo = 1,
            CreatedBy = 31
        };

        _mockMapper
            .Setup(m => m.Map<ZoneEntity>(It.IsAny<CreateZoneDto>()))
            .Returns((CreateZoneDto dto) => new ZoneEntity
            {
                ZoneId = 1,
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
                ZoneId = e.ZoneId,
                ZoneNo = e.ZoneNo,
                Description = e.Description,
                SequenceNo = e.SequenceNo,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.ZoneId);
        Assert.Equal("WKD", result.ZoneNo);
        Assert.Equal("वाकड", result.Description);
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
            Description = "मोशी",
            SequenceNo = 2,
            IsActive = true,
            UpdatedBy = 31
        };

        var existingEntity = new ZoneEntity
        {
            ZoneId = 1,
            ZoneNo = "WKD",
            Description = "वाकड",
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
                ZoneId = e.ZoneId,
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
        Assert.Equal("मोशी", existingEntity.Description);
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
            Description = "वाकड",
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
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
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
            ZoneId = idToDelete,
            ZoneNo = "WKD",
            Description = "वाकड",
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