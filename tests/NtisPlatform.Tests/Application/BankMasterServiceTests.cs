using AutoMapper;
using Moq;
using MockQueryable;
using NtisPlatform.Application.DTOs.Master.BankMaster;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
namespace NtisPlatform.Tests.Application;
public class BankMasterServiceTests
{
    private readonly Mock<IRepository<BankMasterEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly BankMasterService _service;

    public BankMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<BankMasterEntity, int>>();
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

        _service = new BankMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new BankMasterEntity
        {
            Id = 1,
            BankCode = "SBI001",
            BankName = "State Bank of India",
            BranchName = "Main Branch",
            IFSCCode = "SBIN0001234",
            Address = "123 Main Street",
            City = "Mumbai",
            State = "Maharashtra",
            Pincode = "400001",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<BankMasterDTO>(It.IsAny<BankMasterEntity>()))
            .Returns(new BankMasterDTO
            {
                Id = 1,
                BankCode = "SBI001",
                BankName = "State Bank of India",
                BranchName = "Main Branch",
                IFSCCode = "SBIN0001234",
                Address = "123 Main Street",
                City = "Mumbai",
                State = "Maharashtra",
                Pincode = "400001",
                IsActive = true
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("SBI001", result.BankCode);
        Assert.Equal("State Bank of India", result.BankName);
        Assert.Equal("Main Branch", result.BranchName);
        Assert.Equal("SBIN0001234", result.IFSCCode);
        Assert.Equal("Mumbai", result.City);
        Assert.Equal("Maharashtra", result.State);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<BankMasterDTO>(entity), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BankMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<BankMasterDTO>(It.IsAny<BankMasterEntity>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetByIdAsync_InvalidId_ReturnsNull(int invalidId)
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BankMasterEntity?)null);

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
        var entities = new List<BankMasterEntity>
        {
            new() 
            { 
                Id = 1, 
                BankCode = "SBI001",
                BankName = "State Bank of India",
                BranchName = "Main Branch",
                IFSCCode = "SBIN0001234",
                City = "Mumbai",
                State = "Maharashtra",
                IsActive = true
            },
            new() 
            { 
                Id = 2, 
                BankCode = "HDFC001",
                BankName = "HDFC Bank",
                BranchName = "Commercial Branch",
                IFSCCode = "HDFC0001234",
                City = "Delhi",
                State = "Delhi",
                IsActive = true
            },
            new() 
            { 
                Id = 3, 
                BankCode = "ICICI001",
                BankName = "ICICI Bank",
                BranchName = "Corporate Branch",
                IFSCCode = "ICIC0001234",
                City = "Bangalore",
                State = "Karnataka",
                IsActive = false
            }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BankMasterEntity, BankMasterDTO>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new BankMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new BankQueryParameters
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
        Assert.Contains(items, x => x.BankCode == "SBI001" && x.BankName == "State Bank of India");
        Assert.Contains(items, x => x.BankCode == "HDFC001" && x.BankName == "HDFC Bank");
        Assert.Contains(items, x => x.BankCode == "ICICI001" && x.BankName == "ICICI Bank");
    }

    [Fact]
    public async Task GetAllAsync_WithBankCodeFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<BankMasterEntity>
        {
            new() { Id = 1, BankCode = "SBI001", BankName = "State Bank of India", State = "Maharashtra", IsActive = true },
            new() { Id = 2, BankCode = "HDFC001", BankName = "HDFC Bank", State = "Delhi", IsActive = true },
            new() { Id = 3, BankCode = "SBI002", BankName = "SBI Regional", State = "Karnataka", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BankMasterEntity, BankMasterDTO>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new BankMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new BankQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            BankCode = "SBI"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item => 
            Assert.Contains("SBI", item.BankCode, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAllAsync_WithStateFilter_ReturnsFilteredEntities()
    {
        // Arrange
        var entities = new List<BankMasterEntity>
        {
            new() { Id = 1, BankCode = "SBI001", BankName = "State Bank", State = "Maharashtra", IsActive = true },
            new() { Id = 2, BankCode = "HDFC001", BankName = "HDFC Bank", State = "Delhi", IsActive = true },
            new() { Id = 3, BankCode = "ICICI001", BankName = "ICICI Bank", State = "Maharashtra", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BankMasterEntity, BankMasterDTO>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new BankMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new BankQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            State = "Maharashtra"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item => 
            Assert.Contains("Maharashtra", item.State, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var entities = new List<BankMasterEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BankMasterEntity, BankMasterDTO>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new BankMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new BankQueryParameters
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
            .Select(i => new BankMasterEntity
            {
                Id = i,
                BankCode = $"BANK{i:000}",
                BankName = $"Bank {i}",
                BranchName = $"Branch {i}",
                IFSCCode = $"BANK000{i}",
                State = "Test State",
                IsActive = true
            })
            .ToList();

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BankMasterEntity, BankMasterDTO>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new BankMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new BankQueryParameters
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

    [Fact]
    public async Task GetAllAsync_WithSearchTerm_ReturnsMatchingEntities()
    {
        // Arrange
        var entities = new List<BankMasterEntity>
        {
            new() { Id = 1, BankCode = "SBI001", BankName = "State Bank of India", State = "Maharashtra", IsActive = true },
            new() { Id = 2, BankCode = "HDFC001", BankName = "HDFC Bank", State = "Delhi", IsActive = true },
            new() { Id = 3, BankCode = "ICICI001", BankName = "ICICI Bank", State = "Karnataka", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BankMasterEntity, BankMasterDTO>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new BankMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new BankQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "HDFC"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateBankMasterDto
        {
            BankCode = "SBI001",
            BankName = "State Bank of India",
            BranchName = "Main Branch",
            IFSCCode = "SBIN0001234",
            Address = "123 Main Street",
            City = "Mumbai",
            State = "Maharashtra",
            Pincode = "400001",
            IsActive = true,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<BankMasterEntity>(It.IsAny<CreateBankMasterDto>()))
            .Returns((CreateBankMasterDto dto) => new BankMasterEntity
            {
                BankCode = dto.BankCode,
                BankName = dto.BankName,
                BranchName = dto.BranchName,
                IFSCCode = dto.IFSCCode,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                Pincode = dto.Pincode,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<BankMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BankMasterEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<BankMasterDTO>(It.IsAny<BankMasterEntity>()))
            .Returns((BankMasterEntity e) => new BankMasterDTO
            {
                Id = e.Id,
                BankCode = e.BankCode,
                BankName = e.BankName,
                BranchName = e.BranchName,
                IFSCCode = e.IFSCCode,
                Address = e.Address,
                City = e.City,
                State = e.State,
                Pincode = e.Pincode,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("SBI001", result.BankCode);
        Assert.Equal("State Bank of India", result.BankName);
        Assert.Equal("SBIN0001234", result.IFSCCode);
        Assert.Equal("Mumbai", result.City);
        Assert.Equal("Maharashtra", result.State);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(
            It.Is<BankMasterEntity>(e => e.BankCode == "SBI001" && e.BankName == "State Bank of India"),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InactiveBank_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateBankMasterDto
        {
            BankCode = "OLD001",
            BankName = "Old Bank",
            BranchName = "Closed Branch",
            IFSCCode = "OLD00001234",
            City = "Mumbai",
            State = "Maharashtra",
            Pincode = "400001",
            IsActive = false
        };

        _mockMapper
            .Setup(m => m.Map<BankMasterEntity>(It.IsAny<CreateBankMasterDto>()))
            .Returns(new BankMasterEntity
            {
                Id = 0,
                BankCode = "OLD001",
                BankName = "Old Bank",
                BranchName = "Closed Branch",
                IFSCCode = "OLD00001234",
                IsActive = false
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<BankMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BankMasterEntity e, CancellationToken _) =>
            {
                e.Id = 2;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<BankMasterDTO>(It.IsAny<BankMasterEntity>()))
            .Returns(new BankMasterDTO
            {
                Id = 2,
                BankCode = "OLD001",
                BankName = "Old Bank",
                IsActive = false
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateBankCode_ThrowsException()
    {
        // Arrange
        var createDto = new CreateBankMasterDto
        {
            BankCode = "SBI001",
            BankName = "State Bank",
            BranchName = "Main",
            IFSCCode = "SBIN0001234",
            City = "Mumbai",
            State = "Maharashtra",
            Pincode = "400001",
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<BankMasterEntity>(It.IsAny<CreateBankMasterDto>()))
            .Returns(new BankMasterEntity { BankCode = "SBI001" });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<BankMasterEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate BankCode: 'SBI001' already exists"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DuplicateIFSCCode_ThrowsException()
    {
        // Arrange
        var createDto = new CreateBankMasterDto
        {
            BankCode = "NEW001",
            BankName = "New Bank",
            BranchName = "New Branch",
            IFSCCode = "SBIN0001234",  // Duplicate IFSC
            City = "Mumbai",
            State = "Maharashtra",
            Pincode = "400001",
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<BankMasterEntity>(It.IsAny<CreateBankMasterDto>()))
            .Returns(new BankMasterEntity { IFSCCode = "SBIN0001234" });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<BankMasterEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate IFSCCode: 'SBIN0001234' already exists"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateBankMasterDto
        {
            BankCode = "SBI001",
            BankName = "State Bank of India - Updated",
            BranchName = "Main Branch - Updated",
            IFSCCode = "SBIN0001234",
            Address = "456 New Street",
            City = "Mumbai",
            State = "Maharashtra",
            Pincode = "400002",
            IsActive = true,
            UpdatedBy = 1
        };

        var existingEntity = new BankMasterEntity
        {
            Id = 1,
            BankCode = "SBI001",
            BankName = "State Bank of India",
            BranchName = "Main Branch",
            IFSCCode = "SBIN0001234",
            Address = "123 Old Street",
            City = "Mumbai",
            State = "Maharashtra",
            Pincode = "400001",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<BankMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateBankMasterDto>(), It.IsAny<BankMasterEntity>()))
            .Callback((UpdateBankMasterDto src, BankMasterEntity dest) =>
            {
                dest.BankName = src.BankName;
                dest.BranchName = src.BranchName;
                dest.Address = src.Address;
                dest.Pincode = src.Pincode;
            });

        _mockMapper
            .Setup(m => m.Map<BankMasterDTO>(It.IsAny<BankMasterEntity>()))
            .Returns((BankMasterEntity e) => new BankMasterDTO
            {
                Id = e.Id,
                BankCode = e.BankCode,
                BankName = e.BankName,
                BranchName = e.BranchName,
                Address = e.Address,
                Pincode = e.Pincode,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("State Bank of India - Updated", result.BankName);
        Assert.Equal("Main Branch - Updated", result.BranchName);
        Assert.Equal("456 New Street", result.Address);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateBankMasterDto
        {
            BankCode = "TEST001",
            BankName = "Test Bank",
            BranchName = "Test Branch",
            IFSCCode = "TEST0001234",
            City = "Mumbai",
            State = "Maharashtra",
            Pincode = "400001",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BankMasterEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<BankMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSavesSuccessfully()
    {
        // Arrange
        var idToDelete = 1;

        var existingEntity = new BankMasterEntity
        {
            Id = idToDelete,
            BankCode = "OLD001",
            BankName = "Old Bank",
            IsActive = false
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<BankMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<BankMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task DeleteAsync_ActiveBank_ShouldStillDelete()
    {
        // Arrange - Even active banks can be deleted
        var idToDelete = 1;

        var existingEntity = new BankMasterEntity
        {
            Id = idToDelete,
            BankCode = "SBI001",
            BankName = "State Bank",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<BankMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<BankMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BankMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<BankMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Transaction Tests

    [Fact]
    public async Task CreateAsync_VerifiesNoExplicitTransactionUsed()
    {
        // Arrange
        var createDto = new CreateBankMasterDto
        {
            BankCode = "TEST001",
            BankName = "Test Bank",
            BranchName = "Test Branch",
            IFSCCode = "TEST0001234",
            City = "Mumbai",
            State = "Maharashtra",
            Pincode = "400001",
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<BankMasterEntity>(It.IsAny<CreateBankMasterDto>()))
            .Returns(new BankMasterEntity());

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<BankMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankMasterEntity { Id = 1 });

        _mockMapper
            .Setup(m => m.Map<BankMasterDTO>(It.IsAny<BankMasterEntity>()))
            .Returns(new BankMasterDTO());

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
        var existingEntity = new BankMasterEntity
        {
            Id = 1,
            BankCode = "SBI001",
            BankName = "SBI"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<BankMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper.Setup(m => m.Map<BankMasterDTO>(It.IsAny<BankMasterEntity>()))
            .Returns(new BankMasterDTO());

        var updateDto = new UpdateBankMasterDto 
        { 
            BankCode = "SBI001", 
            BankName = "SBI Updated",
            BranchName = "Main",
            IFSCCode = "SBIN0001234",
            City = "Mumbai",
            State = "Maharashtra",
            Pincode = "400001"
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
        var entity = new BankMasterEntity
        {
            Id = 1,
            BankCode = "SBI001",
            BankName = "State Bank"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<BankMasterDTO>(It.IsAny<BankMasterEntity>()))
            .Returns(new BankMasterDTO());

        // Act
        await _service.GetByIdAsync(1);

        // Assert
        _mockMapper.Verify(m => m.Map<BankMasterDTO>(entity), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_VerifiesMapperCalledTwice()
    {
        // Arrange
        var createDto = new CreateBankMasterDto 
        { 
            BankCode = "SBI001", 
            BankName = "SBI",
            BranchName = "Main",
            IFSCCode = "SBIN0001234",
            City = "Mumbai",
            State = "Maharashtra",
            Pincode = "400001"
        };

        _mockMapper.Setup(m => m.Map<BankMasterEntity>(It.IsAny<CreateBankMasterDto>()))
            .Returns(new BankMasterEntity());

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<BankMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankMasterEntity { Id = 1 });

        _mockMapper.Setup(m => m.Map<BankMasterDTO>(It.IsAny<BankMasterEntity>()))
            .Returns(new BankMasterDTO());

        // Act
        await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        _mockMapper.Verify(m => m.Map<BankMasterEntity>(createDto), Times.Once);
        _mockMapper.Verify(m => m.Map<BankMasterDTO>(It.IsAny<BankMasterEntity>()), Times.Once);
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public async Task GetAllAsync_OrderedByBankNameAscending_ReturnsOrderedResults()
    {
        // Arrange
        var entities = new List<BankMasterEntity>
        {
            new() { Id = 1, BankCode = "HDFC001", BankName = "HDFC Bank", State = "Delhi", IsActive = true },
            new() { Id = 2, BankCode = "AXIS001", BankName = "Axis Bank", State = "Mumbai", IsActive = true },
            new() { Id = 3, BankCode = "ICICI001", BankName = "ICICI Bank", State = "Bangalore", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BankMasterEntity, BankMasterDTO>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new BankMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new BankQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "BankName",
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
    public async Task CreateAsync_WithCompleteAddress_CreatesSuccessfully()
    {
        // Arrange - Bank with complete address details
        var createDto = new CreateBankMasterDto
        {
            BankCode = "SBI001",
            BankName = "State Bank of India",
            BranchName = "Main Branch",
            IFSCCode = "SBIN0001234",
            Address = "123 Main Street, Near City Center",
            City = "Mumbai",
            State = "Maharashtra",
            Pincode = "400001",
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<BankMasterEntity>(It.IsAny<CreateBankMasterDto>()))
            .Returns((CreateBankMasterDto dto) => new BankMasterEntity
            {
                BankCode = dto.BankCode,
                BankName = dto.BankName,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                Pincode = dto.Pincode,
                IsActive = dto.IsActive
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<BankMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BankMasterEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<BankMasterDTO>(It.IsAny<BankMasterEntity>()))
            .Returns((BankMasterEntity e) => new BankMasterDTO
            {
                Id = e.Id,
                BankCode = e.BankCode,
                Address = e.Address,
                City = e.City,
                State = e.State,
                Pincode = e.Pincode
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("123 Main Street, Near City Center", result.Address);
        Assert.Equal("Mumbai", result.City);
        Assert.Equal("Maharashtra", result.State);
        Assert.Equal("400001", result.Pincode);
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleFilters_ReturnsFilteredResults()
    {
        // Arrange
        var entities = new List<BankMasterEntity>
        {
            new() { Id = 1, BankCode = "SBI001", BankName = "State Bank", State = "Maharashtra", IsActive = true },
            new() { Id = 2, BankCode = "SBI002", BankName = "State Bank", State = "Maharashtra", IsActive = false },
            new() { Id = 3, BankCode = "HDFC001", BankName = "HDFC Bank", State = "Maharashtra", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BankMasterEntity, BankMasterDTO>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new BankMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var queryParams = new BankQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            BankCode = "SBI",
            State = "Maharashtra"
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item =>
        {
            Assert.Contains("SBI", item.BankCode);
            Assert.Contains("Maharashtra", item.State);
        });
    }

    #endregion
}
