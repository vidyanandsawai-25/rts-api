using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

public class WardServiceTests
{
    private readonly Mock<IRepository<WardEntity, string>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly WardService _service;

    public WardServiceTests()
    {
        _mockRepository = new Mock<IRepository<WardEntity, string>>();
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

        _service = new WardService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new WardEntity
        {
            WardNo = "WKD1",
            ZoneNo ="1",
            Description = "वाकड",
            DescriptionEnglish = "Wakad",
            SequenceNo = 1,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 31,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 31
        };

        _mockRepository.Setup(r => r.GetByIdAsync("WKD1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<WardDto>(It.IsAny<WardEntity>()))
            .Returns(new WardDto
            {
                WardNo = "WKD1",
                ZoneNo = "1",
                Description = "वाकड",
                DescriptionEnglish = "Wakad",
                SequenceNo = 1,
                IsActive = true,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            });

        // Act
        var result = await _service.GetByIdAsync("WKD1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("WKD1", result.WardNo);
        Assert.Equal("वाकड", result.Description);
        Assert.Equal("Wakad", result.DescriptionEnglish);
        Assert.Equal(1, result.SequenceNo);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync("ZZZZ", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WardEntity?)null);

        // Act
        var result = await _service.GetByIdAsync("ZZZZ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<WardEntity>
        {
            new() { WardNo = "MSH",ZoneNo="1", Description = "मोशी", DescriptionEnglish = "Moshi", SequenceNo = 1, IsActive = true, CreatedBy = 31, CreatedDate = DateTime.Now, UpdatedBy=31, UpdatedDate=DateTime.Now },
            new() { WardNo = "TRG",ZoneNo="1", Description = "थेरगाव", DescriptionEnglish = "Thergav", SequenceNo = 2, IsActive = true, CreatedBy = 31, CreatedDate = DateTime.Now, UpdatedBy=31, UpdatedDate=DateTime.Now },
        };

        var mockQuery = entities.BuildMock(); // async IQueryable
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<WardEntity, WardDto>();
        });

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new WardService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new WardQueryParameters
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
        Assert.Contains(items, x => x.WardNo == "MSH");
        Assert.Contains(items, x => x.WardNo == "TRG");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateWardDto
        {
            WardNo = "WKD1",
            ZoneNo = "1",
            Description = "वाकड",
            DescriptionEnglish = "Wakad",
            SequenceNo = 1,
            IsActive = true,
            CreatedBy = 31
        };

        _mockMapper
            .Setup(m => m.Map<WardEntity>(It.IsAny<CreateWardDto>()))
            .Returns((CreateWardDto dto) => new WardEntity
            {
                WardNo = dto.WardNo,
                ZoneNo = dto.ZoneNo,
                Description = dto.Description,
                DescriptionEnglish = dto.DescriptionEnglish,
                SequenceNo = dto.SequenceNo,
                IsActive = dto.IsActive,
                CreatedDate = DateTime.Now,
                CreatedBy = 31,
                UpdatedDate = DateTime.Now,
                UpdatedBy = 31
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<WardEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WardEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<WardDto>(It.IsAny<WardEntity>()))
            .Returns((WardEntity e) => new WardDto
            {
                WardNo = e.WardNo,
                ZoneNo = e.ZoneNo,
                Description = e.Description,
                DescriptionEnglish = e.DescriptionEnglish,
                SequenceNo = e.SequenceNo,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate,
                UpdatedDate = e.UpdatedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("WKD1", result.WardNo);
        Assert.Equal("1", result.ZoneNo);
        Assert.Equal("वाकड", result.Description);
        Assert.Equal("Wakad", result.DescriptionEnglish);
        Assert.Equal(1, result.SequenceNo);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<WardEntity>(), It.IsAny<CancellationToken>()), Times.Once);

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
        var updateDto = new UpdateWardDto
        {
            Description = "मोशी",
            ZoneNo = "1",
            DescriptionEnglish = "Moshi",
            SequenceNo = 1,
            IsActive = true,
            UpdatedBy = 31
        };

        var existingEntity = new WardEntity
        {
            Description = "वाकड",
            ZoneNo = "1",
            DescriptionEnglish = "Wakad",
            SequenceNo = 1,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 31,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 31
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync("WKD1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<WardEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateWardDto>(), It.IsAny<WardEntity>()))
            .Callback((UpdateWardDto src, WardEntity dest) =>
            {
                dest.Description = src.Description;
                dest.ZoneNo = src.ZoneNo;
                dest.DescriptionEnglish = src.DescriptionEnglish;
                dest.SequenceNo = src.SequenceNo;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
                dest.UpdatedDate = DateTime.Now;
            });

        // Act
        await _service.UpdateAsync("WKD1", updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync("WKD1", It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<WardEntity>(), It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal("मोशी", existingEntity.Description);
        Assert.Equal("1", existingEntity.ZoneNo);
        Assert.Equal("Moshi", existingEntity.DescriptionEnglish);
        Assert.Equal(1, existingEntity.SequenceNo);
        Assert.True(existingEntity.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
    {
        // Arrange
        var updateDto = new UpdateWardDto
        {
            Description = "वाकड",
            ZoneNo = "1",
            DescriptionEnglish = "Wakad",
            SequenceNo = 1,
            IsActive = true,
            UpdatedBy = 31
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync("ZZZ", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WardEntity?)null);

        // Act
        await _service.UpdateAsync("ZZZ", updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<WardEntity>(), It.IsAny<CancellationToken>()), Times.Never);

        // No commit / save if entity doesn't exist
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        var idToDelete = "ZZZ";

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WardEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        var idToDelete = "WKD1";

        var existingEntity = new WardEntity
        {
            WardNo = idToDelete,
            ZoneNo = "1",
            Description = "वाकड",
            DescriptionEnglish = "Wakad",
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