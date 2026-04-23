using AutoMapper;
using Moq;
using MockQueryable;
using NtisPlatform.Application.DTOs.Master.YearMaster;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

public class YearMasterServiceTests
{
    private readonly Mock<IRepository<YearMasterEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly YearMasterService _service;

    public YearMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<YearMasterEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        // Setup SaveChangesAsync
        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Optional transaction setups
        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new YearMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new YearMasterEntity
        {
            Id = 1,
            Year = 2024,
            YearCode = "2024-25",
            IsActive = true,
            Status = "Active",
            StartDate = new DateTime(2024, 4, 1),
            EndDate = new DateTime(2025, 3, 31),
            Description = "Financial Year 2024-25"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<YearMasterDto>(It.IsAny<YearMasterEntity>()))
            .Returns(new YearMasterDto
            {
                Id = 1,
                Year = 2024,
                YearCode = "2024-25",
                IsActive = true,
                Status = "Active",
                StartDate = new DateTime(2024, 4, 1),
                EndDate = new DateTime(2025, 3, 31),
                Description = "Financial Year 2024-25"
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(2024, result.Year);
        Assert.Equal("2024-25", result.YearCode);
        Assert.True(result.IsActive);
        Assert.Equal("Active", result.Status);
        Assert.Equal(new DateTime(2024, 4, 1), result.StartDate);
        Assert.Equal(new DateTime(2025, 3, 31), result.EndDate);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<YearMasterDto>(entity), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((YearMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<YearMasterDto>(It.IsAny<YearMasterEntity>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetByIdAsync_InvalidId_ReturnsNull(int invalidId)
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((YearMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(invalidId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<YearMasterEntity>
        {
            new() 
            { 
                Id = 1, 
                Year = 2024, 
                YearCode = "2024-25",
                IsActive = true,
                Status = "Active",
                StartDate = new DateTime(2024, 4, 1),
                EndDate = new DateTime(2025, 3, 31)
            },
            new() 
            { 
                Id = 2, 
                Year = 2023, 
                YearCode = "2023-24",
                IsActive = false,
                Status = "Closed",
                StartDate = new DateTime(2023, 4, 1),
                EndDate = new DateTime(2024, 3, 31)
            },
            new() 
            { 
                Id = 3, 
                Year = 2025, 
                YearCode = "2025-26",
                IsActive = false,
                Status = "Future",
                StartDate = new DateTime(2025, 4, 1),
                EndDate = new DateTime(2026, 3, 31)
            }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<YearMasterEntity, YearMasterDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new YearMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new YearMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(3, items.Count);
        Assert.Contains(items, x => x.Year == 2024 && x.YearCode == "2024-25");
        Assert.Contains(items, x => x.Year == 2023 && x.YearCode == "2023-24");
        Assert.Contains(items, x => x.Year == 2025 && x.YearCode == "2025-26");
    }

    [Fact]
    public async Task GetAllAsync_WithYearFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<YearMasterEntity>
        {
            new() { Id = 1, Year = 2024, YearCode = "2024-25", IsActive = true },
            new() { Id = 2, Year = 2023, YearCode = "2023-24", IsActive = false },
            new() { Id = 3, Year = 2025, YearCode = "2025-26", IsActive = false }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<YearMasterEntity, YearMasterDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new YearMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new YearMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            Year = 2024
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item => Assert.Equal(2024, item.Year));
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var entities = new List<YearMasterEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<YearMasterEntity, YearMasterDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new YearMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new YearMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var entities = Enumerable.Range(1, 25)
            .Select(i => new YearMasterEntity
            {
                Id = i,
                Year = 2000 + i,
                YearCode = $"{2000 + i}-{2001 + i}",
                IsActive = i == 24,
                StartDate = new DateTime(2000 + i, 4, 1),
                EndDate = new DateTime(2001 + i, 3, 31)
            })
            .ToList();

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<YearMasterEntity, YearMasterDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new YearMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new YearMasterQueryParameters
        {
            PageNumber = 2,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateYearMasterDto
        {
            Year = 2024,
            YearCode = "2024-25",
            IsActive = true,
            Status = "Active",
            StartDate = new DateTime(2024, 4, 1),
            EndDate = new DateTime(2025, 3, 31),
            Description = "Financial Year 2024-25",
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<YearMasterEntity>(It.IsAny<CreateYearMasterDto>()))
            .Returns((CreateYearMasterDto dto) => new YearMasterEntity
            {
                Year = dto.Year,
                YearCode = dto.YearCode,
                IsActive = dto.IsActive,
                Status = dto.Status,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Description = dto.Description,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<YearMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((YearMasterEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<YearMasterDto>(It.IsAny<YearMasterEntity>()))
            .Returns((YearMasterEntity e) => new YearMasterDto
            {
                Id = e.Id,
                Year = e.Year,
                YearCode = e.YearCode,
                IsActive = e.IsActive,
                Status = e.Status,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Description = e.Description
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(2024, result.Year);
        Assert.Equal("2024-25", result.YearCode);
        Assert.True(result.IsActive);
        Assert.Equal("Active", result.Status);
        Assert.Equal("Financial Year 2024-25", result.Description);

        _mockRepository.Verify(r => r.AddAsync(
            It.Is<YearMasterEntity>(e => e.Year == 2024 && e.YearCode == "2024-25"),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InactiveYear_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateYearMasterDto
        {
            Year = 2023,
            YearCode = "2023-24",
            IsActive = false,
            Status = "Closed",
            StartDate = new DateTime(2023, 4, 1),
            EndDate = new DateTime(2024, 3, 31),
            Description = "Closed Financial Year"
        };

        _mockMapper
            .Setup(m => m.Map<YearMasterEntity>(It.IsAny<CreateYearMasterDto>()))
            .Returns(new YearMasterEntity
            {
                Id = 0,
                Year = 2023,
                YearCode = "2023-24",
                IsActive = false,
                Status = "Closed",
                StartDate = new DateTime(2023, 4, 1),
                EndDate = new DateTime(2024, 3, 31),
                Description = "Closed Financial Year"
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<YearMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((YearMasterEntity e, CancellationToken _) =>
            {
                e.Id = 2;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<YearMasterDto>(It.IsAny<YearMasterEntity>()))
            .Returns(new YearMasterDto
            {
                Id = 2,
                Year = 2023,
                YearCode = "2023-24",
                IsActive = false,
                Status = "Closed"
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        Assert.Equal("Closed", result.Status);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateYearCode_ThrowsException()
    {
        // Arrange
        var createDto = new CreateYearMasterDto
        {
            Year = 2024,
            YearCode = "2024-25",
            IsActive = true,
            Status = "Active",
            StartDate = new DateTime(2024, 4, 1),
            EndDate = new DateTime(2025, 3, 31)
        };

        _mockMapper
            .Setup(m => m.Map<YearMasterEntity>(It.IsAny<CreateYearMasterDto>()))
            .Returns(new YearMasterEntity { Year = 2024, YearCode = "2024-25" });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<YearMasterEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate YearCode: '2024-25' already exists"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_StartDateAfterEndDate_ThrowsException()
    {
        // Arrange
        var createDto = new CreateYearMasterDto
        {
            Year = 2024,
            YearCode = "2024-25",
            IsActive = true,
            Status = "Active",
            StartDate = new DateTime(2025, 4, 1),
            EndDate = new DateTime(2024, 3, 31)  // End date before start date
        };

        _mockMapper
            .Setup(m => m.Map<YearMasterEntity>(It.IsAny<CreateYearMasterDto>()))
            .Returns(new YearMasterEntity 
            { 
                Year = 2024, 
                YearCode = "2024-25",
                StartDate = createDto.StartDate,
                EndDate = createDto.EndDate
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<YearMasterEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("EndDate must be after StartDate"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateYearMasterDto
        {
            Year = 2024,
            YearCode = "2024-25",
            IsActive = true,
            Status = "Active",
            StartDate = new DateTime(2024, 4, 1),
            EndDate = new DateTime(2025, 3, 31),
            Description = "Updated Description",
            UpdatedBy = 1
        };

        var existingEntity = new YearMasterEntity
        {
            Id = 1,
            Year = 2024,
            YearCode = "2024-25",
            IsActive = false,
            Status = "Draft",
            StartDate = new DateTime(2024, 4, 1),
            EndDate = new DateTime(2025, 3, 31),
            Description = "Old Description"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<YearMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateYearMasterDto>(), It.IsAny<YearMasterEntity>()))
            .Callback((UpdateYearMasterDto src, YearMasterEntity dest) =>
            {
                dest.Year = src.Year;
                dest.YearCode = src.YearCode;
                dest.IsActive = src.IsActive;
                dest.Status = src.Status;
                dest.StartDate = src.StartDate;
                dest.EndDate = src.EndDate;
                dest.Description = src.Description;
            });

        _mockMapper
            .Setup(m => m.Map<YearMasterDto>(It.IsAny<YearMasterEntity>()))
            .Returns((YearMasterEntity e) => new YearMasterDto
            {
                Id = e.Id, 
                Year = e.Year,
                YearCode = e.YearCode,
                IsActive = e.IsActive,
                Status = e.Status,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Description = e.Description
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Description", result.Description);
        Assert.True(result.IsActive);
        Assert.Equal("Active", result.Status);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ChangeActiveStatus_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateYearMasterDto
        {
            Year = 2024,
            YearCode = "2024-25",
            IsActive = false,
            Status = "Closed",
            StartDate = new DateTime(2024, 4, 1),
            EndDate = new DateTime(2025, 3, 31)
        };

        var existingEntity = new YearMasterEntity
        {
            Id = 1,
            Year = 2024,
            YearCode = "2024-25",
            IsActive = true,
            Status = "Active",
            StartDate = new DateTime(2024, 4, 1),
            EndDate = new DateTime(2025, 3, 31)
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<YearMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateYearMasterDto>(), It.IsAny<YearMasterEntity>()))
            .Callback((UpdateYearMasterDto src, YearMasterEntity dest) =>
            {
                dest.IsActive = src.IsActive;
                dest.Status = src.Status;
            });

        _mockMapper
            .Setup(m => m.Map<YearMasterDto>(It.IsAny<YearMasterEntity>()))
            .Returns(new YearMasterDto
            {
                Id = 1,
                Year = 2024,
                YearCode = "2024-25",
                IsActive = false,
                Status = "Closed"
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        Assert.Equal("Closed", result.Status);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateYearMasterDto
        {
            Year = 2024,
            YearCode = "2024-25",
            IsActive = true,
            StartDate = new DateTime(2024, 4, 1),
            EndDate = new DateTime(2025, 3, 31)
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((YearMasterEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<YearMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSavesSuccessfully()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new YearMasterEntity
        {
            Id = idToDelete,
            Year = 2023,
            YearCode = "2023-24",
            IsActive = false,
            Status = "Closed"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<YearMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<YearMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((YearMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<YearMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task DeleteAsync_InvalidId_ReturnsFalse(int invalidId)
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((YearMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(invalidId, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<YearMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ActiveYear_ShouldStillDelete()
    {
        // Arrange - Even active years can be deleted
        var idToDelete = 1;

        var existingEntity = new YearMasterEntity
        {
            Id = idToDelete,
            Year = 2024,
            YearCode = "2024-25",
            IsActive = true,
            Status = "Active"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<YearMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<YearMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Transaction Tests

    [Fact]
    public async Task CreateAsync_VerifiesNoExplicitTransactionUsed()
    {
        // Arrange
        var createDto = new CreateYearMasterDto
        {
            Year = 2024,
            YearCode = "2024-25",
            IsActive = true,
            StartDate = new DateTime(2024, 4, 1),
            EndDate = new DateTime(2025, 3, 31)
        };

        _mockMapper
            .Setup(m => m.Map<YearMasterEntity>(It.IsAny<CreateYearMasterDto>()))
            .Returns(new YearMasterEntity());

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<YearMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YearMasterEntity { Id = 1 });

        _mockMapper
            .Setup(m => m.Map<YearMasterDto>(It.IsAny<YearMasterEntity>()))
            .Returns(new YearMasterDto());

        // Act
        await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_VerifiesNoExplicitTransactionUsed()
    {
        // Arrange
        var existingEntity = new YearMasterEntity
        {
            Id = 1,
            Year = 2024,
            YearCode = "2024-25"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<YearMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper.Setup(m => m.Map<YearMasterDto>(It.IsAny<YearMasterEntity>()))
            .Returns(new YearMasterDto());

        var updateDto = new UpdateYearMasterDto 
        { 
            Year = 2024, 
            YearCode = "2024-25",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddYears(1)
        };

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Mapper Tests

    [Fact]
    public async Task GetByIdAsync_VerifiesMapperCalledOnce()
    {
        // Arrange
        var entity = new YearMasterEntity
        {
            Id = 1,
            Year = 2024,
            YearCode = "2024-25"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<YearMasterDto>(It.IsAny<YearMasterEntity>()))
            .Returns(new YearMasterDto());

        // Act
        await _service.GetByIdAsync(1);

        // Assert
        _mockMapper.Verify(m => m.Map<YearMasterDto>(entity), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_VerifiesMapperCalledTwice()
    {
        // Arrange
        var createDto = new CreateYearMasterDto 
        { 
            Year = 2024, 
            YearCode = "2024-25",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddYears(1)
        };

        _mockMapper.Setup(m => m.Map<YearMasterEntity>(It.IsAny<CreateYearMasterDto>()))
            .Returns(new YearMasterEntity());

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<YearMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YearMasterEntity { Id = 1 });

        _mockMapper.Setup(m => m.Map<YearMasterDto>(It.IsAny<YearMasterEntity>()))
            .Returns(new YearMasterDto());

        // Act
        await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        _mockMapper.Verify(m => m.Map<YearMasterEntity>(createDto), Times.Once);
        _mockMapper.Verify(m => m.Map<YearMasterDto>(It.IsAny<YearMasterEntity>()), Times.Once);
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task GetAllAsync_OrderedByYearDescending_ReturnsOrderedResults()
    {
        // Arrange
        var entities = new List<YearMasterEntity>
        {
            new() { Id = 1, Year = 2024, YearCode = "2024-25", IsActive = true },
            new() { Id = 2, Year = 2023, YearCode = "2023-24", IsActive = false },
            new() { Id = 3, Year = 2025, YearCode = "2025-26", IsActive = false }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<YearMasterEntity, YearMasterDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new YearMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new YearMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "Year",
            SortOrder = "desc"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.True(items.Count > 0);
        
        // Verify descending order
        for (int i = 0; i < items.Count - 1; i++)
        {
            Assert.True(items[i].Year >= items[i + 1].Year);
        }
    }

    [Fact]
    public async Task CreateAsync_FiscalYearSpanningTwoCalendarYears_CreatesSuccessfully()
    {
        // Arrange - Typical fiscal year scenario
        var createDto = new CreateYearMasterDto
        {
            Year = 2024,
            YearCode = "2024-25",
            IsActive = true,
            StartDate = new DateTime(2024, 4, 1),  // April 1, 2024
            EndDate = new DateTime(2025, 3, 31),    // March 31, 2025
            Description = "Fiscal Year spanning two calendar years"
        };

        _mockMapper
            .Setup(m => m.Map<YearMasterEntity>(It.IsAny<CreateYearMasterDto>()))
            .Returns((CreateYearMasterDto dto) => new YearMasterEntity
            {
                Year = dto.Year,
                YearCode = dto.YearCode,
                IsActive = dto.IsActive,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Description = dto.Description
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<YearMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((YearMasterEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<YearMasterDto>(It.IsAny<YearMasterEntity>()))
            .Returns((YearMasterEntity e) => new YearMasterDto
            {
                Id = e.Id,
                Year = e.Year,
                YearCode = e.YearCode,
                StartDate = e.StartDate,
                EndDate = e.EndDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(new DateTime(2024, 4, 1), result.StartDate);
        Assert.Equal(new DateTime(2025, 3, 31), result.EndDate);
        Assert.True(result.EndDate > result.StartDate);
    }

    #endregion
}
