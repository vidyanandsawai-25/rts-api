using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Tests.Application;

public class RetentionYearWiseServiceTests
{
    private readonly Mock<IRepository<RetentionYearWiseEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly RetentionYearWiseService _service;

    public RetentionYearWiseServiceTests()
    {
        _mockRepository = new Mock<IRepository<RetentionYearWiseEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new RetentionYearWiseService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new RetentionYearWiseEntity
        {
            Id = 1,
            FromYear = 2000,
            ToYear = 2020,
            FactorValue = 1.5,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 1
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<RetentionYearWiseDto>(It.IsAny<RetentionYearWiseEntity>()))
            .Returns(new RetentionYearWiseDto
            {
                Id = 1,
                FromYear = 2000,
                ToYear = 2020,
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
        Assert.Equal(2000, result.FromYear);
        Assert.Equal(2020, result.ToYear);
        Assert.Equal(1.5, result.FactorValue);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetentionYearWiseEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }
    [Theory]
    [InlineData(2000, 2001, true)]
    [InlineData(2000, 2000, true)]
    [InlineData(2001, 2000, false)]
    public void CreateRetentionYearWiseDto_Validate_FromYearToYearLogic(int fromYear, int toYear, bool isValid)
    {
        var dto = new CreateRetentionYearWiseDto
        {
            FromYear = fromYear,
            ToYear = toYear,
            FactorValue = 10
        };

        var context = new ValidationContext(dto, null, null);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(dto, context, results, true);

        if (isValid)
        {
            Assert.True(valid);
            Assert.DoesNotContain(results, r => r.ErrorMessage == "FromYear_MustBeLessThanOrEqualToYear");
        }
        else
        {
            Assert.False(valid);
            Assert.Contains(results, r => r.ErrorMessage == "FromYear_MustBeLessThanOrEqualToYear");
        }
    }

    [Theory]
    [InlineData(2000, 2001, true)]
    [InlineData(2000, 2000, true)]
    [InlineData(2001, 2000, false)]
    public void UpdateRetentionYearWiseDto_Validate_FromYearToYearLogic(int fromYear, int toYear, bool isValid)
    {
        var dto = new UpdateRetentionYearWiseDto
        {
            FromYear = fromYear,
            ToYear = toYear,
            FactorValue = 10
        };

        var context = new ValidationContext(dto, null, null);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(dto, context, results, true);

        if (isValid)
        {
            Assert.True(valid);
            Assert.DoesNotContain(results, r => r.ErrorMessage == "FromYear_MustBeLessThanOrEqualToYear");
        }
        else
        {
            Assert.False(valid);
            Assert.Contains(results, r => r.ErrorMessage == "FromYear_MustBeLessThanOrEqualToYear");
        }
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<RetentionYearWiseEntity>
        {
            new() { Id = 1, FromYear = 2000, ToYear = 2020, FactorValue = 1.5, IsActive = true, CreatedDate = DateTime.Now },
            new() { Id = 2, FromYear = 2021, ToYear = 2030, FactorValue = 2.0, IsActive = false, CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<RetentionYearWiseEntity, RetentionYearWiseDto>();
        });

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new RetentionYearWiseService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new RetentionYearWiseQueryParameters
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
        var createDto = new CreateRetentionYearWiseDto
        {
            FromYear = 2000,
            ToYear = 2020,
            FactorValue = 1.5,
            IsActive = true,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<RetentionYearWiseEntity>(It.IsAny<CreateRetentionYearWiseDto>()))
            .Returns((CreateRetentionYearWiseDto dto) => new RetentionYearWiseEntity
            {
                Id = 1,
                FromYear = dto.FromYear,
                ToYear = dto.ToYear,
                FactorValue = dto.FactorValue,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy ?? 0,
                CreatedDate = DateTime.Now
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RetentionYearWiseEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetentionYearWiseEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<RetentionYearWiseDto>(It.IsAny<RetentionYearWiseEntity>()))
            .Returns((RetentionYearWiseEntity e) => new RetentionYearWiseDto
            {
                Id = e.Id,
                FromYear = e.FromYear,
                ToYear = e.ToYear,
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
        Assert.Equal(2000, result.FromYear);
        Assert.Equal(2020, result.ToYear);
        Assert.Equal(1.5, result.FactorValue);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RetentionYearWiseEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateRetentionYearWiseDto
        {
            FromYear = 2010,
            ToYear = 2025,
            FactorValue = 2.5,
            IsActive = false,
            UpdatedBy = 2
        };

        var existingEntity = new RetentionYearWiseEntity
        {
            Id = 1,
            FromYear = 2000,
            ToYear = 2020,
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
            .Setup(r => r.UpdateAsync(It.IsAny<RetentionYearWiseEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRetentionYearWiseDto>(), It.IsAny<RetentionYearWiseEntity>()))
            .Callback((UpdateRetentionYearWiseDto src, RetentionYearWiseEntity dest) =>
            {
                dest.FromYear = src.FromYear;
                dest.ToYear = src.ToYear;
                dest.FactorValue = src.FactorValue;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy ?? 0;
                dest.UpdatedDate = DateTime.Now;
            });

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetentionYearWiseEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(2010, existingEntity.FromYear);
        Assert.Equal(2025, existingEntity.ToYear);
        Assert.Equal(2.5, existingEntity.FactorValue);
        Assert.False(existingEntity.IsActive);
        Assert.Equal(2, existingEntity.UpdatedBy);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
    {
        // Arrange
        var updateDto = new UpdateRetentionYearWiseDto
        {
            FromYear = 2010,
            ToYear = 2025,
            FactorValue = 2.5,
            IsActive = false,
            UpdatedBy = 2
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetentionYearWiseEntity?)null);

        // Act
        await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetentionYearWiseEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetentionYearWiseEntity?)null);

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

        var existingEntity = new RetentionYearWiseEntity
        {
            Id = idToDelete,
            FromYear = 2000,
            ToYear = 2020,
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
