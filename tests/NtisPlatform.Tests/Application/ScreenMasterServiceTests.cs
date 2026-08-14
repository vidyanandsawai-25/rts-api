using AutoMapper;
using MockQueryable;
using Moq;
using MockQueryable.Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

public class ScreenMasterServiceTests
{
    private readonly Mock<IRepository<ScreenMasterEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly ScreenMasterService _service;

    public ScreenMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<ScreenMasterEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _service = new ScreenMasterService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new ScreenMasterEntity
        {
            Id = 1,
            ScreenCode = "SCR001",
            ScreenName = "Dashboard",
            ScreenGroupId = 1,
            ModuleId = 2,
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<ScreenMasterDto>(It.IsAny<ScreenMasterEntity>()))
            .Returns(new ScreenMasterDto 
            { 
                Id = 1, 
                ScreenCode = "SCR001", 
                ScreenName = "Dashboard",
                ScreenGroupId = 1,
                ModuleId = 2
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("SCR001", result.ScreenCode);
        Assert.Equal(1, result.ScreenGroupId);
        Assert.Equal(2, result.ModuleId);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateScreenMasterDto
        {
            ScreenCode = "SCR002",
            ScreenName = "Users",
            ScreenGroupId = 1,
            IsActive = true
        };

        _mockMapper.Setup(m => m.Map<ScreenMasterEntity>(It.IsAny<CreateScreenMasterDto>()))
            .Returns(new ScreenMasterEntity { ScreenCode = "SCR002", ScreenName = "Users" });
        
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ScreenMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScreenMasterEntity e, CancellationToken _) => e);

        _mockMapper.Setup(m => m.Map<ScreenMasterDto>(It.IsAny<ScreenMasterEntity>()))
            .Returns(new ScreenMasterDto { ScreenCode = "SCR002", ScreenName = "Users" });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SCR002", result.ScreenCode);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_ReturnsUpdatedDto()
    {
        // Arrange
        var existingEntity = new ScreenMasterEntity
        {
            Id = 1,
            ScreenCode = "SCR001",
            ScreenName = "Dashboard",
            ScreenGroupId = 1,
            IsActive = true
        };

        var updateDto = new UpdateScreenMasterDto
        {
            ScreenCode = "SCR001",
            ScreenName = "Dashboard Updated",
            ScreenGroupId = 1,
            IsActive = false,
            UpdatedBy = 1
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateScreenMasterDto>(), It.IsAny<ScreenMasterEntity>()))
            .Returns(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ScreenMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map<ScreenMasterDto>(It.IsAny<ScreenMasterEntity>()))
            .Returns(new ScreenMasterDto
            {
                Id = 1,
                ScreenName = "Dashboard Updated",
                IsActive = false
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Dashboard Updated", result.ScreenName);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateScreenMasterDto
        {
            ScreenCode = "SCR999",
            ScreenName = "Non-Existent",
            IsActive = true,
            UpdatedBy = 1
        };

        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScreenMasterEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<ScreenMasterEntity>
        {
            new() { Id = 1, ScreenCode = "SCR001", ScreenName = "Dashboard", ScreenGroupId = 1, ModuleId = 1, IsActive = true, CreatedDate = DateTime.Now },
            new() { Id = 2, ScreenCode = "SCR002", ScreenName = "Users", ScreenGroupId = 1, ModuleId = 1, IsActive = true, CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock(); // async IQueryable
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ScreenMasterEntity, ScreenMasterDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new ScreenMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

        var qp = new ScreenMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_ReturnsTrue()
    {
        // Arrange
        var entity = new ScreenMasterEntity { Id = 1 };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockRepository.Setup(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(1, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScreenMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(999, CancellationToken.None);

        // Assert
        Assert.False(result);
    }
}
