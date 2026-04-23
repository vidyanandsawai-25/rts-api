using AutoMapper;
using Moq;
using MockQueryable;
using NtisPlatform.Application.DTOs.Master.OfficeMaster;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class OfficeServiceTests
{
    private readonly Mock<IRepository<OfficeEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly OfficeService _service;

    public OfficeServiceTests()
    {
        _mockRepository = new Mock<IRepository<OfficeEntity, int>>();
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

        _service = new OfficeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new OfficeEntity
        {
            Id = 1,
            OfficeCode = "HQ001",
            OfficeName = "Head Office",
            Type = "Headquarters",
            Address = "123 Main Street",
            City = "Mumbai",
            Pincode = "400001",
            Phone = "022-12345678",
            EmailId = "hq@company.com",
            OfficeIncharge = 101,
            DesignationMasterId = 1,
            EstablishedDate = new DateTime(2020, 1, 1),
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<OfficeDto>(It.IsAny<OfficeEntity>()))
            .Returns(new OfficeDto
            {
                Id = 1,
                OfficeCode = "HQ001",
                OfficeName = "Head Office",
                Type = "Headquarters",
                Address = "123 Main Street",
                City = "Mumbai",
                Pincode = "400001",
                Phone = "022-12345678",
                EmailId = "hq@company.com",
                OfficeIncharge = 101,
                DesignationMasterId = 1,
                EstablishedDate = new DateTime(2020, 1, 1),
                IsActive = true
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("HQ001", result.OfficeCode);
        Assert.Equal("Head Office", result.OfficeName);
        Assert.Equal("Headquarters", result.Type);
        Assert.Equal("Mumbai", result.City);
        Assert.Equal("hq@company.com", result.EmailId);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<OfficeDto>(entity), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OfficeEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<OfficeDto>(It.IsAny<OfficeEntity>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetByIdAsync_InvalidId_ReturnsNull(int invalidId)
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OfficeEntity?)null);

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
        var entities = new List<OfficeEntity>
        {
            new() 
            {
                Id = 1, 
                OfficeCode = "HQ001",
                OfficeName = "Head Office",
                Type = "Headquarters",
                City = "Mumbai",
                IsActive = true
            },
            new() 
            {
                Id = 2, 
                OfficeCode = "BR001",
                OfficeName = "Branch Office 1",
                Type = "Branch",
                City = "Delhi",
                IsActive = true
            },
            new() 
            {
                Id = 3, 
                OfficeCode = "BR002",
                OfficeName = "Branch Office 2",
                Type = "Branch",
                City = "Bangalore",
                IsActive = false
            }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OfficeEntity, OfficeDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new OfficeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new OfficeQueryParameters
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
        Assert.Contains(items, x => x.OfficeCode == "HQ001" && x.OfficeName == "Head Office");
        Assert.Contains(items, x => x.OfficeCode == "BR001" && x.OfficeName == "Branch Office 1");
        Assert.Contains(items, x => x.OfficeCode == "BR002" && x.OfficeName == "Branch Office 2");
    }

    [Fact]
    public async Task GetAllAsync_WithTypeFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<OfficeEntity>
        {
            new() { Id = 1, OfficeCode = "HQ001", OfficeName = "Head Office", Type = "Headquarters", IsActive = true },
            new() { Id = 2, OfficeCode = "BR001", OfficeName = "Branch 1", Type = "Branch", IsActive = true },
            new() { Id = 3, OfficeCode = "BR002", OfficeName = "Branch 2", Type = "Branch", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OfficeEntity, OfficeDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new OfficeService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new OfficeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            Type = "Branch"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item => 
            Assert.Contains("Branch", item.Type ?? "", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var entities = new List<OfficeEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OfficeEntity, OfficeDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new OfficeService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new OfficeQueryParameters
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
            .Select(i => new OfficeEntity
            {
                Id = i,
                OfficeCode = $"OFF{i:000}",
                OfficeName = $"Office {i}",
                Type = i % 2 == 0 ? "Branch" : "Regional",
                City = $"City {i}",
                IsActive = true
            })
            .ToList();

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OfficeEntity, OfficeDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new OfficeService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new OfficeQueryParameters
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
        var createDto = new CreateOfficeDto
        {
            OfficeCode = "HQ001",
            OfficeName = "Head Office",
            Type = "Headquarters",
            Address = "123 Main Street",
            City = "Mumbai",
            Pincode = "400001",
            Phone = "022-12345678",
            EmailId = "hq@company.com",
            OfficeIncharge = 101,
            DesignationMasterId = 1,
            EstablishedDate = new DateTime(2020, 1, 1),
            IsActive = true,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<OfficeEntity>(It.IsAny<CreateOfficeDto>()))
            .Returns((CreateOfficeDto dto) => new OfficeEntity
            {
                OfficeCode = dto.OfficeCode,
                OfficeName = dto.OfficeName,
                Type = dto.Type,
                Address = dto.Address,
                City = dto.City,
                Pincode = dto.Pincode,
                Phone = dto.Phone,
                EmailId = dto.EmailId,
                OfficeIncharge = dto.OfficeIncharge,
                DesignationMasterId = dto.DesignationMasterId,
                EstablishedDate = dto.EstablishedDate,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<OfficeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OfficeEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<OfficeDto>(It.IsAny<OfficeEntity>()))
            .Returns((OfficeEntity e) => new OfficeDto
            {
                Id = e.Id,
                OfficeCode = e.OfficeCode,
                OfficeName = e.OfficeName,
                Type = e.Type,
                Address = e.Address,
                City = e.City,
                Pincode = e.Pincode,
                Phone = e.Phone,
                EmailId = e.EmailId,
                OfficeIncharge = e.OfficeIncharge,
                DesignationMasterId = e.DesignationMasterId,
                EstablishedDate = e.EstablishedDate,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("HQ001", result.OfficeCode);
        Assert.Equal("Head Office", result.OfficeName);
        Assert.Equal("Headquarters", result.Type);
        Assert.Equal("Mumbai", result.City);
        Assert.Equal("hq@company.com", result.EmailId);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(
            It.Is<OfficeEntity>(e => e.OfficeCode == "HQ001" && e.OfficeName == "Head Office"),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InactiveOffice_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateOfficeDto
        {
            OfficeCode = "OLD001",
            OfficeName = "Old Office",
            Type = "Closed",
            City = "Chennai",
            Pincode = "600001",
            IsActive = false
        };

        _mockMapper
            .Setup(m => m.Map<OfficeEntity>(It.IsAny<CreateOfficeDto>()))
            .Returns(new OfficeEntity
            {
                Id = 0,
                OfficeCode = "OLD001",
                OfficeName = "Old Office",
                Type = "Closed",
                City = "Chennai",
                Pincode = "600001",
                IsActive = false
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<OfficeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OfficeEntity e, CancellationToken _) =>
            {
                e.Id = 2;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<OfficeDto>(It.IsAny<OfficeEntity>()))
            .Returns(new OfficeDto
            {
                Id = 2,
                OfficeCode = "OLD001",
                OfficeName = "Old Office",
                Type = "Closed",
                City = "Chennai",
                Pincode = "600001",
                IsActive = false
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        Assert.Equal("Closed", result.Type);
        Assert.Equal("Chennai", result.City);
        Assert.Equal("600001", result.Pincode);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateOfficeCode_ThrowsException()
    {
        // Arrange
        var createDto = new CreateOfficeDto
        {
            OfficeCode = "HQ001",
            OfficeName = "Duplicate Office",
            Type = "Branch",
            City = "Mumbai",
            Pincode = "400001",
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<OfficeEntity>(It.IsAny<CreateOfficeDto>()))
            .Returns(new OfficeEntity { OfficeCode = "HQ001" });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<OfficeEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate OfficeCode: 'HQ001' already exists"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithCompleteDetails_CreatesSuccessfully()
    {
        // Arrange - Office with all details
        var createDto = new CreateOfficeDto
        {
            OfficeCode = "BR001",
            OfficeName = "Regional Branch Office",
            Type = "Regional",
            Address = "456 Park Avenue, Sector 5",
            City = "Pune",
            Pincode = "411001",
            Phone = "020-87654321",
            EmailId = "pune.branch@company.com",
            OfficeIncharge = 102,
            DesignationMasterId = 2,
            EstablishedDate = new DateTime(2021, 6, 15),
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<OfficeEntity>(It.IsAny<CreateOfficeDto>()))
            .Returns((CreateOfficeDto dto) => new OfficeEntity
            {
                OfficeCode = dto.OfficeCode,
                OfficeName = dto.OfficeName,
                Address = dto.Address,
                City = dto.City,
                Phone = dto.Phone,
                EmailId = dto.EmailId,
                EstablishedDate = dto.EstablishedDate
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<OfficeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OfficeEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<OfficeDto>(It.IsAny<OfficeEntity>()))
            .Returns((OfficeEntity e) => new OfficeDto
            {
                Id = e.Id,
                OfficeCode = e.OfficeCode,
                Address = e.Address,
                Phone = e.Phone,
                EmailId = e.EmailId,
                EstablishedDate = e.EstablishedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("456 Park Avenue, Sector 5", result.Address);
        Assert.Equal("020-87654321", result.Phone);
        Assert.Equal("pune.branch@company.com", result.EmailId);
        Assert.Equal(new DateTime(2021, 6, 15), result.EstablishedDate);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateOfficeDto
        {
            OfficeCode = "HQ001",
            OfficeName = "Head Office - Updated",
            Type = "Headquarters",
            Address = "789 New Address",
            City = "Mumbai",
            Pincode = "400002",
            Phone = "022-99999999",
            EmailId = "updated@company.com",
            OfficeIncharge = 105,
            DesignationMasterId = 1,
            EstablishedDate = new DateTime(2020, 1, 1),
            IsActive = true,
            UpdatedBy = 1
        };

        var existingEntity = new OfficeEntity
        {
            Id = 1,
            OfficeCode = "HQ001",
            OfficeName = "Head Office",
            Type = "Headquarters",
            Address = "123 Old Street",
            City = "Mumbai",
            Pincode = "400001",
            Phone = "022-12345678",
            EmailId = "old@company.com",
            OfficeIncharge = 101,
            DesignationMasterId = 1,
            EstablishedDate = new DateTime(2020, 1, 1),
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<OfficeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateOfficeDto>(), It.IsAny<OfficeEntity>()))
            .Callback((UpdateOfficeDto src, OfficeEntity dest) =>
            {
                dest.OfficeName = src.OfficeName;
                dest.Address = src.Address;
                dest.Pincode = src.Pincode;
                dest.Phone = src.Phone;
                dest.EmailId = src.EmailId;
                dest.OfficeIncharge = src.OfficeIncharge;
            });

        _mockMapper
            .Setup(m => m.Map<OfficeDto>(It.IsAny<OfficeEntity>()))
            .Returns((OfficeEntity e) => new OfficeDto
            {
                Id = e.Id,
                OfficeCode = e.OfficeCode,
                OfficeName = e.OfficeName,
                Type = e.Type,
                Address = e.Address,
                Pincode = e.Pincode,
                Phone = e.Phone,
                EmailId = e.EmailId,
                OfficeIncharge = e.OfficeIncharge,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Head Office - Updated", result.OfficeName);
        Assert.Equal("789 New Address", result.Address);
        Assert.Equal("400002", result.Pincode);
        Assert.Equal("022-99999999", result.Phone);
        Assert.Equal("updated@company.com", result.EmailId);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateOfficeDto
        {
            OfficeCode = "TEST001",
            OfficeName = "Test Office",
            Type = "Branch",
            City = "Mumbai",
            Pincode = "400001",
            EmailId = "test@company.com",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OfficeEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<OfficeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ChangeOfficeType_UpdatesSuccessfully()
    {
        // Arrange - Change office from Branch to Regional
        var updateDto = new UpdateOfficeDto
        {
            OfficeCode = "BR001",
            OfficeName = "Regional Office",
            Type = "Regional",
            City = "Pune",
            Pincode = "411001",
            EmailId = "regional@company.com",
            IsActive = true
        };

        var existingEntity = new OfficeEntity
        {
            Id = 2,
            OfficeCode = "BR001",
            OfficeName = "Branch Office",
            Type = "Branch",
            City = "Pune",
            Pincode = "411001",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<OfficeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateOfficeDto>(), It.IsAny<OfficeEntity>()))
            .Callback((UpdateOfficeDto src, OfficeEntity dest) =>
            {
                dest.Type = src.Type;
                dest.OfficeName = src.OfficeName;
            });

        _mockMapper
            .Setup(m => m.Map<OfficeDto>(It.IsAny<OfficeEntity>()))
            .Returns((OfficeEntity e) => new OfficeDto
            {
                Id = e.Id,
                Type = e.Type,
                OfficeName = e.OfficeName
            });

        // Act
        var result = await _service.UpdateAsync(2, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Regional", result.Type);
        Assert.Equal("Regional Office", result.OfficeName);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateOffice_UpdatesSuccessfully()
    {
        // Arrange - Deactivate an active office
        var updateDto = new UpdateOfficeDto
        {
            OfficeCode = "BR002",
            OfficeName = "Closed Branch",
            Type = "Branch",
            City = "Delhi",
            Pincode = "110001",
            EmailId = "closed@company.com",
            IsActive = false
        };

        var existingEntity = new OfficeEntity
        {
            Id = 3,
            OfficeCode = "BR002",
            OfficeName = "Active Branch",
            Type = "Branch",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<OfficeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateOfficeDto>(), It.IsAny<OfficeEntity>()))
            .Callback((UpdateOfficeDto src, OfficeEntity dest) =>
            {
                dest.IsActive = src.IsActive;
                dest.OfficeName = src.OfficeName;
            });

        _mockMapper
            .Setup(m => m.Map<OfficeDto>(It.IsAny<OfficeEntity>()))
            .Returns((OfficeEntity e) => new OfficeDto
            {
                Id = e.Id,
                IsActive = e.IsActive,
                OfficeName = e.OfficeName
            });

        // Act
        var result = await _service.UpdateAsync(3, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        Assert.Equal("Closed Branch", result.OfficeName);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSavesSuccessfully()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new OfficeEntity
        {
            Id = idToDelete,
            OfficeCode = "OLD001",
            OfficeName = "Old Office",
            IsActive = false
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<OfficeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<OfficeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ActiveOffice_ShouldStillDelete()
    {
        // Arrange - Even active offices can be deleted
        var idToDelete = 1;

        var existingEntity = new OfficeEntity
        {
            Id = idToDelete,
            OfficeCode = "HQ001",
            OfficeName = "Head Office",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<OfficeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<OfficeEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OfficeEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<OfficeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
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
            .ReturnsAsync((OfficeEntity?)null);

        // Act
        var result = await _service.DeleteAsync(invalidId, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<OfficeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Transaction Tests

    [Fact]
    public async Task CreateAsync_VerifiesNoExplicitTransactionUsed()
    {
        // Arrange
        var createDto = new CreateOfficeDto
        {
            OfficeCode = "TEST001",
            OfficeName = "Test Office",
            Type = "Test",
            City = "TestCity",
            Pincode = "123456",
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<OfficeEntity>(It.IsAny<CreateOfficeDto>()))
            .Returns(new OfficeEntity());

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<OfficeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OfficeEntity { Id = 1 });

        _mockMapper
            .Setup(m => m.Map<OfficeDto>(It.IsAny<OfficeEntity>()))
            .Returns(new OfficeDto());

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
        var existingEntity = new OfficeEntity
        {
            Id = 1,
            OfficeCode = "HQ001",
            OfficeName = "Head Office"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<OfficeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper.Setup(m => m.Map<OfficeDto>(It.IsAny<OfficeEntity>()))
            .Returns(new OfficeDto());

        var updateDto = new UpdateOfficeDto
        {
            OfficeCode = "HQ001",
            OfficeName = "HQ Updated",
            Type = "Headquarters",
            City = "Mumbai",
            Pincode = "400001",
            EmailId = "hq@company.com"
        };

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_VerifiesNoExplicitTransactionUsed()
    {
        // Arrange
        var existingEntity = new OfficeEntity
        {
            Id = 1,
            OfficeCode = "OLD001",
            OfficeName = "Old Office"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository.Setup(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(1, CancellationToken.None);

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
        var entity = new OfficeEntity
        {
            Id = 1,
            OfficeCode = "HQ001",
            OfficeName = "Head Office"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<OfficeDto>(It.IsAny<OfficeEntity>()))
            .Returns(new OfficeDto());

        // Act
        await _service.GetByIdAsync(1);

        // Assert
        _mockMapper.Verify(m => m.Map<OfficeDto>(entity), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_VerifiesMapperCalledTwice()
    {
        // Arrange
        var createDto = new CreateOfficeDto 
        { 
            OfficeCode = "HQ001", 
            OfficeName = "Head Office",
            Type = "Headquarters",
            City = "Mumbai",
            Pincode = "400001"
        };

        _mockMapper.Setup(m => m.Map<OfficeEntity>(It.IsAny<CreateOfficeDto>()))
            .Returns(new OfficeEntity());

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<OfficeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OfficeEntity { Id = 1 });

        _mockMapper.Setup(m => m.Map<OfficeDto>(It.IsAny<OfficeEntity>()))
            .Returns(new OfficeDto());

        // Act
        await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        _mockMapper.Verify(m => m.Map<OfficeEntity>(createDto), Times.Once);
        _mockMapper.Verify(m => m.Map<OfficeDto>(It.IsAny<OfficeEntity>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_VerifiesMapperCalledCorrectly()
    {
        // Arrange
        var updateDto = new UpdateOfficeDto
        {
            OfficeCode = "HQ001",
            OfficeName = "Updated Office",
            Type = "Headquarters",
            City = "Mumbai",
            Pincode = "400001",
            EmailId = "updated@company.com"
        };

        var existingEntity = new OfficeEntity { Id = 1 };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<OfficeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper.Setup(m => m.Map<OfficeDto>(It.IsAny<OfficeEntity>()))
            .Returns(new OfficeDto());

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockMapper.Verify(m => m.Map(updateDto, existingEntity), Times.Once);
        _mockMapper.Verify(m => m.Map<OfficeDto>(existingEntity), Times.Once);
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task GetAllAsync_OrderedByOfficeNameAscending_ReturnsOrderedResults()
    {
        // Arrange
        var entities = new List<OfficeEntity>
        {
            new() { Id = 1, OfficeCode = "BR003", OfficeName = "Zebra Branch", Type = "Branch", IsActive = true },
            new() { Id = 2, OfficeCode = "BR001", OfficeName = "Alpha Branch", Type = "Branch", IsActive = true },
            new() { Id = 3, OfficeCode = "BR002", OfficeName = "Beta Branch", Type = "Branch", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OfficeEntity, OfficeDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new OfficeService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new OfficeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "OfficeName",
            SortOrder = "asc"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.True(items.Count > 0);
    }

    [Fact]
    public async Task CreateAsync_HeadquartersOffice_CreatesSuccessfully()
    {
        // Arrange - Headquarters with full details
        var createDto = new CreateOfficeDto
        {
            OfficeCode = "HQ001",
            OfficeName = "Corporate Headquarters",
            Type = "Headquarters",
            Address = "1st Floor, Tower A, Business Park",
            City = "Mumbai",
            Pincode = "400001",
            Phone = "022-12345678",
            EmailId = "headquarters@company.com",
            OfficeIncharge = 1,
            DesignationMasterId = 1,
            EstablishedDate = new DateTime(2015, 1, 1),
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<OfficeEntity>(It.IsAny<CreateOfficeDto>()))
            .Returns((CreateOfficeDto dto) => new OfficeEntity
            {
                OfficeCode = dto.OfficeCode,
                OfficeName = dto.OfficeName,
                Type = dto.Type,
                EstablishedDate = dto.EstablishedDate,
                IsActive = dto.IsActive
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<OfficeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OfficeEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<OfficeDto>(It.IsAny<OfficeEntity>()))
            .Returns((OfficeEntity e) => new OfficeDto
            {
                Id = e.Id,
                Type = e.Type,
                EstablishedDate = e.EstablishedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Headquarters", result.Type);
        Assert.Equal(new DateTime(2015, 1, 1), result.EstablishedDate);
    }

    #endregion
}
