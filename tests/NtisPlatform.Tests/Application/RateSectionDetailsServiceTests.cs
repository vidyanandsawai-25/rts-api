using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

public class RateSectionDetailsServiceTests
{
    private readonly Mock<IRepository<RateSectionDetailsEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly RateSectionDetailsService _service;

    public RateSectionDetailsServiceTests()
    {
        _mockRepository = new Mock<IRepository<RateSectionDetailsEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        // NOT calling SaveChangesAsync directly.
        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new RateSectionDetailsService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new RateSectionDetailsEntity
        {
            RateSectionDetailsID = 1,
            RateSectionNo = "WKD",
            WardNo = "WKD1",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 31,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 31
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<RateSectionDetailsDto>(It.IsAny<RateSectionDetailsEntity>()))
            .Returns(new RateSectionDetailsDto
            {
                RateSectionDetailsID = 1,
                RateSectionNo = "WKD",
                WardNo = "WKD1",
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.RateSectionDetailsID);
        Assert.Equal("WKD", result.RateSectionNo);
        Assert.Equal("WKD1", result.WardNo);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RateSectionDetailsEntity?)null);

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
            new() { RateSectionDetailsID = 1, RateSectionNo = "MSH", WardNo = "MSH1",  IsActive = true, CreatedBy = 31, CreatedDate = DateTime.Now,UpdatedBy = 31, UpdatedDate = DateTime.Now },
            new() { RateSectionDetailsID = 2, RateSectionNo = "TRG", WardNo = "TRG1",  IsActive = true, CreatedBy = 31, CreatedDate = DateTime.Now,UpdatedBy = 31, UpdatedDate = DateTime.Now  },
        };

        var mockQuery = entities.BuildMock(); // async IQueryable
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<RateSectionDetailsEntity, RateSectionDetailsDto>();
        });

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new RateSectionDetailsService(
            _mockRepository.Object,
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
        Assert.Contains(items, x => x.RateSectionNo == "MSH");
        Assert.Contains(items, x => x.RateSectionNo == "TRG");
        Assert.Contains(items, x => x.WardNo == "MSH1");
        Assert.Contains(items, x => x.WardNo == "TRG1");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateRateSectionDetailsDto
        {
            RateSectionNo = "WKD",
            WardNo = "WKD1",
            IsActive = true,
            CreatedBy = 31
        };

        _mockMapper
            .Setup(m => m.Map<RateSectionDetailsEntity>(It.IsAny<CreateRateSectionDetailsDto>()))
            .Returns((CreateRateSectionDetailsDto dto) => new RateSectionDetailsEntity
            {
                RateSectionDetailsID = 1,
                RateSectionNo = "WKD",
                WardNo = "WKD1",
                IsActive = true,
                CreatedDate = DateTime.Now,
                CreatedBy = 31,
                UpdatedDate = DateTime.Now,
                UpdatedBy = 31
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RateSectionDetailsEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RateSectionDetailsEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<RateSectionDetailsDto>(It.IsAny<RateSectionDetailsEntity>()))
            .Returns((RateSectionDetailsEntity e) => new RateSectionDetailsDto
            {
                RateSectionDetailsID = e.RateSectionDetailsID,
                RateSectionNo = e.RateSectionNo,
                WardNo = e.WardNo,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate,
                UpdatedDate = e.UpdatedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.RateSectionDetailsID);
        Assert.Equal("WKD", result.RateSectionNo);
        Assert.Equal("WKD1", result.WardNo);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RateSectionDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);

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
        var updateDto = new UpdateRateSectionDetailsDto
        {
            RateSectionNo = "WKD",
            WardNo = "WKD1",
            IsActive = true,
            UpdatedBy = 31
        };

        var existingEntity = new RateSectionDetailsEntity
        {
            RateSectionDetailsID = 1,
            RateSectionNo = "MSH",
            WardNo = "MSH1",
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
                dest.RateSectionNo = src.RateSectionNo;
                dest.WardNo = src.WardNo;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
            });

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RateSectionDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal("WKD", existingEntity.RateSectionNo);
        Assert.Equal("WKD1", existingEntity.WardNo);
        Assert.True(existingEntity.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
    {
        // Arrange
        var updateDto = new UpdateRateSectionDetailsDto
        {
            RateSectionNo = "WKD",
            WardNo = "WKD1",
            IsActive = true,
            UpdatedBy = 31
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RateSectionDetailsEntity?)null);

        // Act
        await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RateSectionDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Never);

        // No commit / save if entity doesn't exist
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
        int idToDelete = 999;

        var existingEntity = new RateSectionDetailsEntity
        {
            RateSectionDetailsID = idToDelete,
            RateSectionNo = "MSH",
            WardNo = "MSH1",
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

