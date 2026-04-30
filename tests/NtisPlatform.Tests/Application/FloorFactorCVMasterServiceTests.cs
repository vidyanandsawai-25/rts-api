using AutoMapper;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Master.FloorFactorCVMaster;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Application.DTOs.Bulk;

namespace NtisPlatform.Tests.Application;

public class FloorFactorCVMasterServiceTests
{
    private readonly Mock<IRepository<FloorFactorCVMasterEntity, int>> _mockRepository;
    private readonly Mock<IRepository<FloorEntity, int>> _mockFloorRepository;
    private readonly Mock<IRepository<AssessmentYearRangeCVEntity, int>> _mockYearRangeCVRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly IMapper _mapper;
    private readonly FloorFactorCVMasterService _service;

    public FloorFactorCVMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<FloorFactorCVMasterEntity, int>>();
        _mockFloorRepository = new Mock<IRepository<FloorEntity, int>>();
        _mockYearRangeCVRepository = new Mock<IRepository<AssessmentYearRangeCVEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<FloorFactorCVMasterMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);


        _service = new FloorFactorCVMasterService(
            _mockRepository.Object,
            _mockFloorRepository.Object,
            _mockYearRangeCVRepository.Object,
            _mockUnitOfWork.Object,
            _mapper);
    }

    [Fact]
    public async Task GetAllAsync_YearRangeWithNoFactorRows_ReturnsAllPlaceholders()
    {
        // No factor rows for the year range
        var entities = new List<FloorFactorCVMasterEntity>();
        var mockQuery = entities.BuildMockDbSet();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery.Object);

        var floorEntities = new List<FloorEntity>
        {
            new FloorEntity { Id = 1, FloorCode = "1", Description = "First Floor" },
            new FloorEntity { Id = 2, FloorCode = "2", Description = "Second Floor" }
        }.BuildMockDbSet();
        _mockFloorRepository.Setup(r => r.GetQueryable()).Returns(floorEntities.Object);
        var yearRangeEntities = new List<AssessmentYearRangeCVEntity>
        {
            new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2000, ToYear = 2020 }
        }.BuildMockDbSet();
        _mockYearRangeCVRepository.Setup(r => r.GetQueryable()).Returns(yearRangeEntities.Object);

        var qp = new FloorFactorCVMasterQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount); // 2 floors × 1 year range
        Assert.All(result.Items, x => Assert.Equal(0, x.Id)); // All should be placeholders
    }

    [Fact]
    public async Task GetAllAsync_YearRangeWithPartialFactorRows_ReturnsRealAndPlaceholderRows()
    {
        // Only one floor has a factor row for the year range
        var entities = new List<FloorFactorCVMasterEntity>
        {
            new FloorFactorCVMasterEntity { Id = 1, FloorId = 1, FactorWithLift = 1.0m, FactorWithoutLift = 1.0m, YearRangeCVId = 1, IsActive = true }
        };
        var mockQuery = entities.BuildMockDbSet();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery.Object);

        var floorEntities = new List<FloorEntity>
        {
            new FloorEntity { Id = 1, FloorCode = "1", Description = "First Floor" },
            new FloorEntity { Id = 2, FloorCode = "2", Description = "Second Floor" }
        }.BuildMockDbSet();
        _mockFloorRepository.Setup(r => r.GetQueryable()).Returns(floorEntities.Object);
        var yearRangeEntities = new List<AssessmentYearRangeCVEntity>
        {
            new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2000, ToYear = 2020 }
        }.BuildMockDbSet();
        _mockYearRangeCVRepository.Setup(r => r.GetQueryable()).Returns(yearRangeEntities.Object);

        var qp = new FloorFactorCVMasterQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount); // 2 floors × 1 year range
        Assert.Contains(result.Items, x => x.Id == 1 && x.FloorId == 1); // Real data row
        Assert.Contains(result.Items, x => x.Id == 0 && x.FloorId == 2); // Placeholder for missing floor
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new FloorFactorCVMasterEntity
        {
            Id = 5,
            FloorId = 4,
            FactorWithLift = 1.00m,
            FactorWithoutLift = 1.00m,
            YearRangeCVId = 1,
            IsActive = true,
            CreatedDate = DateTime.Parse("2026-04-23T10:49:18.900")
        };
        var floor = new FloorEntity { Id = 4, FloorCode = "F4", Description = "Fourth Floor" };
        var yearRange = new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2020, ToYear = 2030 };

        var entityList = new List<FloorFactorCVMasterEntity> { entity };
        var floorList = new List<FloorEntity> { floor };
        var yearRangeList = new List<AssessmentYearRangeCVEntity> { yearRange };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(entityList.BuildMockDbSet().Object);
        _mockFloorRepository.Setup(r => r.GetQueryable()).Returns(floorList.BuildMockDbSet().Object);
        _mockYearRangeCVRepository.Setup(r => r.GetQueryable()).Returns(yearRangeList.BuildMockDbSet().Object);

        // Act
        var result = await _service.GetByIdAsync(5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Id);
        Assert.Equal(4, result.FloorId);
        Assert.Equal("F4", result.FloorCode);
        Assert.Equal("Fourth Floor", result.FloorDescription); 
        Assert.Equal(1.00m, result.FactorWithLift);
        Assert.Equal(1.00m, result.FactorWithoutLift);
        Assert.Equal(1, result.YearRangeCVId);
        Assert.Equal(2020, result.FromYear);
        Assert.Equal(2030, result.ToYear);
        Assert.True(result.IsActive);
        Assert.Equal(DateTime.Parse("2026-04-23T10:49:18.900"), result.CreatedDate);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Setup all queryables used by GetByIdAsync to return empty async-capable queryables
        var emptyFloorFactorList = new List<FloorFactorCVMasterEntity>().BuildMockDbSet();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(emptyFloorFactorList.Object);
        var emptyFloorList = new List<FloorEntity>().BuildMockDbSet();
        _mockFloorRepository.Setup(r => r.GetQueryable()).Returns(emptyFloorList.Object);
        var emptyYearRangeList = new List<AssessmentYearRangeCVEntity>().BuildMockDbSet();
        _mockYearRangeCVRepository.Setup(r => r.GetQueryable()).Returns(emptyYearRangeList.Object);
        var result = await _service.GetByIdAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResult()
    {
        var entities = new List<FloorFactorCVMasterEntity>
        {
            new FloorFactorCVMasterEntity { Id = 1, FloorId = 1, FactorWithLift = 1.0m, FactorWithoutLift = 1.0m, YearRangeCVId = 1, IsActive = true },
            new FloorFactorCVMasterEntity { Id = 2, FloorId = 2, FactorWithLift = 1.0m, FactorWithoutLift = 1.0m, YearRangeCVId = 1, IsActive = true }
        };
        var mockQuery = entities.BuildMockDbSet();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery.Object);


        // Mock floor and year range repositories to return matching data
        var floorEntities = new List<FloorEntity>
        {
            new FloorEntity { Id = 1, FloorCode = "1", Description = "First Floor" },
            new FloorEntity { Id = 2, FloorCode = "2", Description = "Second Floor" }
        }.BuildMockDbSet();
        _mockFloorRepository.Setup(r => r.GetQueryable()).Returns(floorEntities.Object);
        var yearRangeEntities = new List<AssessmentYearRangeCVEntity>
        {
            new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2000, ToYear = 2020 }
        }.BuildMockDbSet();
        _mockYearRangeCVRepository.Setup(r => r.GetQueryable()).Returns(yearRangeEntities.Object);

        var qp = new FloorFactorCVMasterQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 2);
    }
    [Fact]
    public async Task BulkCreateAsync_ValidItems_CreatesAll()
    {
        var createDtos = new[]
        {
            new CreateFloorFactorCVMasterDto { FloorId = 1, FactorWithLift = 1.1m, FactorWithoutLift = 1.0m, YearRangeCVId = 1, IsActive = true },
            new CreateFloorFactorCVMasterDto { FloorId = 2, FactorWithLift = 2.2m, FactorWithoutLift = 2.0m, YearRangeCVId = 1, IsActive = true }
        };

        _mockRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<FloorFactorCVMasterEntity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.BulkCreateAsync(createDtos);

        Assert.NotNull(result);
        Assert.Equal(2, result.SuccessCount);
        Assert.True(result.Errors == null || result.Errors.Count == 0);
    }

    [Fact]
    public async Task BulkUpdateAsync_ValidItems_UpdatesAll()
    {
        var updateDtos = new[]
        {
            new BulkUpdateItem<int, UpdateFloorFactorCVMasterDto>(1, new UpdateFloorFactorCVMasterDto { FloorId = 1, FactorWithLift = 1.5m, FactorWithoutLift = 1.4m, YearRangeCVId = 1, IsActive = true }),
            new BulkUpdateItem<int, UpdateFloorFactorCVMasterDto>(2, new UpdateFloorFactorCVMasterDto { FloorId = 2, FactorWithLift = 2.5m, FactorWithoutLift = 2.4m, YearRangeCVId = 1, IsActive = true })
        };

        _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => new FloorFactorCVMasterEntity { Id = id });

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<FloorFactorCVMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.BulkUpdateAsync(updateDtos);

        Assert.NotNull(result);
        Assert.Equal(2, result.SuccessCount);
        Assert.True(result.Errors == null || result.Errors.Count == 0);
    }
}
