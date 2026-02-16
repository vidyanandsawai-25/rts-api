using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.AssessmentYearRange;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

public class AssessmentYearRangeServiceTests
{
    private readonly Mock<IRepository<AssessmentYearRangeEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly AssessmentYearRangeService _service;

    public AssessmentYearRangeServiceTests()
    {
        _mockRepository = new Mock<IRepository<AssessmentYearRangeEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new AssessmentYearRangeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new AssessmentYearRangeEntity
        {
            YearId = 1,
            FromYear = 2000,
            ToYear = 2020,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 1
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<AssessmentYearRangeDto>(It.IsAny<AssessmentYearRangeEntity>()))
            .Returns(new AssessmentYearRangeDto
            {
                YearId = 1,
                FromYear = entity.FromYear,
                ToYear = entity.ToYear,
                IsActive = true,
                CreatedDate = entity.CreatedDate ?? DateTime.Now,
                UpdatedDate = entity.UpdatedDate
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.YearId);
        Assert.Equal(2000, result.FromYear);
        Assert.Equal(2020, result.ToYear);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssessmentYearRangeEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<AssessmentYearRangeEntity>
        {
            new() { YearId = 1, FromYear = 2000, ToYear = 2020, IsActive = true, CreatedDate = DateTime.Now },
            new() { YearId = 2, FromYear = 2021, ToYear = 2030, IsActive = false, CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<AssessmentYearRangeEntity, AssessmentYearRangeDto>();
        });

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new AssessmentYearRangeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new AssessmentYearRangeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.YearId == 1);
        Assert.Contains(items, x => x.YearId == 2);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateAssessmentYearRangeDto
        {
            FromYear = 2000,
            ToYear = 2020,
            IsActive = true,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<AssessmentYearRangeEntity>(It.IsAny<CreateAssessmentYearRangeDto>()))
            .Returns((CreateAssessmentYearRangeDto dto) => new AssessmentYearRangeEntity
            {
                YearId = 1,
                FromYear = dto.FromYear,
                ToYear = dto.ToYear,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy ?? 0,
                CreatedDate = DateTime.Now
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AssessmentYearRangeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssessmentYearRangeEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<AssessmentYearRangeDto>(It.IsAny<AssessmentYearRangeEntity>()))
            .Returns((AssessmentYearRangeEntity e) => new AssessmentYearRangeDto
            {
                YearId = e.YearId,
                FromYear = e.FromYear,
                ToYear = e.ToYear,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate ?? DateTime.Now,
                UpdatedDate = e.UpdatedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.YearId);
        Assert.Equal(2000, result.FromYear);
        Assert.Equal(2020, result.ToYear);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<AssessmentYearRangeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateAssessmentYearRangeDto
        {
            FromYear = 2010,
            ToYear = 2025,
            IsActive = false,
            UpdatedBy = 2
        };

        var existingEntity = new AssessmentYearRangeEntity
        {
            YearId = 1,
            FromYear = 2000,
            ToYear = 2020,
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
            .Setup(r => r.UpdateAsync(It.IsAny<AssessmentYearRangeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateAssessmentYearRangeDto>(), It.IsAny<AssessmentYearRangeEntity>()))
            .Callback((UpdateAssessmentYearRangeDto src, AssessmentYearRangeEntity dest) =>
            {
                dest.FromYear = src.FromYear;
                dest.ToYear = src.ToYear;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy ?? 0;
                dest.UpdatedDate = DateTime.Now;
            });

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<AssessmentYearRangeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(2010, existingEntity.FromYear);
        Assert.Equal(2025, existingEntity.ToYear);
        Assert.False(existingEntity.IsActive);
        Assert.Equal(2, existingEntity.UpdatedBy);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
    {
        // Arrange
        var updateDto = new UpdateAssessmentYearRangeDto
        {
            FromYear = 2010,
            ToYear = 2025,
            IsActive = false,
            UpdatedBy = 2
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssessmentYearRangeEntity?)null);

        // Act
        await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<AssessmentYearRangeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssessmentYearRangeEntity?)null);

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

        var existingEntity = new AssessmentYearRangeEntity
        {
            YearId = idToDelete,
            FromYear = 2000,
            ToYear = 2020,
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